"""Verify extracted PKR members against source lengths and CRCs; record SHA-256."""
import argparse
import hashlib
import json
from pathlib import Path
import zlib


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--index', required=True, type=Path)
    parser.add_argument('--archive', required=True)
    parser.add_argument('--root', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    source = next(item for item in json.loads(args.index.read_text(encoding='utf-8'))
                  if item['archive'] == args.archive)
    root = args.root.resolve(strict=True)
    records, errors = [], []
    for entry in source['entries']:
        path = (root / entry['path']).resolve()
        if not path.is_relative_to(root):
            raise ValueError(f'Path outside extraction root: {path}')
        digest, crc, size = hashlib.sha256(), 0, 0
        try:
            with path.open('rb') as stream:
                while chunk := stream.read(1024 * 1024):
                    digest.update(chunk)
                    crc = zlib.crc32(chunk, crc)
                    size += len(chunk)
            if size != entry['size_bytes'] or f'{crc:08x}' != entry['crc32']:
                raise ValueError('Length or CRC differs from archive metadata')
            records.append(dict(path=entry['path'], size_bytes=size,
                                crc32=f'{crc:08x}', sha256=digest.hexdigest()))
        except (OSError, ValueError) as error:
            errors.append(dict(path=entry['path'], error=str(error)))
    report = dict(archive=args.archive, archive_sha256=source['sha256'],
                  checked=len(records) + len(errors), passed=len(records),
                  errors=errors, entries=records)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({key: value for key, value in report.items() if key != 'entries'}, indent=2))
    raise SystemExit(1 if errors else 0)


if __name__ == '__main__':
    main()
