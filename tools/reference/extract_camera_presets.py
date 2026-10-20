"""Recover Spider-Man's per-surface camera preset layout from the inspected PC build.

Static, bounded reads only: the executable is opened as bytes and never run.
Function addresses come from the pinned decomp's tools/names.json. The tool

1. decodes the five CPlayer::Set{Floor,Wall,Ceiling,Swing,Falling}Camera bodies,
   binding each 16-bit global load to the CCamera::SetCam* call that consumes it;
2. classifies every preset global as file-backed .data (static initial value) or
   zero-filled virtual .data/BSS (assigned at runtime);
3. walks CPlayer_SetSpideyCamValue's two jump-table levels to map the TRG
   command SetSpideyCamValue(mode, param, value, frames, apply_now) onto those
   globals, and finds any other code that writes them;
4. tallies every SetSpideyCamValue command in the decoded TRG scripts.

Output is private (binary-derived), like the native voice catalog.
"""
from __future__ import annotations
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import struct
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/reference'))
from extract_native_voice_catalog import INSPECTED_SHA256, PeImage  # noqa: E402

PRESETS = ('Floor', 'Wall', 'Ceiling', 'Swing', 'Falling')
# Consumer setter -> preset parameter index, in the order Set*Camera passes them.
SETTERS = {'SetCamXOffset': 0, 'SetCamYOffset': 1, 'SetCamZOffset': 2,
           'SetCamXZDistance': 3, 'SetCamYDistance': 4}
PARAMS = ('x_offset', 'y_offset', 'z_offset', 'xz_distance', 'y_distance')
# Decomp declares the Ceiling preset globals u16 and the others i16.
UNSIGNED = {'Ceiling'}


class Image(PeImage):
    """PeImage plus virtual sizes, so zero-filled BSS can be told apart from file data."""

    def __init__(self, data: bytes):
        super().__init__(data)
        pe = self.u32(60)
        count = struct.unpack_from('<H', data, pe + 6)[0]
        optional_size = struct.unpack_from('<H', data, pe + 20)[0]
        self.layout = []
        for index in range(count):
            offset = pe + 24 + optional_size + index * 40
            name = data[offset:offset + 8].rstrip(b'\0').decode('ascii', 'replace')
            virtual_size, rva, raw_size, raw = struct.unpack_from('<IIII', data, offset + 8)
            characteristics = struct.unpack_from('<I', data, offset + 36)[0]
            self.layout.append(dict(name=name, rva=rva, virtual_size=virtual_size, raw_size=raw_size,
                                    raw=raw, executable=bool(characteristics & 0x20000000)))

    def classify(self, address: int) -> dict:
        rva = address - self.base
        for section in self.layout:
            if section['rva'] <= rva < section['rva'] + max(section['virtual_size'], section['raw_size']):
                backed = rva + 2 <= section['rva'] + section['raw_size']
                return dict(section=section['name'], file_backed=backed)
        raise ValueError(f'Address {address:#x} is outside every section')

    def read(self, address: int, length: int) -> bytes:
        start = self.offset(address - self.base, length)
        return self.data[start:start + length]

    def dword(self, address: int) -> int:
        return struct.unpack('<I', self.read(address, 4))[0]


class Symbols:
    def __init__(self, names_json: dict):
        self.by_address = {int(a): v['name'] for a, v in names_json['functions'].items()}
        self.ordered = sorted(self.by_address)

    def find(self, fragment: str) -> int:
        hits = [a for a, n in self.by_address.items() if fragment in n]
        if len(hits) != 1:
            raise ValueError(f'Expected one function matching {fragment!r}, found {len(hits)}')
        return hits[0]

    def extent(self, address: int) -> tuple[int, int]:
        index = self.ordered.index(address)
        return address, self.ordered[index + 1]

    def owner(self, address: int) -> str:
        best = max((a for a in self.ordered if a <= address), default=None)
        return self.by_address.get(best, '?')


def word_load(code: bytes, i: int):
    """mov ax,[abs] / mov r16,[abs] / movsx|movzx r32,word [abs]; returns (length, address)."""
    if code[i:i + 2] == b'\x66\xa1':
        return 6, struct.unpack_from('<I', code, i + 2)[0]
    if code[i:i + 2] == b'\x66\x8b' and i + 7 <= len(code) and code[i + 2] & 0xC7 == 0x05:
        return 7, struct.unpack_from('<I', code, i + 3)[0]
    if code[i:i + 2] in (b'\x0f\xbf', b'\x0f\xb7') and i + 7 <= len(code) and code[i + 2] & 0xC7 == 0x05:
        return 7, struct.unpack_from('<I', code, i + 3)[0]
    return None


