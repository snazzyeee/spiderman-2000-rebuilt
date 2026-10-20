import struct
import sys
from pathlib import Path
import unittest
sys.path.insert(0,str(Path(__file__).parents[1]))
from decode_sfx_ids import decode_sfx, decode_kat


def record(sample=5,alias=11):
    return struct.pack('<4I',(60<<24)|(sample<<8),0x10001000,alias,0)


class SfxIdentityTests(unittest.TestCase):
    def test_global_order_and_level_alias_are_distinct(self):
        blob=record()+struct.pack('<I',0xffffffff)
        self.assertEqual(decode_sfx(blob,True)[0]['native_sfx_id'],1)
        row=decode_sfx(blob)[0]
        self.assertEqual((row['native_sfx_id'],row['sample_index']),(0x800b,5))
        self.assertEqual(decode_sfx(blob+b'\0'*64),[row])

    def test_sfx_corruption_fails_closed(self):
        valid=record()+struct.pack('<I',0xffffffff)
        for blob in [b'',record(),valid+b'x',record()+record()+struct.pack('<I',0xffffffff),b'\1'+valid[1:],valid[:12]+b'\1\0\0\0'+valid[16:]]:
            with self.subTest(blob=blob[:8]),self.assertRaises(ValueError):decode_sfx(blob)

    def test_kat_payload_bounds_and_encoding(self):
        blob=struct.pack('<I',1)+struct.pack('<7I16s',1,48,4,16000,0,16,0,b'')+b'1234'
        self.assertEqual(decode_kat(blob)[0]['offset'],48)
        for bad in [blob[:-1],struct.pack('<I',148)+blob[4:],blob[:24]+struct.pack('<I',7)+blob[28:]]:
            with self.assertRaises(ValueError):decode_kat(bad)
