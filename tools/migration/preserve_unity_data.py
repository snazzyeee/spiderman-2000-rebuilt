"""One-time migration audit. Copy endangered data before native, guarded deletion.

This program never deletes files or runs original game code. Its private ledger
proves SHA-256 equality before and after the repository restructuring.
"""
import argparse
from collections import Counter
import hashlib
import json
import os
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[2]
AUDIT = ROOT/'staging/migration_audit'
ART = {'.wav', '.glb', '.gltf', '.fbx', '.obj', '.png', '.tga', '.bmp',
       '.jpg', '.jpeg', '.webp', '.gif', '.ogg', '.mp3', '.flac', '.ttf', '.otf'}
SERIALIZED = {'.asset', '.unity', '.prefab', '.mat', '.anim', '.controller',
              '.overridecontroller', '.mixer', '.playable', '.inputactions', '.uxml', '.uss'}
PURGE = ('Game/', 'Builds/', 'Logs/', 'Tools/Unity/', 'Tools/Build/',
         'Tools/UnityEditor/', 'Tools/UnityDownloads/', 'Tools/ThirdParty/unity-cli/')


def digest(path):
    with open(long_path(path), 'rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def contained(path):
    value = path.resolve()
    if not value.is_relative_to(ROOT.resolve()) or value == ROOT.resolve() or '.git' in value.relative_to(ROOT).parts:
        raise ValueError('Path outside allowed migration tree: '+str(path))
    return value


def files():
    for directory, folders, names in os.walk(ROOT, followlinks=False):
        relative = Path(directory).relative_to(ROOT)
        folders[:] = [name for name in folders if name != '.git'
                      and not Path(directory, name).is_symlink()
                      and (relative/name).as_posix() not in {'staging', 'assets'}]
        for name in names:
            path = Path(directory)/name
            if path.is_symlink():
                raise ValueError('Inspect symlink before migration: '+str(path))
            yield path


def relocated(relative):
    if relative.startswith('Godot/assets/models/l1a1_'):
        return relative.replace('Godot/assets/models/', 'assets/environments/', 1)
    mappings = [('LocalReference/', 'staging/raw_extracted/'),
        ('Godot/assets/models/', 'assets/characters/'),
        ('Godot/assets/world/', 'assets/environments/'),
        ('Godot/assets/', 'assets/'), ('Godot/scripts/', 'src/'),
        ('Godot/scenes/', 'scenes/'), ('Tools/Reference/', 'tools/reference/'),
        ('Tools/ThirdParty/', 'tools/third_party/'), ('Tools/pipeline/', 'tools/pipeline/'),
        ('Tools/', 'tools/'), ('Docs/', 'docs/'), ('TestResults/', 'staging/validation/'),
        ('research_notes/', 'docs/research_notes/'), ('reports/', 'docs/reports/')]
    for old, new in mappings:
        if relative.startswith(old):
            return new+relative[len(old):]
    if relative == 'Godot/project.godot':
        return 'project.godot'
    if relative == 'Spider.Man.2000.zip' or relative.startswith('Spider.Man.2000/'):
        return 'staging/raw_extracted/install_media/'+relative
    return relative


def saved_art(relative):
    value = relative.lower()
    if '/audio/' in value or value.endswith('.wav'):
        category = 'audio'
    elif any(word in value for word in ['spiderman', '/spidey', '/henchman']):
        category = 'characters'
    elif any(word in value for word in ['/l1a1', '/l1a2', '/l1a3', '/l1a4', '/scimpact', '/webdome']):
        category = 'environments'
    else:
        category = 'ui'
    return f'assets/{category}/source_archive/{relative}'


def prepare():
    AUDIT.mkdir(parents=True, exist_ok=True)
    ledger_path = AUDIT/'preservation_inventory.json'
    if ledger_path.exists():
        resume_copies()
        capture_serialized_fields()
        return
    rows, copies, snapshots = [], [], []
    for path in list(files()):
        relative = path.relative_to(ROOT).as_posix()
        extension = path.suffix.lower()
        if extension == '.meta':
            continue
        doomed = any(relative.startswith(prefix) for prefix in PURGE)
        source_data = relative.startswith(('LocalReference/', 'TestResults/', 'research_notes/'))
        extracted = relative.startswith('Game/Assets/_LocalReference/')
        protected = extension in ART or source_data or extracted and extension not in SERIALIZED
        python_tool = relative.startswith('Tools/') and extension == '.py'
        own_cs = relative.startswith('Game/Assets/_Game/') and extension == '.cs'
        other_cs = relative.startswith('Game/Assets/') and extension == '.cs' and not own_cs
        if relative == 'TestResults/unity-preservation-prepare.txt' or not (protected or python_tool or own_cs or other_cs):
            continue
        if doomed:
            if relative.startswith('Game/Assets/_LocalReference/Audio/'):
                target = 'staging/raw_extracted/audio_source/'+relative.split('/Audio/', 1)[1]
            elif own_cs:
                target = 'docs/gameplay_references/'+relative.removeprefix('Game/Assets/_Game/')
            elif other_cs:
                target = 'staging/raw_extracted/code_reference/'+relative
            elif python_tool:
                target = 'staging/raw_extracted/legacy_python/'+relative
            elif extension in ART or extension == '.bin':
                target = saved_art(relative)
            else:
                target = 'staging/raw_extracted/legacy_data/'+relative
            copies.append((path, target))
        else:
            target = relocated(relative)
            # Preserve mutable manifest inputs byte-for-byte separately; runtime
            # copies will receive new res:// paths during the engine migration.
            if extension == '.json' and relative.startswith('Godot/assets/'):
                snapshots.append((path, 'staging/migration_audit/original_manifests/'+relative))
                target = 'staging/migration_audit/original_manifests/'+relative
            elif python_tool and not relative.startswith('Tools/ThirdParty/'):
                target = 'staging/migration_audit/python_before/'+relative
                snapshots.append((path, target))
        rows.append(dict(source=relative, target=target, bytes=path.stat().st_size,
                         sha256=digest(path), copied=doomed or target.startswith('staging/migration_audit/')))
        if len(rows) % 2000 == 0:
            print(json.dumps(dict(inventoried=len(rows))), flush=True)
    data = dict(schema_version=1, original_root=str(ROOT), files=rows,
                serialized_reference_count=0,
                protected_extensions=dict(Counter(Path(x['source']).suffix.lower() for x in rows)),
                preserved_bytes=sum(x['bytes'] for x in rows))
    ledger_path.write_text(json.dumps(data, indent=2), encoding='utf-8')
    resume_copies()
    capture_serialized_fields()
    print(json.dumps(dict(files=len(rows), copied=len(copies), snapshots=len(snapshots),
                         bytes=data['preserved_bytes'])))


def capture_serialized_fields():
    ledger_path = AUDIT/'preservation_inventory.json'
    data = json.loads(ledger_path.read_text(encoding='utf-8'))
    # Retain Inspector-authored gameplay and animation fields as ordinary JSON,
    # without retaining Unity serialized files or their engine metadata.
    serialized = []
    source_assets = ROOT/'Game/Assets'
    for source in source_assets.rglob('*'):
        if source.is_file() and source.suffix.lower() in SERIALIZED and (
                '_Game' in source.parts or '_LocalReference' in source.parts):
            payload = source.read_bytes()
            if b'\0' in payload:
                # Record opaque formats only by identity. Rig/animation content
                # is independently preserved in the CRC-verified GLBs.
                text, encoding = '', 'opaque_identity_only'
            else:
                text, encoding = payload.decode('utf-8-sig', errors='strict'), 'utf8'
            serialized.append(dict(source=source.relative_to(ROOT).as_posix(),
                bytes=len(payload), sha256=hashlib.sha256(payload).hexdigest(),
                encoding=encoding, fields_as_text=text))
    (AUDIT/'unity_authored_fields.json').write_text(json.dumps(serialized, ensure_ascii=False), encoding='utf-8')
    data['serialized_reference_count'] = len(serialized)
    ledger_path.write_text(json.dumps(data, indent=2), encoding='utf-8')
    verify(copied_only=True)
    print(json.dumps(dict(serialized_fields=len(serialized))), flush=True)


def long_path(path):
    return '\\\\?\\'+str(path.resolve()) if os.name == 'nt' else str(path)


def resume_copies():
    data = json.loads((AUDIT/'preservation_inventory.json').read_text(encoding='utf-8'))
    count = 0
    for row in data['files']:
        if not row['copied']:
            continue
        destination = contained(ROOT/row['target'])
        source = contained(ROOT/row['source'])
        if not os.path.isfile(long_path(destination)) or digest(destination) != row['sha256']:
            if digest(source) != row['sha256']:
                raise ValueError('Source changed since inventory: '+row['source'])
            os.makedirs(long_path(destination.parent), exist_ok=True)
            # Streaming with extended Windows paths avoids CopyFile2 MAX_PATH
            # failures when preserving deeply nested Unity package textures.
            with open(long_path(source), 'rb') as reader, open(long_path(destination), 'wb') as writer:
                shutil.copyfileobj(reader, writer, 1024*1024)
            if digest(destination) != row['sha256']:
                raise ValueError('Copy digest mismatch: '+row['source'])
        count += 1
        if count % 1000 == 0:
            print(json.dumps(dict(preservation_copies_checked=count)), flush=True)
    verify(copied_only=True)


ACCEPTED_EXCEPTIONS = {
    ('TestResults/godot-smoke.json', 'staging/validation/godot-smoke.json'):
        'Regenerated Godot smoke test report after inventory; original hash retained, inventory unchanged.'
}


def verify(copied_only=False):
    data = json.loads((AUDIT/'preservation_inventory.json').read_text(encoding='utf-8'))
    failures, accepted, count = [], [], 0
    for row in data['files']:
        if copied_only and not row['copied']:
            continue
        destination = contained(ROOT/row['target'])
        if not os.path.isfile(long_path(destination)) or os.stat(long_path(destination)).st_size != row['bytes'] or digest(destination) != row['sha256']:
            identity = (row['source'], row['target'])
            # A missing report is not a regenerated report. Both paths must match
            # the single allowlisted identity; this is not a wildcard bypass.
            if not copied_only and identity in ACCEPTED_EXCEPTIONS and os.path.isfile(long_path(destination)):
                accepted.append(dict(source=row['source'], target=row['target'],
                    original_sha256=row['sha256'], current_sha256=digest(destination),
                    reason=ACCEPTED_EXCEPTIONS[identity]))
            else:
                failures.append(dict(source=row['source'], target=row['target']))
        count += 1
    report = dict(passed=not failures, checked=count, failures=failures,
                  copied_only=copied_only, accepted_exceptions=accepted)
    (AUDIT/('copy_verification.json' if copied_only else 'final_verification.json')).write_text(json.dumps(report, indent=2), encoding='utf-8')
    if failures:
        raise ValueError('Preservation verification failed: '+str(failures[:5]))
    print(json.dumps(report))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=['prepare', 'verify'])
    args = parser.parse_args()
    prepare() if args.operation == 'prepare' else verify()
