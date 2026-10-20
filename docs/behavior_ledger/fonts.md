# Original bitmap fonts

## Format and evidence

Evidence `pkr:font-layout-audit-20261021`, confidence **recovered**: all six
font members have a little-endian 32-bit glyph count, then count records of
four signed 32-bit values: slice width, height, baseline and visible width.
The table starts at byte four, followed by sixteen 16-bit palette words.
Bitmap payload starts at `36 + 16 * count`. Each glyph has `4 * slice_width`
pixels per row, two pixels per byte (low nibble first), followed by padding to
a four-byte boundary. Its payload span is `align4(2 * slice_width * height)`.
The spans account for each complete file exactly; there is no trailing kerning
table or external sheet-coordinate table in these members.

Evidence `decomp:FontTools.h#SDataGlyph@f8a0392` and
`decomp:FontTools.cpp#Font::Font@f8a0392 (@Ok)`, confidence **recovered**:
the constructor consumes that table/palette/payload, retains width, height and
baseline, and defaults inter-character spacing to two source pixels. Global
scale is a separate setting (constructor default 3800/4096); it is not stored
in the font. Nominal glyph advance is width plus spacing, not padded bitmap
width. The baseline may exceed the glyph height. No pair kerning was found.
The commented width implementation is not treated as matched proof; the static
width function remains available for further audit.

Evidence `remake:font-packed-sheet-inspection`, confidence **recovered**:
decoding the low nibble first yields recognizable sequential alphabet/digit
glyphs; private inspection sheet is staging/reference/font-inspection.png.
Palette components are five-bit RGB; zero is the color key. Nonzero entries,
including high-bit black, are retained as visible colors. Converting the
five-bit components to eight-bit channels is a deterministic normalization;
the private manifest retains all original palette words and high bits.
Semi-transparency behavior beyond the color key remains **partial**.

Evidence `pkr:font-bmp-header-audit`, confidence **recovered**: the five named
BMP companions use a 40-byte DIB header, four-bit indexed BI_RGB rows, sixteen
BGR palette entries, and bottom-up storage. These are separate tall authoring
sheets; their names alone do not prove a sheet-to-glyph rectangle mapping.
sp_fnt01 and sp_fnt01b must not be silently interchanged. Runtime export uses
the self-contained FNT bitmaps. Both sets are CRC-checked and inventoried.

## Character mapping

Evidence `exe:0x0043EBB0..0x0043F0BF`, confidence **recovered**, canonical
SHA-256 e4e221b168ccf52130c8c56a1535289afacc1906fb55d54545e33d50e43eec86.
The bounded static read is recorded privately in
staging/reference/font-charmap-static.txt. The decomp getCharIndex body is a
stub, despite its @Ok tag, and is not used as the mapping implementation.

Normal map (mode zero): A–Z and a–z share glyphs 0–25; digits 0–9 use 26–35.
Underscore uses 36, question mark 37, exclamation 38, colon 39, period 40,
hyphen 41, apostrophe 42 and plus 43, provided that index exists. Space is a
non-glyph; its nominal width is integer 80% of glyph zero's width in the
available width implementation (confidence **partial**). Digit-only mode one
uses digits 0–9 and colon at ten; mode two has different lower-case and accent
mapping. Unsupported characters and dynamic button prompts are unresolved by
this milestone and use the existing fallback. Do not invent Unicode bindings
for the unused glyphs by visually naming them.

## UI roles, authored choices and deviations

Evidence `decomp:pshell.cpp#PShell_BigFont/PShell_NormalFont/PShell_SmallFont@f8a0392
(@Ok)` and `decomp:shell.cpp#Shell_VerySmallFont@f8a0392 (@Ok)`, confidence
**recovered**: shell helpers choose font_big, sp_fnt00, sp_fnt02 and sp_fnt03
respectively. This does not prove every screen's role. The exact original HUD,
subtitle and button-prompt choices remain unknown.

Remake role choices (confidence **authored**): title uses font_big; menu, HUD
and dialogue captions use sp_fnt00; subtitles use sp_fnt02. Theme loading is
optional: absent private resources preserve Godot's current fallback. Options
selects Faithful or Enhanced (Enhanced default), persists under user settings
and applies to live registered controls. Unsupported glyphs keep font fallback.
This choice is presentation tuning, not a recovered screen-to-font binding.

Faithful export keeps original RGBA pixels and an integer nearest-neighbor 4x
atlas. Enhanced export traces the union boundary of source pixels at authored
50% luminance coverage, removes collinear points and builds a scalable TrueType
outline with pinned fontTools. Godot rasterizes the outline at the requested
size. This is an authored smoothing/coverage decision, not recovered art.
No cloud service, substituted glyph design or guessed character mapping is used.
Both exports use identical advances/baselines in source-pixel units; TTF metrics
are integral multiples of 64. Any tracing failure is recorded per glyph and
fails conversion; no silent replacement is permitted. Pixel colors remain in
the faithful variant; the enhanced outline is monochrome coverage.

## Verification contract

Bound reads to 1 MiB/font, 256 glyphs, 256 pixels per dimension, 4 Mi pixels
total; require exact payload accounting. Reject invalid palette/header/table,
overflow/truncation/trailing data and unsupported BMP encodings. Synthetic
fixtures cover palette/color-key, nibble order, row padding, baseline/advance,
signed/oversized metrics, top/down BMP rows and malformed palette indices.
Trace tests cover holes, disconnected/diagonal pixels and deterministic metrics.
Runtime tests check optional-resource fallback and variant changes. Added smoke
checks verify original font use in menu and subtitles when present, otherwise
report an explicit skip without changing any existing smoke assertion.
Private comparison renders at 720p/1080p/1440p show the original decoded sheet,
faithful atlas and enhanced text; these are visual evidence, not human approval.