def word_store(code: bytes, i: int):
    """mov [abs],ax / mov [abs],r16 / mov word [abs],imm16; returns (length, address, immediate|None)."""
    if code[i:i + 2] == b'\x66\xa3' and i + 6 <= len(code):
        return 6, struct.unpack_from('<I', code, i + 2)[0], None
    if code[i:i + 2] == b'\x66\x89' and i + 7 <= len(code) and code[i + 2] & 0xC7 == 0x05:
        return 7, struct.unpack_from('<I', code, i + 3)[0], None
    if code[i:i + 3] == b'\x66\xc7\x05' and i + 9 <= len(code):
        return 9, struct.unpack_from('<I', code, i + 3)[0], struct.unpack_from('<h', code, i + 7)[0]
    return None


def preset_globals(image: Image, symbols: Symbols) -> dict:
    setters = {symbols.find(f'?{name}@CCamera@@'): index for name, index in SETTERS.items()}
    presets = {}
    for preset in PRESETS:
        start, end = symbols.extent(symbols.find(f'?Set{preset}Camera@CPlayer@@'))
        code = image.read(start, end - start)
        pending, bound = None, [None] * 5
        i = 0
        while i < len(code):
            load = word_load(code, i)
            if load:
                pending = load[1]
                i += load[0]
                continue
            if code[i] == 0xE8 and i + 5 <= len(code):
                target = (start + i + 5 + struct.unpack_from('<i', code, i + 1)[0]) & 0xFFFFFFFF
                if target in setters and pending is not None:
                    bound[setters[target]] = pending
                    pending = None
                    i += 5
                    continue
            i += 1
        if None in bound:
            raise ValueError(f'Set{preset}Camera did not bind all five preset globals: {bound}')
        presets[preset] = dict(function_address=f'{start:#x}', globals=bound)
    return presets


def table_target(code: bytes, base: int, start: int, limit: int):
    """Find `jmp dword [reg*4+table]` (FF 24 85/95) within limit bytes of start."""
    for i in range(start, min(start + limit, len(code) - 7)):
        if code[i:i + 2] == b'\xff\x24' and code[i + 2] in (0x85, 0x95):
            return struct.unpack_from('<I', code, i + 3)[0]
    return None


def command_mapping(image: Image, symbols: Symbols) -> dict:
    start, end = symbols.extent(symbols.find('CPlayer_SetSpideyCamValue'))
    code = image.read(start, end - start)
    outer = table_target(code, start, 0, 64)
    if outer is None or not start <= outer < end:
        raise ValueError('SetSpideyCamValue outer jump table not found inside the function')
    modes, untabled = {}, []
    for mode in range(6):
        case = image.dword(outer + mode * 4)
        if not start <= case < end:
            raise ValueError('Outer jump target escapes the function')
        inner = table_target(code, start, case - start, 48)
        if inner is None or not start <= inner < end:
            untabled.append(dict(mode=mode, case_address=f'{case:#x}'))
            continue
        params = []
        for param in range(5):
            target = image.dword(inner + param * 4)
            if not start <= target < end:
                raise ValueError('Inner jump target escapes the function')
            found = None
            for i in range(target - start, min(target - start + 32, len(code))):
                store = word_store(code, i)
                if store:
                    found = store[1]
                    break
            params.append(found)
        modes[mode] = params
    return dict(function_address=f'{start:#x}', outer_table=f'{outer:#x}', modes=modes, untabled=untabled)


def writers(image: Image, symbols: Symbols, addresses: set[int]) -> dict:
    found = defaultdict(list)
    for section in image.layout:
        if not section['executable']:
            continue
        code = image.data[section['raw']:section['raw'] + section['raw_size']]
        base = image.base + section['rva']
        for i in range(len(code) - 6):
            store = word_store(code, i)
            if store and store[1] in addresses:
                site = base + i
                found[store[1]].append(dict(site=f'{site:#x}', function=symbols.owner(site), immediate=store[2]))
    return found


def trg_census(trg_json: Path) -> dict:
    census = defaultdict(Counter)
    per_level = defaultdict(Counter)
    for path in sorted(trg_json.glob('*.json')):
        level = path.stem.split('_', 1)[1].rsplit('_', 1)[0].upper()
        document = json.loads(path.read_text(encoding='utf-8'))
        for node in document.get('nodes', []):
            for command in node.get('commands', []):
                if command.get('name') == 'SetSpideyCamValue':
                    if len(command.get('args', [])) != 5:
                        # The decoder's operand count disagrees with the native signature; keep it visible.
                        per_level[level][('irregular_arity', len(command.get('args', [])))] += 1
                        continue
                    mode, param, value, frames, apply_now = command['args']
                    census[(mode, param)][(value - 65536 if value >= 32768 else value, frames, apply_now)] += 1
                    per_level[level][(mode, param)] += 1
    return census, per_level


