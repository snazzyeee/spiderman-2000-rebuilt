"""Bounded bitmap font/BMP decoders. Contract: behavior_ledger/fonts.md."""
import struct

MAX_BYTES = 1024 * 1024
MAX_PIXELS = 4 * 1024 * 1024


def palette_rgba(word):
    return tuple(((word >> shift) & 31) * 255 // 31 for shift in (0, 5, 10)) + (255 if word else 0,)


def decode_font(blob):
    if not 52 <= len(blob) <= MAX_BYTES:
        raise ValueError('Font file size outside bounds')
    count, = struct.unpack_from('<I', blob)
    if not 1 <= count <= 256 or 36 + count * 16 > len(blob):
        raise ValueError('Font glyph table outside bounds')
    palette = list(struct.unpack_from('<16H', blob, 4 + count * 16))
    colors = [palette_rgba(word) for word in palette]
    cursor, pixels, glyphs = 36 + count * 16, 0, []
    for index in range(count):
        slices, height, baseline, width = struct.unpack_from('<4i', blob, 4 + index * 16)
        if not (1 <= slices <= 64 and 1 <= height <= 255 and 0 <= baseline <= 255
                and 0 < width <= min(255, slices * 4)):
            raise ValueError('Unsupported font glyph metric')
        padded_width = slices * 4
        pixels += padded_width * height
        size = slices * height * 2
        span = (size + 3) & ~3
        if pixels > MAX_PIXELS or cursor + span > len(blob):
            raise ValueError('Glyph bitmap escapes bounds')
        indices = []
        for value in blob[cursor:cursor + size]:
            indices.extend((value & 15, value >> 4))
        rgba = bytes(channel for value in indices for channel in colors[value])
        glyphs.append(dict(index=index, width=width, height=height, baseline=baseline,
                           padded_width=padded_width, offset=cursor, bytes=size, rgba=rgba))
        cursor += span
    if cursor != len(blob):
        raise ValueError('Unexpected trailing font data')
    ascent = max(glyph['baseline'] for glyph in glyphs)
    descent = max(0, max(glyph['height'] - glyph['baseline'] for glyph in glyphs))
    return dict(glyphs=glyphs, palette=palette, ascent=ascent, descent=descent,
                line_height=ascent + descent, spacing=2)


def normal_charmap(count):
    mapping = {ord(letter): ord(letter.upper()) - 65 for letter in
               'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz'}
    mapping.update({ord(str(i)): 26 + i for i in range(10)})
    mapping.update({ord(character): index for character, index in
                    zip('_?!:.-\'+', range(36, 44))})
    return {code: index for code, index in mapping.items() if index < count}


def decode_bmp(blob):
    if not 54 <= len(blob) <= MAX_BYTES or blob[:2] != b'BM':
        raise ValueError('Unsupported BMP size/signature')
    size, = struct.unpack_from('<I', blob, 2)
    offset, = struct.unpack_from('<I', blob, 10)
    dib, width, signed_height, planes, depth, compression, image_size = struct.unpack_from('<IiiHHII', blob, 14)
    used, = struct.unpack_from('<I', blob, 46)
    height = abs(signed_height)
    if (size != len(blob) or dib != 40 or not 0 < width <= 8192 or not 0 < height <= 8192
            or width * height > MAX_PIXELS or planes != 1 or depth not in (4, 8) or compression != 0):
        raise ValueError('Unsupported BMP header/encoding')
    count = used or 1 << depth
    stride = ((width * depth + 31) // 32) * 4
    if (not 1 <= count <= 1 << depth or offset < 54 + count * 4
            or offset + stride * height != len(blob) or image_size not in (0, stride * height)):
        raise ValueError('BMP palette/row bounds mismatch')
    palette = []
    for i in range(count):
        blue, green, red, _ = struct.unpack_from('<4B', blob, 54 + i * 4)
        palette.append((red, green, blue, 255 if red or green or blue else 0))
    rgba = bytearray()
    for y in range(height):
        source_y = y if signed_height < 0 else height - 1 - y
        row = blob[offset + source_y * stride:offset + (source_y + 1) * stride]
        for x in range(width):
            index = row[x] if depth == 8 else (row[x // 2] >> (4 if x % 2 == 0 else 0)) & 15
            if index >= count:
                raise ValueError('BMP pixel references missing palette entry')
            rgba.extend(palette[index])
    return dict(width=width, height=height, depth=depth, palette=palette, rgba=bytes(rgba))


def trace_mask(mask):
    """Trace oriented union boundaries, retaining holes and diagonal components."""
    height, width = len(mask), len(mask[0]) if mask else 0
    if not width or height > 256 or width > 256 or any(len(row) != width for row in mask):
        raise ValueError('Invalid trace mask bounds')
    edges = set()
    for y, row in enumerate(mask):
        for x, filled in enumerate(row):
            if not filled:
                continue
            corners = [(x, y), (x + 1, y), (x + 1, y + 1), (x, y + 1)]
            for a, b in zip(corners, corners[1:] + corners[:1]):
                if (b, a) in edges:
                    edges.remove((b, a))
                else:
                    edges.add((a, b))
    outgoing = {}
    for a, b in edges:
        outgoing.setdefault(a, set()).add(b)
    loops = []
    directions = {(1, 0): 0, (0, 1): 1, (-1, 0): 2, (0, -1): 3}
    while edges:
        start, point = min(edges)
        loop = [start]
        previous = start
        for _ in range(width * height * 4 + 1):
            edges.remove((previous, point))
            outgoing[previous].remove(point)
            if point == start:
                break
            loop.append(point)
            incoming = directions[(point[0] - previous[0], point[1] - previous[1])]
            options = outgoing.get(point, set())
            if not options:
                raise ValueError('Open glyph contour')
            def turn(next_point):
                direction = directions[(next_point[0] - point[0], next_point[1] - point[1])]
                return {1: 0, 0: 1, 3: 2, 2: 3}[(direction - incoming) % 4]
            previous, point = point, min(options, key=turn)
        else:
            raise ValueError('Glyph contour exceeds edge budget')
        reduced = []
        for i, b in enumerate(loop):
            a, c = loop[i - 1], loop[(i + 1) % len(loop)]
            if (b[0] - a[0]) * (c[1] - b[1]) != (b[1] - a[1]) * (c[0] - b[0]):
                reduced.append(b)
        if len(reduced) < 4:
            raise ValueError('Degenerate glyph contour')
        loops.append(reduced)
    return loops
