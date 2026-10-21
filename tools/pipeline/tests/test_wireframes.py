import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
import sys
ROOT=Path(__file__).resolve().parents[3];sys.path.insert(0,str(ROOT/'tools/design'))
import render_wireframes as renderer
class WireframeTests(unittest.TestCase):
 def test_all_screens_styles_are_generated_well_formed_and_current(self):
  doc=json.loads((ROOT/'src/ui/layout/screens.json').read_text())
  with tempfile.TemporaryDirectory() as folder:
   renderer.generate(doc,Path(folder))
   for screen in doc['screens']:
    for style in ('classic','enhanced'):
     name=screen['id']+'_'+style+'.svg';source=(Path(folder)/name).read_text();ET.fromstring(source)
     self.assertEqual(source,(ROOT/'docs/design/wireframes'/name).read_text())
     self.assertNotIn('<image',source)
     if screen['id']=='main_menu':
      self.assertEqual(source.count('<circle'),5)
      self.assertEqual(source.count('marker-end='),4)
   self.assertEqual(len(list(Path(folder).glob('*.svg'))),len(doc['screens'])*2)
