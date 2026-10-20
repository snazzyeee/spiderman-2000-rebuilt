"""Export Level trigger graphs (nodes, links, entry points, restarts) from PC TRG files.

Inputs are the original .trg bytes and the pinned Neversoft Multitool's JSON
decode of the same file. The Multitool decode supplies node fields, positions
and command names; this tool independently parses the raw header, node offset
table and per-type link lists (layout recorded in docs/behavior_ledger/trg.md)
and refuses to export when the two disagree. Original game code is never run.

Outputs are private derived data: levels/<CODE>/trg_graph.json (gitignored).
"""
from __future__ import annotations
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import struct
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/reference'))
import psx_units  # noqa: E402

MAGIC = b'_TRG'
DEFAULT_LEVELS = ('L1A1', 'L1A2', 'L1A2A', 'L1A3', 'L1A4')
# u16 index of a node's link count, by node type (behavior ledger: trg.md).
LINK_WORD = {1: 3, 2: 1, 3: 1, 6: 1, 8: 1, 9: 1, 10: 1, 12: 1, 13: 1, 5: 2, 20: 2, 1000: 1, 1001: 1, 1002: 2}
LINKLESS = {4, 15, 255}
# Known decoder disagreements where the raw layout (as the game reads it) wins:
# for CAMPT nodes Multitool reports the zero padding word after a zero link count as link [0].
DECODER_QUIRKS = {13: ([], [0])}
AUTOEXEC_TYPES = {4, 15}
RESTART_TYPE = 8
COMMAND_POINT_TYPE = 6


def parse_raw(data: bytes) -> dict:
    if len(data) < 12 or data[:4] != MAGIC:
        raise ValueError('Not a TRG file')
    major, minor, count = struct.unpack_from('<HHI', data, 4)
    if not 0 < count <= 65535 or 12 + count * 4 > len(data):
        raise ValueError('TRG node table escapes file')
    offsets = list(struct.unpack_from(f'<{count}I', data, 12))
    nodes = []
    for index, offset in enumerate(offsets):
        if not 12 + count * 4 <= offset < len(data) - 1:
            raise ValueError(f'Node {index} offset escapes file')
        node_type = struct.unpack_from('<H', data, offset)[0]
        links = None
        if node_type in LINK_WORD:
            at = offset + 2 * LINK_WORD[node_type]
            if at + 2 > len(data):
                raise ValueError(f'Node {index} link count escapes file')
            link_count = struct.unpack_from('<H', data, at)[0]
            if at + 2 + 2 * link_count > len(data):
                raise ValueError(f'Node {index} links escape file')
            links = list(struct.unpack_from(f'<{link_count}H', data, at + 2))
            if any(link >= count for link in links):
                raise ValueError(f'Node {index} links to a missing node')
        elif node_type not in LINKLESS:
            raise ValueError(f'Node {index} has unrecognized type {node_type}')
        nodes.append(dict(offset=offset, type=node_type, links=links))
    return dict(version=[major, minor], count=count, nodes=nodes)


def locate_position(data: bytes, start: int, end: int, position: dict) -> int | None:
    raw = [round(position[axis] * psx_units.FIXED_ONE) for axis in 'xyz']
    needle = struct.pack('<3i', *raw)
    at = data.find(needle, start, end)
    return None if at < 0 else at - start


def cross_check(data: bytes, raw: dict, decoded: dict) -> dict:
    if decoded['nodeCount'] != raw['count'] or [decoded['versionMajor'], decoded['versionMinor']] != raw['version']:
        raise ValueError('Decoder header disagrees with raw TRG header')
    mismatches = []
    quirks = Counter()
    located = positioned = 0
    bounds = [n['offset'] for n in raw['nodes']] + [len(data)]
    order = sorted(range(raw['count']), key=lambda i: raw['nodes'][i]['offset'])
    following = {index: bounds[-1] for index in order}
    for a, b in zip(order, order[1:]):
        following[a] = raw['nodes'][b]['offset']
    for node, mine in zip(decoded['nodes'], raw['nodes']):
        if node['offset'] != mine['offset'] or node['typeId'] != mine['type']:
            mismatches.append(dict(node=node['index'], field='offset/type'))
        if mine['links'] is not None and node.get('links', []) != mine['links']:
            if DECODER_QUIRKS.get(mine['type']) == (mine['links'], node.get('links', [])):
                quirks[node['type']] += 1
            else:
                mismatches.append(dict(node=node['index'], field='links'))
        if 'position' in node:
            positioned += 1
            if locate_position(data, mine['offset'], following[node['index']], node['position']) is not None:
                located += 1
    if mismatches:
        raise ValueError(f'Decoder disagrees with raw TRG layout: {mismatches[:5]}')
    return dict(nodes_checked=raw['count'], type_or_offset_mismatches=0, link_mismatches=0,
                decoder_link_quirks_overridden=dict(quirks),
                positions_located_in_raw_node=located, positioned_nodes=positioned)


