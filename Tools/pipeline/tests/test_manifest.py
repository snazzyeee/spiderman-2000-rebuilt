import importlib.util
from pathlib import Path
import unittest

PATH = Path(__file__).resolve().parents[1]/'build_dialogue_manifest.py'
spec = importlib.util.spec_from_file_location('godot_manifest', PATH)
pipeline = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pipeline)


class ManifestTests(unittest.TestCase):
    def test_speakers_line_breaks_stage_directions_and_user_level_names(self):
        rows = pipeline.parse_reference('''<div class="mw-parser-output"><h2>Get to the Bank!</h2>
            <p>(A camera pans.)</p><p>Guide (quietly): Cross the next roof.<br>Hero: On my way!</p>
            <p>(A helicopter arrives.)</p><p>That was close indeed.</p></div>''')
        self.assertEqual(len(rows), 3)
        self.assertEqual(rows[0]['speaker'], 'Guide')
        self.assertEqual(rows[1]['speaker'], 'Hero')
        self.assertEqual(rows[2]['speaker'], 'Hero')
        self.assertEqual(rows[0]['level'], 'L1A1')

    def test_numeric_order_does_not_override_text_similarity(self):
        rows = [dict(index=0, level='L1A1', section='A', speaker='Guide', text='Cross the next roof quickly.'),
                dict(index=1, level='L1A1', section='A', speaker='Hero', text='Stop the villain at the bank.')]
        matches = pipeline.rank_matches('Stop the villain at the bank', 'L1A1', rows, rows[0])
        self.assertEqual(matches[0][2]['index'], 1)
        self.assertEqual(matches[0][0], 1.)
        self.assertEqual(pipeline.rank_matches('yes', 'L1A1', rows), [])

    def test_cross_level_headings_excluded_but_unknown_cutscene_not_invented(self):
        rows = [dict(index=0, level='L1A2', section='Bank Approach', speaker='Hero', text='Cross the next roof quickly.')]
        self.assertEqual(pipeline.rank_matches('Cross the next roof quickly', 'L1A1', rows), [])
        parsed = pipeline.parse_reference('<h2>THE EXPOSITION</h2><p>Hero: Cross the next roof quickly.</p>')
        self.assertEqual(parsed[0]['level'], '')


if __name__ == '__main__':
    unittest.main()
