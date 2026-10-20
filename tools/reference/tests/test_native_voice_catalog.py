import importlib.util
import json
import pathlib
import struct
import tempfile
import unittest


def module(name, filename):
    spec = importlib.util.spec_from_file_location(name, pathlib.Path(__file__).parents[1] / filename)
    result = importlib.util.module_from_spec(spec); spec.loader.exec_module(result)
    return result


native = module("native_catalog_tested", "extract_native_voice_catalog.py")
batch = module("native_batch_tested", "batch_process_audio.py")


class NativeCatalogTests(unittest.TestCase):
    def image(self):
        data = bytearray(1024); data[:2] = b"MZ"; struct.pack_into("<I", data, 60, 128)
        data[128:132] = b"PE\0\0"; struct.pack_into("<HH", data, 132, 0x14c, 1)
        struct.pack_into("<H", data, 148, 224); struct.pack_into("<H", data, 152, 0x10b)
        struct.pack_into("<I", data, 180, 0x400000)
        struct.pack_into("<IIII", data, 384, 256, 0x1000, 256, 512)
        data[544:556] = b"TEST_00.bik\0"
        return data

    def test_file_backed_addresses_and_voice_strings_are_bounded(self):
        image = native.PeImage(self.image())
        self.assertEqual(image.filename(0x401020), "TEST_00.bik")
        with self.assertRaises(ValueError): image.offset(0x2000)
        with self.assertRaises(ValueError): image.offset(0x10ff, 2)

    def test_invalid_pe_and_uninspected_build_fail_closed(self):
        with self.assertRaises(ValueError): native.PeImage(b"bad")
        with self.assertRaises(ValueError): native.extract(self.image())
        data = self.image(); struct.pack_into("<I", data, 396, 0xffffff00)
        with self.assertRaises(ValueError): native.PeImage(data)

    def test_bank_track_join_uses_table_identity_instead_of_filename_suffix(self):
        root = pathlib.Path(__file__).parents[3] / "staging" / "validation"; root.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=root) as directory:
            catalog = {"schema_version": 1, "format": "neversoft_pc_voice_bank_v1", "stride": 16, "bank_count": 79,
                       "source_sha256": "synthetic-test-fixture", "entries": [
                           {"native_id": i, "bank": i // 16, "track": i % 16, "file_name": f"fixture_{i}.bik"} for i in range(1264)]}
            catalog["entries"][26]["file_name"] = "L1A1_20.bik"
            path = pathlib.Path(directory) / "catalog.json"; path.write_text(json.dumps(catalog))
            event = {"file": "fixture.trg.json", "scene_or_mission_id": "L1A1", "node_id": 45,
                     "command": {"name": "PlayXA", "args": [1, 10]}}
            references, report = batch.read_native_references(path, [event])
            self.assertEqual(report["resolved_commands"], 1)
            entry = {"source_path": "Voice/L1A1_20.bik"}
            batch.annotate(entry, {"transcript": "Synthetic words", "segments": [{}]}, [event], {}, references)
            self.assertEqual(entry["trigger_condition"], "TRG:L1A1:node45:PlayXA(1,10)")
            self.assertEqual(entry["trigger_condition_status"], "native_indexed_event")
            event["command"]["args"] = [79, 0]
            with self.assertRaises(ValueError): batch.read_native_references(path, [event])


if __name__ == "__main__":
    unittest.main()
