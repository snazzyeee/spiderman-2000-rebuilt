# Frontend evidence and remake policy

## Menu SFX

Evidence `exe:frontend-sfx-static-20261021`, confidence **recovered**. Canonical
executable SHA-256 e4e221b168ccf52130c8c56a1535289afacc1906fb55d54545e33d50e43eec86;
decomp names at f8a039248908e085f8fba8106769aea7f8f9de67. A fingerprint-gated,
read-only x86 scan bounded each of 110 named frontend functions by the next
names.json function (less than 64 KiB each), finding 91 direct SFX calls.
Private contexts: staging/reference/frontend-sfx-static.json. No executable ran.
Some functions contain data/jump tables: absence of a decoded call is not proof
of absence. Positive call sites were inspected with their controlling branches.

| Event | Global ID | Positive call-site evidence |
|---|---:|---|
| Changed selection | 0x29 | CMenu_Update 0x44080b after comparing previous/new selection; Shell_MainMenu 0x494046 after changed-index check |
| Confirm selection | 0x1F | Shell_MainMenu 0x4941de reached from valid confirm branches; Shell_Options 0x49755c |
| Back/cancel | 0x23 | Shell_Options 0x497538; repeated exit branches in Shell_Difficulty and PCSHELL_DoDisplayOptions |
| Reject unavailable selection | 0x1B | Shell_MainMenu 0x493ead on unavailable-entry branches |

Cheat accept/reject/exit 0x1D/0x1B/0x23 are corroborated by
`decomp:shell.cpp#Shell_Cheats@f8a0392` and static calls. They are not proof of
generic confirm ID. Actor punch IDs 0xE..0x11 are corroborated by
`decomp:shell.cpp#CRudeWordHitterSpidey::AI@f8a0392` and four calls in
0x48db10..0x48dc10. These are actor events, not UI button sounds.

Use audio_ids.md exact global bindings, never menu.kat bank order. Private
WAV frame counts / sample rates resolve move to 0.303333 s, confirm to 1.367 s,
back to 1.111 s (`staging/reference/frontend-branches-static.txt`). The owner's
revised decision (`owner:menu-audio-modes-20261021`, confidence **authored**)
supersedes the earlier universal cap: Original (default) plays recovered IDs
at their full original length; unverified sounds still have a 0.6 s cap.
Short caps all sounds at 0.6 s; Off plays none. Reject looping, empty, absent
or non-finite streams in every mode. Do not truncate or substitute sounds.
Persist mode under audio/menu_sounds, apply live even on a failed save (report
failure), and stop previous menu playback before every new distinct event.
Initial focus and repeated hover on the same item remain silent. Reject event
uses recovered global ID 0x1B. Actor reactions use separate recovered IDs.

## Preliminary design audit (section 3 remains incomplete)

These checks do not validate Gemini's screen rectangles, strings, unlock rules
or per-screen font use. Every such claim remains **Gemini proposal (unverified)**
until a separate claim audit confirms it. No owner reference screenshots were
present in staging/reference/menu_original/ at this inspection.

- `decomp:front.cpp#CMenu::CentreX/CentreY/GetMenuHeight@f8a0392 (@Ok)`,
  confidence **recovered**: virtual extent 512x240. Center justification uses
  x=256; other justification modes offset by measured menu width. Vertical
  centering depends on visible menu height. A single fixed rectangle cannot be
  inferred from these helpers. Constructor line spacing comes from its caller.
- `decomp:PCShell.cpp#PCSHELL_CoordsDCtoPC/PCSHELL_CoordsPCtoDC@f8a0392 (@Ok)`,
  confidence **recovered**: scale virtual x by output_width/512 and y by
  output_height/240 (inverse for mouse input). At 640x480 that means factors
  1.25 and 2 respectively; virtual pixels are not square display pixels.
