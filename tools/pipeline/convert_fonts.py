"""CRC-check and convert private embedded bitmap fonts; see fonts.md ledger."""
import hashlib
import json
from pathlib import Path
import sys
import zlib

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/reference'))
from decode_fonts import decode_font, decode_bmp, normal_charmap, trace_mask, MAX_BYTES
from extract_audio_manifest import pkr_entries

FONTS = ['font_big', 'sp_fnt00', 'sp_fnt01', 'sp_fnt02', 'sp_fnt03', 'OLD_sp_fnt01']
SHEETS = ['sp_fnt00', 'sp_fnt01', 'sp_fnt01b', 'sp_fnt02', 'sp_fnt03']


def dependencies():
    sys.path.insert(0, str(ROOT / 'tools/third_party/font-tools/python'))
    try:
        import PIL
        import fontTools
    except ImportError as error:
        raise RuntimeError('Run tools/pipeline/setup_font_tools.py first') from error
    if PIL.__version__ != '11.3.0' or fontTools.__version__ != '4.60.1':
        raise ValueError('Font dependency version differs from setup pin')


def extract(archive):
    wanted = {name.casefold() + '.fnt' for name in FONTS} | {name + '.bmp' for name in SHEETS}
    result = {}
    with archive.open('rb') as stream:
        for member, crc, compressed, offset, length, stored in pkr_entries(archive):
            if member.parent.as_posix().casefold() != 'data' or member.name.casefold() not in wanted:
                continue
            if not 0 < length <= MAX_BYTES or not 0 < stored <= MAX_BYTES * 2:
                raise ValueError('Font archive read exceeds budget')
            stream.seek(offset)
            blob = stream.read(stored)
            if len(blob) != stored:
                raise ValueError('Truncated font archive member')
            if compressed:
                decoder = zlib.decompressobj()
                blob = decoder.decompress(blob, length + 1)
                if not decoder.eof or decoder.unused_data or decoder.unconsumed_tail:
                    raise ValueError('Font compression escapes bounds')
            if len(blob) != length or zlib.crc32(blob) != crc:
                raise ValueError('Font archive CRC/length mismatch')
            result[member.name.casefold()] = dict(member=str(member), crc32=f'{crc:08x}',
                sha256=hashlib.sha256(blob).hexdigest(), blob=blob)
    if set(result) != wanted:
        raise ValueError('Missing font/sheet archive members')
    return result