def export(trg_path: Path, json_path: Path, level: str, decoder_sha256: str) -> dict:
    data = trg_path.read_bytes()
    raw = parse_raw(data)
    decoded = json.loads(json_path.read_text(encoding='utf-8'))
    check = cross_check(data, raw, decoded)
    names = {}
    nodes = []
    # Links come from the raw parse, which is authoritative where a decoder quirk is overridden.
    incoming = Counter(link for mine in raw['nodes'] for link in mine['links'] or [])
    for node, mine in zip(decoded['nodes'], raw['nodes']):
        row = dict(index=node['index'], type=node['type'], type_id=node['typeId'], offset=node['offset'],
                   links=mine['links'] or [], incoming_links=incoming[node['index']])
        for key in ('subType', 'subTypeName', 'baddyFlags', 'checksum', 'name', 'pickupType', 'groundedCheck'):
            if key in node:
                row[key] = node[key]
        if 'position' in node:
            world = [round(node['position'][axis] * psx_units.FIXED_ONE) for axis in 'xyz']
            row['position_world'] = world
            row['position_godot'] = [round(v, 5) for v in psx_units.world_to_godot(world)]
        if 'angles' in node:
            row['angles_degrees'] = [node['angles'][axis] for axis in 'xyz']
        for key in ('commands', 'script'):
            if key in node:
                row[key] = node[key]
        if 'name' in node:
            names[node['name']] = node['index']
        nodes.append(row)
    restarts = [dict(index=n['index'], name=n.get('name', '')) for n in nodes if n['type_id'] == RESTART_TYPE]
    autoexec = []
    for n in nodes:
        if n['type_id'] in AUTOEXEC_TYPES:
            initial = [c['args'][0] for c in n.get('commands', []) if c.get('name') == 'SetRestart' and c.get('args')]
            autoexec.append(dict(index=n['index'], initial_restart=initial[0] if initial else None,
                                 initial_restart_node=names.get(initial[0]) if initial else None))
    opcodes = Counter(c.get('name', '?') for n in nodes for key in ('commands', 'script') for c in n.get(key, []))
    return dict(schema_version=1, level=level,
        source=dict(trg_file=trg_path.name, trg_sha256=hashlib.sha256(data).hexdigest(),
                    decoder='neversoft-multitool 1.3.4', decoder_sha256=decoder_sha256,
                    decoded_json_sha256=hashlib.sha256(json_path.read_bytes()).hexdigest()),
        header=dict(version=raw['version'], node_count=raw['count']),
        units=psx_units.describe(),
        semantics=dict(links='Targets of this node\'s pulses, signals, suspends and kills (Trig_SendPulse/'
                             'Trig_SendSignalToLinks/SendKillFromNode walk this list)',
                       restart='Named checkpoint; SetRestart(name) selects it and Trig_ExecuteRestart runs its commands',
                       autoexec='Command list run once at level start (Trig_ExecuteAutoexec)',
                       command_point='Checksum-addressed command list triggered by collision or Trig_TriggerCommandPoint',
                       opcode_names='Multitool names; Unknown_0x* opcodes are undecoded and must not be guessed'),
        cross_check=check,
        entry_points=dict(autoexec=autoexec, restarts=restarts),
        command_points=[dict(index=n['index'], checksum=n.get('checksum', 0)) for n in nodes if n['type_id'] == COMMAND_POINT_TYPE],
        type_counts=dict(Counter(n['type'] for n in nodes)),
        opcode_counts=dict(opcodes.most_common()),
        nodes=nodes)


def find_pair(trg_dir: Path, json_dir: Path, level: str) -> tuple[Path, Path]:
    def matching(folder: Path, suffix: str) -> list[Path]:
        return [p for p in folder.glob('*' + suffix) if p.stem.split('_', 1)[1].lower() == f'{level}_t'.lower()]
    trg, decoded = matching(trg_dir, '.trg'), matching(json_dir, '.json')
    if len(trg) != 1 or len(decoded) != 1 or trg[0].stem != decoded[0].stem:
        raise ValueError(f'Expected one matching .trg/.json pair for {level}')
    return trg[0], decoded[0]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    work = ROOT / 'staging/raw_extracted/audio_source/_work~'
    parser.add_argument('--levels', nargs='+', default=list(DEFAULT_LEVELS))
    parser.add_argument('--trg-dir', type=Path, default=work / 'trg_input')
    parser.add_argument('--json-dir', type=Path, default=work / 'trg_json')
    parser.add_argument('--decoder', type=Path, default=ROOT / 'tools/third_party/neversoft-multitool-1.3.4/NeversoftMultitool-Windows-CLI-x64/NeversoftMultitool.exe')
    parser.add_argument('--output-root', type=Path, default=ROOT / 'levels')
    args = parser.parse_args()
    from batch_process_audio import write_json
    from prepare_phase1_assets import CONVERTER_SHA256
    decoder_sha256 = hashlib.sha256(args.decoder.read_bytes()).hexdigest()
    if decoder_sha256 != CONVERTER_SHA256:
        raise SystemExit('Multitool digest differs from the pinned converter')
    for level in args.levels:
        trg, decoded = find_pair(args.trg_dir, args.json_dir, level)
        graph = export(trg, decoded, level.upper(), decoder_sha256)
        write_json(args.output_root / level.upper() / 'trg_graph.json', graph)
        print(json.dumps(dict(level=graph['level'], nodes=graph['header']['node_count'],
                              restarts=len(graph['entry_points']['restarts']),
                              autoexec=len(graph['entry_points']['autoexec']),
                              command_points=len(graph['command_points']), cross_check=graph['cross_check'])))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
