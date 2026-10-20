# Spider-Man 2000 revamp — Godot 4

The active engine is **Godot 4.7.2**, with Forward+ as the default renderer and
Mobile kept as a supported fallback through the launcher. The project contains a playable
Mission 1 rooftop slice using locally extracted original PC models, textures
and audio. Mechanical fidelity and complete dialogue verification remain goals.

The Godot project lives at the repository root (project.godot, src/, scenes/).
Unity is not required for anything. The former Unity project's gameplay code is
kept as reading reference in [docs/gameplay_references](docs/gameplay_references/README.md),
and its full history is on the archive/unity-pre-migration branch. Historical
Unity build instructions remain in [docs/MAIN_MENU.md](docs/MAIN_MENU.md).

## Play or open the prepared local project

From this repository root:

```powershell
./tools/pipeline/run_godot.ps1
./tools/pipeline/run_godot.ps1 -Renderer mobile
./tools/pipeline/run_godot.ps1 -Editor
```

The main scene is scenes/main_menu.tscn. Game -> New Game plays the original
intro audio, then a separate authored briefing, then the rooftop mission.
Gamepad and keyboard/mouse controls, menu navigation, volume settings, original
comic gallery, checkpoint loading and a free rooftop practice mode are included.

## Reproduce the private assets

With Godot closed, the existing local PC archive and extraction/ASR data present:

```powershell
./tools/pipeline/prepare_godot.ps1 -Offline -SmokeTest
```

Omit -Offline to fetch the supplied transcript reference again. The workflow
uses the pinned converter, CRC/SHA-checked inputs, requests/BeautifulSoup,
FFmpeg, the isolated Python environment and a hash-verified portable Godot.
It never executes original game programs. No packaged Windows export was made;
the prepared source project runs with the portable editor/runtime.

[docs/GODOT_MIGRATION.md](docs/GODOT_MIGRATION.md) documents exact commands,
directory structure, component connections, controls, manifest fields,
validation and remaining limits.

## Generated outputs and evidence

- assets/audio/dialogue/dialogue_manifest.json: all 3,661 clips with logical Godot
  paths, ASR/reference status, speaker evidence and native trigger references.
- assets/characters/ and assets/environments/: 107 exported GLBs, including 105 rigged assets and
  low-resolution variants, plus two original rooftop geometry files.
- assets/ui/: 34 original menu/comic images converted to PNG.
- assets/environments/mission1_layout.json: a three-checkpoint authored route
  derived directly from Godot collision queries over the original geometry.

The player preserves 18 bones and 300 animation slots. Numeric animation
bindings and the authored mission route are not proof of original gameplay
semantics. The transcript page yielded 225 parsed reference lines and five
strong whole-clip candidates. The 991 dialogue/movie entries retain 918 draft
ASR transcripts and 73 no-speech results; complete verified transcripts,
speaker labels and original trigger guards have not been recovered. The user
supplied L1A1-L1A4 display names retain that provenance. Filename ordering does
not invent playback conditions. See [native ID evidence](docs/NATIVE_VOICE_IDS.md),
[local ASR](docs/BATCH_AUDIO.md) and [external reference scope](docs/REFERENCE_SOURCES.md).

## Public source and private data

Validation: all 40 original integration checks plus two native-audio checks
(42 total), and all 150 synthetic traversal checks passed headlessly and on real
Mobile and Forward+ Vulkan on the RX 580. All 47 Python tests passed. Phase 0
Unity deletion is complete. The original landmark gate remains 19/21; TRG
placement is still unverified. See [current handoff](docs/HANDOFF.md) and the
[historical Phase 2 report](docs/PHASE2_REPORT.md).

Public synthetic traversal scenes require no original assets:

```powershell
./tools/pipeline/run_godot.ps1 -Headless -TraversalTests
./tools/pipeline/run_godot.ps1 -Renderer forward_plus -TraversalTests
./tools/pipeline/run_godot.ps1 -Renderer mobile -TraversalTests
```

Q / R1 targets a zip; T / R2 zips forward. These assignments, the pendulum rates,
wall attach input, edge probes, grace length and swing animation binding are
authored tuning. Recovered events bind through the behavior ledger JSON.

Godot scripts/scenes/project settings, Python conversion tools, synthetic tests
and documentation are tracked. Original and derived assets, transcripts,
private manifests, caches, models, tools, captures and builds stay ignored.
A public source clone needs locally supplied PC files and conversion to assemble
original art. There is no embedded proprietary game in the public repository.

[Repository workflow](docs/RepositoryWorkflow.md) ·
[Godot migration guide](docs/GODOT_MIGRATION.md) ·
[Unity purge report](docs/migration/PURGE_REPORT.md) ·
[Behavior ledger](docs/behavior_ledger/README.md) ·
[Archived Unity mission guide](docs/MISSION1.md)
