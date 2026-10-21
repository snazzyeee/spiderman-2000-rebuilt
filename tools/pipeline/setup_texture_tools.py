"""Install the official SHA-pinned portable Vulkan upscaler for offline use."""
import hashlib
from pathlib import Path, PurePosixPath
import sys
import urllib.request
import zipfile

ROOT=Path(__file__).resolve().parents[2]
VERSION='20250915'
NAME=f'waifu2x-ncnn-vulkan-{VERSION}-windows'
SHA256='7425be94b94e4c8f37a1e433ac0e0100c43790e2c37418f4b65d8235adfbdc87'
URL=f'https://github.com/nihui/waifu2x-ncnn-vulkan/releases/download/{VERSION}/{NAME}.zip'
DESTINATION=ROOT/'tools/third_party'/NAME

def checked_members(archive):
    members=archive.infolist()
    if len(members)>1000 or sum(i.file_size for i in members)>256*1024*1024: raise ValueError('Tool extraction exceeds budget')
    for info in members:
        p=PurePosixPath(info.filename)
        if p.is_absolute() or '..' in p.parts or ':' in info.filename or '\\' in info.filename or (info.external_attr>>16)&0o170000==0o120000:
            raise ValueError('Unsafe tool archive member')
    return members

def verify_install():
    archive=DESTINATION/(NAME+'.zip')
    if archive.stat().st_size>64*1024*1024 or hashlib.sha256(archive.read_bytes()).hexdigest()!=SHA256: raise ValueError('Upscaler archive hash mismatch')
    with zipfile.ZipFile(archive) as package:
        for info in checked_members(package):
            if info.is_dir(): continue
            path=DESTINATION/info.filename
            if not path.is_file() or hashlib.sha256(path.read_bytes()).digest()!=hashlib.sha256(package.read(info)).digest():
                raise ValueError('Installed tool/model differs from pinned archive: '+info.filename)
    return DESTINATION/NAME

def main():
    if sys.platform!='win32' or sys.maxsize<=2**32: raise RuntimeError('This pin supports Windows x64 only')
    DESTINATION.mkdir(parents=True,exist_ok=True)
    path=DESTINATION/(NAME+'.zip')
    if not path.exists():
        with urllib.request.urlopen(URL,timeout=60) as response:
            blob=response.read(64*1024*1024+1)
        if len(blob)>64*1024*1024 or hashlib.sha256(blob).hexdigest()!=SHA256: raise ValueError('Tool download bounds/hash mismatch')
        path.write_bytes(blob)
    if hashlib.sha256(path.read_bytes()).hexdigest()!=SHA256: raise ValueError('Cached tool hash mismatch')
    with zipfile.ZipFile(path) as package:
        checked_members(package)
        package.extractall(DESTINATION)
    print(verify_install())

if __name__=='__main__': main()
