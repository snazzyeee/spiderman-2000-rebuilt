import importlib.util
import json
import pathlib
import struct
import sys
import tempfile
import unittest

REFERENCE = pathlib.Path(__file__).parents[1]
sys.path.insert(0, str(REFERENCE))


def module(name, filename):
    spec = importlib.util.spec_from_file_location(name, REFERENCE / filename)
    result = importlib.util.module_from_spec(spec); spec.loader.exec_module(result)
    return result


units = module('psx_units_tested', 'psx_units.py')
harvest = module('harvest_tested', 'harvest_anim_ids.py')
camera = module('camera_tested', 'extract_camera_presets.py')
trg = module('trg_tested', 'export_trg_graph.py')


class UnitsTests(unittest.TestCase):
    def test_shared_vectors(self):
        vectors = json.loads((REFERENCE / 'tests/data/units_vectors.json').read_text())
        tolerance = vectors['tolerance']
        for raw, expected in vectors['fixed_to_float']:
            self.assertAlmostEqual(units.fixed_to_float(raw), expected, delta=tolerance)
        for raw, expected in vectors['angle_to_radians']:
            self.assertAlmostEqual(units.angle_to_radians(raw), expected, delta=tolerance)
        for start, end, expected in vectors['wrap_angle_delta']:
            self.assertEqual(units.wrap_angle_delta(start, end), expected)
        for name in ('world_to_godot', 'fixed_world_to_godot'):
            for raw, expected in vectors[name]:
                for value, wanted in zip(getattr(units, name)(raw), expected):
                    self.assertAlmostEqual(value, wanted, delta=tolerance)

    def test_fixed_round_trip(self):
        self.assertEqual(units.float_to_fixed(units.fixed_to_float(-12345)), -12345)


