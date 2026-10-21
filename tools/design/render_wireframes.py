"""Generate annotated blueprints exclusively from tracked layout data."""
import argparse
from html import escape
import json
from pathlib import Path
from layout import ROOT, validate

TINT={'image':'#7c8c97','text':'#6651a5','menu_list':'#296b4f','button':'#296b4f','3d_actor':'#775020'}

def render(screen, style):
 p=screen['styles'][style];w,h=p['canvas'];m=48;footer=150
 rows=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{w+2*m}" height="{h+2*m+footer}" viewBox="0 0 {w+2*m} {h+2*m+footer}">',f'<title>{escape(screen["id"])} / {style}</title>',f'<rect width="100%" height="100%" fill="#f5f5f1"/>','<defs><marker id="arrow" markerWidth="6" markerHeight="6" refX="5" refY="3" orient="auto"><path d="M0,0 L6,3 L0,6" fill="#333"/></marker></defs>',f'<g transform="translate({m},{m})" font-family="Segoe UI,Inter,sans-serif" font-size="{12 if w>1000 else 10}">']
 for x in range(0,w+1,8):rows.append(f'<path d="M{x},0 V{h}" stroke="{"#c4c8c7" if x%64==0 else "#e4e6e3"}" stroke-width="0.5"/>')
 for y in range(0,h+1,8):rows.append(f'<path d="M0,{y} H{w}" stroke="{"#c4c8c7" if y%64==0 else "#e4e6e3"}" stroke-width="0.5"/>')
 for x in range(0,w+1,64):rows.append(f'<text x="{x}" y="-12" fill="#333">{x}</text>')
 for y in range(0,h+1,64):rows.append(f'<text x="-40" y="{y+4}" fill="#333">{y}</text>')
 rows.append(f'<rect width="{w}" height="{h}" fill="none" stroke="#20282e"/>')
 if style=='enhanced':rows.append(f'<rect x="{(w-h*4/3)/2:g}" width="{h*4/3:g}" height="{h}" fill="none" stroke="#444" stroke-dasharray="12 4"/>')
 for fraction,name in [(.90,'title safe 90%'),(.93,'action safe 93%')]:
  rows.append(f'<rect x="{w*(1-fraction)/2:g}" y="{h*(1-fraction)/2:g}" width="{w*fraction:g}" height="{h*fraction:g}" fill="none" stroke="#727b83" stroke-dasharray="5 5"/><text x="{w*(1-fraction)/2+4:g}" y="{h*(1-fraction)/2+13:g}" fill="#58626b">{name}</text>')
 for e in p['elements']:
  x,y,ew,eh=e['rect'];confidence=e['confidence'];stroke='#243139' if confidence in ('recovered','verified') else '#be690d' if confidence.startswith('Gemini') else '#2361a1';dash='' if confidence in ('verified','recovered') else '8 5' if confidence.startswith('Gemini') else '2 4'
  rows.append(f'<g><rect x="{x}" y="{y}" width="{ew}" height="{eh}" fill="{TINT.get(e["type"],"#5f686d")}" fill-opacity="0.06" stroke="{stroke}" stroke-width="1.5" stroke-dasharray="{dash}"/>')
  labels=[f'{e["id"]} | {e["type"]}',f'{x:g},{y:g} {ew:g}×{eh:g}',f'{e["font"]} {e["font_size"]} px | z {e["z"]}']
  for i,line in enumerate(labels):rows.append(f'<text x="{x+8}" y="{y+18+i*15}" fill="#253039">{escape(line)}</text>')
  rows.append('</g>')
 prev=None
 for number,id in enumerate(p['focus_order'],1):
  e=next(e for e in p['elements'] if e['id']==id);x,y,_,_=e['rect'];x+=18;y+=eh-16
  if prev:rows.append(f'<path d="M{prev[0]},{prev[1]} L{x},{y}" stroke="#333" marker-end="url(#arrow)"/>')
  rows.append(f'<circle cx="{x}" cy="{y}" r="12" fill="#fff" stroke="#333"/><text x="{x-4}" y="{y+4}" fill="#222">{number}</text>');prev=(x,y)
 rows.append(f'<text x="8" y="{h+25}" fill="#222">Back → {escape(p["back"])} | {escape(screen["id"])} / {style}</text>')
 rows.append(f'<text x="8" y="{h+50}" fill="#222">Evidence: solid dark = verified/recovered; dashed orange = Gemini proposal; dotted blue = authored</text>')
 rows.append(f'<text x="8" y="{h+73}" fill="#222">Types: image • text • menu list • button • 3D actor • HUD meter (outlined or 6% tint)</text>')
 rows.append(f'<text x="8" y="{h+96}" fill="#222">Blueprint only; no original art/logo embedded. Pixel dimensions refer to the canvas.</text></g></svg>')
 return '\n'.join(rows)+'\n'

def generate(document, destination):
 validate(document);destination.mkdir(parents=True,exist_ok=True);rows=[]
 for screen in document['screens']:
  files=[]
  for style in ('classic','enhanced'):
   name=screen['id']+'_'+style+'.svg';(destination/name).write_text(render(screen,style),encoding='utf-8');files.append(name)
  rows.append('<section><h2>'+escape(screen['id'])+'</h2><div>'+''.join(f'<figure><figcaption>{style}</figcaption><a href="{name}"><img src="{name}" alt="{escape(screen["id"])} {style} blueprint"/></a></figure>' for name,style in zip(files,('Classic','Enhanced')))+'</div></section>')
 (destination/'index.html').write_text('<!doctype html><html lang="en"><meta charset="utf-8"><title>Frontend blueprints</title><style>body{background:#eee;font:16px Segoe UI,sans-serif;margin:24px}section{margin-bottom:32px}section>div{display:grid;grid-template-columns:1fr 1fr;gap:24px}figure{margin:0}img{width:100%;background:white}figcaption{padding:8px}</style><h1>Frontend layout blueprints</h1><p>Generated from screens.json. Open an SVG to inspect at native size.</p>'+''.join(rows)+'</html>',encoding='utf-8')

def overlays(document, screenshots, destination):
 # Same data, raster box layer; never composite a different screen/style capture.
 import sys
 sys.path.insert(0,str(ROOT/'tools/third_party/font-tools/python'))
 from PIL import Image, ImageDraw
 destination.mkdir(parents=True,exist_ok=True);count=0
 for screen in document['screens']:
  for style,p in screen['styles'].items():
   for source in screenshots.glob(screen['id']+'_'+style+'_*.png'):
    base=Image.open(source).convert('RGBA');layer=Image.new('RGBA',base.size);draw=ImageDraw.Draw(layer);sx=base.width/p['canvas'][0];sy=base.height/p['canvas'][1]
    for e in p['elements']:
     x,y,w,h=e['rect'];draw.rectangle((x*sx,y*sy,(x+w)*sx,(y+h)*sy),outline=(0,90,180,102),width=2);draw.text((x*sx+4,y*sy+4),e['id'],fill=(0,60,130,102))
    Image.alpha_composite(base,layer).save(destination/source.name);count+=1
 return count

if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--overlays',action='store_true');args=parser.parse_args()
 doc=json.loads((ROOT/'src/ui/layout/screens.json').read_text());generate(doc,ROOT/'docs/design/wireframes')
 print('Generated',len(doc['screens'])*2,'SVGs')
 if args.overlays:print('Matched private overlays:',overlays(doc,ROOT/'staging/validation/menu',ROOT/'staging/validation/menu/overlays'))
