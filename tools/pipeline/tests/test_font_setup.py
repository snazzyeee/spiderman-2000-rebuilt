"""Unsupported hosts must fail before any download or extraction."""
import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('setup_font_tools', Path(__file__).parents[1] / 'setup_font_tools.py')
installer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(installer)


class FontSetupTests(unittest.TestCase):
    def test_unsupported_runtime_has_no_side_effects(self):
        for host, version, machine, bits in (
            ('linux', (3, 12), 'AMD64', 64),
            ('win32', (3, 11), 'AMD64', 64),
            ('win32', (3, 13), 'AMD64', 64),
            ('win32', (3, 12), 'ARM64', 64),
            ('win32', (3, 12), 'AMD64', 32),
        ):
            with self.subTest(host=host, version=version, machine=machine, bits=bits):
                with patch.object(installer.sys, 'platform', host), patch.object(installer.sys, 'version_info', version), patch.object(installer.platform, 'machine', return_value=machine), patch.object(installer.struct, 'calcsize', return_value=bits // 8), patch.object(Path, 'mkdir') as mkdir, patch.object(installer.urllib.request, 'urlopen') as download:
                    with self.assertRaisesRegex(RuntimeError, 'Windows x64.*Python 3.12'):
                        installer.install()
                    mkdir.assert_not_called()
                    download.assert_not_called()

    def test_supported_runtime(self):
        with patch.object(installer.sys, 'platform', 'win32'), patch.object(installer.sys, 'version_info', (3, 12)), patch.object(installer.platform, 'machine', return_value='AMD64'), patch.object(installer.struct, 'calcsize', return_value=8):
            installer.require_supported_runtime()
