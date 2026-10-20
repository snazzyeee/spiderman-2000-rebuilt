"""Migration exceptions are tested exclusively with synthetic reports."""
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('preservation', Path(__file__).parents[2]/'migration/preserve_unity_data.py')
preservation = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preservation)


class PreservationTests(unittest.TestCase):
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
