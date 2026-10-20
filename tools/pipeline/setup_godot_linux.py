"""Install the official hash-pinned Linux editor for headless CI, never game data."""
import hashlib
from pathlib import Path
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[2]
VERSION = '4.7.2-stable'
ARCHIVE = f'Godot_v{VERSION}_linux.x86_64.zip'
BINARY = ARCHIVE[:-4]
SHA256 = 'cadd3204e728a35d3f13adb7fd0d7902636b79f6b95c40c265eb73b6c35329e4'
URL = f'https://github.com/godotengine/godot/releases/download/{VERSION}/{ARCHIVE}'
MAX_ARCHIVE = 256 * 1024 * 1024
MAX_BINARY = 512 * 1024 * 1024


def install(destination):
    destination = Path(destination)
    destination.mkdir(parents=True, exist_ok=True)
    archive = destination / ARCHIVE
    if not archive.exists():
        temporary = archive.with_suffix('.download')
        try:
            with urllib.request.urlopen(URL, timeout=60) as source, temporary.open('wb') as target:
                total = 0
                while chunk := source.read(1024 * 1024):
                    total += len(chunk)
                    if total > MAX_ARCHIVE:
                        raise ValueError('Godot download exceeds bound')
                    target.write(chunk)
            temporary.replace(archive)
        finally:
            temporary.unlink(missing_ok=True)
    if archive.stat().st_size > MAX_ARCHIVE:
        raise ValueError('Godot archive exceeds bound')
    with archive.open('rb') as source:
        digest = hashlib.file_digest(source, 'sha256').hexdigest()
    if digest != SHA256:
        raise ValueError('Godot archive SHA-256 mismatch; refusing extraction')
    with zipfile.ZipFile(archive) as bundle:
        members = bundle.infolist()
        if len(members) != 1 or members[0].filename != BINARY or not 0 < members[0].file_size <= MAX_BINARY:
            raise ValueError('Unexpected Godot archive contents')
        target = destination / BINARY
        temporary = target.with_suffix('.tmp')
        with bundle.open(members[0]) as source, temporary.open('wb') as output:
            while chunk := source.read(1024 * 1024):
                output.write(chunk)
        temporary.chmod(0o755)
        temporary.replace(target)
    (destination / '_sc_').touch()
    return target


if __name__ == '__main__':
    print(install(ROOT / 'tools/third_party/godot-4.7.2-linux'))
