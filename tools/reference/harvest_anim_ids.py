"""Harvest numeric animation-slot evidence from the pinned spidey-decomp sources.

Reads decompiled C++ as text only. Nothing is built, executed or copied: the
output records slot numbers, the actor that uses them, the enclosing function's
name and decomp status, and a source location. These are behavior-ledger facts,
not code. Labels are derived from function names and carry explicit confidence.
"""
from __future__ import annotations
import argparse
from collections import defaultdict
import json
from pathlib import Path
import re
import struct
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
DECOMP_COMMIT = 'f8a039248908e085f8fba8106769aea7f8f9de67'
PLAYER_RECEIVERS = ('MechList', 'pSpidey', 'pPlayer')
# A decomp source file whose name equals a converted model key animates that model.
FILE_MODELS = {'carnage': 'carnage', 'docock': 'docock', 'thug': 'thug', 'rhino': 'rhino',
               'scorpion': 'scorpion', 'venom': 'venom', 'mysterio': 'mysterio',
               'superock': 'superock', 'lizman': 'lizman', 'lizard': 'lizard',
               'blackcat': 'blackcat', 'hostage': 'hostage', 'torch': 'torch'}
INTEGER = r'(0x[0-9A-Fa-f]+|\d+)[uU]?'
CALL = re.compile(r'(?:(\w+)\s*->\s*)?\b(PlaySingleAnim|RunAnim)\s*\(\s*' + INTEGER + r'\s*[,)]')
TEST = re.compile(r'(?:(\w+)\s*->\s*)?\bmAnim\s*(==|!=)\s*' + INTEGER + r'\b')
HEADER = re.compile(r'^(?![\s{}#/])[^;=]*?\b(?:(\w+)\s*::\s*)?(~?\w+)\s*\([^;]*$')
TAG = re.compile(r'//\s*@(Ok|NotOk|Matching|AlmostMatching|SMALLTODO|MEDIUMTODO|BIGTODO|TODO)\b')


def snake(name: str) -> str:
    name = re.sub(r'^(Check|Set|Do|Handle)(?=[A-Z])', '', name)
    return re.sub(r'(?<=[a-z0-9])(?=[A-Z])', '_', name).lower()


def functions(lines: list[str]):
    """Yield (line_index, class, function, status_tags) for each column-0 definition."""
    for index, line in enumerate(lines):
        match = HEADER.match(line)
        if not match or line.rstrip().endswith(';'):
            continue
        # A definition opens a body at column 0 within a few lines.
        if not any(lines[j].startswith('{') or lines[j].rstrip().endswith('{')
                   for j in range(index, min(index + 8, len(lines)))):
            continue
        tags = []
        j = index - 1
        while j >= 0 and (lines[j].strip().startswith('//') or not lines[j].strip()):
            tags += TAG.findall(lines[j])
            j -= 1
        yield index, match[1] or '', match[2], tags


def harvest_file(path: Path, decomp: Path) -> list[dict]:
    lines = path.read_text(encoding='latin-1').splitlines()
    starts = list(functions(lines))
    records = []
    for number, line in enumerate(lines):
        code = line.split('//', 1)[0]
        hits = [('play', m[1], m[2], m[3]) for m in CALL.finditer(code)]
        hits += [('state_test', m[1], m[2], m[3]) for m in TEST.finditer(code)]
        if not hits:
            continue
        owner = max((s for s in starts if s[0] <= number), default=(0, '', '', []), key=lambda s: s[0])
        _, cls, function, tags = owner
        for role, receiver, operator, literal in hits:
            if receiver in PLAYER_RECEIVERS or (receiver in (None, 'this') and cls == 'CPlayer'):
                actor, models = 'CPlayer', ['spidey']
            elif receiver in (None, 'this'):
                actor = cls or path.stem
                models = [FILE_MODELS[path.stem]] if path.stem in FILE_MODELS else []
            else:
                actor, models = 'unknown:' + receiver, []
            status = 'Ok' if 'Ok' in tags or 'Matching' in tags else (tags[0] if tags else 'untagged')
            confidence = ('high' if status == 'Ok' and actor == 'CPlayer'
                          else 'medium' if status in ('Ok', 'NotOk', 'AlmostMatching') and models
                          else 'low')
            records.append(dict(actor=actor, models=models, slot=int(literal, 0), role=role,
                operator=operator if role == 'state_test' else '', function=f'{cls}::{function}' if cls else function,
                function_status=status, confidence=confidence,
                source=f'decomp:{path.relative_to(decomp).as_posix()}:{number + 1}@{DECOMP_COMMIT[:7]}'))
    return records


