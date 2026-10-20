# Godot migration and reproduction

The active project is the repository root, pinned to official Godot 4.7.2 stable
(ed1daf0bf). Forward+ is the default renderer; it enables SSAO and volumetric
fog. Mobile remains supported through the launcher as a fallback. Both use original low-poly
meshes, lit glTF materials, shadows and fog. Neither renderer implies a measured
frame-time guarantee. Unity remains an archived implementation reference.

## Run the prepared local project

From this repository root:

```powershell
.\tools\pipeline\run_godot.ps1
.\tools\pipeline\run_godot.ps1 -Renderer mobile
.\tools\pipeline\run_godot.ps1 -Editor
```

These launch the portable editor/runtime from the ignored tools/third_party
directory. No export-template installation is needed to play from source.
A packaged Windows release has not been exported during this migration.
Use the complete Godot folder and its locally generated assets.

## Rebuild the private assets

Inputs are the local PC data.pkr, the existing extraction/ASR manifest and WAVs,
Python 3.11+ with requests/BeautifulSoup, FFmpeg, and the pinned converter.
No Unity editor is required. The current isolated Python is in
tools/third_party/audio-python/Scripts/python.exe. With Godot closed:

```powershell
.\tools\pipeline\prepare_godot.ps1 -Offline -SmokeTest
```

Omit -Offline to retrieve the public transcript again. Pass -PythonPath or
-Manifest to use a different Python or extraction manifest. The script checks
each exit status, verifies the official editor ZIP, runs CRC-checked archive
conversion, writes the audio registry, imports resources, and derives the route
directly using Godot collision queries. It does not run original game code.

Individual steps:

```powershell
.\tools\reference\setup_converter.ps1
.\tools\pipeline\setup_godot.ps1
& .\tools\third_party\audio-python\Scripts\python.exe tools/pipeline/convert_characters.py
& .\tools\third_party\audio-python\Scripts\python.exe tools/pipeline/prepare_godot_ui.py
& .\tools\third_party\audio-python\Scripts\python.exe tools/pipeline/build_dialogue_manifest.py --offline
& .\tools\third_party\godot-4.7.2\Godot_v4.7.2-stable_win64_console.exe --headless --editor --path . --import
& .\tools\third_party\godot-4.7.2\Godot_v4.7.2-stable_win64_console.exe --headless --path . --script res://src/diagnostics/generate_layout.gd
```

The earlier [audio extraction](AUDIO_PIPELINE.md) and [local ASR](BATCH_AUDIO.md)
tools are standalone Python programs. Their extracted audio, evidence and TRG
JSON now default to staging/raw_extracted/audio_source; they never required Unity.
The scraper reads stable source filenames from that extraction manifest because
the WAV output names themselves include extraction hashes. It verifies every
referenced WAV's SHA-256 before copying it to Godot's logical filenames.

## Project hierarchy

```text
project.godot                    Renderer settings and service autoloads
scenes/main_menu.tscn            Five-branch menu, original art/rig/audio
scenes/mission1.tscn             Original L1A1 rooftop mission assembly
src/services/                    Session, input bindings, audio registry/buses
src/ui/                          Menu pages, mixer settings, gallery
src/player/                      Box-based kinematic motor, orbit/spring arm
src/mission/                     Lifecycle, objectives, encounter, enemy states
src/world/                       GLB fitting, lighting and material integration
src/diagnostics/                 Route generation and integration runner
assets/                          Private generated content; entirely ignored
  audio/dialogue/dialogue_manifest.json
  audio/{dialogue,music,sfx}/*.wav
  characters/*.glb, lowres/*.glb, conversion_manifest.json
  environments/l1a1_{g,o}.glb, mission1_layout.json
  ui/*.png
  */source_archive/              Preserved pre-migration art (.gdignore)
staging/                         Private raw extraction, validation, logs (.gdignore)
tools/                           Pipeline, reference and migration tools (.gdignore)
docs/                            Documentation and gameplay references (.gdignore)
```

The actual main scene contains a scripted root; it assembles components and
imports PackedScene resources deterministically at runtime. Inspectors remain
available for imported GLBs. Model fitting preserves the imported skeleton,
inverse binds, weights and animation tracks. Godot creates native Skeleton3D
and AnimationPlayer components. The animation slots retain numeric names;
the selected locomotion slots are authored bindings, not verified semantics.

