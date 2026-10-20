"""Fingerprint-gated bounded frontend SFX call inventory; never executes input."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

from extract_native_voice_catalog import INSPECTED_SHA256, PeImage

ROOT = Path(__file__).resolve().parents[2]
MAX_INPUT = 16 * 1024 * 1024


def frontend_ranges(functions):
    if not 2 <= len(functions) <= 20000:
        raise ValueError('Function count outside scan budget')
    rows = sorted(functions, key=lambda row: row['address'])
    result = []
    for row, following in zip(rows, rows[1:]):
        start, end = row['address'], following['address']
        if not isinstance(start, int) or not isinstance(end, int) or start >= end:
            raise ValueError('Invalid function boundary')
        name = row['name']
        if not isinstance(name, str) or len(name) > 512:
            raise ValueError('Invalid function name')
        if any(token in name.lower() for token in ('shell', 'front_', 'cmenu', 'crudeword')):
            if end - start > 65535:
                raise ValueError('Frontend function exceeds byte budget')
            result.append((name, start, end))
    if len(result) > 1024 or sum(end - start for _, start, end in result) > 2 * 1024 * 1024:
        raise ValueError('Frontend scan exceeds total budget')
    return result


def call_contexts(instructions, targets):
    """Inventory positive direct calls only; do not infer IDs across branches."""
    result = []
    for index, (address, mnemonic, operand) in enumerate(instructions):
        if mnemonic == 'call' and operand in targets:
            result.append(dict(call=hex(address), target=operand,
                               context=instructions[max(0, index - 10):index + 4]))
    return result


def scan(data, names):
    digest = hashlib.sha256(data).hexdigest()
    if len(data) > MAX_INPUT or digest != INSPECTED_SHA256:
        raise ValueError('Uninspected executable fingerprint or input budget exceeded')
    image = PeImage(data)
    functions = list(names['functions'].values())
    ranges = frontend_ranges(functions)
    targets = {hex(row['address']) for row in functions
               if row['name'].startswith(('?SFX_Play@@', '?SFX_PlayPos@@'))}
    if len(targets) != 2:
        raise ValueError('Expected unique SFX call targets')
    sys.path.insert(0, str(ROOT / 'tools/third_party/font-tools/python'))
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    calls = []
    for name, start, end in ranges:
        offset = image.offset(start - image.base, end - start)
        instructions = [(item.address, item.mnemonic, item.op_str)
                        for item in decoder.disasm(data[offset:offset + end - start], start)]
        for item in call_contexts(instructions, targets):
            calls.append(dict(function=name, start=hex(start), end=hex(end), **item))
    return dict(sha256=digest, scanned=len(ranges), calls=calls,
                limitation='Positive calls only; embedded jump tables may end linear decoding')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--executable', type=Path, default=ROOT / 'staging/raw_extracted/PC/app/SpideyPC.exe')
    parser.add_argument('--names', type=Path, default=ROOT / 'tools/third_party/spidey-decomp/tools/names.json')
    parser.add_argument('--output', type=Path, default=ROOT / 'staging/reference/frontend-sfx-static.json')
    args = parser.parse_args()
    if args.executable.stat().st_size > MAX_INPUT or args.names.stat().st_size > 8 * 1024 * 1024:
        raise ValueError('Input exceeds bounded read budget')
    with args.executable.open('rb') as source:
        data = source.read(MAX_INPUT + 1)
    result = scan(data, json.loads(args.names.read_text(encoding='utf-8')))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(f"Scanned {result['scanned']} functions; {len(result['calls'])} positive SFX calls")


if __name__ == '__main__':
    main()
