import struct
import unittest
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parents[1]))
from find_frontend_background import animation_frames

class BackgroundTests(unittest.TestCase):
    def fixture(self):
        body=struct.pack('<I8sIII',1,b'test',1,0,3)
        return struct.pack('<IIIIII',0x20004,16,0,0,0x45,len(body))+body
    def test_bounded_frame_records(self):
        self.assertEqual(animation_frames(self.fixture()),{'test':[(0,3)]})
    def test_truncation_and_frame_budget(self):
        data=self.fixture()
        for bad in (data[:8],data[:-1],data[:36]+struct.pack('<I',1000)+data[40:]):
            with self.subTest(size=len(bad)),self.assertRaises(ValueError): animation_frames(bad)
