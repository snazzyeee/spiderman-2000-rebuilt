# Active frontend

The frontend is built by LayoutScreen from src/ui/layout/screens.json. The same
screen/style rectangles generate all 42 annotated blueprints and the private QA
captures. Enhanced is the default at a 1920x1080 base; Classic uses a centered,
pillarboxed 640x480 presentation derived from the 512x240 virtual frame.

| Classification | Current elements |
|---|---|
| Original private assets | Spider-Man rig/animations, original font exports, title illustration/header crop, native sound clips, gallery covers |
| Recovered behaviour | Menu move/select/back/reject IDs; generic CMenu centering and coordinate mapping; frontend actor 0/100 cycle and punch sound family |
| Gemini proposal (unverified) | Per-screen historical rectangles, unproven option lists/unlocks, HUD/death/comic layouts and original subtitle role |
| Authored presentation | Separate actor/menu regions, three lights/ground/contact shadow, camera, breathing/parallax, ink focus border, wings, transitions, carousel and modern preference controls |

Options includes Audio > Menu sounds (Original/Short/Off), Font variant, Menu
style (Classic/Enhanced), volume sliders and Accessibility preferences. The latter
contains reduced motion, side fill, cursor policy, remembered-cheat preference,
attract media and training scope. Cheats stay inactive pending a verified table;
Phased and Practice currently share the one converted traversal-practice slice.
Those future capabilities are not represented as working original unlocks.

Input supports keyboard, mouse hover/click and Godot's gamepad navigation. Focus
has a wider left ink edge, not only a colour change. Transitions are <=0.25 s,
skippable; reduced motion uses 0.1 s and disables breathing/parallax. Attract
starts after 60 s and wakes on input, alternating the staged character and comic
covers (Comics preference suppresses the actor). No unverified biography or game
text is displayed. Cursor uses the OS pointer with hybrid visibility until the
original cursor asset is verified.

Menu audio uses recovered global IDs through Dialogue.play_sfx_id. Original is
the default and plays recovered clips at full length. Short caps all clips at
0.6 s; Off is silent. Previous sound stops first. Repeated hover is silent.

The title screen retains full Title.bmp art. The header uses a private authored
crop of its original lettering. Decoded logo.rle is the Neversoft emblem, not
the game title, and is not substituted. prepare_frontend_art.py CRC-checks and
bounds all extracted inputs and uses the existing SHA-pinned decoder. All
original/derived media and provenance manifests remain ignored.

Historical fidelity remains limited: Shell_DrawBackground's menubg resource has
not yet been tied to a converted texture, and shell flow bodies are stubs.
The stretched shell_bg guess is removed; the current dark/halftone backdrop and
working branch flow are authored. Unknown screens show explicit proposal regions
instead of invented original labels. font_big remains unused until its mapping
is recovered. This is not a certificate of a pixel-faithful original frontend.

Run tools/pipeline/run_regression.ps1 for all retained suites plus layout and
design QA. Real renderers capture all 21 screens × two styles × three resolutions:
252 PNGs across Forward+ and Mobile, under staging/validation/menu/. Six contact
sheets and exact-SVG 40% overlays are produced by composite_overlays.cjs. The
contrast test compares specified foreground colours with captured backdrop
pixels; original palette outline/antialiasing contrast remains outside that
measurement. Frame-interval evidence is a local menu benchmark, not a gameplay
performance claim. See README.md for current status and remaining limits.

Pause now uses the same loader, fonts, action data and focus treatment over a
70% dimmed live scene. Resume/recovery/menu callbacks retain existing cinematic
transport and control locks. User-triggered launches display the original L1Cov
loading view before a threaded scene request; loading hints/spinner remain
unverified and are omitted. The retained low-level direct-launch API supports
the existing lifecycle smoke gate, while a new synthetic scene verifies the
asynchronous user path. Original HUD code is unchanged; HUD/comic/death proposal
claims and gaps are recorded in the ledger and claim audit.

Polish pass: Classic main items are horizontally centred, with transparent
normal styles and an authored focus underline preserving the non-colour focus
gate. The line separation is authored; the centring formula is recovered.
Classic now uses the CRC-checked shell.psx menubg texture, independently joined
through frame/header index 88. It contains one frame, not an animated sequence.
Enhanced's skyline/halftone, ink caption plates, radial contact shadow, input
device hint labels and rotating right-edge comic teaser are authored. Private
comic and rig art remain original. No additional font tracking is introduced.

The logo cut-out remains blocked: a colour/edge mask candidate and private
alpha crops exist, but the hand occludes source lettering and the automatic
mask is not certified free of background. The old cropped header remains live
until the candidate passes that gate. The script preserves any existing mask
so a correction is never silently overwritten. Its high-resolution previews
are raster enlargements; traced alpha is exported separately, and a filled
high-resolution vector reconstruction is still outstanding. Focus halftone/
scale-pop and exact rendered-width QA are also outstanding; the enhanced plates
currently have ink borders/skew/shadows only. Hint labels are ASCII controls,
not recovered device glyph art. These limitations prevent calling the entire
owner polish acceptance complete even when the retained regression suites pass.

Texture/display milestone: the initial offline enhancement batch contains 59
private images (menu/comic art, eight Spider-Man textures and original font
atlases). Enhanced art prefers hand-curated hd_overrides, then generated hd;
Original bypasses both. Rig bindings use decoded RGBA hashes, not material order.
The third font variant, Remastered, doubles original converter cells/advances.
Options > Video persists/applies window mode, resolution including 16:10, VSync,
frame cap, renderer-specific scaling/AA, anisotropy, texture/font variants,
shadows and Forward+-only SSAO/fog. Existing Options audio sliders remain.

The pipeline inventories all private PNGs; run enhance_textures.py --all for the
remaining batch. Explicit repeatable --tiling and --scissor names select wrap
padding and binary alpha, rather than guessing material semantics from filenames.
Private category sheets and texture-budget estimates are in
staging/validation/textures/. The full environment/character enhancement batch
and individual atlas-edge/art quality review are still outstanding. Godot video
memory telemetry excludes driver/OS allocations; neither it nor the conservative
RGBA8 estimate certifies total dedicated GPU residency under every setting.
