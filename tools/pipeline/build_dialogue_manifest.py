"""Build private Godot audio registry with conservative, auditable transcript matching."""
import argparse
import hashlib
import json
import re
import shutil
import sys
from collections import Counter
from difflib import SequenceMatcher
from pathlib import Path

import requests
from bs4 import BeautifulSoup

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/reference'))
from batch_process_audio import write_json
from extract_native_voice_catalog import INSPECTED_SHA256

URL = 'https://transcripts-fanon.fandom.com/wiki/Spider-Man_(2000_Video_Game)/Transcript'
LEVELS = {'L1A1': 'Get to the Bank!', 'L1A2': 'Bank Approach',
          'L1A3': 'Hostage Situation', 'L1A4': 'Stop the Bomb!'}
LEVEL_PATTERN = re.compile(r'^(L[0-9A-Z]+A\d+[A-Z]?)_', re.I)


def normalize(text):
    return ' '.join(re.findall(r"[a-z0-9]+", text.casefold()))


def parse_reference(html):
    soup = BeautifulSoup(html, 'html.parser')
    content = soup.select_one('.mw-parser-output') or soup
    section, speaker, rows = '', '', []
    for element in content.find_all(['h2', 'h3', 'p']):
        if element.name in ('h2', 'h3'):
            section = element.get_text(' ', strip=True).replace('[edit]', '').strip()
            speaker = ''
            continue
        if not section or 'OPENING LOGOS' in section.upper():
            continue
        # HTML paragraphs may hold several speakers separated by line breaks.
        for br in element.find_all('br'):
            br.replace_with('\n')
        for raw in element.get_text('', strip=False).splitlines():
            text = raw.strip()
            match = re.match(r'^([^:]{1,100}):\s*(.+)$', text)
            if match:
                speaker = re.sub(r'\s*\([^)]*\)', '', match[1]).strip()
                text = match[2]
            elif not speaker or text.startswith('('):
                continue
            text = re.sub(r'\([^)]*\)', '', text).strip()
            if len(normalize(text).split()) < 2:
                continue
            level = next((key for key, name in LEVELS.items()
                          if normalize(name) == normalize(section)), '')
            rows.append(dict(index=len(rows), section=section, level=level,
                             speaker=speaker, text=text, normalized=normalize(text)))
    if not rows:
        raise ValueError('Reference page contains no recognized speaker paragraphs')
    return rows


def fetch_reference(cache, offline=False):
    if offline:
        return cache.read_text(encoding='utf-8')
    session = requests.Session()
    session.headers['User-Agent'] = 'SpiderManLocalResearch/1.0 (private transcript comparison)'
    try:
        response = session.get(URL, timeout=(15, 45))
        response.raise_for_status()
        html = response.text
        parse_reference(html)
    except (requests.RequestException, ValueError):
        response = session.get('https://transcripts-fanon.fandom.com/api.php', params={
            'action': 'parse', 'page': 'Spider-Man_(2000_Video_Game)/Transcript',
            'prop': 'text', 'format': 'json'}, timeout=(15, 45))
        response.raise_for_status()
        html = response.json()['parse']['text']['*']
        parse_reference(html)
    cache.parent.mkdir(parents=True, exist_ok=True)
    cache.write_text(html, encoding='utf-8')
    return html


def rank_matches(transcript, level, rows, previous=None):
    query = normalize(transcript)
    if len(query.split()) < 4:
        return []
    query_words = set(query.split())
    ranked = []
    for row in rows:
        if row['level'] and level and row['level'] != level:
            continue
        reference = row.get('normalized') or normalize(row['text'])
        if 2*min(len(query), len(reference))/max(1, len(query)+len(reference)) < .45:
            continue
        reference_words = set(reference.split())
        if len(query_words & reference_words) < max(2, .25*min(len(query_words), len(reference_words))):
            continue
        # Ordering is only a tie-break within the same known section; numeric
        # suffixes never establish dialogue identities or trigger semantics.
        matcher = SequenceMatcher(None, query, reference, autojunk=False)
        if matcher.quick_ratio() < .45:
            continue
        score = matcher.ratio()
        if score < .45:
            continue
        order = bool(previous and previous['section'] == row['section']
                     and row['index'] == previous['index'] + 1)
        ranked.append((score, order, row))
    ranked.sort(key=lambda item: (item[0], item[1]), reverse=True)
    return ranked[:3]


