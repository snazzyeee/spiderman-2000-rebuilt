"""Migration exceptions are tested exclusively with synthetic reports."""
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import sys
import struct
import zlib
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).parents[2]/'migration'))
import smoke_outputs
spec = importlib.util.spec_from_file_location('preservation', Path(__file__).parents[2]/'migration/preserve_unity_data.py')
preservation = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preservation)


class PreservationTests(unittest.TestCase):
    def test_exact_png_exceptions_require_valid_capture_proofs(self):
        def chunk(kind, payload):
            return struct.pack('>I',len(payload))+kind+payload+struct.pack('>I',zlib.crc32(kind+payload))
        png = b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',2,2,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(b'\0'+b'\0'*6+b'\0'+b'\0'*6))+chunk(b'IEND',b'')
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); audit = root/'audit'; audit.mkdir()
            (root/'project.godot').write_text('window/size/viewport_width=2\nwindow/size/viewport_height=2\n')
            writer = root/'src/diagnostics/smoke_runner.gd'; writer.parent.mkdir(parents=True)
            writer.write_text('\n'.join('await capture("'+n+'")' for n in smoke_outputs.CAPTURES)+'\n"godot-"+name_of_view+".png"')
            rows=[]
            for source,target in smoke_outputs.PNG_EXCEPTIONS:
                path=root/target;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(png)
                rows.append(dict(source=source,target=target,sha256=hashlib.sha256(b'old').hexdigest(),bytes=3,copied=False))
            inventory=audit/'preservation_inventory.json';inventory.write_text(json.dumps(dict(files=rows))); before=inventory.read_bytes()
            with patch.object(preservation,'ROOT',root),patch.object(preservation,'AUDIT',audit):
                preservation.verify()
                report=json.loads((audit/'final_verification.json').read_text())
                self.assertEqual(len(report['accepted_exceptions']),5)
                self.assertEqual(inventory.read_bytes(),before)
                for source,target in [('TestResults/godot-sixth.png','staging/validation/godot-sixth.png'),('Other/godot-menu.png','staging/validation/godot-menu.png'),('TestResults/godot-menu.png','staging/elsewhere.png')]:
                    path=root/target;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(png)
                    inventory.write_text(json.dumps(dict(files=rows+[dict(rows[0],source=source,target=target)])))
                    with self.assertRaises(ValueError): preservation.verify()
                inventory.write_text(json.dumps(dict(files=rows)))
                (root/rows[0]['target']).write_bytes(png[:-1]+b'x')
                with self.assertRaises(ValueError): preservation.verify()

    def test_smoke_outputs_cannot_overlap_inventory(self):
        self.assertEqual(len(smoke_outputs.check_no_collisions({'files':[]})),6)
        with self.assertRaises(ValueError):
            smoke_outputs.check_no_collisions({'files':[{'target':'staging/validation/smoke/godot-menu.png'}]})
    def test_only_regenerated_report_is_accepted_and_inventory_is_immutable(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            audit = root/'audit'; audit.mkdir()
            target = root/'staging/validation/godot-smoke.json'
            target.parent.mkdir(parents=True); target.write_text('{"passed":true}')
            original = hashlib.sha256(b'original').hexdigest()
            rows = [dict(source='TestResults/godot-smoke.json', target='staging/validation/godot-smoke.json',
                         bytes=8, sha256=original, copied=False)]
            inventory = audit/'preservation_inventory.json'
            inventory.write_text(json.dumps(dict(files=rows)))
            before = inventory.read_bytes()
            with patch.object(preservation, 'ROOT', root), patch.object(preservation, 'AUDIT', audit):
                preservation.verify()
                report = json.loads((audit/'final_verification.json').read_text())
                self.assertTrue(report['passed'])
                self.assertEqual(report['accepted_exceptions'][0]['original_sha256'], original)
                self.assertTrue(report['accepted_exceptions'][0]['reason'])
                self.assertEqual(inventory.read_bytes(), before)
                rows.append(dict(rows[0], source='TestResults/other.json'))
                inventory.write_text(json.dumps(dict(files=rows)))
                with self.assertRaises(ValueError):
                    preservation.verify()
                rows.pop(); inventory.write_text(json.dumps(dict(files=rows)))
                target.unlink()
                with self.assertRaises(ValueError):
                    preservation.verify()
