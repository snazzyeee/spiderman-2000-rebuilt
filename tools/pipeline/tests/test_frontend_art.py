import io
from pathlib import Path
import sys
import tempfile
import unittest
import zlib
sys.path.insert(0,str(Path(__file__).parents[1]))
from prepare_frontend_art import checked_extract, MAX_BYTES
class FrontendArtTests(unittest.TestCase):
 def test_bounds_crc_and_path_are_mandatory(self):
  blob=b'synthetic image fixture';entry=dict(path='data/synthetic.bmp',offset=0,stored_bytes=len(blob),size_bytes=len(blob),compressed=False,crc32=f'{zlib.crc32(blob):08x}')
  with tempfile.TemporaryDirectory() as folder:
   self.assertEqual(checked_extract(io.BytesIO(blob),entry,Path(folder)).read_bytes(),blob)
   for changed in (dict(size_bytes=MAX_BYTES+1),dict(stored_bytes=MAX_BYTES+1),dict(crc32='00000000'),dict(path='../escape.bmp')):
    with self.subTest(changed=changed),self.assertRaises(ValueError):checked_extract(io.BytesIO(blob),dict(entry,**changed),Path(folder))