def export_registry(document, rows, source_root, target_root, copy_audio=True):
    registry, used, previous = {}, set(), None
    category_dirs = {'Dialogue': 'dialogue', 'Music': 'music', 'SFX': 'sfx'}
    for clip in sorted(document['clips'], key=lambda c: c['source_path'].casefold()):
        category = clip['category']
        stem = Path(clip['source_path']).stem
        # Preserve logical names (L1A1_01) independently of hashed extraction names.
        key = stem.upper() if LEVEL_PATTERN.match(stem) else stem
        if key.casefold() in used:
            key += '__' + hashlib.sha256(clip['clip_id'].encode()).hexdigest()[:10]
        used.add(key.casefold())
        match = LEVEL_PATTERN.match(stem)
        level = match[1].upper() if match else clip.get('level_id', '') or ''
        text = clip.get('subtitle_text') or clip.get('transcript') or ''
        ranked = rank_matches(text, level, rows, previous) if category == 'Dialogue' else []
        candidates = [dict(score=round(score, 4), section=row['section'],
                           reference_index=row['index'], speaker=row['speaker'])
                      for score, _, row in ranked]
        accepted = bool(ranked and ranked[0][0] >= .87
                        and (len(ranked) == 1 or ranked[0][0] - ranked[1][0] >= .08))
        # Do not turn a fuzzy reference candidate into verified subtitles.
        speaker = clip.get('speaker', 'Unknown')
        status = clip.get('transcript_status', 'unresolved')
        if accepted:
            previous = ranked[0][2]
            if speaker == 'Unknown':
                speaker = previous['speaker']
            status = 'reference_matched_asr_draft'
        relative = Path(clip['file_name'])
        source = (source_root / relative).resolve()
        if not source.is_relative_to(source_root.resolve()):
            raise ValueError('Audio path escapes extraction root')
        if not source.is_file():
            raise FileNotFoundError(source)
        digest = hashlib.sha256(source.read_bytes()).hexdigest()
        if digest != clip['wav_sha256']:
            raise ValueError('WAV digest mismatch: ' + clip['clip_id'])
        folder = category_dirs[category]
        destination = target_root / folder / (key + '.wav')
        if copy_audio:
            destination.parent.mkdir(parents=True, exist_ok=True)
            if not destination.exists() or hashlib.sha256(destination.read_bytes()).hexdigest() != digest:
                shutil.copyfile(source, destination)
        registry[key] = dict(file=f'res://assets/audio/{folder}/{key}.wav', level=level,
            level_name=LEVELS.get(level, level), speaker=speaker, text=text,
            trigger=clip.get('trigger_condition') or '', category=category,
            original_clip_id=clip['clip_id'], source_path=clip['source_path'],
            text_status=status, reference_candidates=candidates,
            reference_match_accepted=accepted, reference_url=URL if candidates else '',
            trigger_status=clip.get('trigger_condition_status', 'unresolved'),
            native_trigger_references=clip.get('native_trigger_references', []),
            wav_sha256=digest, duration_seconds=clip['duration_seconds'],
            level_name_evidence='user_supplied' if level in LEVELS else 'filename',
            speaker_status='reference_candidate' if accepted and clip.get('speaker') == 'Unknown'
                           else clip.get('speaker_evidence', 'unresolved'))
    return registry


