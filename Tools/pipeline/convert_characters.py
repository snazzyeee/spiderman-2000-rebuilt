"""CRC-checked PC archive extraction and surveyed rigged GLB export for Godot."""
import argparse
import csv
import hashlib
import json
from pathlib import Path
import re
import struct
import subprocess
import sys
import zlib

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT/'Tools/Reference'))
from pkr_index import inspect
from prepare_phase1_assets import CONVERTER_SHA256
from batch_process_audio import write_json


def extract_member(stream, entry, root):
    target = (root/entry['path']).resolve()
    if not target.is_relative_to(root.resolve()):
        raise ValueError('Archive traversal')
    stream.seek(entry['offset'])
    payload = stream.read(entry['stored_bytes'])
    if entry['compressed']:
        decoder = zlib.decompressobj()
        payload = decoder.decompress(payload, entry['size_bytes']+1)
        if not decoder.eof or decoder.unconsumed_tail or decoder.unused_data:
            raise ValueError('Compressed member bounds: '+entry['path'])
    if len(payload) != entry['size_bytes'] or f'{zlib.crc32(payload):08x}' != entry['crc32']:
        raise ValueError('Member CRC/length mismatch: '+entry['path'])
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(payload)
    return target


def inspect_glb(path, modernize=False):
    blob = path.read_bytes()
    if len(blob) < 20 or struct.unpack_from('<III', blob) != (0x46546c67, 2, len(blob)):
        raise ValueError('Invalid GLB header: '+str(path))
    offset, chunks, document = 12, [], None
    while offset < len(blob):
        length, kind = struct.unpack_from('<II', blob, offset)
        offset += 8
        chunk = blob[offset:offset+length]
        if len(chunk) != length or length % 4:
            raise ValueError('Invalid GLB chunk')
        if kind == 0x4e4f534a:
            document = json.loads(chunk)
        chunks.append((kind, chunk))
        offset += length
    if not document or not document.get('meshes'):
        raise ValueError('No renderable mesh')
    if modernize:
        for image in document.get('images', []):
            if 'name' in image:
                image['name'] = image['name'].lower()
        for material in document.get('materials', []):
            material.get('extensions', {}).pop('KHR_materials_unlit', None)
            material.setdefault('pbrMetallicRoughness', {}).update(metallicFactor=0, roughnessFactor=.86)
            material['doubleSided'] = True
        for key in ('extensionsRequired', 'extensionsUsed'):
            if key in document:
                document[key] = [x for x in document[key] if x != 'KHR_materials_unlit']
        output = b''
        for kind, chunk in chunks:
            if kind == 0x4e4f534a:
                chunk = json.dumps(document, separators=(',', ':')).encode()
                chunk += b' '*(-len(chunk)%4)
            output += struct.pack('<II', len(chunk), kind)+chunk
        path.write_bytes(struct.pack('<III', 0x46546c67, 2, 12+len(output))+output)
    return dict(meshes=len(document['meshes']), skins=len(document.get('skins', [])),
                joints=[len(x['joints']) for x in document.get('skins', [])],
                animations=len(document.get('animations', [])), textures=len(document.get('images', [])))


