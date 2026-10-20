"""Read PKR3 metadata without executing or extracting any archive member.

Format references:
https://github.com/krystalgamer/spidey-tools/tree/master/pkr_extractor
Archive names and offsets are untrusted; validate them before writing the index.
"""

import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path, PureWindowsPath
import struct

DIRECTORY = struct.Struct('<32sII')
FILE = struct.Struct('<32sIIIII')


def name_from(raw):
    if b'\0' not in raw:
        raise ValueError('Unterminated archive name')
    name = raw.split(b'\0', 1)[0].decode('ascii')
    path = PureWindowsPath(name)
    if not name or path.drive or path.root or '..' in path.parts:
        raise ValueError(f'Unsafe archive path: {name!r}')
    if any(':' in part for part in path.parts):
        raise ValueError(f'Invalid archive path: {name!r}')
    return path.as_posix()


def inspect(archive):
    size = archive.stat().st_size
    with archive.open('rb') as stream:
        header = stream.read(8)
        if len(header) != 8:
            raise ValueError('Truncated archive header')
        magic, offset = struct.unpack('<4sI', header)
        if magic != b'PKR3' or not 8 <= offset <= size - 12:
            raise ValueError('Invalid PKR3 header or directory offset')
        stream.seek(offset)
        unknown, directory_count, file_count = struct.unpack('<III', stream.read(12))
        table_size = directory_count * DIRECTORY.size + file_count * FILE.size
        if directory_count > 100_000 or file_count > 1_000_000:
            raise ValueError('Unreasonable table count')
        if offset + 12 + table_size > size:
            raise ValueError('Directory/file table exceeds archive bounds')
        directories = [DIRECTORY.unpack(stream.read(DIRECTORY.size))
                       for _ in range(directory_count)]
        if sum(row[2] for row in directories) != file_count:
            raise ValueError('Directory and file counts disagree')
        entries = []
        seen = set()
        for raw_directory, directory_unknown, count in directories:
            directory = name_from(raw_directory)
            for _ in range(count):
                raw_name, crc, flag, start, unpacked, packed = FILE.unpack(stream.read(FILE.size))
                path = str(PureWindowsPath(directory, name_from(raw_name)).as_posix())
                if path.casefold() in seen:
                    raise ValueError(f'Duplicate path: {path}')
                seen.add(path.casefold())
                if flag not in (2, 0xFFFFFFFE):
                    raise ValueError(f'Unknown compression flag: {flag:#x}')
                stored = packed if flag == 2 else unpacked
                if start < 8 or start + stored > offset:
                    raise ValueError(f'Payload outside data region: {path}')
                entries.append(dict(path=path, offset=start, stored_bytes=stored,
                                    size_bytes=unpacked, compressed=flag == 2,
                                    crc32=f'{crc:08x}'))
    with archive.open('rb') as stream:
        digest = hashlib.file_digest(stream, 'sha256').hexdigest()
    return dict(schema_version=1, archive=archive.name, sha256=digest,
                archive_bytes=size, directory_count=directory_count,
                file_count=file_count, table_offset=offset,
                file_types=dict(sorted(Counter(Path(e['path']).suffix.lower() or '<none>'
                                               for e in entries).items())),
                entries=entries)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archives', nargs='+', type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    results = [inspect(path) for path in args.archives]
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(results, indent=2) + '\n', encoding='utf-8')
    for result in results:
        print(json.dumps({key: value for key, value in result.items()
                          if key != 'entries'}, indent=2))


if __name__ == '__main__':
    main()
