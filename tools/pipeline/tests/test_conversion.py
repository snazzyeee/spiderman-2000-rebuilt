import importlib.util
import io
import json
from pathlib import Path
import struct
import tempfile
import unittest
import zlib

spec = importlib.util.spec_from_file_location('godot_conversion', Path(__file__).resolve().parents[1]/'convert_characters.py')
pipeline = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pipeline)


class ConversionTests(unittest.TestCase):
    def test_archive_integrity_and_containment(self):
        data = b'original synthetic model'
        entry = dict(path='data/model.psx', offset=0, stored_bytes=len(data), size_bytes=len(data),
                     compressed=False, crc32=f'{zlib.crc32(data):08x}')
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            self.assertEqual(pipeline.extract_member(io.BytesIO(data), entry, root).read_bytes(), data)
            with self.assertRaises(ValueError):
                pipeline.extract_member(io.BytesIO(data), dict(entry, path='../escape.psx'), root)
            with self.assertRaises(ValueError):
                pipeline.extract_member(io.BytesIO(b'x'*len(data)), entry, root)

    def test_material_conversion_preserves_rig_and_animation(self):
        doc = dict(asset={'version':'2.0'}, meshes=[{'primitives':[]}], skins=[{'joints':[0,1]}],
                   nodes=[{},{}], animations=[{'channels':[], 'samplers':[]}],
                   materials=[{'extensions':{'KHR_materials_unlit':{}}}],
                   extensionsUsed=['KHR_materials_unlit'])
        chunk = json.dumps(doc).encode()
        chunk += b' '*(-len(chunk)%4)
        blob = struct.pack('<III',0x46546c67,2,len(chunk)+20)+struct.pack('<II',len(chunk),0x4e4f534a)+chunk
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder)/'synthetic.glb'
            path.write_bytes(blob)
            info = pipeline.inspect_glb(path, True)
            self.assertEqual(info['joints'],[2])
            self.assertEqual(info['animations'],1)
            modified = json.loads(path.read_bytes()[20:])
            self.assertEqual(modified['skins'],doc['skins'])
            self.assertEqual(modified['animations'],doc['animations'])
            self.assertEqual(modified['materials'][0]['pbrMetallicRoughness']['metallicFactor'],0)
            self.assertNotIn('KHR_materials_unlit',modified['extensionsUsed'])

    def test_invalid_glb_fails_closed(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder)/'invalid.glb'
            path.write_bytes(b'not a model')
            with self.assertRaises(ValueError):
                pipeline.inspect_glb(path)


if __name__ == '__main__':
    unittest.main()
