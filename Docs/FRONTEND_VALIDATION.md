# Frontend and audio validation — 2026-10-20

## Reproduction and environment

Run the private asset preparation in MAIN_MENU.md, then:

    python -m unittest discover -s Tools/Reference/tests -v
    .\Tools\Build\build_frontend.ps1 -RunTests -SmokeTest

Unity 6000.6.6f1 (d20c9ac777da), Windows x64 Mono development build, URP 17.6.0,
Input System 1.20.0, Cinemachine 6.6.0 and bundled Timeline 6.6.0.
Rendered player checks use the local AMD Radeon RX 580 2048SP at 1280x720.
The standalone runner injects a virtual gamepad and uses a separate test save.
It does not overwrite the interactive player's mission save.

## Audio extraction

The complete local run produced 3,661 nonempty 16-bit PCM WAVs, preserving
sample rates and channel counts: 2,663 SFX, seven music streams and 991
dialogue/movie entries. Reported decode failures, unsupported audio inputs and
carriers without audio streams were all zero. WAV payload totals approximately
598 MB. The Unity database imports all 3,661 stable clip identities.

Eight standalone Python tests cover archive CRC/path/length validation,
bounded embedded stream carving, unknown metadata, evidence-backed override
requirements, actual PCM conversion and invalid-cache regeneration.

All 991 dialogue/movie entries still require complete verified semantic
annotation. Filename conventions and numeric TRG operands do not establish
exact transcripts or playback semantics. The completeness gate intentionally
rejects this state. Authored menu SFX roles and the Stan_00 intro association
are distinguished from verified original behavior in the private manifest.

## Unity and standalone coverage

Validated results: 36 Unity tests passed (10 EditMode, 26 PlayMode), with zero
failures or skips; eight Python tests passed. The rendered standalone player
passed 26 checks with an empty error list. Its complete directory is about
273 MB; extracted WAVs and Unity caches are additional local storage.

The Unity suite includes the earlier traversal, combat, mission, Timeline and
camera checks plus dialogue registry validation, subtitle/audio ownership,
corrupt/incompatible saves, gamepad menu navigation, actual mixer gain changes,
gallery/controls content, credits pagination, original intro playback, skip
cleanup and checkpoint loading with cleared encounter state.

The rendered standalone runner checks the requested five branches, configured
original character and audio, mixer buses, absence of capsule scaffolding,
gamepad navigation/submit/cancel, persisted volume changes, gallery content,
credits directive parsing, controls layout, narrated New Game, skip cleanup,
checkpoint persistence and Load Game without replaying the intro.

The main menu, Options, Gallery, Credits and Controls captures were inspected.
The credits use 22 layout-derived pages. Submenu descriptions are confined to
the left area, and reading pages hide that description to keep their text clear.

Local machine-readable results and private images:

- TestResults/frontend-editmode.xml and frontend-playmode.xml
- TestResults/menu-smoke.json
- TestResults/menu-private.png, menu-private-options.png,
  menu-private-gallery.png, menu-private-credits.png and menu-private-controls.png
- Logs/frontend-*.log and Logs/menu-smoke.log

Build: Builds/Frontend/SpiderManRevamp.exe with its accompanying data/runtime
files. Keep the complete directory together. The build and all original-derived
images/audio/scenes/fonts remain private and ignored by Git.

## Practical limits

These are functional and rendered checks, not a sustained frame-time benchmark
or a guarantee of bug-free play. Audio was decoded and routed successfully;
the full corpus has not been manually listened to or transcribed.

The visible mission briefing is authored text. It is not a transcript of the
original Stan recording. The menu implements the requested modernized five
branches with original art/audio and procedural texture detail; it does not
establish pixel parity, original unlock rules, every original submenu or a
high-resolution repaint of all source art. Training uses the remake rooftop
practice mode. Original mission scripting, placement, animation meanings and
mechanical timing remain unverified.

A public source-only clone lacks proprietary assets, generated scenes and font
copies. Private integration tests explicitly skip when their inputs are absent.
Successful checks on this local private player do not mean an asset-free clone
contains a redistributable original game.
