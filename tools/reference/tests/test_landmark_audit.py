import unittest
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parents[1]))
from analyze_landmarks import attribute_candidates
from prepare_landmark_geometry import collect


class LandmarkAuditTests(unittest.TestCase):
    def test_bank_identity_uses_exact_decoded_origin_not_mesh_name(self):
        comparison={'comparisons':[dict(model=2,actual_translation=[4,-8,-12],translation_error=[1,2,3])]}
        banks={'synthetic':{'objects':[dict(index=7,model=9,origin_world=[9,18,27])]}}
        result=attribute_candidates(comparison,banks)
        self.assertEqual(result[0]['matches'],[dict(bank='synthetic',object=7,model=9)])
        comparison['comparisons'][0]['actual_translation'][0]+=1
        self.assertEqual(attribute_candidates(comparison,banks)[0]['matches'],[])

    def test_bank_collection_rejects_unresolved_or_unrelated_arguments(self):
        graph={'nodes':[{'index':3,'commands':[
            dict(name='SpoolEnv',args=['SYNTH_G']),dict(name='SpoolIn',args=['SYNTH_O']),
            dict(name='SpoolEnv',args=['\x86']),dict(name='SpoolIn',args=['actor']),
            dict(name='Unknown',args=['OTHER_G'])]}]}
        refs=collect({'SYNTH':graph})
        self.assertEqual(set(refs),{'SYNTH_G','SYNTH_O'})
        self.assertEqual(refs['SYNTH_G'][0]['node'],3)
