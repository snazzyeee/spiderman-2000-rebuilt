import struct
import sys
from pathlib import Path
import unittest

sys.path.insert(0, str(Path(__file__).parents[1]))
from decode_fonts import decode_font, decode_bmp, normal_charmap, trace_mask


def font_blob():
    return (struct.pack('<I8i', 2, 1, 1, 2, 3, 1, 2, 1, 4)
            + struct.pack('<16H', 0, 0x801f, *([0xffff] * 14))
            + b'\x10\x21\0\0' + b'\x11\x11\x22\x22')


def bmp(top_down=False):
    width, height, offset = 3, 2, 62
    rows = [b'\x10\x10\0\0', b'\x01\x00\0\0']
    data = b''.join(rows if top_down else rows[::-1])
    return (b'BM' + struct.pack('<IHHI', offset + len(data), 0, 0, offset)
            + struct.pack('<IiiHHIIiiII', 40, width, -height if top_down else height, 1, 4, 0,
                          len(data), 0, 0, 2, 0) + b'\0\0\0\0\0\0\xff\0' + data)


class BitmapFontTests(unittest.TestCase):
    def test_glyph_padding_nibbles_palette_baseline_and_metrics(self):
        result = decode_font(font_blob())
        first, second = result['glyphs']
        self.assertEqual((first['offset'], second['offset']), (68, 72))
        self.assertEqual(first['rgba'][:8], b'\0\0\0\0\xff\0\0\xff')
        self.assertEqual(first['baseline'], 2)  # baseline may exceed height
        self.assertEqual((result['ascent'], result['descent'], result['spacing']), (2, 1, 2))

    def test_font_bounds_and_exact_accounting(self):
        valid = font_blob()
        cases = [b'', valid[:-1], valid + b'x', struct.pack('<I', 257) + valid[4:]]
        for offset, value in [(4, -1), (4, 65), (8, 257), (12, -1), (16, 5)]:
            bad = bytearray(valid)
            struct.pack_into('<i', bad, offset, value)
            cases.append(bad)
        for blob in cases:
            with self.subTest(size=len(blob)), self.assertRaises(ValueError):
                decode_font(blob)

    def test_bmp_palette_nibble_order_padding_and_orientation(self):
        a, b = decode_bmp(bmp()), decode_bmp(bmp(True))
        self.assertEqual(a, b)
        self.assertEqual(a['rgba'][:12], b'\xff\0\0\xff\0\0\0\0\xff\0\0\xff')

    def test_bmp_encoding_palette_and_payload_fail_closed(self):
        valid = bmp()
        cases = [b'', valid[:-1], valid + b'x']
        for offset, value, form in [(18, 0x7fffffff, '<i'), (30, 1, '<I'), (10, 54, '<I'),
                                    (28, 24, '<H'), (46, 17, '<I')]:
            bad = bytearray(valid)
            struct.pack_into(form, bad, offset, value)
            cases.append(bad)
        bad = bytearray(valid)
        bad[62] = 0xf0
        cases.append(bad)
        for blob in cases:
            with self.subTest(size=len(blob)), self.assertRaises(ValueError):
                decode_bmp(blob)

    def test_normal_character_map_only_exposes_existing_glyphs(self):
        limited = normal_charmap(2)
        self.assertEqual(limited, {65: 0, 66: 1, 97: 0, 98: 1})
        full = normal_charmap(44)
        self.assertEqual([full[ord(c)] for c in 'AZ09_?!:.-\'+'], [0, 25, 26, 35, *range(36, 44)])
        self.assertNotIn(ord(','), full)

    def test_trace_preserves_holes_diagonal_components_and_pixel_area(self):
        for mask, count in [([[1]], 1), ([[1, 0], [0, 1]], 2),
                            ([[1, 1, 1], [1, 0, 1], [1, 1, 1]], 2),
                            ([[0, 0], [0, 0]], 0)]:
            contours = trace_mask(mask)
            self.assertEqual(len(contours), count)
            area = sum(a[0] * b[1] - b[0] * a[1] for loop in contours
                       for a, b in zip(loop, loop[1:] + loop[:1]))
            self.assertEqual(area, 2 * sum(map(sum, mask)))
            self.assertEqual(contours, trace_mask(mask))
        with self.assertRaises(ValueError):
            trace_mask([[1], [1, 0]])
