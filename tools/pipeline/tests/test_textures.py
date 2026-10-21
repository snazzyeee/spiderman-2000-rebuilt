from pathlib import Path
import sys
import unittest
import io
import zipfile
sys.path.insert(0,str(Path(__file__).parents[1]))
from enhance_textures import cache_key, vram_bytes, scale_bmfont
from setup_texture_tools import checked_members

class TextureTests(unittest.TestCase):
    def test_tool_archive_rejects_traversal_and_links(self):
        for name in ('../escape','/absolute','C:/drive','back\\slash'):
            with self.subTest(name=name),zipfile.ZipFile(io.BytesIO(),'w') as archive:
                info=zipfile.ZipInfo('synthetic')
                info.filename=name
                archive.writestr(info,b'synthetic')
                with self.assertRaises(ValueError):checked_members(archive)
        with zipfile.ZipFile(io.BytesIO(),'w') as archive:
            info=zipfile.ZipInfo('symlink');info.external_attr=0o120777<<16
            archive.writestr(info,b'target')
            with self.assertRaises(ValueError):checked_members(archive)
    def test_cache_includes_all_settings_and_model(self):
        key=cache_key('source','model',dict(alpha=True,pad=16))
        self.assertEqual(key,cache_key('source','model',dict(pad=16,alpha=True)))
        self.assertNotEqual(key,cache_key('source','model2',dict(pad=16,alpha=True)))
        self.assertNotEqual(key,cache_key('source','model',dict(pad=0,alpha=True)))
    def test_mips_include_small_nonsquare_tails(self):
        self.assertEqual(vram_bytes(4,2),44)
        with self.assertRaises(ValueError):vram_bytes(0,2)
    def test_font_cells_and_advances_scale_without_changing_identity(self):
        result=scale_bmfont('common lineHeight=8 base=6 scaleW=10 scaleH=20\npage id=0 file="old.png"\nchar id=65 x=2 y=3 width=4 height=5 xoffset=-1 yoffset=0 xadvance=7',2,'new.png')
        self.assertIn('lineHeight=16 base=12 scaleW=20 scaleH=40',result)
        self.assertIn('id=65 x=4 y=6 width=8 height=10 xoffset=-2 yoffset=0 xadvance=14',result)
        self.assertIn('file="new.png"',result)
