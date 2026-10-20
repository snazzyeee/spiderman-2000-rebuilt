# Active Godot menu typography

The five root entries remain Game, Options, Training, Special and Gallery.
Original art currently used: converted shell_bg and the original Spider-Man
rig; comic panels are available through the existing gallery flow. Lighting,
layout, title text, focus style, selected idle and remake UI-role assignments
are authored. Dynamic fly-bys, parallax, new transitions, attract mode, expanded
gallery/costume tools, 1080p layout and performance capture remain pending.
No Gemini style guide has been read or applied by this font milestone.

Original embedded fonts now supply title/menu/HUD/captions (sp_fnt00) and
subtitles (sp_fnt02). These role choices are authored, with structural/font
facts in behavior_ledger/fonts.md. font_big is decoded but its specialized
character binding is unresolved and it is not used for the mixed title/year.
Options selects Faithful (4x nearest original pixels) or Enhanced (palette-layer
TrueType outlines), default Enhanced; settings persist and update live. Missing
private fonts and unsupported characters use the existing fallback.

Generate private resources with convert_fonts.py (via prepare_godot.ps1).
All font assets, palette manifests and 720p/1080p/1440p comparisons remain under
assets/ and staging/validation/fonts/. No original font/art/text is tracked.
Run run_regression.ps1 for all Python, smoke, traversal and font checks on the
three targets. The original 42 smoke checks are retained; two font checks add
to 44. The separate font suite has 29 checks with assets or 10 plus an explicit
private-resource skip without assets. This is not a 60 fps menu benchmark.

## Historical Unity menu (superseded)

> Historical Unity workflow: its prepare_* scripts have been removed after preservation and purge. Use tools/pipeline/prepare_godot.ps1 and docs/GODOT_MIGRATION.md for the active Godot pipeline.

# Main menu, original audio and Mission 1 entry

## Reproduce the private player

Use Unity 6000.6.6f1. Close the project before batch commands. Put data.pkr and
media.pkr under LocalReference/PC/app. The original executables are never run.

    .\Tools\Reference\setup_converter.ps1
    python Tools/Reference/prepare_phase1_assets.py
    python Tools/Reference/prepare_mission1_assets.py
    .\Tools\Build\build_frontend.ps1 -ExtractAudio -RunTests -SmokeTest

FFmpeg/FFprobe must be on PATH. Subsequent builds can omit -ExtractAudio.
The preparation tool copies verified original BMP/text assets and a local
display font, and safely unpacks the editor-bundled TMP essentials. No interactive
package-import window is needed. For a custom font/editor path, see the
--font and --tmp-package options of prepare_frontend_assets.py.

Open Assets/_LocalReference/Frontend/MainMenu.unity. Local executable:
Builds/Frontend/SpiderManRevamp.exe, together with its data/runtime files.
Rebuild > Main Menu provides assembly/build commands. The builder preserves an
existing private menu scene; Inspector art/layout edits are not silently replaced.

## Concrete files and Inspector wiring

| Component | Setup |
|---|---|
| DialogueDatabase | Private manifest TextAsset plus stable-ID/AudioClip bindings, created by DialogueAssetBuilder |
| DialogueManager | Persistent service prefab, shared mixer and Dialogue/SFX/Music groups |
| MainMenuManager | Five MainButtons in Game/Options/Training/Special/Gallery order; MainRoot, PageRoot, Rows; row/slider prefabs; title/description/status/body labels; gallery textures/credits and transition CanvasGroup |
| InputSystemUIInputModule | One EventSystem; AssignDefaultActions; D-pad/stick/keyboard navigation, submit and cancel |
| MenuSelectionEffect | Selectable, TMP label, optional web underline Image, description and owning manager |
| MainMenuBackdrop | Original posed character root and background web RectTransform |
| MissionMenuBridge | MissionManager on the same director; consumes launch mode, applies camera preferences and saves safe checkpoint state |
| MissionHud | Pause/completion Main Menu buttons are shown only when the menu scene is in the build |

