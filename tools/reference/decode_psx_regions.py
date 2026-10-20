"""Bounded, read-only v4/v6 PSX object/model/packet audit. Never runs original code.

This deliberately does not guess transforms or parse face packets. Unknown
versions, escaping pointers, overlapping tables and truncated arrays fail closed.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import re

MAX_BYTES = 64 * 1024 * 1024
MAX_ENTRIES = 65536
MAX_TOTAL_VERTICES = 1000000


def decode(blob):
    if not 16 <= len(blob) <= MAX_BYTES:
        raise ValueError('PSX file size out of bounds')

    def read(fmt, offset):
        size = struct.calcsize(fmt)
        if offset < 0 or offset + size > len(blob):
            raise ValueError('Read outside file')
        return struct.unpack_from(fmt, blob, offset)

    magic, meta, count = read('<III', 0)
    if magic not in (0x00020004, 0x00020006) or not 0 < count <= MAX_ENTRIES:
        raise ValueError('Unsupported PSX header/count')
    end = 12 + count * 36
    meshes, = read('<I', end)
    if not 0 < meshes <= MAX_ENTRIES:
        raise ValueError('Model count out of bounds')
    pointers = read('<'+'I'*meshes, end+4)
    table_end = end+4+4*meshes
    if not table_end <= meta < len(blob):
        raise ValueError('Packet pointer overlaps tables')
    objects = []
    for index in range(count):
        offset = 12+36*index
        flags, x, y, z = read('<Iiii', offset)
        model, = read('<H', offset+22)
        if model >= meshes:
            raise ValueError('Invalid object model index')
        objects.append(dict(index=index, offset=offset, flags=flags, model=model,
                            origin_fixed=[x,y,z], origin_world=[x/4096,y/4096,z/4096]))
    models, total_vertices = [], 0
    for index, pointer in enumerate(pointers):
        if pointer % 4 or not table_end <= pointer < meta:
            raise ValueError('Invalid model pointer')
        flags, vertices, normals, faces = read('<HHHH', pointer)
        total_vertices += vertices
        if total_vertices > MAX_TOTAL_VERTICES:
            raise ValueError('Aggregate vertex read budget exceeded')
        if pointer+28+8*(vertices+normals) > meta:
            raise ValueError('Model arrays overlap packets')
        xyz = [read('<hhhH', pointer+28+8*i) for i in range(vertices)]
        models.append(dict(index=index, offset=pointer, flags=flags, vertices=vertices,
                           normals=normals, faces=faces,
                           vertex_bounds_raw=[[min(v[a] for v in xyz) for a in range(3)],
                                              [max(v[a] for v in xyz) for a in range(3)]] if xyz else None))
    packets, cursor = [], meta
    for _ in range(256):
        tag, = read('<I', cursor)
        if tag == 0xffffffff:
            break
        size, = read('<I', cursor+4)
        if cursor+8+size > len(blob) or size % 4:
            raise ValueError('Invalid packet extent')
        packets.append(dict(tag=f'{tag:08x}', offset=cursor, bytes=size))
        cursor += 8+size
    else:
        raise ValueError('Missing packet terminator')
    checksums = read('<'+'I'*meshes, cursor+4)
    return dict(sha256=hashlib.sha256(blob).hexdigest(), bytes=len(blob), version=magic & 0xffff,
                packet_offset=meta, objects=objects, models=models, packets=packets,
                model_checksums=[f'{v:08x}' for v in checksums])


def compare_glb(path, decoded):
    with path.open('rb') as stream:
        header = stream.read(20)
        magic, version, size, length, kind = struct.unpack('<IIIII', header)
        if magic != 0x46546c67 or version != 2 or kind != 0x4e4f534a or not 0 < length <= MAX_BYTES or size != path.stat().st_size:
            raise ValueError('Invalid bounded GLB')
        document = json.loads(stream.read(length))
    if len(document.get('nodes', [])) > MAX_ENTRIES or len(document.get('meshes', [])) > MAX_ENTRIES:
        raise ValueError('GLB comparison table budget exceeded')
    objects_by_model = {}
    for obj in decoded['objects']:
        objects_by_model.setdefault(obj['model'], []).append(obj)
    comparisons = []
    for node in document.get('nodes', []):
        if 'mesh' not in node:
            continue
        mesh = document['meshes'][node['mesh']]
        match = re.fullmatch(r'mesh_([0-9a-fA-F]{8})', mesh.get('name', ''))
        if not match:
            continue
        index = int(match[1], 16)
        objects = objects_by_model.get(index, [])
        if len(objects) != 1:
            continue
        expected = [v*s/2.25 for v,s in zip(objects[0]['origin_world'], [1,-1,-1])]
        matrix = node.get('matrix')
        actual = matrix[12:15] if matrix else node.get('translation', [0,0,0])
        comparisons.append(dict(model=index, expected_translation=expected,
            actual_translation=actual, translation_error=[a-b for a,b in zip(actual,expected)],
            raw_vertex_bounds=decoded['models'][index]['vertex_bounds_raw'],
            glb_position_bounds=[dict(min=document['accessors'][p['attributes']['POSITION']].get('min'),
                                     max=document['accessors'][p['attributes']['POSITION']].get('max'))
                                 for p in mesh.get('primitives', [])],
            decoded_converter_divisor=2.25))
    return dict(comparisons=comparisons,
                comparison_status='Candidate correspondence by mesh name; source bank is not encoded in exported node names',
                matching_translations=sum(max(map(abs,c['translation_error'])) < 0.01 for c in comparisons),
                max_translation_error=max((abs(v) for c in comparisons for v in c['translation_error']), default=None),
                nodes=[dict(name=n.get('name'), translation=n.get('translation'),
                           matrix=n.get('matrix'), scale=n.get('scale'))
                       for n in document.get('nodes', [])],
                decoded_origins_world=[o['origin_world'] for o in decoded['objects']],
                note='Comparison only: no fitted scale or offset is applied')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('--glb', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    with args.source.open('rb') as stream:
        result = decode(stream.read(MAX_BYTES+1))
    if args.glb:
        result['glb_comparison'] = compare_glb(args.glb, result)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(dict(objects=len(result['objects']), models=len(result['models']), packets=result['packets'])))


if __name__ == '__main__':
    main()