def write_bitmap(font, name, output, scale):
    from PIL import Image
    cell = max(g['padded_width'] for g in font['glyphs']) + 2
    row_height = max(g['height'] for g in font['glyphs']) + 2
    atlas = Image.new('RGBA', (cell * 10, row_height * ((len(font['glyphs']) + 9) // 10)))
    positions = {}
    for glyph in font['glyphs']:
        x, y = glyph['index'] % 10 * cell, glyph['index'] // 10 * row_height
        image = Image.frombytes('RGBA', (glyph['padded_width'], glyph['height']), glyph['rgba'])
        atlas.paste(image, (x, y))
        positions[glyph['index']] = (x, y)
    atlas = atlas.resize((atlas.width * scale, atlas.height * scale), Image.Resampling.NEAREST)
    texture = f'{name}.png'
    atlas.save(output / texture)
    mapping = normal_charmap(len(font['glyphs']))
    lines = [f'info face="{name}" size={font["line_height"] * scale}',
             f'common lineHeight={font["line_height"] * scale} base={font["ascent"] * scale} scaleW={atlas.width} scaleH={atlas.height} pages=1 packed=0',
             f'page id=0 file="{texture}"', f'chars count={len(mapping) + 1}']
    for code, index in sorted(mapping.items()):
        glyph = font['glyphs'][index]
        x, y = positions[index]
        lines.append(f'char id={code} x={x * scale} y={y * scale} width={glyph["padded_width"] * scale} height={glyph["height"] * scale} xoffset=0 yoffset={(font["ascent"] - glyph["baseline"]) * scale} xadvance={(glyph["width"] + font["spacing"]) * scale} page=0 chnl=15')
    space = font['glyphs'][0]['width'] * 80 // 100
    lines.append(f'char id=32 x=0 y=0 width=0 height=0 xoffset=0 yoffset=0 xadvance={space * scale} page=0 chnl=15')
    lines.append('kernings count=0')
    (output / f'{name}.fnt').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    return {str(code): (font['glyphs'][index]['width'] + font['spacing']) * scale for code, index in mapping.items()} | {'32': space * scale}


def write_outline(font, name, output, failures):
    from fontTools.fontBuilder import FontBuilder
    from fontTools.pens.ttGlyphPen import TTGlyphPen
    from fontTools.colorLib.builder import buildCOLR, buildCPAL
    from decode_fonts import palette_rgba
    builder = FontBuilder(font['line_height'] * 64, isTTF=True)
    names = ['.notdef', 'space'] + [f'g{i}' for i in range(len(font['glyphs']))]
    empty = TTGlyphPen(None).glyph()
    outlines = {'.notdef': empty, 'space': empty}
    space = font['glyphs'][0]['width'] * 80 // 100
    metrics = {'.notdef': (space * 64, 0), 'space': (space * 64, 0)}
    palette = [palette_rgba(word) for word in font['palette']]
    layers = {}

    def outline(mask, baseline):
        contours = trace_mask(mask)
        area_twice = sum(a[0] * b[1] - b[0] * a[1] for contour in contours
                         for a, b in zip(contour, contour[1:] + contour[:1]))
        if area_twice != 2 * sum(sum(row) for row in mask):
            raise ValueError('Traced palette area differs from original pixels')
        pen = TTGlyphPen(None)
        for contour in contours:
            points = [(x * 64, (baseline - y) * 64) for x, y in contour]
            pen.moveTo(points[0])
            for point in points[1:]:
                pen.lineTo(point)
            pen.closePath()
        return pen.glyph()

    for glyph in font['glyphs']:
        try:
            width, height = glyph['padded_width'], glyph['height']
            pixels = list(zip(*[iter(glyph['rgba'])] * 4))
            key = f'g{glyph["index"]}'
            advance = ((glyph['width'] + font['spacing']) * 64, 0)
            visible = [[bool(pixels[y * width + x][3]) for x in range(width)] for y in range(height)]
            outlines[key] = outline(visible, glyph['baseline'])
            first_x = min(x for y, row in enumerate(visible) for x, filled in enumerate(row) if filled)
            metrics[key] = (advance[0], first_x * 64)
            layers[key] = []
            for color_index, color in enumerate(palette):
                if not color[3] or color not in pixels:
                    continue
                layer = f'{key}_p{color_index}'
                mask = [[pixels[y * width + x] == color for x in range(width)] for y in range(height)]
                outlines[layer] = outline(mask, glyph['baseline'])
                layer_x = min(x for row in mask for x, filled in enumerate(row) if filled)
                metrics[layer] = (advance[0], layer_x * 64)
                names.append(layer)
                layers[key].append((layer, color_index))
            if not layers[key]:
                raise ValueError('Glyph has no visible palette layers')
        except Exception as error:
            failures.append(dict(font=name, glyph=glyph['index'], reason=str(error)))
    if failures:
        raise ValueError('Glyph tracing failed; see private conversion manifest')
    mapping = normal_charmap(len(font['glyphs']))
    builder.setupGlyphOrder(names)
    builder.setupCharacterMap({code: f'g{index}' for code, index in mapping.items()} | {32: 'space'})
    builder.setupGlyf(outlines)
    builder.setupHorizontalMetrics(metrics)
    builder.setupHorizontalHeader(ascent=font['ascent'] * 64, descent=-font['descent'] * 64, lineGap=0)
    builder.setupNameTable(dict(familyName=name, styleName='Regular', uniqueFontIdentifier=name,
                                fullName=name, psName=name.replace('_', '-')))
    builder.setupOS2(sTypoAscender=font['ascent'] * 64, sTypoDescender=-font['descent'] * 64,
                     sTypoLineGap=0, usWinAscent=font['ascent'] * 64, usWinDescent=font['descent'] * 64)
    builder.setupPost()
    builder.setupMaxp()
    builder.font['COLR'] = buildCOLR(layers, version=0, glyphMap=builder.font.getReverseGlyphMap())
    builder.font['CPAL'] = buildCPAL([[tuple(channel / 255 for channel in color) for color in palette]])
    builder.font['head'].created = builder.font['head'].modified = 2082844800
    builder.font.recalcTimestamp = False
    builder.save(output / f'{name}.ttf')
    return {str(code): metrics[f'g{index}'][0] for code, index in mapping.items()} | {'32': space * 64}


def comparisons(font, output, name):
    from PIL import Image, ImageDraw, ImageFont
    folder = ROOT / 'staging/validation/fonts'
    folder.mkdir(parents=True, exist_ok=True)
    mapping = normal_charmap(len(font['glyphs']))
    sample = 'ABC XYZ 0123'
    for width, height, scale in [(1280, 720, 1), (1920, 1080, 2), (2560, 1440, 3)]:
        image = Image.new('RGBA', (width, height), (24, 24, 30, 255))
        draw = ImageDraw.Draw(image)
        for row, integer_scale in [(0, 1), (1, scale)]:
            x, baseline = 40, 100 + row * height // 3 + font['ascent'] * integer_scale
            draw.text((40, baseline - 40), 'Decoded original pixels' if row == 0 else 'Faithful nearest pixels', fill='white')
            for character in sample:
                if character == ' ':
                    x += font['glyphs'][0]['width'] * 80 // 100 * integer_scale
                    continue
                g = font['glyphs'][mapping[ord(character)]]
                glyph = Image.frombytes('RGBA', (g['padded_width'], g['height']), g['rgba'])
                glyph = glyph.resize((glyph.width * integer_scale, glyph.height * integer_scale), Image.Resampling.NEAREST)
                image.alpha_composite(glyph, (x, baseline - g['baseline'] * integer_scale))
                x += (g['width'] + 2) * integer_scale
        y = height * 2 // 3
        draw.text((40, y - 30), 'Enhanced palette-layer outline', fill='white')
        ttf = ImageFont.truetype(str(output / f'{name}_enhanced.ttf'), font['line_height'] * scale)
        draw.text((40, y), sample, font=ttf, fill='white', embedded_color=True)
        image.convert('RGB').save(folder / f'{name}-{width}x{height}.png')


def main():
    dependencies()
    from PIL import Image
    members = extract(ROOT / 'staging/raw_extracted/PC/app/data.pkr')
    output = ROOT / 'assets/ui/fonts'
    output.mkdir(parents=True, exist_ok=True)
    manifest = dict(schema_version=1, outline_policy='exact_palette_layers', faithful_scale=4, fonts=[], sheets=[], tracing_failures=[])
    manifest_path = output / 'font_manifest.json'
    try:
        for name in FONTS:
            source = members[name.casefold() + '.fnt']
            font = decode_font(source['blob'])
            original = write_bitmap(font, name + '_original', output, 1)
            faithful = write_bitmap(font, name + '_faithful', output, 4)
            enhanced = write_outline(font, name + '_enhanced', output, manifest['tracing_failures'])
            if any(faithful[code] != advance * 4 or enhanced[code] != advance * 64 for code, advance in original.items()):
                raise ValueError('Font metrics differ across variants')
            manifest['fonts'].append(dict(name=name, source_member=source['member'], source_sha256=source['sha256'],
                source_crc32=source['crc32'], palette=font['palette'], line_height=font['line_height'],
                glyphs=[{key: value for key, value in glyph.items() if key != 'rgba'} for glyph in font['glyphs']],
                mapping=normal_charmap(len(font['glyphs'])), original_advances=original,
                faithful_advances=faithful, enhanced_advance_units=enhanced,
                faithful=name + '_faithful.fnt', enhanced=name + '_enhanced.ttf'))
            comparisons(font, output, name)
        for name in SHEETS:
            source = members[name + '.bmp']
            sheet = decode_bmp(source['blob'])
            Image.frombytes('RGBA', (sheet['width'], sheet['height']), sheet['rgba']).save(output / (name + '_source_sheet.png'))
            manifest['sheets'].append(dict(name=name, source_member=source['member'], source_sha256=source['sha256'],
                                           width=sheet['width'], height=sheet['height'], glyph_mapping='unverified'))
        manifest['passed'] = True
    except Exception:
        manifest['passed'] = False
        raise
    finally:
        manifest_path.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(fonts=len(manifest['fonts']), sheets=len(manifest['sheets']),
                          tracing_failures=len(manifest['tracing_failures']), metrics_exact=True)))


if __name__ == '__main__':
    main()
