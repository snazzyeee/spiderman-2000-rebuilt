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
performance claim. See HANDOFF.md for exact results and remaining limits.

Pause now uses the same loader, fonts, action data and focus treatment over a
70% dimmed live scene. Resume/recovery/menu callbacks retain existing cinematic
transport and control locks. User-triggered launches display the original L1Cov
loading view before a threaded scene request; loading hints/spinner remain
unverified and are omitted. The retained low-level direct-launch API supports
the existing lifecycle smoke gate, while a new synthetic scene verifies the
asynchronous user path. Original HUD code is unchanged; HUD/comic/death proposal
claims and gaps are recorded in the ledger and claim audit.