All component references and generated private prefabs/materials are already
wired by MainMenuSceneBuilder. Runtime code uses no editor APIs. The TMP and
UGUI assembly references come from the pinned UGUI package, not an extra legacy
TextMeshPro installation.

## Navigation and actual behavior

    Main
      Game -> New Game -> fade -> narrated Timeline -> separate briefing -> gameplay
           -> Load Game -> saved checkpoint, without replaying intro
      Options -> Master/Music/SFX/Dialogue volume, mouse look speed,
                 invert Y and fullscreen
      Training -> Rooftop Practice / Controls
      Special -> Music Archive / Original Credits
      Gallery -> 32 original comic images, Previous / Next

Arrow keys, D-pad and left stick navigate; Enter/A submits; Escape/B returns.
Sliders use left/right. Mouse hover moves selection, and click submits. Selection
and submit play original samples from menu.kat. Options persist in PlayerPrefs.
Keyboard/mouse and gamepad gameplay bindings are listed in Controls.md.

The Special archive plays the extracted original music streams. Credits are
read from the original PC credits file, with its RGB directives converted to
TMP styling and layout-driven pagination. Gallery uses original cb00..cb31 art.
Training is a remake traversal-practice mode on the existing slice rooftops:
damage is disabled, the bank fight is bypassed and it does not overwrite the
normal save. It does not claim to reconstruct the original game's training levels.

SaveGameStore writes a versioned, atomically replaced mission-save.json under
Application.persistentDataPath. It rejects corrupt/incompatible data and never
touches the original game's saves. During combat the safe approach checkpoint
is saved; after combat the cleared flag is preserved. Load restores health,
energy, position and encounter clearance. Pause/completion can return to Main Menu.

## Narration and mapping honesty

New Game resolves the original data/Voice/stan_00.bik stream through the private
database. The Timeline spans its 32.35-second audio duration and routes it through
DialogueManager. Skip stops that narration owner and starts the separate authored
MISSION BRIEFING, with gameplay control still locked. Briefing completion then
enables gameplay. No objective/briefing UI is displayed over the recording.
See MISSION_LIFECYCLE.md for ownership, events and transport controls. The
briefing is not presented as a verbatim Stan Lee subtitle.

The menu-bank sample roles, theme selection and Stan_00-to-remake-intro association
are authored bindings to authentic extracted files. Original executable playback
semantics have not been established. See AUDIO_PIPELINE.md for evidence fields,
unresolved trigger commands and the verified-completeness gate.

## Art and fidelity

The menu uses the extracted, skinned Spider-Man model and an original numeric
animation pose sampled at assembly. Original albedo textures remain intact.
Modern detail is provided by a generated 1024-square fabric normal texture,
2048-by-1024 procedural web/halftone background, 2048-square SDF font atlas,
URP lighting, selection animation and ambient textured particles.

This is a modernized implementation of the requested five branches, not a
pixel-identical recreation. The original Windows manual separately lists New
Game, Continue and File System, with additional Records/Special/Gallery sections:
[original Windows manual](https://oldgamesdownload.com/wp-content/uploads/manuals/spider-man_win_manual_en_wb1.pdf).
The requested Game (New/Load) grouping is therefore an authored navigation choice.
Original logo/pose typography, every original submenu and unlock logic are not
claimed as reconstructed. The pose's original animation semantics are unverified.

The image-generation tool declined a proposed title-art modernization. No generated
illustration was used. All character art comes from the local original files;
the fabric/web textures are deterministic authored generators in the builder.

## Privacy and validation

Original/converted art, font copies, audio, manifests, generated database, menu
scene, Timeline, prefabs, textures, builds and screenshots remain ignored.
Editor-bundled TMP essentials are also restored locally and excluded from Git.
Only authored code, tests, settings and documentation are pushed.

See FRONTEND_VALIDATION.md for current tests, rendered checks and limitations.
