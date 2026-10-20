import sys
from pathlib import Path
import unittest

sys.path.insert(0, str(Path(__file__).parents[1]))
from scan_frontend_sfx import call_contexts, frontend_ranges, scan


class FrontendSfxTests(unittest.TestCase):
    def test_fingerprint_rejects_synthetic_input_before_decode(self):
        with self.assertRaisesRegex(ValueError, 'fingerprint'):
            scan(b'synthetic executable fixture', {})

    def test_named_ranges_are_bounded_by_next_function(self):
        rows = [dict(name='Shell_Synthetic', address=100),
                dict(name='unrelated', address=120), dict(name='end', address=130)]
        self.assertEqual(frontend_ranges(rows), [('Shell_Synthetic', 100, 120)])
        for invalid in ([], [rows[0], rows[0]],
                        [rows[0], dict(name='end', address=100000)]):
            with self.assertRaises(ValueError):
                frontend_ranges(invalid)

    def test_direct_calls_only_and_context_budget(self):
        instructions = [(i, 'nop', '') for i in range(30)]
        instructions += [(30, 'call', '0x1234'), (31, 'call', 'eax'),
                         (32, 'jmp', '0x1234'), (33, 'call', '0x5678')]
        rows = call_contexts(instructions, {'0x1234'})
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]['call'], '0x1e')
        self.assertEqual(len(rows[0]['context']), 14)
        self.assertEqual(rows[0]['context'][0][0], 20)