def build(exe: Path, names: Path, trg_json: Path | None) -> dict:
    data = exe.read_bytes()
    fingerprint = hashlib.sha256(data).hexdigest()
    if fingerprint != INSPECTED_SHA256:
        raise ValueError('Uninspected executable fingerprint; refusing to assume this build\'s layout')
    image = Image(data)
    symbols = Symbols(json.loads(names.read_text(encoding='utf-8')))
    presets = preset_globals(image, symbols)
    mapping = command_mapping(image, symbols)
    by_address = {address: (preset, PARAMS[index]) for preset, info in presets.items()
                  for index, address in enumerate(info['globals'])}
    mode_names = {}
    for mode, params in mapping['modes'].items():
        named = {by_address.get(a, (None,))[0] for a in params}
        mode_names[mode] = named.pop() if len(named) == 1 and None not in named else None
    sites = writers(image, symbols, set(by_address) | {a for p in mapping['modes'].values() for a in p if a})
    census, per_level = trg_census(trg_json) if trg_json and trg_json.is_dir() else ({}, {})
    preset_rows = {}
    for preset, info in presets.items():
        rows = {}
        for index, address in enumerate(info['globals']):
            storage = image.classify(address)
            initial = None
            if storage['file_backed']:
                initial = struct.unpack('<H' if preset in UNSIGNED else '<h', image.read(address, 2))[0]
            mode = next((m for m, n in mode_names.items() if n == preset), None)
            values = census.get((mode, index), Counter()) if mode is not None else Counter()
            rows[PARAMS[index]] = dict(address=f'{address:#x}', storage=storage, static_initial_value=initial,
                assignment='static' if storage['file_backed'] else 'dynamic (zero-filled until written)',
                writers=[w for w in sites.get(address, [])],
                trg_values=[dict(value=v, frames=f, apply_now=a, count=c) for (v, f, a), c in values.most_common()])
        preset_rows[preset] = dict(function_address=info['function_address'], parameters=rows)
    return dict(schema_version=1, format='spidey_pc_camera_presets_v1', source_sha256=fingerprint,
        method='Read-only static decode of Set*Camera loads, SetSpideyCamValue jump tables and absolute 16-bit stores; executable never run',
        units=dict(offsets_and_distances='PSX world units (i16)', frames='frames over which CCamera interpolates linearly'),
        trg_command=dict(name='SetSpideyCamValue', opcode=180, signature='(mode, param, value, frames, apply_now)',
                         function_address=mapping['function_address'], outer_jump_table=mapping['outer_table'],
                         mode_to_preset={str(m): n for m, n in sorted(mode_names.items())},
                         unmapped_mode_globals={str(m): [f'{a:#x}' if a else None for a in p]
                                                for m, p in sorted(mapping['modes'].items()) if mode_names[m] is None},
                         modes_without_parameter_table=mapping['untabled'],
                         param_order=list(PARAMS)),
        presets=preset_rows,
        trg_usage_by_level={level: sum(n for k, n in c.items() if k[0] != 'irregular_arity')
                            for level, c in sorted(per_level.items())},
        trg_irregular_arity={level: {str(k[1]): n for k, n in c.items() if k[0] == 'irregular_arity'}
                             for level, c in sorted(per_level.items()) if any(k[0] == 'irregular_arity' for k in c)},
        summary=dict(static=sum(r['assignment'] == 'static' for p in preset_rows.values() for r in p['parameters'].values()),
                     dynamic=sum(r['assignment'] != 'static' for p in preset_rows.values() for r in p['parameters'].values())))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('executable', type=Path, nargs='?', default=ROOT / 'staging/raw_extracted/PC/app/SpideyPC.exe')
    parser.add_argument('--names', type=Path, default=ROOT / 'tools/third_party/spidey-decomp/tools/names.json')
    parser.add_argument('--trg-json', type=Path, default=ROOT / 'staging/raw_extracted/audio_source/_work~/trg_json')
    parser.add_argument('--output', type=Path, default=ROOT / 'staging/reference/camera_presets.json')
    args = parser.parse_args()
    from batch_process_audio import write_json
    document = build(args.executable, args.names, args.trg_json)
    write_json(args.output, document)
    print(json.dumps(dict(summary=document['summary'], mode_to_preset=document['trg_command']['mode_to_preset'])))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