class HarvestTests(unittest.TestCase):
    SOURCE = '\n'.join([
        '// @Ok', 'void CPlayer::CheckGroundGone(void)', '{', '\tthis->PlaySingleAnim(212, 0, -1);', '}', '',
        '// @MEDIUMTODO', 'void CPlayer::CheckJump(void)', '{', '\tPlaySingleAnim(0x10u, 0, -1);', '}', '',
        '// @Ok', 'INLINE i32 CThug::SpideyAnimUppercut(void)', '{', '\treturn MechList->mAnim == 106;', '}', ''])

    def test_call_sites_actors_and_confidence(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            (root / 'thug.cpp').write_text(self.SOURCE + '\n// @Ok\nvoid CThug::Die(void)\n{\n\tthis->RunAnim(50, 0, -1);\n}\n')
            records = harvest.harvest_file(root / 'thug.cpp', root)
        by_slot = {r['slot']: r for r in records}
        self.assertEqual(by_slot[212]['actor'], 'CPlayer')
        self.assertEqual(by_slot[212]['confidence'], 'high')
        self.assertEqual(by_slot[212]['function'], 'CPlayer::CheckGroundGone')
        self.assertEqual(by_slot[16]['confidence'], 'low')            # TODO body
        self.assertEqual(by_slot[106]['role'], 'state_test')           # player slot tested by a baddy
        self.assertEqual(by_slot[106]['actor'], 'CPlayer')
        self.assertEqual(by_slot[50]['models'], ['thug'])
        self.assertEqual(harvest.snake('CheckStickToCeiling'), 'stick_to_ceiling')

    def test_out_of_range_slots_are_downgraded(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            (root / 'thug.cpp').write_text('// @Ok\nvoid CThug::Die(void)\n{\n\tthis->RunAnim(50, 0, -1);\n}\n')
            assets = root / 'assets'; assets.mkdir()
            document = json.dumps({'animations': [{}] * 10}).encode(); document += b' ' * (-len(document) % 4)
            (assets / 'thug.glb').write_bytes(struct.pack('<III', 0x46546c67, 2, 20 + len(document))
                                              + struct.pack('<II', len(document), 0x4e4f534a) + document)
            entry = harvest.build(root, assets)['entries'][0]
        self.assertFalse(entry['slot_in_model_range'])
        self.assertEqual(entry['confidence'], 'low')
        self.assertEqual(entry['animation_name'], 'thug_anim_50')


def synthetic_pe(text: bytes, data_raw: int = 0x100, data_virtual: int = 0x1000):
    """PE32 with .text at RVA 0x1000 and .data at RVA 0x3000 (partially file-backed)."""
    image = bytearray(0x400 + len(text) + data_raw)
    image[:2] = b'MZ'; struct.pack_into('<I', image, 60, 0x80)
    image[0x80:0x84] = b'PE\0\0'; struct.pack_into('<HH', image, 0x84, 0x14c, 2)
    struct.pack_into('<H', image, 0x94, 224); struct.pack_into('<H', image, 0x98, 0x10b)
    struct.pack_into('<I', image, 0x98 + 28, 0x400000)
    section = 0x98 + 224
    image[section:section + 5] = b'.text'
    struct.pack_into('<IIII', image, section + 8, len(text), 0x1000, len(text), 0x400)
    struct.pack_into('<I', image, section + 36, 0x60000020)
    image[section + 40:section + 45] = b'.data'
    struct.pack_into('<IIII', image, section + 48, data_virtual, 0x3000, data_raw, 0x400 + len(text))
    struct.pack_into('<I', image, section + 76, 0xC0000040)
    image[0x400:0x400 + len(text)] = text
    return bytes(image)


class CameraTests(unittest.TestCase):
    def test_instruction_forms(self):
        self.assertEqual(camera.word_load(b'\x66\xa1\x10\x30\x40\x00', 0), (6, 0x403010))
        self.assertEqual(camera.word_load(b'\x66\x8b\x0d\x12\x30\x40\x00', 0), (7, 0x403012))
        self.assertEqual(camera.word_load(b'\x0f\xbf\x05\x14\x30\x40\x00', 0), (7, 0x403014))
        self.assertIsNone(camera.word_load(b'\x66\x8b\x4d\x08\x00\x00\x00', 0))   # [ebp+8] is not absolute
        self.assertEqual(camera.word_store(b'\x66\x89\x15\x10\x30\x40\x00', 0), (7, 0x403010, None))
        self.assertEqual(camera.word_store(b'\x66\xc7\x05\x10\x30\x40\x00\xbc\x02', 0), (9, 0x403010, 700))

    def test_storage_classification_and_preset_binding(self):
        names = {'functions': {}}
        setters = {}
        code = bytearray()
        def function(name, body):
            address = 0x401000 + len(code)
            names['functions'][str(address)] = {'name': name, 'address': address}
            code.extend(body + b'\xc3' + b'\x90' * (-(len(body) + 1) % 16))
            return address
        for name in camera.SETTERS:
            setters[name] = function(f'?{name}@CCamera@@QAEXFG@Z', b'\x90')
        for preset_index, preset in enumerate(camera.PRESETS):
            start = 0x401000 + len(code)
            body = bytearray()
            for param, name in enumerate(camera.SETTERS):
                body += b'\x66\xa1' + struct.pack('<I', 0x403000 + 0x10 * preset_index + 2 * param)
                call_site = start + len(body)
                body += b'\xe8' + struct.pack('<i', setters[name] - (call_site + 5))
            function(f'?Set{preset}Camera@CPlayer@@QAEXH@Z', bytes(body))
        function('end', b'\x90')
        image = camera.Image(synthetic_pe(bytes(code)))
        symbols = camera.Symbols(names)
        presets = camera.preset_globals(image, symbols)
        self.assertEqual(presets['Swing']['globals'], [0x403030 + 2 * p for p in range(5)])
        self.assertTrue(image.classify(0x403010)['file_backed'])
        self.assertFalse(image.classify(0x403200)['file_backed'])     # beyond raw size: zero-filled
        with self.assertRaises(ValueError):
            image.classify(0x500000)

    def test_uninspected_executable_fails_closed(self):
        with tempfile.TemporaryDirectory() as folder:
            exe = pathlib.Path(folder) / 'SpideyPC.exe'; exe.write_bytes(synthetic_pe(b'\xc3' * 16))
            names = pathlib.Path(folder) / 'names.json'; names.write_text('{"functions": {}}')
            with self.assertRaises(ValueError):
                camera.build(exe, names, None)


def synthetic_trg():
    """Node 0: BADDY linking to 1; node 1: POINT with position; node 2: AUTOEXEC; node 3: CAMPT."""
    nodes = [struct.pack('<4H2H', 1, 203, 0, 2, 1, 3),
             struct.pack('<HHH', 3, 0, 0) + b'\0\0' + struct.pack('<3i', 4096, -8192, 12288),
             struct.pack('<H', 4) + b'\0\0',
             struct.pack('<HHH', 13, 0, 0) + b'\0\0' + struct.pack('<3i', 1, 2, 3)]
    header = b'_TRG' + struct.pack('<HHI', 2, 1, len(nodes))
    offsets, body, at = [], b'', len(header) + 4 * len(nodes)
    for node in nodes:
        offsets.append(at + len(body)); body += node
    data = header + struct.pack(f'<{len(nodes)}I', *offsets) + body
    decoded = {'versionMajor': 2, 'versionMinor': 1, 'nodeCount': 4, 'nodes': [
        {'index': 0, 'typeId': 1, 'type': 'BADDY', 'offset': offsets[0], 'links': [1, 3], 'subType': 203},
        {'index': 1, 'typeId': 3, 'type': 'POINT', 'offset': offsets[1], 'links': [],
         'position': {'x': 1.0, 'y': -2.0, 'z': 3.0}},
        {'index': 2, 'typeId': 4, 'type': 'AUTOEXEC', 'offset': offsets[2],
         'commands': [{'opcode': 140, 'name': 'SetRestart', 'args': ['Missing']}]},
        {'index': 3, 'typeId': 13, 'type': 'CAMPT', 'offset': offsets[3], 'links': [0],
         'position': {'x': 1 / 4096, 'y': 2 / 4096, 'z': 3 / 4096}}]}
    return data, decoded


class TrgTests(unittest.TestCase):
    def test_raw_layout_and_cross_check(self):
        data, decoded = synthetic_trg()
        raw = trg.parse_raw(data)
        self.assertEqual([n['links'] for n in raw['nodes']], [[1, 3], [], None, []])
        check = trg.cross_check(data, raw, decoded)
        self.assertEqual(check['positions_located_in_raw_node'], 2)
        self.assertEqual(check['decoder_link_quirks_overridden'], {'CAMPT': 1})

    def test_export_uses_raw_links_and_entry_points(self):
        data, decoded = synthetic_trg()
        with tempfile.TemporaryDirectory() as folder:
            trg_path = pathlib.Path(folder) / 'x_L1A1_T.trg'; trg_path.write_bytes(data)
            json_path = pathlib.Path(folder) / 'x_L1A1_T.json'; json_path.write_text(json.dumps(decoded))
            graph = trg.export(trg_path, json_path, 'L1A1', 'synthetic')
        self.assertEqual(graph['nodes'][3]['links'], [])
        self.assertEqual(graph['nodes'][1]['position_world'], [4096, -8192, 12288])
        self.assertEqual(graph['nodes'][3]['incoming_links'], 1)
        self.assertEqual(graph['entry_points']['autoexec'][0]['initial_restart'], 'Missing')
        self.assertIsNone(graph['entry_points']['autoexec'][0]['initial_restart_node'])

    def test_disagreement_and_bounds_fail_closed(self):
        data, decoded = synthetic_trg()
        decoded['nodes'][0]['links'] = [1]
        with self.assertRaises(ValueError):
            trg.cross_check(data, trg.parse_raw(data), decoded)
        with self.assertRaises(ValueError):
            trg.parse_raw(data[:20])
        broken = bytearray(data); struct.pack_into('<H', broken, struct.unpack_from('<I', data, 12)[0] + 6, 9)
        with self.assertRaises(ValueError):
            trg.parse_raw(bytes(broken))                                  # link to missing node


if __name__ == '__main__':
    unittest.main()