- `decomp:PCShell.cpp#resetActionMaps@f8a0392 (@Ok, Matching)`, confidence
  **recovered**: two control menus originate at (30,60) and (332,60) in the
  virtual frame, with line separation 15. This does not recover final row text.
- `decomp:shell.cpp#Shell_DrawTitleBar@f8a0392 (@Ok, Matching)`, confidence
  **recovered**: position is caller-supplied, not a fixed Y=25. The helper
  conditionally selects the big font and returns to the normal font; top RGB
  128,128,128 and bottom RGB 69,60,107. Per-screen callers still need checking.
- `decomp:pshell.cpp#PShell_DefaultText/PShell_InstructionalText/PShell_NormalFont/PShell_SmallFont@f8a0392 (@Ok)`,
  confidence **recovered**: default helper selects sp_fnt00, instructional
  helper selects sp_fnt02. The small-font helper alone is not evidence of
  original dialogue subtitles. That role remains the authored fonts.md choice.
- `decomp:shell.cpp#Shell_DrawBackground@f8a0392 (@Ok, Matching)`, confidence
  **recovered**: draws the menubg animation resource over 512x240. This does
  not establish that shell_bg.png is an equivalent resource or composition.
- `decomp:shell.cpp#Shell_LegalScreen@f8a0392 (@Ok)`, confidence **recovered**:
  loads LegalPC.bmp and waits until the 180-vblank deadline passes. The visible
  body does not implement the proposal's any-button early exit.
- `decomp:shell.cpp#Shell_MainMenu/Shell_Options/Shell_Gallery/Shell_Special/Shell_TitleScreen/Shell_DoShell@f8a0392`,
  confidence **unknown** for page flow from these bodies: they are TODO stubs.
  Names alone cannot verify Gemini's page flow or option lists.
- `decomp:shell.cpp#CRudeWordHitterSpidey::AI@f8a0392 (@Ok)`, confidence
  **recovered**: cycles animation 0 and 0x64 after completion, with a one-time
  random punch sound among 0xE..0x11 at frame 7. Constructor placement is
  tagged NotOk, confidence **partial**. Existence of this cheat/input actor
  does not prove placement or idle animation for the main menu actor.

All enhanced placement, motion, contrast, side-fill and safe-area decisions are
authored presentation requirements; no runtime layout data has been adopted yet.

## Layout contract and claim disposition (2026-10-21)

Evidence `remake:frontend-layout-v1`, confidence **authored**: screens.json is
an explicit presentation plan, not proof of original rectangles/page flow.
Classic uses a 640x480 presentation of the recovered 512x240 coordinate frame;
enhanced uses 1920x1080. Store every pixel rectangle, asset path, font role,
colour, focus/back/sound route and per-element provenance here. Unknown content
is an empty outlined proposal region; no invented original strings or unlocks.
The main actor occupies the right region, menu the left, with no intersection.
Screen/style layout choices and modern controls are authored. Normal font is
sp_fnt00; small text sp_fnt02 per fonts.md's authored role policy. Specialized
font_big is not rendered until its mapping is recovered.

Disposition of every Gemini element is in docs/design/claim_audit.md; raw
schema/rectangle/asset checks go privately to proposal-audit.json. A valid
normalized rectangle does not verify historical placement. Generic helper
citations support only those helpers; all screen-specific rectangles, content,
flow and role inferences remain Gemini proposal (unverified). Archive/resource
names are not filesystem paths. Missing resources stay disabled, not guessed.

Owner-question defaults: authored wings (switch to black pillarbox), hybrid
cursor (switch to always visible), remember accepted cheats (switch off;
cheat activation remains disabled pending verified table), character/comic
attract carousel (switch to comics only), phased training (switch to practice
only). Never expose unconverted challenges as working. Attract delay 60 s,
transition <=0.35 s, reduced motion disables parallax/breathing/focus pulses.
Persist preferences in frontend settings. Main navigation remains the five
existing entries in order; unsupported original branches have proposal regions.