def write_layout(output):
    # glTFast 6.20 converts handedness by negating X (CachedFunction.cs),
    # whereas Godot retains glTF coordinates. Reuse the private authored route.
    source = ROOT/'Game/Assets/_LocalReference/Mission1/Generated/MissionLayout.asset'
    if not source.exists():
        raise FileNotFoundError('Existing private route missing: '+str(source))
    text = source.read_text()
    def vector(value):
        match = re.search(r'\{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}', value)
        return [-float(match[1]), float(match[2]), float(match[3])]
    spawn = vector(re.search(r'StartingRoof: (.+)', text)[1])
    points = [vector(x) for x in re.search(r'CheckpointRoofs:(.*?)BankArena:', text, re.S)[1].splitlines() if '{x:' in x]
    write_json(output, dict(spawn=spawn, checkpoints=points,
        bank=vector(re.search(r'm_Center: (.+)', text)[1]),
        skylight=vector(re.search(r'Skylight: (.+)', text)[1]),
        coordinate_transform='Unity glTFast 6.20 to Godot glTF: negate X',
        source='private authored Unity route, not recovered original mission scripting'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--archive', type=Path, default=ROOT/'LocalReference/PC/app/data.pkr')
    parser.add_argument('--converter', type=Path, default=ROOT/'Tools/ThirdParty/neversoft-multitool-1.3.4/NeversoftMultitool-Windows-CLI-x64/NeversoftMultitool.exe')
    args = parser.parse_args()
    if hashlib.sha256(args.converter.read_bytes()).hexdigest() != CONVERTER_SHA256:
        raise ValueError('Pinned converter digest mismatch')
    staging = ROOT/'LocalReference/GodotMigration'
    raw = staging/'raw'
    inventory = inspect(args.archive)
    entries = [e for e in inventory['entries'] if Path(e['path']).suffix.lower() in ('.psx', '.psh')
               and (not re.search(r'_[glo]\.psx$', e['path'], re.I)
                    or Path(e['path']).name.lower() in ('l1a1_g.psx', 'l1a1_o.psx'))]
    with args.archive.open('rb') as stream:
        for entry in entries:
            extract_member(stream, entry, raw)
    logs = ROOT/'Logs/GodotConversion'
    logs.mkdir(parents=True, exist_ok=True)
    survey = staging/'survey.csv'
    with (logs/'survey.log').open('w') as log:
        subprocess.run([str(args.converter), 'psx-anim-survey', str(raw), '-o', str(survey)],
                       stdout=log, stderr=subprocess.STDOUT, check=True, timeout=180)
    records, errors, dispositions = [], [], []
    rows = list(csv.DictReader(survey.open(encoding='utf-8-sig')))
    output = ROOT/'Godot/assets/models'
    for row in rows:
        relative = Path(row['relpath'])
        animated = row['has_hierarchy'].lower() == 'true' and int(row['bones']) > 1
        level = relative.name.lower() in ('l1a1_g.psx', 'l1a1_o.psx')
        if not animated and not level:
            dispositions.append(dict(source=relative.as_posix(), reason='survey found no multi-bone hierarchy', survey=row))
            continue
        source = raw/relative
        key = relative.with_suffix('').as_posix().lower().removeprefix('data/')
        target = output/(key+'.glb')
        target.parent.mkdir(parents=True, exist_ok=True)
        # Windows accepts case-insensitive lookups; Godot exports and Linux do
        # not. Normalize converter-produced uppercase filenames even on cache hits.
        for actual in target.parent.glob('*.glb'):
            if actual.name.casefold() == target.name.casefold() and actual.name != target.name:
                temporary = actual.with_suffix('.rename-glb')
                actual.rename(temporary)
                temporary.rename(target)
        digest = hashlib.sha256(source.read_bytes()).hexdigest()
        cache = target.with_suffix('.conversion.json')
        try:
            if cache.exists() and target.exists():
                old = json.loads(cache.read_text())
                if old.get('source_sha256') == digest and old.get('converter_sha256') == CONVERTER_SHA256 and old.get('glb_sha256') == hashlib.sha256(target.read_bytes()).hexdigest():
                    if old.get('conversion_version') != 2:
                        inspect_glb(target, modernize=True)
                        old.update(conversion_version=2, glb_sha256=hashlib.sha256(target.read_bytes()).hexdigest())
                        write_json(cache, old)
                    records.append(old)
                    continue
            command = [str(args.converter), 'psx-anim-export', str(source), '--anim', '-1', '--fps', '30', '-o', str(target)] if animated else [
                str(args.converter), 'psx-mesh', str(source), '-o', str(target.parent)]
            with (logs/(key.replace('/', '_')+'.log')).open('w') as log:
                subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, check=True, timeout=180)
            actual = next(x for x in target.parent.glob('*.glb') if x.name.casefold() == target.name.casefold())
            if actual.name != target.name:
                temporary = actual.with_suffix('.rename-glb')
                actual.rename(temporary)
                temporary.rename(target)
            details = inspect_glb(target, modernize=True)
            if animated and not details['skins']:
                raise ValueError('Rigged survey candidate exported without a skin')
            record = dict(source=relative.as_posix(), file='res://assets/models/'+key+'.glb',
                source_sha256=digest, converter_sha256=CONVERTER_SHA256,
                glb_sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                animation_semantics_verified=False, conversion_version=2, **details)
            write_json(cache, record)
            records.append(record)
            print(json.dumps(dict(converted=key, **details)), flush=True)
        except Exception as error:
            errors.append(dict(source=str(relative), error=str(error)))
    report = dict(archive_sha256=inventory['sha256'], extracted_members=len(entries),
        surveyed_psx=len(rows), converted=records, not_rigged=dispositions, errors=errors)
    write_json(ROOT/'Godot/assets/models/conversion_manifest.json', report)
    write_json(ROOT/'TestResults/godot-conversion.json', report)
    write_layout(ROOT/'Godot/assets/world/mission1_layout.json')
    print(json.dumps(dict(converted=len(records), not_rigged=len(dispositions), failures=len(errors))))
    return 1 if errors else 0


if __name__ == '__main__':
    raise SystemExit(main())
