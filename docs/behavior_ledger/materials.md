# Texture enhancement policy

`remake:materials-v1` — authored, 2026-10-21. Original PNGs are immutable. Offline
waifu2x-ncnn-vulkan 20250915 uses CUnet for illustration/UI/font atlases and
upconv_7_photo for character/environment textures. These category choices are
authored, not claims about original material behaviour. Setup verifies the
published archive SHA and every installed tool/model against that archive.
Cache identity includes source/model hashes, version, scale, alpha, padding,
noise and tile settings. Wrap padding uses modulo source coordinates; remove
the padding after inference. Alpha is inferred separately and may be thresholded
at 128 for explicit scissor inputs. Never infer scissor/tiling semantics by name.

`materials-inventory-20261021` — high confidence, private bounded PNG headers:
initial inventory contains 1,574 PNGs, estimated 146,690,812 bytes if all loaded
as RGBA8 with a complete mip chain. This includes converted/duplicate resources;
it is deliberately conservative, not measured live GPU residency. First batch
covers UI/frontend, the eight exported spidey PNGs and six original font atlases.
The converted private GLB embeds PNG images instead of referencing the exported
PNGs. Bind its materials by exact decoded RGBA pixel SHA-256 to the source PNG
manifest; do not assume material/texture ordering. No game behaviour inferred.

Runtime policy: Original selects original images. Enhanced first selects a
matching hand-curated assets/hd_overrides relative path, then assets/hd, then the
original. The tracked original_only list contains names only. Generate mipmaps
and request anisotropic filtering for the selected textures. Missing optional
resources fall back without errors. Material instances are duplicated before
overrides; imported originals are not mutated. The menu's texture-quality
setting is stored under video in settings.cfg and applies to art/rig live.

Remastered is a third authored font rendering variant. The original converter's
atlas and BMFont cell/offset/advance data are doubled exactly; RGB and alpha use
the same enhancement pipeline. This preserves nominal cells and advances, not
original pixels or historical edge shapes. Faithful and Enhanced remain available.
Inference across a packed atlas may bleed near cell boundaries; private contact
sheet review is required before claiming every glyph is improved. No original
atlas or derived font is tracked.

`remake:video-options-v1` — authored live/persistent settings: windowed,
borderless or fullscreen; 1280x720, 1920x1080, 2560x1440 or 1920x1200;
VSync, frame cap; 100/85/75/50 percent render scale; renderer-specific AA;
anisotropy; texture/font variants; shadow atlas quality; SSAO and volumetric
fog only on Forward+. Forward+ reduced render scales use FSR2; Mobile uses
bilinear. Defaults preserve 1080p, 60 fps, native scale and disabled optional
effects. Settings apply to root and menu actor viewports, lights and existing
world environments; menu creates its own environment. No renderer restart is
required. Viewport/RenderingServer APIs were checked against the official Godot
class references (viewport scaling/AA/anisotropy and directional shadow atlas).
These options describe remake rendering, not recovered original behaviour.
The root viewport expands to the actual output aspect; each LayoutScreen still
fits/centres its own reference frame. Without expansion a 16:10 window returns a
16:9 viewport texture, so physical capture dimensions are explicitly gated.
At the actual 16:10 scale, Classic fallback focus boxes had thick top borders
which failed the retained contrast sample. Reduce that authored top border to
one reference pixel, retaining the thick left marker. Do not move the contrast
sample or lower its 4.5 threshold. Main Classic still uses its separate underline.
Fullscreen uses the desktop display mode; the resolution selector applies to
windowed/borderless modes and is disabled in Fullscreen. Exclusive display-mode
switching is not implemented or claimed. Render scale remains independently live.
