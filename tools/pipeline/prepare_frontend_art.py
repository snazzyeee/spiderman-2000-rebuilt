"""Bounded CRC-checked private frontend art conversion using the pinned decoder."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import zlib
from convert_characters import extract_member, inspect
from convert_fonts import dependencies
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'reference'))
from pinned_tools import CONVERTER_SHA256
ROOT=Path(__file__).resolve().parents[2]
MAX_BYTES=4*1024*1024

def checked_extract(stream,entry,root):
 if not 0<entry['size_bytes']<=MAX_BYTES or not 0<entry['stored_bytes']<=MAX_BYTES:raise ValueError('Art member exceeds read budget')
 return extract_member(stream,entry,root)

def main():
 dependencies()
 from PIL import Image
 Image.MAX_IMAGE_PIXELS=16*1024*1024
 archive=ROOT/'staging/raw_extracted/PC/app/data.pkr';source=ROOT/'staging/reference/frontend-art';output=ROOT/'assets/ui/frontend'
 converter=ROOT/'tools/third_party/neversoft-multitool-1.3.4/NeversoftMultitool-Windows-CLI-x64/NeversoftMultitool.exe'
 if hashlib.sha256(converter.read_bytes()).hexdigest()!=CONVERTER_SHA256:raise ValueError('Decoder digest mismatch')
 entries=[e for e in inspect(archive)['entries'] if re.fullmatch(r'data/(logo\.rle|Title\.bmp|LegalPC\.bmp|L[1-8]Cov\.bmp)',e['path'],re.I)]
 if {Path(e['path']).name.lower() for e in entries} != {'logo.rle','title.bmp','legalpc.bmp'} | {f'l{i}cov.bmp' for i in (1,2,4,5,7,8)}:raise ValueError('Frontend member inventory changed')
 manifest=[]
 with archive.open('rb') as stream:
  for e in entries:
   path=checked_extract(stream,e,source)
   manifest.append(dict(member=e['path'],crc32=e['crc32'],source_sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
 output.mkdir(parents=True,exist_ok=True)
 log=ROOT/'staging/validation/frontend-art.log';log.parent.mkdir(parents=True,exist_ok=True)
 with log.open('w') as handle:subprocess.run([str(converter),'rle',str(source/'data'),'-o',str(output)],stdout=handle,stderr=subprocess.STDOUT,timeout=30,check=True)
 for row in manifest:
  matches=list(output.rglob(Path(row['member']).stem+'.png'))
  if len(matches)!=1:raise ValueError('Missing/ambiguous decoded output: '+row['member'])
  png=matches[0]
  with Image.open(png) as image:
   if max(image.size)>4096 or image.width*image.height>16*1024*1024:raise ValueError('Decoded image exceeds pixel budget')
   image.verify()
  row.update(output=png.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(png.read_bytes()).hexdigest())
 with Image.open(output/'Title.png') as title:
  title.crop((110,18,404,86)).save(output/'title_mark.png')
 (output/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 print('CRC-checked/decoded',len(manifest),'private frontend images')
if __name__=='__main__':main()