def attach_native_identities(registry, clips, xa_catalog=None, sfx_catalog=None):
    """Exact source identity plus verified extraction WAV hash, never fuzzy names."""
    by_source={}
    by_basename={}
    for clip in clips:
        source=clip['source_path'].replace('\\','/').casefold()
        by_source.setdefault(source,[]).append(clip)
        by_basename.setdefault(Path(source).name,[]).append(clip)
    xa_names={}
    if xa_catalog is not None:
        if xa_catalog.get('source_sha256')!=INSPECTED_SHA256 or xa_catalog.get('stride')!=16 or xa_catalog.get('bank_count')!=79 or len(xa_catalog.get('entries',[]))!=79*16:
            raise ValueError('Uninspected or incomplete native XA catalog')
        seen=set()
        for row in xa_catalog['entries']:
            bank,track,flat=row['bank'],row['track'],row['native_id']
            if not 0<=bank<79 or not 0<=track<16 or flat!=bank*16+track or flat in seen or not re.fullmatch(r'[A-Za-z0-9_]+\.bik',row['file_name'],re.I):
                raise ValueError('Malformed native XA identity')
            seen.add(flat)
            xa_names.setdefault(row['file_name'].casefold(),[]).append(dict(bank=bank,track=track,flat_id=flat))
    if sfx_catalog is not None and sfx_catalog.get('format')!='bounded_pc_sfx_kat_identity_v1':
        raise ValueError('Unknown SFX catalog format')
    sfx_sources={}
    for row in (sfx_catalog or {}).get('records',[]):
        if row.get('payload_present'):
            sfx_sources.setdefault(row['source_suffix'].casefold(),[]).append(dict(bank=row['bank'],id=row['native_sfx_id']))
    joined_xa=joined_sfx=0
    fields=('native_bank','native_track','native_flat_id','native_xa_identities','native_sfx_bank','native_sfx_id','native_sfx_identities')
    for entry in registry.values():
        for field in fields:entry.pop(field,None)
        source=entry['source_path'].replace('\\','/').casefold()
        candidates=[c for c in by_source.get(source,[]) if c['wav_sha256']==entry['wav_sha256']]
        if len(candidates)!=1:
            entry['native_identity_join_status']='unresolved_source_or_hash';continue
        basename=Path(source).name
        if entry['category']=='Dialogue' and basename in xa_names and len(by_basename[basename])==1:
            identities=xa_names[basename]
            primary=identities[0]
            entry.update(native_bank=primary['bank'],native_track=primary['track'],native_flat_id=primary['flat_id'],native_xa_identities=identities)
            joined_xa+=1
        suffix='/'.join(source.split('/')[-2:])
        if entry['category']=='SFX' and suffix in sfx_sources:
            identities=sfx_sources[suffix]
            primary=identities[0]
            entry.update(native_sfx_bank=primary['bank'],native_sfx_id=primary['id'],native_sfx_identities=identities)
            joined_sfx+=1
        entry['native_identity_join_status']='exact_source_and_wav_sha256' if any(f in entry for f in ('native_flat_id','native_sfx_id')) else 'unresolved'
    return dict(xa_entries=joined_xa,sfx_entries=joined_sfx)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, default=ROOT/'staging/raw_extracted/audio_source/dialogue_manifest.json')
    parser.add_argument('--output', type=Path, default=ROOT/'assets/audio/dialogue/dialogue_manifest.json')
    # Copied WAVs land under res://assets/audio/{dialogue,music,sfx}, independent of the manifest location.
    parser.add_argument('--audio-root', type=Path, default=ROOT/'assets/audio')
    parser.add_argument('--html', type=Path, default=ROOT/'staging/raw_extracted/GodotMigration/transcript.html')
    parser.add_argument('--offline', action='store_true')
    parser.add_argument('--xa-catalog',type=Path,default=ROOT/'staging/raw_extracted/Dialogue/native_voice_catalog.json')
    parser.add_argument('--sfx-catalog',type=Path,default=ROOT/'staging/reference/native_sfx_catalog.json')
    args = parser.parse_args()
    html = fetch_reference(args.html, args.offline)
    rows = parse_reference(html)
    document = json.loads(args.input.read_text(encoding='utf-8'))
    registry = export_registry(document, rows, args.input.parent, args.audio_root)
    identities=attach_native_identities(registry,document['clips'],
        json.loads(args.xa_catalog.read_text()) if args.xa_catalog.exists() else None,
        json.loads(args.sfx_catalog.read_text()) if args.sfx_catalog.exists() else None)
    write_json(args.output, registry)
    report = dict(clips=len(registry), reference_lines=len(rows), native_identity_joins=identities,
        matched=sum(x['reference_match_accepted'] for x in registry.values()),
        text_status=dict(Counter(x['text_status'] for x in registry.values() if x['category']=='Dialogue')),
        source_html_sha256=hashlib.sha256(html.encode()).hexdigest(),
        categories=dict(Counter(x['category'] for x in registry.values())))
    write_json(ROOT/'staging/validation/godot-dialogue.json', report)
    print(json.dumps(report))


if __name__ == '__main__':
    main()
