"""CRC-checked, bounded push-offset audit; no inferred geometry correction.

Format/evidence contract: docs/behavior_ledger/offset_files.md.
Never executes original code. All decoded records remain in private staging.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zlib

from extract_audio_manifest import pkr_entries
from decode_psx_regions import MAX_BYTES as MAX_GEOMETRY_BYTES, decode as decode_geometry

MAX_BYTES = 4096
MAX_ROWS = 256
MAX_STORED_BYTES = 8192
ROOT = Path(__file__).resolve().parents[2]


def decode(blob):
    if not 1 <= len(blob) <= MAX_BYTES:
        raise ValueError('Offset member size outside bounds')
    if not re.fullmatch(rb'[0-9\- \t\r\n]+', blob):
        raise ValueError('Unsupported offset text characters')
    tokens = blob.split()
    if not tokens or any(not re.fullmatch(rb'-?[0-9]{1,10}', token) for token in tokens):
        raise ValueError('Malformed decimal token')
    count = int(tokens[0])
    if not 0 <= count <= MAX_ROWS or len(tokens) != 1 + count * 3:
        raise ValueError('Offset count/token length mismatch')
    rows, seen = [], set()
    for index in range(count):
        key, at_six, at_four = map(int, tokens[1 + index * 3:4 + index * 3])
        if not 0 <= key <= 0x7fffffff or key in seen:
            raise ValueError('Invalid or duplicate offset key')
        if not all(-32768 <= value <= 32767 for value in (at_six, at_four)):
            raise ValueError('Offset parameter outside signed-short range')
        seen.add(key)
        rows.append(dict(key=key, field_at_4=at_four, field_at_6=at_six))
    return rows


def read_member(stream, entry):
    member, crc, compressed, offset, length, stored = entry
    if not 0 < length <= MAX_BYTES or not 0 < stored <= MAX_STORED_BYTES:
        raise ValueError('Offset member exceeds read budget')
    stream.seek(offset)
    blob = stream.read(stored)
    if len(blob) != stored:
        raise ValueError('Truncated offset payload')
    if compressed:
        decoder = zlib.decompressobj()
        try:
            blob = decoder.decompress(blob, length + 1)
        except zlib.error as error:
            raise ValueError('Invalid compressed offset payload') from error
        if not decoder.eof or decoder.unused_data or decoder.unconsumed_tail:
            raise ValueError('Offset compressed stream escapes bounds')
    if len(blob) != length or zlib.crc32(blob) != crc:
        raise ValueError('Offset CRC/length mismatch: ' + str(member))
    return blob


def audit(archive, geometry_directory=None):
    selected = [entry for entry in pkr_entries(archive)
                if entry[0].parent.as_posix().casefold() == 'data/offsets'
                and entry[0].suffix.casefold() == '.off']
    if not 0 < len(selected) <= 64:
        raise ValueError('Missing or excessive offset member table')
    records = []
    with archive.open('rb') as stream:
        for entry in selected:
            member = entry[0]
            if not re.fullmatch(r'[a-zA-Z0-9]+_g\.off', member.name, re.IGNORECASE):
                raise ValueError('Unsupported offset member identity')
            blob = read_member(stream, entry)
            rows = decode(blob)
            record = dict(member=str(member), source_sha256=hashlib.sha256(blob).hexdigest(),
                          crc32=f'{entry[1]:08x}', bytes=len(blob), count=len(rows), records=rows,
                          geometric_transform_applied=False)
            if geometry_directory is not None:
                matches = [p for p in geometry_directory.iterdir()
                           if p.name.casefold() == member.stem.casefold() + '.psx']
                if len(matches) != 1:
                    raise ValueError('Missing or ambiguous matching G bank: ' + member.stem)
                source = matches[0]
                if source.stat().st_size > MAX_GEOMETRY_BYTES:
                    raise ValueError('G bank exceeds bounded audit size')
                geometry_blob = source.read_bytes()
                geometry = decode_geometry(geometry_blob)
                record['geometry'] = dict(source_sha256=hashlib.sha256(geometry_blob).hexdigest(),
                    objects=len(geometry['objects']), models=len(geometry['models']),
                    all_keys_in_object_range=all(row['key'] < len(geometry['objects']) for row in rows),
                    all_keys_in_model_range=all(row['key'] < len(geometry['models']) for row in rows))
            records.append(record)
    return dict(schema_version=1, format='level_push_offsets_v1', files=records,
                l1a1_offset_present=any(Path(row['member']).stem.casefold() == 'l1a1_g' for row in records),
                interpretation='Record consumer/units unknown; fields are not XYZ and no transform is justified')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--archive', type=Path, default=ROOT / 'staging/raw_extracted/PC/app/data.pkr')
    parser.add_argument('--geometry-directory', type=Path,
                        default=ROOT / 'staging/raw_extracted/Extracted/data/data')
    parser.add_argument('--output', type=Path, default=ROOT / 'staging/reference/level-offsets.json')
    args = parser.parse_args()
    if not args.output.resolve().is_relative_to((ROOT / 'staging').resolve()):
        raise ValueError('Decoded game records must stay in private staging')
    report = audit(args.archive, args.geometry_directory)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(files=len(report['files']), records=sum(row['count'] for row in report['files']),
                          l1a1_offset_present=report['l1a1_offset_present'], transforms_applied=0)))


if __name__ == '__main__':
    main()
