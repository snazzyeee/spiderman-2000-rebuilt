"""Exact smoke output identities and bounded proof for regenerated PNG captures."""
import hashlib
import json
from pathlib import Path
import re
import struct
import zlib

CAPTURES = ('briefing', 'intro', 'menu', 'mission', 'rooftop')
OUTPUT_DIRECTORY = 'staging/validation/smoke'
OUTPUT_NAMES = ('godot-smoke.json',) + tuple(f'godot-{name}.png' for name in CAPTURES)
PNG_EXCEPTIONS = {(f'TestResults/godot-{name}.png', f'staging/validation/godot-{name}.png'): name for name in CAPTURES}


def validate_png(path, expected_size):
    if not 45 <= path.stat().st_size <= 16*1024*1024:
        raise ValueError('Capture PNG size out of bounds')
    blob = path.read_bytes()
    if blob[:8] != b'\x89PNG\r\n\x1a\n':
        raise ValueError('Not a PNG')
    cursor, header, data, ended = 8, None, bytearray(), False
    while cursor < len(blob):
        if cursor+12 > len(blob):
            raise ValueError('Truncated PNG chunk')
        size, = struct.unpack_from('>I', blob, cursor)
        end = cursor+12+size
        if end > len(blob):
            raise ValueError('PNG chunk escapes file')
        kind = blob[cursor+4:cursor+8]; payload = blob[cursor+8:end-4]
        crc, = struct.unpack_from('>I',blob,end-4)
        if zlib.crc32(kind+payload) != crc:
            raise ValueError('PNG CRC mismatch')
        if kind == b'IHDR':
            if header is not None or cursor != 8 or size != 13:
                raise ValueError('Invalid PNG header ordering')
            header = struct.unpack('>IIBBBBB',payload)
        elif kind == b'IDAT':
            data.extend(payload)
        elif kind == b'IEND':
            if size or end != len(blob):
                raise ValueError('PNG trailing bytes')
            ended = True
        elif kind[0] & 32 == 0:
            raise ValueError('Unsupported critical PNG chunk')
        cursor = end
    if not ended or header is None or tuple(header[:2]) != tuple(expected_size) or header[2] != 8 or header[3] not in (2,6) or header[4:] != (0,0,0):
        raise ValueError('PNG does not match the smoke capture format/dimensions')
    stride = expected_size[0]*(3 if header[3] == 2 else 4)+1
    expected = stride*expected_size[1]
    decoder = zlib.decompressobj()
    pixels = decoder.decompress(data, expected+1)
    if len(pixels) != expected or not decoder.eof or decoder.unconsumed_tail or decoder.unused_data or any(pixels[i] > 4 for i in range(0,expected,stride)):
        raise ValueError('Invalid bounded PNG scanlines')
    return list(expected_size)


def check_no_collisions(inventory, directory=OUTPUT_DIRECTORY):
    outputs = {f'{directory}/{name}'.casefold() for name in OUTPUT_NAMES}
    collisions = outputs & {row['target'].casefold() for row in inventory['files']}
    if collisions:
        raise ValueError('Smoke output overlaps preserved target: '+str(sorted(collisions)))
    return sorted(outputs)


def prove_capture(root, row):
    identity = (row['source'], row['target'])
    if identity not in PNG_EXCEPTIONS:
        raise ValueError('Capture identity is not authorized')
    project = (root/'project.godot').read_text(encoding='utf-8')
    dimensions = []
    for axis in ('width','height'):
        match = re.search(rf'^window/size/viewport_{axis}=(\d+)$',project,re.M)
        if not match or not 0 < int(match[1]) <= 4096:
            raise ValueError('Unbounded or missing smoke viewport')
        dimensions.append(int(match[1]))
    writer = (root/'src/diagnostics/smoke_runner.gd').read_text(encoding='utf-8')
    name = PNG_EXCEPTIONS[identity]
    if f'await capture("{name}")' not in writer or '"godot-"+name_of_view+".png"' not in writer:
        raise ValueError('Smoke writer does not produce this capture')
    size = validate_png(root/row['target'], dimensions)
    return dict(dimensions=size, writer=f'capture("{name}")',
                reason='Regenerated renderer smoke capture; inventory identity, bounded PNG and writer verified.')


def audit(root):
    path = root/'staging/migration_audit/preservation_inventory.json'
    inventory = json.loads(path.read_text(encoding='utf-8'))
    rows = []
    for identity in PNG_EXCEPTIONS:
        matches = [r for r in inventory['files'] if (r['source'],r['target']) == identity]
        if len(matches) != 1:
            raise ValueError('Capture missing/duplicated in inventory: '+identity[0])
        row = matches[0]
        rows.append(dict(source=row['source'],target=row['target'],original_sha256=row['sha256'],
                         current_sha256=hashlib.sha256((root/row['target']).read_bytes()).hexdigest(),**prove_capture(root,row)))
    report = dict(passed=True,inventory_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                  new_output_paths=check_no_collisions(inventory),files=rows)
    (path.parent/'capture_exception_proof.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    return report


if __name__ == '__main__':
    import sys
    root = Path(__file__).resolve().parents[2]
    if '--check-output-paths' in sys.argv:
        path = root/'staging/migration_audit/preservation_inventory.json'
        inventory = json.loads(path.read_text(encoding='utf-8')) if path.exists() else {'files':[]}
        print(json.dumps(dict(smoke_output_paths=check_no_collisions(inventory))))
    else:
        print(json.dumps(audit(root),indent=2))
