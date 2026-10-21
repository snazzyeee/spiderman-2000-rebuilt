import copy
import importlib.util
import json
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('layout',ROOT/'tools/design/layout.py')
layout=importlib.util.module_from_spec(spec);spec.loader.exec_module(layout)

class LayoutTests(unittest.TestCase):
 def setUp(self):self.doc=json.loads((ROOT/'src/ui/layout/screens.json').read_text())
 def test_all_screens_both_styles_and_geometry(self):
  layout.validate(self.doc)
  self.assertTrue({'main_menu','pause','loading','hud','comic_intro','death_restart'} <= {s['id'] for s in self.doc['screens']})
 def test_geometry_and_focus_fail_closed(self):
  for mutation in ('escape','overlap','focus','provenance'):
   d=copy.deepcopy(self.doc);p=d['screens'][2]['styles']['enhanced']
   if mutation=='escape':p['elements'][0]['rect'][0]=-1
   if mutation=='overlap':p['elements'].append(dict(p['elements'][-1],id='duplicate_area'))
   if mutation=='focus':p['focus_order']=['missing']
   if mutation=='provenance':p['elements'][0]['evidence']=[]
   with self.subTest(mutation=mutation),self.assertRaises(ValueError):layout.validate(d)
 def test_untrusted_schema_and_rects(self):
  self.assertFalse(layout.audit_proposal({})['valid'])
  d=dict(schema_version=1,screens=[dict(id='synthetic',elements=[dict(id='box',type='text',evidence=['synthetic'],confidence='likely',classic_rect=[0,0,2,1],enhanced_rect=[0,0,float('nan'),1])])])
  self.assertFalse(layout.audit_proposal(d)['valid'])
