from pathlib import Path
import sys
import unittest
sys.path.insert(0,str(Path(__file__).parents[1]))
from extract_title_logo import candidate, choose_mask

class LogoTests(unittest.TestCase):
    def test_blue_seed_neutral_outline_and_red_exclusion(self):
        row = [(240,30,20),(0,0,0),(70,120,170),(70,120,170),(230,230,230),(240,30,20)]
        self.assertEqual(candidate([row]), [[0,255,255,255,255,0]])

    def test_isolated_noise_and_bounds(self):
        self.assertEqual(candidate([[(70,120,170)]]), [[0]])
        with self.assertRaises(ValueError): candidate([[(0,0,0)]*513])

    def test_correction_mask_always_wins_and_wrong_size_fails(self):
        class Mask:
            size = (2,2)
            def convert(self, mode): return self
        mask = Mask()
        self.assertIs(choose_mask(mask, object(), (2,2))[0], mask)
        with self.assertRaises(ValueError): choose_mask(mask, object(), (3,2))
