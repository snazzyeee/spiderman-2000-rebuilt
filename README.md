# Spider-Man 2000 revamp — Godot 4

The active engine is **Godot 4.7.2**, with Mobile as the default renderer and
Forward+ available through the launcher. The project contains a playable
Mission 1 rooftop slice using locally extracted original PC models, textures
and audio. Mechanical fidelity and complete dialogue verification remain goals.

The former Unity project is retained under Game/ as an implementation reference.
Unity is not required to generate the new Godot route, import GLBs or run the
Godot game. Historical Unity build instructions remain in [Docs/MAIN_MENU.md](Docs/MAIN_MENU.md).

## Play or open the prepared local project

From this repository root:

```powershell
./Tools/pipeline/run_godot.ps1
./Tools/pipeline/run_godot.ps1 -Renderer forward_plus
./Tools/pipeline/run_godot.ps1 -Editor
```

The main scene is Godot/scenes/main_menu.tscn. Game -> New Game plays the original
intro audio, then a separate authored briefing, then the rooftop mission.
Gamepad and keyboard/mouse controls, menu navigation, volume settings, original
comic gallery, checkpoint loading and a free rooftop practice mode are included.

## Reproduce the private assets

With Godot closed, the existing local PC archive and extraction/ASR data present:

```powershell
./Tools/pipeline/prepare_godot.ps1 -Offline -SmokeTest
```

Omit -Offline to fetch the supplied transcript reference again. The workflow
uses the pinned converter, CRC/SHA-checked inputs, requests/BeautifulSoup,
FFmpeg, the isolated Python environment and a hash-verified portable Godot.
It never executes original game programs. No packaged Windows export was made;
the prepared source project runs with the portable editor/runtime.

[Docs/GODOT_MIGRATION.md](Docs/GODOT_MIGRATION.md) documents exact commands,
directory structure, component connections, controls, manifest fields,
validation and remaining limits.

## Generated outputs and evidence

- Godot/assets/audio/dialogue_manifest.json: all 3,661 clips with logical Godot
  paths, ASR/reference status, speaker evidence and native trigger references.
- Godot/assets/models/: 107 exported GLBs, including 105 rigged assets and
  low-resolution variants, plus two original rooftop geometry files.
- Godot/assets/ui/: 34 original menu/comic images converted to PNG.
- Godot/assets/world/mission1_layout.json: a three-checkpoint authored route
  derived directly from Godot collision queries over the original geometry.

The player preserves 18 bones and 300 animation slots. Numeric animation
bindings and the authored mission route are not proof of original gameplay
semantics. The transcript page yielded 225 parsed reference lines and five
strong whole-clip candidates. The 991 dialogue/movie entries retain 918 draft
ASR transcripts and 73 no-speech results; complete verified transcripts,
speaker labels and original trigger guards have not been recovered. The user
supplied L1A1-L1A4 display names retain that provenance. Filename ordering does
not invent playback conditions. See [native ID evidence](Docs/NATIVE_VOICE_IDS.md),
[local ASR](Docs/BATCH_AUDIO.md) and [external reference scope](Docs/REFERENCE_SOURCES.md).

## Public source and private data

Validation: 40 Godot integration checks passed headlessly and on both real
Mobile and Forward+ Vulkan renderers on the RX 580. All 23 Python pipeline and
reference-tool tests passed. See the migration guide for test scope and limits.

Godot scripts/scenes/project settings, Python conversion tools, synthetic tests
and documentation are tracked. Original and derived assets, transcripts,
private manifests, caches, models, tools, captures and builds stay ignored.
A public source clone needs locally supplied PC files and conversion to assemble
original art. There is no embedded proprietary game in the public repository.

[Repository workflow](Docs/RepositoryWorkflow.md) ·
[Godot migration guide](Docs/GODOT_MIGRATION.md) ·
[Archived Unity mission guide](Docs/MISSION1.md)
