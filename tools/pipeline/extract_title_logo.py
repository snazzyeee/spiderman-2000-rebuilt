"""Create a private, correctable logo mask; never invent occluded letter pixels.

The automatic mask is a candidate, not a historical reconstruction certificate.
Existing masks always win. Delete the mask explicitly to regenerate the candidate.
"""
import argparse
import hashlib
import json
from pathlib import Path
from convert_fonts import dependencies, ROOT

def candidate(rgb):
    """Blue seeds, neutral outline growth, and isolated-pixel cleanup."""
    height, width = len(rgb), len(rgb[0])
    if not 0 < width <= 512 or not 0 < height <= 256 or any(len(row) != width for row in rgb):
        raise ValueError('Invalid segmentation dimensions')
    seeds = {(x,y) for y,row in enumerate(rgb) for x,(r,g,b) in enumerate(row)
             if b > r+12 and g > r+7 and b > 65 and abs(b-g) < 100}
    mask = set(seeds)
    for _ in range(2):
        added = set()
        for x,y in mask:
            for dx,dy in ((-1,0),(1,0),(0,-1),(0,1)):
                a,c = x+dx,y+dy
                if 0 <= a < width and 0 <= c < height:
                    r,g,b = rgb[c][a]
                    if max(r,g,b)-min(r,g,b) < 30: added.add((a,c))
        mask |= added
    # Morphological area opening removes detached one-pixel noise, without
    # closing holes or fabricating the portions hidden by the actor's hand.
    return [[int((x,y) in mask and any((x+dx,y+dy) in mask for dx,dy in ((1,0),(-1,0),(0,1),(0,-1))))*255
             for x in range(width)] for y in range(height)]

def choose_mask(existing, generated, size):
    if existing is not None:
        if existing.size != size: raise ValueError('Existing correction mask dimensions differ from source crop')
        return existing.convert('L'), 'existing mask preserved'
    return generated, 'automatic candidate; visual review required'

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=ROOT/'assets/ui/frontend/Title.png')
    parser.add_argument('--output', type=Path, default=ROOT/'assets/ui/frontend')
    args = parser.parse_args()
    dependencies()
    from PIL import Image, ImageDraw
    Image.MAX_IMAGE_PIXELS = 1024*1024
    if args.source.stat().st_size > 4*1024*1024: raise ValueError('Source exceeds read budget')
    with Image.open(args.source) as source:
        if source.size != (512,240): raise ValueError('Unexpected title reference frame')
        crop = source.convert('RGB').crop((110,24,394,88))
    pixels = list(crop.getdata())
    rgb = [pixels[y*crop.width:(y+1)*crop.width] for y in range(crop.height)]
    generated = Image.new('L', crop.size)
    generated.putdata([v for row in candidate(rgb) for v in row])
    args.output.mkdir(parents=True, exist_ok=True)
    path = args.output/'title_logo_mask.png'
    existing = Image.open(path) if path.exists() else None
    mask,status = choose_mask(existing, generated, crop.size)
    if existing: existing.close()
    if not path.exists(): mask.save(path)
    result = crop.convert('RGBA'); result.putalpha(mask)
    result.save(args.output/'title_logo_candidate.png')
    # Exact pixel-boundary vectors, tiled to respect the existing font tracer's
    # 256-pixel bound. The SVG retains holes using evenodd fill. It is private.
    from decode_fonts import trace_mask
    paths = []
    for start in range(0,crop.width,128):
        rows = [[mask.getpixel((x,y)) >= 128 for x in range(start,min(start+128,crop.width))] for y in range(crop.height)]
        for loop in trace_mask(rows):
            paths.append('M'+' L'.join(f'{x+start},{y}' for x,y in loop)+' Z')
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {crop.width} {crop.height}"><path fill="white" fill-rule="evenodd" d="{" ".join(paths)}"/></svg>'
    (args.output/'title_logo_mask.svg').write_text(svg)
    # Keep palette samples exact in RGB; alpha alone is supersampled for clean
    # review at 1080p and 1440p. No missing source detail is reconstructed.
    review = ROOT/'staging/validation/menu/logo-review';review.mkdir(parents=True,exist_ok=True)
    for scale in (4,6):
        enlarged = result.resize((crop.width*scale,crop.height*scale),Image.Resampling.NEAREST)
        enlarged.putalpha(mask.resize(enlarged.size,Image.Resampling.LANCZOS))
        enlarged.save(review/f'logo-alpha-{scale}x.png')
    report = dict(source_sha256=hashlib.sha256(args.source.read_bytes()).hexdigest(), crop=[110,24,284,64], mask_sha256=hashlib.sha256(path.read_bytes()).hexdigest(), status=status,
                  gate='BLOCKED: automatic segmentation cannot certify removal of all background or restore hand-occluded lettering; candidate not installed in runtime', tracer='existing in-repo bounded pixel contour tracer; no Potrace dependency installed')
    (review/'report.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2))

if __name__ == '__main__': main()
