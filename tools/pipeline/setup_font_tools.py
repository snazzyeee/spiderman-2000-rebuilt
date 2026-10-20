"""Fetch pinned offline conversion/audit libraries into ignored third_party.

This setup targets the repository's Windows x64 Python 3.12 runtime. Conversion
uses no cloud service; network access is limited to these hash-checked wheels.
"""
import hashlib
import platform
import struct
import sys
from pathlib import Path, PurePosixPath
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[2]
PACKAGES = [
    ('pillow-11.3.0-cp312-cp312-win_amd64.whl',
     'https://files.pythonhosted.org/packages/8c/ce/e7dfc873bdd9828f3b6e5c2bbb74e47a98ec23cc5c74fc4e54462f0d9204/pillow-11.3.0-cp312-cp312-win_amd64.whl',
     'a6444696fce635783440b7f7a9fc24b3ad10a9ea3f0ab66c5905be1c19ccf17d'),
    ('fonttools-4.60.1-py3-none-any.whl',
     'https://files.pythonhosted.org/packages/c7/93/0dd45cd283c32dea1545151d8c3637b4b8c53cdb3a625aeb2885b184d74d/fonttools-4.60.1-py3-none-any.whl',
     '906306ac7afe2156fcf0042173d6ebbb05416af70f6b370967b47f8f00103bbb'),
    ('capstone-5.0.6-py3-none-win_amd64.whl',
     'https://files.pythonhosted.org/packages/da/72/ff7894c2fb5716d9a3ce9c27ba34b29d991a11d8442d2ef0fcdc5564ba7e/capstone-5.0.6-py3-none-win_amd64.whl',
     '761c3deae00b22ac697081cdae1383bb90659dd0d79387a09cf5bdbb22b17064'),
]


def require_supported_runtime():
    if (sys.platform != 'win32' or sys.version_info[:2] != (3, 12)
            or platform.machine().lower() not in ('amd64', 'x86_64')
            or struct.calcsize('P') != 8):
        raise RuntimeError('Font tools require Windows x64 with Python 3.12; use tools/third_party/audio-python/Scripts/python.exe')


def install():
    require_supported_runtime()
    destination = ROOT / 'tools/third_party/font-tools'
    libraries = destination / 'python'
    libraries.mkdir(parents=True, exist_ok=True)
    for name, url, expected in PACKAGES:
        wheel = destination / name
        if not wheel.exists():
            with urllib.request.urlopen(url, timeout=60) as response:
                blob = response.read(32 * 1024 * 1024 + 1)
            if len(blob) > 32 * 1024 * 1024:
                raise ValueError('Wheel exceeds download budget')
            if hashlib.sha256(blob).hexdigest() != expected:
                raise ValueError('Wheel digest mismatch: ' + name)
            wheel.write_bytes(blob)
        if wheel.stat().st_size > 32 * 1024 * 1024 or hashlib.sha256(wheel.read_bytes()).hexdigest() != expected:
            raise ValueError('Cached wheel digest mismatch: ' + name)
        with zipfile.ZipFile(wheel) as archive:
            members = archive.infolist()
            if len(members) > 20000 or sum(info.file_size for info in members) > 200 * 1024 * 1024:
                raise ValueError('Wheel extraction exceeds budget')
            for info in members:
                path = PurePosixPath(info.filename)
                if path.is_absolute() or '..' in path.parts or ':' in info.filename or '\\' in info.filename:
                    raise ValueError('Unsafe wheel member')
            archive.extractall(libraries)
    print(libraries)


if __name__ == '__main__':
    install()