The survey exported 107 GLBs: 105 multi-bone rig assets, including low-resolution
variants, and two L1A1 geometry files. They contain 6,002 animation slots in
aggregate, including variants; this is not 6,002 distinct authored motions.
Fifty-six other surveyed PSX files lacked a multi-bone hierarchy and are listed
with diagnostics rather than labeled as exported characters. No conversion
failures were reported. The player retains 18 bones and 300 animation slots.
Thirty-four original menu/comic BMPs were converted directly from data.pkr to
PNG. Textures retain their source resolution; high-resolution repainting and
pixel-identical original menu layout are not claimed.

Godot uses glTF coordinates. The archived Unity glTFast importer negates X,
not Z; the optional legacy route import accounts for that. New route generation
uses Godot geometry directly and found 104 supported roof candidates locally.
The three-checkpoint route and mesh-6 bank entry role are authored choices.

## Dialogue matching and data semantics

[The supplied page](https://transcripts-fanon.fandom.com/wiki/Spider-Man_%282000_Video_Game%29/Transcript)
is parsed with requests and BeautifulSoup. A public MediaWiki API fallback
handles a rejected normal page request. HTML is cached privately for offline
reproduction. Speaker labels, headings, continuations and stage directions are
handled separately. The page is a community cutscene reference across platform
versions; its headings do not provide a complete Level/Act gameplay transcript.

The four L1A1–L1A4 display names come from the user's supplied labels and retain
user_supplied provenance. Other level codes come from filenames. They do not
establish playback conditions. ASR similarity ranks candidates; known section
scope filters them, and sequential order only breaks ties. Acceptance requires
score >= .87 and a margin >= .08 over the next candidate. Ambiguous and short
matches remain unresolved. Reference matching never becomes verified by itself.

The completed registry contains all 3,661 clips: 991 dialogue/movie entries,
2,663 SFX and seven music streams. Parsing produced 225 reference lines and
five accepted whole-clip candidates. ASR remains the text source: 918 entries
have draft recognized speech and 73 have no_speech_detected status. Matching
does not overwrite drafts with the entire online transcript or assert all
speakers and lines are now verified. Movie soundtracks can contain multiple
speakers; assigning their entire track one speaker would be misleading.

The required file, level, level_name, speaker, text and trigger keys are present
alongside text_status, reference candidates/scores, speaker provenance, native
references, original clip identity and WAV digest. Empty trigger means unknown.
The recovered 29 PlayXA commands still map to 28 clips with concrete native
event references. Full surrounding guards remain unreconstructed. The script
does not invent ON_LEVEL_START from filename order. Stan_00's remake intro role
is an authored scene binding, distinct from verified original trigger logic.

## Runtime flow and controls

Game -> New Game -> IntroCinematic -> MissionBriefing -> GameplayActive ->
ordered rooftop checkpoints -> bank encounter -> cleared skylight -> Complete.
Intro natural completion and explicit skip emit intro_finished, which alone
begins the authored briefing. Movement, combat, objective HUD and competing
speech are locked throughout the intro. The briefing remains control-locked
until Enter/A. Dialogue owns an exclusive cinematic token; stopping restores
the user's configured music volume after ducking. Only verified text is emitted
as character-attributed subtitles. Draft data remains available in the registry.

Keyboard/mouse: WASD move, mouse look, Space jump, RMB hold swing, Q zip, C wall
latch/release, F melee, R web, E skylight, Escape pause. Gamepad: left stick
move, right stick look, A jump/briefing advance, RT swing, RB zip, LB crawl,
X melee, Y web, B skylight, Start pause. Menu B cancels. Mission pause uses a
separate action so B interaction does not open pause accidentally.

Swing anchors require a real world-collision raycast, clear line of sight,
height/range checks and a live collider. The CharacterBody3D moves along a
fixed-radius pendulum with tangent velocity, gravity, damping and pump input.
Swept collision releases the rope. Zip also uses swept collision. Wall latch
casts from the player's chest, and crawl slides along the recovered normal.
SpringArm3D sphere casting handles camera collision. Player and enemies use
box collision shapes, with no capsules or visible whitebox geometry.

Audio uses Dialogue/Music/SFX buses, music ducking, positional impacts and
altitude/speed-driven SFX low-pass filtering. The five menu entries support
native Godot keyboard/gamepad focus. Original menu-bank 000/001 sounds are
bound for shift/confirm/cancel as authored roles. Options persist volumes,
Gallery reads original comic textures, Training is free rooftop practice,
and Load resumes the private remake checkpoint without replaying the intro.

## Validation and remaining limits

Validated locally on 2026-10-20: all 40 integration checks passed headlessly,
and all 40 passed separately with real Windows Vulkan rendering in Mobile and
Forward+ on the AMD Radeon RX 580 2048SP (Vulkan 1.3.260). The final rendered
error logs were empty. Six new Python pipeline tests and all 17 existing
reference-tool regressions passed. The complete prepare_godot.ps1 -Offline
-SmokeTest workflow was executed successfully. Runtime checks also inject real
menu key events, rather than only calling page methods.

Private reports: staging/validation/godot-mobile-smoke.json,
godot-forward_plus-smoke.json, godot-dialogue.json and godot-conversion.json.
Private captures: staging/validation/smoke/godot-menu.png, godot-intro.png,
godot-briefing.png, godot-rooftop.png and godot-mission.png. The rendered menu,
intro, supported rooftop spawn and completed encounter were visually inspected.
These reports and images are excluded from source control.

```powershell
& .\tools\third_party\audio-python\Scripts\python.exe -m unittest discover -s tools/pipeline/tests -v
.\tools\pipeline\run_godot.ps1 -Headless -SmokeTest
.\tools\pipeline\run_godot.ps1 -Renderer mobile -SmokeTest
.\tools\pipeline\run_godot.ps1 -Renderer forward_plus -SmokeTest
```

The runner writes staging/validation/smoke/godot-smoke.json and uses a separate automation
checkpoint. It checks menu/rig/clip imports, audio ownership and mixer restore,
natural and skipped intro completion, pause locks, supported spawn, movement,
actual airborne pendulum steps, fixed radius/tangent velocity, ordered objective
gates, web stun and mission completion. Objective progression uses controlled
teleports and direct damage calls; it is not a full human-played traversal or
combat benchmark. The old Unity suite remains evidence for the archived engine,
not proof that every feature has been ported. Wall mantle, combo polish,
original animation meanings, exact mission scripts, unlock rules, full dialogue
verification and sustained performance measurement remain work for later slices.

All original data, converted assets, downloaded transcript HTML, WAVs, ASR drafts,
reports, rendered captures and engine caches remain private and ignored. Only
source code, settings, synthetic tests and documentation are pushed. A public
source clone requires local archive conversion to assemble the original art.

Smoke PNGs and reports now live exclusively in staging/validation/smoke/. The launcher checks their six exact paths against every preservation target. Run tools/pipeline/run_regression.ps1 -Milestone <name> for Python, clean import and the full headless/Forward+/Mobile smoke and traversal matrix; private logs/reports live under staging/validation/regression/<name>/.


## Current milestone commands and results

Phase 0 deletion is complete. Preserve audit evidence remains private under
staging/migration_audit/. The shared converter digest is in
 tools/reference/pinned_tools.py; the Unity-only prepare_* scripts are removed.
The historical validation counts above describe the earlier promotion checkpoint.
Current results are 52 Python tests, 42 smoke checks (all original 40 plus two
native-audio checks) and 150 traversal checks on all three targets. The original
units gate remains 19/21, supporting only 28/146 landmarks.

With Godot's editor closed, rebuild exact audio identity catalogs and the registry:

```powershell
./tools/third_party/audio-python/Scripts/python.exe tools/reference/extract_native_voice_catalog.py
./tools/third_party/audio-python/Scripts/python.exe tools/reference/decode_sfx_ids.py
./tools/third_party/audio-python/Scripts/python.exe tools/pipeline/build_dialogue_manifest.py --offline
./tools/pipeline/run_regression.ps1 -Milestone audio-identities
```

The first command only statically reads the fingerprinted PC executable. SFX/KAT
inputs default to staging/raw_extracted/Extracted/data/data; use --directory to
select a different extraction. Catalogs remain private. Exact source path and
WAV SHA-256 joins produce 901 XA registry entries and 1,993 SFX entries; missing
KATs and invalid sample references stay unresolved. See behavior_ledger/audio_ids.md.
prepare_godot.ps1 now performs these identity steps before registry generation,
then uses the guarded launcher for import and route generation.

Landmark diagnostics retain the failing original gate:

```powershell
./tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/verify_units.gd
./tools/third_party/audio-python/Scripts/python.exe tools/reference/analyze_landmarks.py
./tools/third_party/audio-python/Scripts/python.exe tools/reference/prepare_landmark_geometry.py
./tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/verify_extra_geometry.gd
```

The first command intentionally exits nonzero while the original gate fails.
Per-node diagnostics and supplemental geometry observations are private:
staging/validation/units-landmark.json, units-extra-geometry.json and
staging/reference/landmark_geometry/manifest.json. The supplemental result never
replaces the original gate or enables TRG placement. No height or offset was fitted.

## Editor paths and public CI

The four root folders export_templates/, feature_profiles/, script_templates/
and text_editor_themes/ are Godot editor data, not project content. In Godot
4.7.2's editor/file_system/editor_paths.cpp constructor, a failed change to the
host data/config directory leaves the filesystem access object in its working
directory; the constructor still creates these four relative subfolders. This
explains the observed empty root folders after restricted editor/import launches.
The normal host %APPDATA%/Godot installation also contains the same four names.
See https://docs.godotengine.org/en/stable/tutorials/io/data_paths.html#self-contained-mode.

Both setup and run launchers now create the supported _sc_ marker beside the
pinned binary. Editor data/config/cache go to tools/third_party/godot-4.7.2/editor_data/
(or godot-4.7.2-linux/editor_data/), inside the ignored writable tool installation.
The four fallback names are explicitly ignored; empty leftovers can be removed
without moving project resources. Existing host editor preferences are untouched.

.github/workflows/synthetic-regression.yml runs without secrets or game assets:
Python synthetic fixtures, hash-pinned Linux Godot 4.7.2, guarded import and all
150 synthetic traversal assertions. ci_report.py rejects incomplete suites and
engine/script errors. CI explicitly reports the 42-check private-asset smoke
suite as skipped with a reason; the existing full local three-target smoke gate
continues unchanged. Evidence is uploaded as synthetic-validation.
Linux setup: python tools/pipeline/setup_godot_linux.py, then use the same
PowerShell run_godot.ps1 -Import / -Headless -TraversalTests launcher.
Versions, download hashes and licenses are recorded in THIRD_PARTY.md.

## Level offset audit

Run `tools/third_party/audio-python/Scripts/python.exe tools/reference/decode_level_offsets.py`
to CRC-check and decode the nine private archive offset members. Optional
`--archive`, `--geometry-directory` and private `--output` paths select inputs
and report location. The default report is staging/reference/level-offsets.json.
The tool validates the G-bank counts alongside each record key and applies no
transform. Its three public tests use synthetic text and compressed payloads.
The consumer/units remain unknown; a with/without geometry correction is blocked
by evidence, and the existing landmark gate remains mandatory. See
behavior_ledger/offset_files.md and behavior_ledger/units.md.

## Private original fonts

Run tools/pipeline/setup_font_tools.py with the pinned audio Python once (network
only for the version/hash-pinned wheels), then tools/pipeline/convert_fonts.py.
prepare_godot.ps1 includes both steps before import. FontTools/Pillow/Capstone
versions, hashes and licenses are recorded in THIRD_PARTY.md and the setup script.
The converter CRC-checks six embedded FNT and five independent BMP members.
Outputs remain private in assets/ui/fonts/: original/4x nearest BMFont resources,
palette-layer traced TrueType resources and font_manifest.json. Every variant's
advances are checked exactly; every color contour's area must match its source
pixels. Tracing failures fail conversion and are recorded per glyph.

Private comparisons: staging/validation/fonts/<font>-<width>x<height>.png at
1280x720, 1920x1080 and 2560x1440. These use neutral sample text and are generated
from decoded original pixels, Faithful and Enhanced variants. Source sheets are
inventoried separately: their glyph-rectangle mapping is unverified. No original
font or derived image is committed. fonts.md documents recovered facts, the
bounded executable mapping audit and authored UI roles.

The initial import after creating the new fonts produced two dummy-renderer
texture errors; it is retained as failed evidence in regression/fonts-pipeline/.
An unchanged import recheck and the complete fonts-pipeline-final matrix then
passed with zero errors (58 Python, 42 smoke and 150 traversal on each target).
Do not describe this as a proven zero-error first import from an empty cache.
