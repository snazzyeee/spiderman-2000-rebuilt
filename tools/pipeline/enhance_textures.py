"""Offline, bounded texture enhancement; originals and correction overrides are read-only."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
from convert_fonts import dependencies, ROOT
from setup_texture_tools import verify_install, VERSION, SHA256

def cache_key(source_hash, model_hash, settings):
    return hashlib.sha256(json.dumps(dict(source=source_hash,model=model_hash,settings=settings),sort_keys=True,separators=(',',':')).encode()).hexdigest()

def vram_bytes(width,height):
    total=0
    if min(width,height)<=0: raise ValueError('Invalid texture extent')
    while True:
        total+=width*height*4
        if width==height==1:return total
        width=max(1,width//2);height=max(1,height//2)

def scale_bmfont(text, factor, texture):
    fields={'size','lineHeight','base','scaleW','scaleH','x','y','width','height','xoffset','yoffset','xadvance','amount','spacing','outline'}
    if factor not in (2,4): raise ValueError('Unsupported font scale')
    def replace(match):
        key,value=match.groups()
        return key+'='+str(int(value)*factor) if key in fields else match[0]
    result=re.sub(r'\b([A-Za-z]+)=(-?\d+)',replace,text)
    return re.sub(r'file="[^"]+"',f'file="{texture}"',result)

def category(path):
    name=path.as_posix().lower()
    return 'illustration' if '/ui/' in name else 'general'

def enhance(source, *, tiling=False, scissor=False, tool=None):
    from PIL import Image
    tool=tool or verify_install()
    if not source.resolve().is_relative_to((ROOT/'assets').resolve()) or source.stat().st_size>32*1024*1024: raise ValueError('Source escapes bounded asset root')
    model='models-cunet' if category(source)=='illustration' else 'models-upconv_7_photo'
    model_dir=tool/model
    model_hash=hashlib.sha256(b''.join(hashlib.sha256(p.read_bytes()).digest() for p in sorted(model_dir.iterdir()) if p.is_file())).hexdigest()
    settings=dict(engine=VERSION,archive=SHA256,model=model,scale=2,noise=-1,tile=128,padding=16 if tiling else 0,scissor=scissor,alpha='separate inference',pipeline=1)
    digest=hashlib.sha256(source.read_bytes()).hexdigest();key=cache_key(digest,model_hash,settings)
    cache=ROOT/'staging/cache/textures'/key;cache.mkdir(parents=True,exist_ok=True)
    result=cache/'result.png';record=cache/'record.json'
    if result.exists() and record.exists():
        cached=json.loads(record.read_text())
        if hashlib.sha256(result.read_bytes()).hexdigest()==cached.get('sha256'):
            if 'pixel_sha256' not in cached:
                with Image.open(source) as image:
                    if max(image.size)>4096:raise ValueError('Pixel budget exceeded')
                    cached['pixel_sha256']=hashlib.sha256(image.convert('RGBA').tobytes()).hexdigest()
                record.write_text(json.dumps(cached,indent=2)+'\n')
            return result,cached
    with Image.open(source) as original:
        if max(original.size)>4096 or original.width*original.height>16*1024*1024:raise ValueError('Texture pixel budget exceeded')
        rgba=original.convert('RGBA')
    pixel_sha256=hashlib.sha256(rgba.tobytes()).hexdigest()
    width,height=rgba.size;pad=settings['padding']
    if pad:
        padded=Image.new('RGBA',(width+pad*2,height+pad*2))
        for y in range(-pad,height+pad):
            for x in range(-pad,width+pad):padded.putpixel((x+pad,y+pad),rgba.getpixel((x%width,y%height)))
        rgba=padded
    rgb=cache/'rgb.png';rgba.convert('RGB').save(rgb)
    alpha=cache/'alpha.png';rgba.getchannel('A').convert('RGB').save(alpha)
    log=cache/'inference.log'
    with log.open('w') as handle:
        for kind in ('rgb','alpha'):
            subprocess.run([str(tool/'waifu2x-ncnn-vulkan.exe'),'-i',str(cache/(kind+'.png')),'-o',str(cache/(kind+'-2x.png')),'-s','2','-n','-1','-t','128','-j','1:1:1','-g','0','-m',str(model_dir)],stdout=handle,stderr=subprocess.STDOUT,check=True,timeout=180)
    with Image.open(cache/'rgb-2x.png') as image,Image.open(cache/'alpha-2x.png') as mask:
        if image.size!=(rgba.width*2,rgba.height*2) or mask.size!=image.size:raise ValueError('Unexpected inference extent')
        image=image.convert('RGBA');a=mask.convert('L')
        if scissor:a=a.point(lambda value:255 if value>=128 else 0)
        image.putalpha(a)
        image.crop((pad*2,pad*2,(pad+width)*2,(pad+height)*2)).save(result)
    data=dict(source=source.relative_to(ROOT/'assets').as_posix(),source_sha256=digest,pixel_sha256=pixel_sha256,settings=settings,model_sha256=model_hash,cache_key=key,sha256=hashlib.sha256(result.read_bytes()).hexdigest(),width=width*2,height=height*2,rgba8_mip_bytes=vram_bytes(width*2,height*2))
    record.write_text(json.dumps(data,indent=2)+'\n')
    return result,data

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inventory-only',action='store_true')
    parser.add_argument('--all',action='store_true',help='Enhance all inventoried PNGs, not just the initial menu/rig/font batch')
    parser.add_argument('--tiling',action='append',default=[],help='Explicit assets-relative name requiring wrap padding (repeatable)')
    parser.add_argument('--scissor',action='append',default=[],help='Explicit assets-relative name requiring binary alpha (repeatable)')
    args=parser.parse_args();dependencies()
    from PIL import Image,ImageDraw
    Image.MAX_IMAGE_PIXELS=16*1024*1024
    out=ROOT/'staging/validation/textures';out.mkdir(parents=True,exist_ok=True)
    sources=[p for p in (ROOT/'assets').rglob('*.png') if not any(part in ('hd','hd_overrides','source_archive') for part in p.relative_to(ROOT/'assets').parts)]
    inventory=[]
    for path in sources:
        if path.stat().st_size>32*1024*1024:raise ValueError('Inventory read budget exceeded')
        with Image.open(path) as image:
            if max(image.size)>4096:raise ValueError('Inventory extent exceeds budget')
            inventory.append(dict(path=path.relative_to(ROOT/'assets').as_posix(),width=image.width,height=image.height,category=category(path),rgba8_mip_bytes=vram_bytes(image.width,image.height)))
    (out/'inventory.json').write_text(json.dumps(inventory,indent=2)+'\n')
    if args.inventory_only: print('Inventoried',len(inventory),'textures');return
    tool=verify_install();manifest=[];sheets={}
    excluded=json.loads((ROOT/'tools/pipeline/texture_overrides.json').read_text())['original_only']
    chosen=[p for p in sources if p.parent==ROOT/'assets/ui' or p.parent==ROOT/'assets/ui/frontend' or (p.parent==ROOT/'assets/characters' and p.stem.startswith('spidey_')) or (p.parent==ROOT/'assets/ui/fonts' and p.stem.endswith('_original'))]
    if args.all: chosen=sources
    for source in sorted(chosen):
        relative=source.relative_to(ROOT/'assets')
        if relative.as_posix() in excluded or source.stem in ('title_logo_mask','title_logo_candidate'):continue
        result,data=enhance(source,tool=tool,tiling=relative.as_posix() in args.tiling,scissor=relative.as_posix() in args.scissor)
        destination=ROOT/'assets/hd'/relative;destination.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(result,destination);manifest.append(data)
        # Every private import receives mips; the runtime selects anisotropic filtering.
        import_file=destination.with_suffix('.png.import')
        if import_file.exists():
            text=import_file.read_text();text=text.replace('mipmaps/generate=false','mipmaps/generate=true');import_file.write_text(text)
        if source.stem.endswith('_original') and source.parent.name=='fonts':
            original=source.with_suffix('.fnt')
            target=source.with_name(source.stem.replace('_original','_remastered'))
            shutil.copyfile(result,target.with_suffix('.png'))
            target.with_suffix('.fnt').write_text(scale_bmfont(original.read_text(),2,target.name+'.png'))
        key=category(source)
        if len(sheets.get(key,[]))<16:
            tile=Image.new('RGB',(512,180),'#eeeeee')
            for index,path in enumerate((source,result)):
                with Image.open(path) as im:
                    im=im.convert('RGBA');im.thumbnail((248,145));tile.paste(im,(index*256,25),im)
            ImageDraw.Draw(tile).text((8,5),relative.as_posix()[:65],fill='black')
            sheets.setdefault(key,[]).append(tile)
        print('Enhanced',relative.as_posix(),flush=True)
    (ROOT/'assets/hd_overrides').mkdir(exist_ok=True)
    total=sum(row['rgba8_mip_bytes'] for row in manifest)
    # Deliberately conservative estimate: all original textures + all generated
    # enhancements resident simultaneously. This is a texture estimate, not GPU telemetry.
    conservative=total+sum(row['rgba8_mip_bytes'] for row in inventory)
    report=dict(schema_version=1,tool_archive_sha256=SHA256,textures=manifest,enhanced_mip_bytes=total,conservative_texture_bytes=conservative,budget_bytes=4*1024**3,passed=conservative<4*1024**3,scope='RGBA8 mip texture estimate; excludes render targets, geometry, driver allocations')
    (ROOT/'assets/hd/manifest.json').write_text(json.dumps(report,indent=2)+'\n')
    (out/'vram.json').write_text(json.dumps({k:v for k,v in report.items() if k!='textures'},indent=2)+'\n')
    for name,tiles in sheets.items():
        sheet=Image.new('RGB',(512,len(tiles)*180),'white')
        for index,tile in enumerate(tiles):sheet.paste(tile,(0,index*180))
        sheet.save(out/(name+'-contact-sheet.png'))
    if not report['passed']:raise ValueError('Conservative texture estimate exceeds budget')

if __name__=='__main__': main()
