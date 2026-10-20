import copy
import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile


def module(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).parents[1] / (name + '.py'))
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


installer = module('setup_godot_linux')
checker = module('ci_report')


class CiEvidenceTests(unittest.TestCase):
    def test_download_hash_and_archive_identity_fail_closed(self):
        with tempfile.TemporaryDirectory() as temporary:
            folder = Path(temporary)
            archive = folder / installer.ARCHIVE
            for member in (installer.BINARY, '../escape'):
                with zipfile.ZipFile(archive, 'w') as bundle:
                    bundle.writestr(member, b'synthetic executable fixture')
                with self.assertRaises(ValueError):
                    installer.install(folder)
                digest = hashlib.sha256(archive.read_bytes()).hexdigest()
                with patch.object(installer, 'SHA256', digest):
                    if member == installer.BINARY:
                        self.assertEqual(installer.install(folder).read_bytes(), b'synthetic executable fixture')
                    else:
                        with self.assertRaises(ValueError):
                            installer.install(folder)
            self.assertFalse((folder.parent / 'escape').exists())

    def test_ci_rejects_incomplete_suites_and_engine_errors(self):
        counts = {'camera': 56, 'swing': 13, 'wallcrawl': 21, 'zip': 14, 'webbing': 41}
        valid = dict(passed=True, display_driver='headless', renderer='forward_plus',
                     checks=[dict(passed=True) for _ in range(150)],
                     suites={name: dict(checks=count, passed=True) for name, count in counts.items()})
        checker.validate(valid, ['Godot synthetic run'])
        missing = copy.deepcopy(valid)
        missing['suites']['zip']['checks'] = 13
        for report, logs in ((missing, []), (valid, ['SCRIPT ERROR: synthetic']),
                             (dict(valid, checks=[]), [])):
            with self.assertRaises(ValueError):
                checker.validate(report, logs)