## Private art conversion and in-game evidence

Evidence `pkr:frontend-art-inventory-20261021`, confidence **recovered** for
archive identity/CRC only: data/logo.rle (CRC bd62c269,98090 bytes), Title.bmp
(CRC 009b2fa6,123960 bytes), LegalPC.bmp (CRC a4ca41b9,123958 bytes), L1Cov.bmp
(CRC 0dbe6db6,123958 bytes). Level cover family L*Cov.bmp is extracted by exact
archive-member names, CRC/length checked before decode. No image meaning or
layout is inferred from filename. Use the existing SHA-pinned MIT multitool
1.3.4 to decode only these bounded (<4 MiB/member) inputs, with a 30 s timeout;
verify PNG dimensions <=4096 and pixel budget <=16 Mi pixels. Keep originals,
PNGs and hash/CRC provenance private. This relies on the pinned decoder; it does
not claim an independently recovered RLE codec. Title and logo remain separate
assets; inspect them before choosing the header path.

Evidence `decomp:panel.cpp#Panel_Init@f8a0392 (@Ok, Matching)`, confidence
**recovered**: initializes Sp, Compass and Webcart animation resources.
Panel_SetStretchedScreenCoords is a generic caller-coordinate transformation;
it does not prove the proposed health-meter shape or its normalized rectangle.
Panel_Display/DisplayCompass/DisplayHealthBar/DisplayTimer are TODO stubs:
HUD rendering positions, colour thresholds, flashing, wound thresholds and
format strings remain Gemini proposal (unverified). Panel_CreateCompass stores
a fixed-point-adjusted world target; this alone does not prove its screen slot.
Panel_CreateHealthBar selects encounter textures for the listed body types;
this does not prove normalized bar geometry. No HUD rewrite is authorized here.
Pause uses authored existing Resume/recovery/Main Menu actions through the
layout loader while preserving cinematic transport locks. Loading uses private
cover art during the asynchronous scene request; hints/spinner behaviour remain
unverified and are omitted. Comic intro/death screen wording, timing and flow
from the proposals are unverified and not adopted.

Evidence `pkr:cover-family-inventory-20261021`, confidence **recovered**: this archive has BMP covers for levels 1,2,4,5,7,8 only. No BMP for 3/6; never synthesize an original cover. The conversion gate uses this exact inspected inventory.

Evidence `remake:frontend-art-visual-inspection-20261021`, confidence **recovered
visual observation**: decoded logo.rle is the Neversoft publisher emblem, not
Spider-Man title lettering. Title.bmp contains the game-title lettering at the
top of the full title illustration. Authored header crop uses x=110,y=18,w=294,
h=68 source pixels; retain full Title for the title screen and never replace
the game title with the publisher emblem. All pixels remain private. Exported
GLB animation names preserve numeric source slots (spidey_anim_N); use name N,
not array ordinal, because missing slots shift indices. For the authored menu
reaction use the recovered cycle 0/100, sound once at animation frame 7;
original main-menu staging association remains unverified. Breathing transforms,
three lights, plane, camera and parallax are authored tuning in screens.json.

Authored action routes (`remake:frontend-actions-v1`): preserve Game/New Game/
Load Game, Training/Rooftop Practice/Controls, Special/Replay Opening/Controls,
Gallery/Previous/Next and Back. Options adds style/font/audio and accessibility
choices. Controls describe the remake bindings, never original configuration
text. These routes preserve the working remake while original shell flow is
unknown. Remembered-cheat preference has no activation effect until table
recovery. Phased training currently exposes only converted practice. Placeholder
screens are explicit unverified regions. Do not call these original page flow.
Design QA measures region bounds, sibling intersection, safe-area enclosure,
focus border geometry and declared foreground against actual backdrop pixels.
Palette outline/antialiasing contrast is not certified. The real-renderer
benchmark records 180 uncapped 1080p menu frame intervals after 60 warm-up frames.
