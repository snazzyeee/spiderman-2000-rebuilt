import io
from pathlib import PurePosixPath
import sys
import unittest
import zlib

sys.path.insert(0, str(__import__('pathlib').Path(__file__).parents[1]))
from decode_level_offsets import decode, read_member, MAX_BYTES


class LevelOffsetTests(unittest.TestCase):
    def test_signed_parameters_keep_loader_field_order(self):
        rows = decode(b'2\r\n7 -12 34\r\n18 32767 -32768\r\n')
        self.assertEqual(rows, [dict(key=7, field_at_4=34, field_at_6=-12),
                                dict(key=18, field_at_4=-32768, field_at_6=32767)])
        self.assertEqual(decode(b'0\n'), [])

    def test_malformed_counts_tokens_keys_and_ranges_fail_closed(self):
        for blob in (b'', b'1\n', b'0 1', b'-1', b'257', b'1 3 4', b'1 3 4 5 6',
                     b'1 1 32768 0', b'1 1 0 -32769', b'1 -2 0 0', b'1 2147483648 0 0',
                     b'2 4 0 0 4 1 2', b'1 +4 0 0', b'1 4 0x10 0', b'0\0',
                     b'1 99999999999999999999 0 0', b' ' * (MAX_BYTES + 1)):
            with self.subTest(blob=blob[:30]), self.assertRaises(ValueError):
                decode(blob)

    def test_crc_length_compression_and_read_budget(self):
        raw = b'1\n2 -4 5\n'
        for compressed in (False, True):
            blob = zlib.compress(raw) if compressed else raw
            entry = (PurePosixPath('data/offsets/synthetic_g.off'), zlib.crc32(raw),
                     compressed, 3, len(raw), len(blob))
            self.assertEqual(read_member(io.BytesIO(b'pad' + blob), entry), raw)
            for changed in (entry[:1] + (0,) + entry[2:],
                            entry[:4] + (len(raw) - 1,) + entry[5:],
                            entry[:4] + (MAX_BYTES + 1,) + entry[5:],
                            entry[:5] + (len(blob) + 1,)):
                with self.assertRaises(ValueError):
                    read_member(io.BytesIO(b'pad' + blob), changed)
        for payload in (zlib.compress(raw) + b'trailing', zlib.compress(raw)[:-1], b'invalid'):
            entry = (PurePosixPath('data/offsets/synthetic_g.off'), zlib.crc32(raw),
                     True, 0, len(raw), len(payload))
            with self.assertRaises(ValueError):
                read_member(io.BytesIO(payload), entry)
