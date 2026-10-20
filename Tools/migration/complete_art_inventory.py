"""Extend the pre-purge ledger with image formats and external glTF buffers."""
from collections import Counter
import json
from pathlib import Path
from urllib.parse import unquote, urlparse

import preserve_unity_data as safety

EXTRA = {'.svg', '.svgz', '.ico', '.hdr', '.exr', '.dds', '.psd', '.ktx', '.ktx2',
         '.aif', '.aiff', '.opus', '.aac', '.wma'}


def main():
    ledger = safety.AUDIT/'preservation_inventory.json'
    document = json.loads(ledger.read_text(encoding='utf-8'))
    known = {row['source'].casefold() for row in document['files']}
    pending = []
    for path in safety.files():
        if path.suffix.lower() in EXTRA:
            pending.append(path)
    for row in document['files']:
        if Path(row['source']).suffix.lower() != '.gltf':
            continue
        source = safety.ROOT/row['source']
        gltf = json.loads(source.read_text(encoding='utf-8'))
        for entry in gltf.get('buffers', [])+gltf.get('images', []):
            uri = entry.get('uri', '')
            if not uri or urlparse(uri).scheme:
                continue
            companion = safety.contained(source.parent/unquote(uri))
            if not companion.is_file():
                raise FileNotFoundError('External glTF companion missing before purge: '+str(companion))
            pending.append(companion)
    additions = 0
    for source in pending:
        relative = source.relative_to(safety.ROOT).as_posix()
        if relative.casefold() in known:
            continue
        doomed = any(relative.startswith(prefix) for prefix in safety.PURGE)
        target = safety.saved_art(relative) if doomed else safety.relocated(relative)
        document['files'].append(dict(source=relative, target=target, bytes=source.stat().st_size,
            sha256=safety.digest(source), copied=doomed))
        known.add(relative.casefold())
        additions += 1
    document['preserved_bytes'] = sum(row['bytes'] for row in document['files'])
    document['protected_extensions'] = dict(Counter(Path(row['source']).suffix.lower() for row in document['files']))
    ledger.write_text(json.dumps(document, indent=2), encoding='utf-8')
    if additions:
        safety.resume_copies()
    print(json.dumps(dict(additional_art_or_buffers=additions, total=len(document['files']))))


if __name__ == '__main__':
    main()
