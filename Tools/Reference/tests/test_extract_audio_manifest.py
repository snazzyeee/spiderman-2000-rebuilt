import importlib.util
import json
from pathlib import Path
import shutil
import struct
import tempfile
import unittest
import wave
import zlib

SCRIPT = Path(__file__).resolve().parents[1] / "extract_audio_manifest.py"
spec = importlib.util.spec_from_file_location("extract_audio_manifest", SCRIPT)
audio = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audio)


class ExtractionTests(unittest.TestCase):
    def setUp(self):
        test_output = SCRIPT.parents[2] / "TestResults" / "audio-unit"
        test_output.mkdir(parents=True, exist_ok=True)
        self.temporary = tempfile.TemporaryDirectory(dir=test_output)
        self.root = Path(self.temporary.name)

    def tearDown(self):
        self.temporary.cleanup()

    def archive(self, payload=b"RIFF", bad_crc=False):
        compressed = zlib.compress(payload)
        table = 8 + len(compressed)
        path = self.root / "sample.pkr"
        path.write_bytes(struct.pack("<4sI", b"PKR3", table) + compressed
                         + struct.pack("<III", 0, 1, 1)
                         + struct.pack("<32sII", b"data/audio", 0, 1)
                         + struct.pack("<32sIIIII", b"sample.wav", zlib.crc32(payload) ^ int(bad_crc),
                                       2, 8, len(payload), len(compressed)))
        return path

    def test_compressed_archive_crc_and_payload(self):
        records = audio.unpack_archive(self.archive(), self.root / "out")
        self.assertEqual(records[0][0].read_bytes(), b"RIFF")

    def test_corrupt_archive_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "CRC"):
            audio.unpack_archive(self.archive(bad_crc=True), self.root / "out")

    def test_invalid_paths_are_rejected(self):
        for value in ("../escape.wav", "C:/escape.wav", "/root.wav", "data/NUL.wav", "data/name. "):
            with self.subTest(value=value), self.assertRaises(ValueError):
                audio.safe_member(value)

    def test_truncated_table_is_rejected(self):
        archive = self.archive()
        archive.write_bytes(archive.read_bytes()[:-1])
        with self.assertRaises(ValueError):
            audio.pkr_entries(archive)

    def test_numeric_trigger_is_not_fabricated_as_dialogue(self):
        clip = dict(clip_id="voice.l1a1_00", source_path="data/Voice/L1A1_00.bik", **audio.describe("data/Voice/L1A1_00.bik"))
        evidence = {"trigger_commands": [{"command": {"name": "PlayXA", "args": [0, 2]}}]}
        audio.apply_mappings([clip], evidence, {})
        self.assertEqual(clip["scene_or_mission_id"], "L1A1")
        self.assertEqual(clip["subtitle_text"], "")
        self.assertEqual(clip["speaker"], "Unknown")
        self.assertEqual(clip["trigger_references"], [])

    def test_override_requires_evidence_and_preserves_authored_status(self):
        clip = dict(clip_id="test", source_path="sample.wav", **audio.describe("sample.wav"))
        with self.assertRaises(ValueError):
            audio.apply_mappings([clip], {"trigger_commands": []}, {"clips": [{"clip_id": "test"}]})
        mapping = dict(clip_id="test", status="authored", evidence="test binding", subtitle_text="Test line")
        audio.apply_mappings([clip], {"trigger_commands": []}, {"clips": [mapping]})
        self.assertEqual(clip["subtitle_status"], "authored")

    def test_bounded_embedded_stream_carving(self):
        path = self.root / "model.psx"
        # A header advertising bytes beyond the model must not become an audio file.
        path.write_bytes(b"prefixVAGp" + b"\0" * 8 + struct.pack(">II", 1600, 22050) + b"\0" * 60)
        self.assertEqual(audio.carve_psx(path, self.root / "carved"), [])

    @unittest.skipUnless(shutil.which("ffmpeg") and shutil.which("ffprobe"), "FFmpeg required")
    def test_decode_pcm_and_detect_damaged_cache(self):
        source = self.root / "input.wav"
        with wave.open(str(source), "wb") as target:
            target.setparams((1, 1, 22050, 2205, "NONE", "not compressed"))
            target.writeframes(bytes([128, 150, 106]) * 735)
        output = self.root / "output"
        (output / "Wav").mkdir(parents=True)
        args = ((source, "raw/input.wav"), output, shutil.which("ffmpeg"), shutil.which("ffprobe"))
        first = audio.decode_one(*args, {})[0]
        decoded = output / first["file_name"]
        self.assertEqual(audio.pcm_info(decoded)["sample_rate"], 22050)
        decoded.write_bytes(b"corrupt cache")
        second = audio.decode_one(*args, {first["clip_id"]: first})[0]
        self.assertEqual(second["wav_sha256"], first["wav_sha256"])


if __name__ == "__main__":
    unittest.main()
