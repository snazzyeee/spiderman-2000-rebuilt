"""Read the inspected PC build's voice-ID catalog without executing the binary."""
from __future__ import annotations
import argparse
import hashlib
from pathlib import Path
import re
import struct

INSPECTED_SHA256 = "e4e221b168ccf52130c8c56a1535289afacc1906fb55d54545e33d50e43eec86"


class PeImage:
    def __init__(self, data: bytes):
        self.data = data
        if len(data) < 64 or data[:2] != b"MZ":
            raise ValueError("Not a PE image")
        pe = self.u32(60)
        if pe + 24 > len(data) or data[pe:pe + 4] != b"PE\0\0":
            raise ValueError("Invalid PE signature")
        machine, count = struct.unpack_from("<HH", data, pe + 4)
        optional_size = struct.unpack_from("<H", data, pe + 20)[0]
        if machine != 0x14c or optional_size < 32 or not 0 < count <= 96:
            raise ValueError("Only bounded x86 PE32 metadata is supported")
        optional = pe + 24
        if optional + optional_size + count * 40 > len(data) or struct.unpack_from("<H", data, optional)[0] != 0x10b:
            raise ValueError("Invalid PE32 optional header")
        self.base = self.u32(optional + 28)
        self.sections = []
        for index in range(count):
            offset = optional + optional_size + index * 40
            _, rva, size, raw = struct.unpack_from("<IIII", data, offset + 8)
            if raw + size > len(data):
                raise ValueError("PE section escapes input file")
            self.sections.append((rva, size, raw))

    def u32(self, offset: int) -> int:
        if offset < 0 or offset + 4 > len(self.data):
            raise ValueError("PE integer escapes input file")
        return struct.unpack_from("<I", self.data, offset)[0]

    def offset(self, rva: int, length: int = 1) -> int:
        for start, size, raw in self.sections:
            if start <= rva and rva + length <= start + size:
                return raw + rva - start
        raise ValueError("PE address is outside file-backed sections")

    def filename(self, address: int) -> str:
        start = self.offset(address - self.base)
        end = self.data.find(b"\0", start, min(start + 128, len(self.data)))
        if end < 0:
            raise ValueError("Unterminated voice filename")
        self.offset(address - self.base, end - start + 1)
        name = self.data[start:end].decode("ascii")
        if not re.fullmatch(r"[A-Za-z0-9_]+\.bik", name, re.I):
            raise ValueError("Unexpected voice table member")
        return name


def extract(data: bytes) -> dict:
    fingerprint = hashlib.sha256(data).hexdigest()
    if fingerprint != INSPECTED_SHA256:
        raise ValueError("Uninspected executable fingerprint; refusing to assume this build's native audio layout")
    image = PeImage(data)
    start = image.offset(0x163fd8, 79 * 16 * 4)
    entries = [{"native_id": index, "bank": index // 16, "track": index % 16,
                "file_name": image.filename(image.u32(start + index * 4))} for index in range(79 * 16)]
    return {"schema_version": 1, "format": "neversoft_pc_voice_bank_v1", "source_sha256": fingerprint,
            "image_base": image.base, "table_rva": 0x163fd8, "bank_count": 79, "stride": 16,
            "index_formula": "bank * 16 + track",
            "evidence": {"bank_track_entry_rva": 0x79ee0, "index_calculation_rva": 0x7a0a7,
                         "pc_playback_wrapper_rva": 0x116050, "filename_lookup_rva": 0x104650,
                         "method": "Read-only static argument/data-flow inspection; executable never run"},
            "entries": entries}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("executable", type=Path, nargs="?", default=Path("LocalReference/PC/app/SpideyPC.exe"))
    parser.add_argument("--output", type=Path, default=Path("LocalReference/Dialogue/native_voice_catalog.json"))
    args = parser.parse_args()
    from batch_process_audio import write_json
    document = extract(args.executable.read_bytes())
    write_json(args.output, document)
    print(f"Read-only voice catalog: {len(document['entries'])} slots, "
          f"{len(set(entry['file_name'].lower() for entry in document['entries']))} unique filenames")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