def model_animation_counts(assets: Path) -> dict[str, int]:
    counts = {}
    for glb in assets.glob('*.glb'):
        data = glb.read_bytes()
        if len(data) < 20 or data[:4] != b'glTF':
            continue
        length = struct.unpack_from('<I', data, 12)[0]
        document = json.loads(data[20:20 + length])
        counts[glb.stem.lower()] = len(document.get('animations', []))
    return counts


def build(decomp: Path, assets: Path | None) -> dict:
    records = []
    for path in sorted(decomp.glob('*.cpp')):
        records += harvest_file(path, decomp)
    counts = model_animation_counts(assets) if assets and assets.is_dir() else {}
    slots = defaultdict(list)
    for record in records:
        for model in record['models'] or ['unmapped']:
            slots[(record['actor'], model, record['slot'])].append(record)
    rank = {'high': 2, 'medium': 1, 'low': 0}
    entries = []
    for (actor, model, slot), evidence in sorted(slots.items()):
        count = counts.get(model)
        in_range = None if count is None else slot < count
        best = max(evidence, key=lambda r: rank[r['confidence']])['confidence']
        if in_range is False:
            best = 'low'
        labels = sorted({snake(r['function'].split('::')[-1]) for r in evidence})
        entries.append(dict(actor=actor, model=model, slot=slot,
            animation_name=f'{model}_anim_{slot}' if model != 'unmapped' else '',
            label=labels[0] if len(labels) == 1 else ' | '.join(labels), labels=labels,
            confidence=best, model_animation_count=count, slot_in_model_range=in_range,
            evidence=[{k: r[k] for k in ('role', 'operator', 'function', 'function_status', 'source')} for r in evidence]))
    return dict(schema_version=1, decomp_commit=DECOMP_COMMIT,
        method='Text scan of PlaySingleAnim/RunAnim literal arguments and mAnim comparisons; labels derive from enclosing function names',
        confidence_rules=dict(high='enclosing function tagged @Ok and the call targets the player',
                              medium='decompiled (@Ok/@NotOk) and the actor maps to a converted model',
                              low='TODO/untagged function, unmapped actor, or slot outside the model animation range'),
        index_mapping='slot N is the converted animation named <model>_anim_N (Multitool export names; contiguous from 0)',
        semantics_status='evidence_only: labels name the code path that uses a slot, not verified motion content',
        model_animation_counts={m: counts[m] for m in sorted({e['model'] for e in entries}) if m in counts},
        entries=entries)


def decomp_head(decomp: Path) -> str:
    result = subprocess.run(['git', '-C', str(decomp), 'rev-parse', 'HEAD'], capture_output=True, text=True, check=True)
    return result.stdout.strip()


def preserve_runtime_bindings(document: dict, previous: dict) -> None:
    """Keep reviewed ledger event choices across an evidence refresh, fail closed."""
    bindings = previous.get('runtime_bindings', {})
    names = {e['animation_name'] for e in document['entries'] if e['actor'] == 'CPlayer'}
    if any(name not in names for name in bindings.values()):
        raise ValueError('Reviewed runtime binding lost its player-slot evidence')
    if bindings:
        document['runtime_bindings'] = dict(bindings)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--decomp', type=Path, default=ROOT / 'tools/third_party/spidey-decomp')
    parser.add_argument('--assets', type=Path, default=ROOT / 'assets/characters')
    parser.add_argument('--output', type=Path, default=ROOT / 'docs/behavior_ledger/data/animation_semantics.json')
    args = parser.parse_args()
    if decomp_head(args.decomp) != DECOMP_COMMIT:
        raise SystemExit('Decomp checkout differs from the pinned commit; run tools/reference/setup_decomp.ps1')
    document = build(args.decomp, args.assets)
    if args.output.exists():
        preserve_runtime_bindings(document, json.loads(args.output.read_text(encoding='utf-8')))
    sys.path.insert(0, str(ROOT / 'tools/reference'))
    from batch_process_audio import write_json
    write_json(args.output, document)
    by = defaultdict(int)
    for entry in document['entries']:
        by[entry['confidence']] += 1
    player = [e for e in document['entries'] if e['actor'] == 'CPlayer']
    print(json.dumps(dict(entries=len(document['entries']), player_slots=len(player), confidence=dict(by),
                          out_of_range=sum(e['slot_in_model_range'] is False for e in document['entries']))))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
