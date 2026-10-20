import struct
import unittest
from decode_psx_regions import decode


def fixture():
    blob = bytearray(128)
    struct.pack_into('<III', blob, 0, 0x00020006, 100, 1)
    struct.pack_into('<Iiii', blob, 12, 0, -4096, 8192, 12288)
    struct.pack_into('<II', blob, 48, 1, 56)
    struct.pack_into('<HHHH', blob, 56, 8, 1, 0, 0)
    struct.pack_into('<hhhH', blob, 84, 9, -18, 27, 0)
    struct.pack_into('<II', blob, 100, 0xffffffff, 123)
    return blob


class RegionTests(unittest.TestCase):
    def test_exact_origins_and_vertex_bounds(self):
        result = decode(fixture())
        self.assertEqual(result['objects'][0]['origin_world'], [-1, 2, 3])
        self.assertEqual(result['models'][0]['vertex_bounds_raw'], [[9,-18,27]]*2)

    def test_fail_closed_on_corrupt_counts_pointers_and_packets(self):
        for at, value in [(0,0), (8,65537), (48,0), (52,12), (100,1)]:
            blob = fixture(); struct.pack_into('<I', blob, at, value)
            with self.subTest(at=at), self.assertRaises(ValueError):
                decode(blob)
        blob = fixture(); struct.pack_into('<II', blob, 100, 1, 0xffffffff)
        with self.assertRaises(ValueError):
            decode(blob)
        with self.assertRaises(ValueError):
            decode(fixture()[:90])
