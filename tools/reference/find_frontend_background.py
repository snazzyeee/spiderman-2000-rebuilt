"""Bounded, CRC-checked frontend resource search; never runs original code."""
import hashlib
import json
from pathlib import Path
import sys
import struct
import subprocess
import shutil
from pinned_tools import CONVERTER_SHA256
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT/'tools/pipeline'))
from convert_characters import extract_member
from pkr_index import inspect
from extract_native_voice_catalog import INSPECTED_SHA256

def animation_frames(data):
    """Bounded PSX tagged animation records (own description in frontend ledger)."""
    def word(offset):
        if offset < 0 or offset+4 > len(data): raise ValueError('Truncated PSX metadata')
        return struct.unpack_from('<I',data,offset)[0]
    if len(data)>4*1024*1024 or word(0)!=0x20004: raise ValueError('Unsupported PSX frame')
    offset=word(4)
    for _ in range(200):
        tag=word(offset)
        if tag==0xffffffff: return {}
        end=offset+8+word(offset+4)
        if end>len(data) or end<=offset+8: raise ValueError('Invalid packet bounds')
        if tag==0x45:
            count=word(offset+8);cursor=offset+12;result={}
            if count>=200: raise ValueError('Animation count exceeds budget')
            for _ in range(count):
                if cursor+12>end: raise ValueError('Truncated animation name')
                name=data[cursor:cursor+8].split(b'\0')[0].decode('ascii')
                frames=word(cursor+8);cursor+=12
                if frames>=1000 or cursor+frames*8>end or name in result: raise ValueError('Invalid frame count/name')
                result[name]=[struct.unpack_from('<II',data,cursor+i*8) for i in range(frames)]
                cursor+=frames*8
            if cursor!=end: raise ValueError('Animation packet trailing bytes')
            return result
        offset=end
    raise ValueError('Packet traversal budget exceeded')

def main():
    archive = ROOT/'staging/raw_extracted/PC/app/data.pkr'
    entries = inspect(archive)['entries']
    selected = [e for e in entries if any(token in e['path'].lower() for token in ('shell', 'front', 'menu'))]
    output = ROOT/'staging/reference/frontend-background'
    report = dict(inventory_count=len(entries), name_hits=[e['path'] for e in entries if 'menubg' in e['path'].lower()], members=[])
    total = 0
    with archive.open('rb') as stream:
        for entry in selected:
            if not 0 < entry['size_bytes'] <= 4*1024*1024 or not 0 < entry['stored_bytes'] <= 4*1024*1024:
                raise ValueError('Member exceeds static search budget')
            total += entry['size_bytes']
            if total > 32*1024*1024: raise ValueError('Aggregate search budget exceeded')
            path = extract_member(stream, entry, output)
            data = path.read_bytes()
            report['members'].append(dict(path=entry['path'], bytes=len(data), crc32=entry['crc32'], sha256=hashlib.sha256(data).hexdigest(), menubg_offset=data.lower().find(b'menubg')))
    exe = ROOT/'staging/raw_extracted/PC/app/SpideyPC.exe'
    if exe.stat().st_size > 8*1024*1024: raise ValueError('Executable exceeds static budget')
    data = exe.read_bytes()
    if hashlib.sha256(data).hexdigest() != INSPECTED_SHA256: raise ValueError('Executable fingerprint mismatch')
    report['exe_sha256'] = INSPECTED_SHA256
    report['exe_menubg_offset'] = data.lower().find(b'menubg')
    report['status'] = 'Name search only; resource identity not established by a string hit'
    shell=(output/'data/shell.psx').read_bytes()
    frames=animation_frames(shell).get('menubg')
    if frames != [(0,88)]: raise ValueError('Inspected menubg frame identity changed')
    texture_offset=0x4fedc
    header=struct.unpack_from('<IIIIHH',shell,texture_offset)
    if header[1]!=256 or header[3:]!=(88,512,512): raise ValueError('Inspected texture-index join changed')
    converter=ROOT/'tools/third_party/neversoft-multitool-1.3.4/NeversoftMultitool-Windows-CLI-x64/NeversoftMultitool.exe'
    if hashlib.sha256(converter.read_bytes()).hexdigest()!=CONVERTER_SHA256: raise ValueError('Decoder hash mismatch')
    with (output/'decode.log').open('w') as log:
        subprocess.run([str(converter),'psx',str(output/'data'),'-o',str(output/'decoded'),'--subdirs','--no-dds'],stdout=log,stderr=subprocess.STDOUT,timeout=30,check=True)
    decoded=output/'decoded/shell/shell_0004FEDC.png'
    if not decoded.is_file() or decoded.stat().st_size>4*1024*1024: raise ValueError('Missing/bounded texture output')
    destination=ROOT/'assets/ui/frontend/menubg.png';destination.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(decoded,destination)
    report.update(status='Recovered: single menubg frame joins texture header index 88',frames=frames,texture_header_offset=texture_offset,decoded_sha256=hashlib.sha256(decoded.read_bytes()).hexdigest(),output=destination.relative_to(ROOT).as_posix())
    output.mkdir(parents=True, exist_ok=True)
    (output/'search.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(report, indent=2))

if __name__ == '__main__': main()
