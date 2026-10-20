import importlib.util
from pathlib import Path
import unittest

spec=importlib.util.spec_from_file_location('native_registry',Path(__file__).parents[1]/'build_dialogue_manifest.py')
builder=importlib.util.module_from_spec(spec);spec.loader.exec_module(builder)


def catalog():
    rows=[dict(native_id=i,bank=i//16,track=i%16,file_name=f'synthetic_{i}.bik') for i in range(79*16)]
    rows[530]['file_name']=rows[531]['file_name']='example_voice.bik'
    return dict(source_sha256=builder.INSPECTED_SHA256,bank_count=79,stride=16,entries=rows)


class NativeBindingTests(unittest.TestCase):
    def test_exact_source_hash_join_keeps_all_catalog_identities(self):
        clip=dict(source_path='Voice/example_voice.bik',wav_sha256='a'*64)
        registry={'example':dict(clip,category='Dialogue')}
        result=builder.attach_native_identities(registry,[clip],catalog())
        self.assertEqual(result['xa_entries'],1)
        self.assertEqual(registry['example']['native_flat_id'],530)
        self.assertEqual([i['flat_id'] for i in registry['example']['native_xa_identities']],[530,531])
        registry['example']['wav_sha256']='b'*64
        builder.attach_native_identities(registry,[clip],catalog())
        self.assertNotIn('native_flat_id',registry['example'])

    def test_sfx_joins_bank_index_and_hash_without_filename_guess(self):
        clip=dict(source_path='data/SYNTH.kat/005.wav',wav_sha256='c'*64)
        registry={'example':dict(clip,category='SFX')}
        source=dict(format='bounded_pc_sfx_kat_identity_v1',records=[dict(bank='synth',source_suffix='SYNTH.kat/005.wav',native_sfx_id=0x800b,payload_present=True)])
        builder.attach_native_identities(registry,[clip],sfx_catalog=source)
        self.assertEqual(registry['example']['native_sfx_bank'],'synth')
        self.assertEqual(registry['example']['native_sfx_id'],0x800b)

    def test_bad_fingerprint_or_flat_identity_rejected(self):
        bad=catalog();bad['source_sha256']='0'*64
        with self.assertRaises(ValueError):builder.attach_native_identities({},[],bad)
        bad=catalog();bad['entries'][1]['native_id']=9999
        with self.assertRaises(ValueError):builder.attach_native_identities({},[],bad)
