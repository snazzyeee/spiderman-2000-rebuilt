import importlib.util
import pathlib
import tempfile
import unittest

SPEC = importlib.util.spec_from_file_location("batch_audio", pathlib.Path(__file__).parents[1] / "batch_process_audio.py")
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class BatchAudioTests(unittest.TestCase):
    def test_requested_character_prefixes_are_case_insensitive(self):
        for prefix, expected in {"Na": "Stan Lee", "Sp": "Spider-Man", "SM": "Spider-Man", "Th": "Thug", "Co": "Police"}.items():
            entry = {"source_path": f"Voice/{prefix}_01.bik"}
            MODULE.annotate(entry, {"transcript": "Synthetic test", "segments": [{}]}, [])
            self.assertEqual(entry["speaker"], expected)
            self.assertEqual(entry["transcript_status"], "asr_draft")

    def test_level_join_retains_numeric_candidates_without_inventing_conditions(self):
        event = {"scene_or_mission_id": "L1A1", "node_id": 9, "command": {"name": "PlayXA", "args": [1, 3]}}
        entry = {"source_path": "Voice/L1A1_03.bik", "speaker": "Unknown"}
        MODULE.annotate(entry, {"transcript": "", "segments": []}, [event])
        self.assertEqual(entry["level_id"], "L1A1")
        self.assertEqual(entry["level_trigger_candidates"], [event])
        self.assertIsNone(entry["trigger_condition"])
        self.assertEqual(entry["speaker"], "Unknown")
        self.assertEqual(entry["transcript_status"], "no_speech_detected")

    def test_asr_cannot_overwrite_verified_speaker_or_subtitle(self):
        entry = {"source_path": "Voice/Sp_01.bik", "mapping_status": "verified", "speaker": "Verified person",
                 "subtitle_text": "Checked words", "mappings": [{"status": "verified", "playback_trigger": "ON_LEVEL_START"}]}
        MODULE.annotate(entry, {"transcript": "Draft words", "segments": [{}]}, [])
        self.assertEqual(entry["speaker"], "Verified person")
        self.assertEqual(entry["subtitle_text"], "Checked words")
        self.assertEqual(entry["trigger_condition"], "ON_LEVEL_START")

    def test_audio_paths_cannot_escape_input_root(self):
        private_tests = pathlib.Path(__file__).parents[3] / "TestResults"
        private_tests.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=private_tests) as directory:
            root = pathlib.Path(directory)
            with self.assertRaises(ValueError):
                MODULE.safe_audio(root, "../escape.wav")


if __name__ == "__main__":
    unittest.main()
