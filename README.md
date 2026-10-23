# Spider-Man 2000 Rebuilt — Godot 4

[![Synthetic regression](https://github.com/snazzyeee/spiderman-2000-rebuilt/actions/workflows/synthetic-regression.yml/badge.svg?branch=main)](https://github.com/snazzyeee/spiderman-2000-rebuilt/actions/workflows/synthetic-regression.yml)

An unofficial, work-in-progress reimplementation of Spider-Man (2000, PC) in
**Godot 4.7.2**. This is a fan project. It is not affiliated with or endorsed by
Marvel, Activision, Neversoft or the original game's rights holders.

**This repository contains no game files.** It holds independently written
Godot code, conversion tools, synthetic tests and documentation. Original
models, textures, audio, dialogue, fonts and executables are never committed and
must not be submitted. The playable slice needs game files you supply yourself
from a copy you legally own (see [Asset pipeline](#asset-pipeline-optional)).

## Project status

Early development. What exists today:

- **Traversal systems** — web swing, zip, wall/ceiling crawl, camera profiles,
  webbing meter and damage rules — covered by 150 synthetic checks that need no
  game files.
- **Frontend** — main, options, pause and loading screens driven by
  `src/ui/layout/screens.json`, with layout, font-fallback and menu-audio tests.
- **Mission 1 rooftop slice** — playable *only* with locally converted original
  assets. The authored route, animation bindings and tuning are approximations,
  not proven original behaviour.

Known gaps: dialogue transcripts are unverified machine drafts; trigger (TRG)
placement is unverified; the original-landmark alignment gate passes 19 of 21;
no packaged release exists; only Windows has been used for asset-backed play.
The [behavior ledger](docs/behavior_ledger/README.md) records what is recovered
evidence and what is authored guesswork.

## What works without game files

On a fresh clone with no game files:

- the project imports and opens in Godot 4.7.2;
- `scenes/main_menu.tscn` launches (verified headless; original art, fonts and
  sounds are simply absent);
- all Python tests and Godot synthetic suites below run, with asset-dependent
  checks reported as explicit skips.

You can develop and test gameplay systems, UI layout, tooling and docs this way.

## Quick start

The launcher scripts download a hash-verified official Godot 4.7.2 into the
ignored `tools/third_party/` folder.

**Windows (PowerShell):**

```powershell
./tools/pipeline/run_godot.ps1 -Editor                      # open the editor
./tools/pipeline/run_godot.ps1                              # run the game
./tools/pipeline/run_godot.ps1 -Headless -TraversalTests    # synthetic traversal tests
```

**Linux (needs Python 3.11+ and [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-linux)),**
as used by CI:

```bash
python3 tools/pipeline/setup_godot_linux.py
pwsh ./tools/pipeline/run_godot.ps1 -Headless -TraversalTests
```

macOS has no launcher yet. With your own Godot 4.7.2 you can open the project
folder directly; this path is untested.

## Running the tests

```bash
python -m pip install --require-hashes -r tools/pipeline/ci-requirements.txt
python -m unittest discover -s tools/reference/tests
python -m unittest discover -s tools/pipeline/tests
```

Godot suites (prefix with `pwsh` on Linux):

```powershell
./tools/pipeline/run_godot.ps1 -Import
./tools/pipeline/run_godot.ps1 -Headless -TraversalTests
./tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/font_tests.gd
./tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/menu_audio_tests.gd
./tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/layout_tests.gd
./tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/design_qa_tests.gd
./tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/loading_tests.gd
```

At the time of writing: 81 Python tests; 150 traversal, 15 font, 24 menu-audio,
90 layout, 1,028 design-QA and 6 loading checks. CI
([`.github/workflows/synthetic-regression.yml`](.github/workflows/synthetic-regression.yml))
runs exactly these on Ubuntu. A green CI run shows the synthetic contracts hold;
it does not establish fidelity to the original game.

## Asset pipeline (optional)

To play the Mission 1 slice you convert files from your own copy of the PC game.
The tools only read data files; they never run the original game or installer.
[docs/GODOT_MIGRATION.md](docs/GODOT_MIGRATION.md) documents the commands,
tool setup, outputs and limits. Converted output goes to the ignored `assets/`
and `staging/` folders and must never be committed.

The pipeline was developed against a single PC build. Some reference tools check
the executable's SHA-256 and refuse unknown builds; other editions are untested.

## Documentation

- [Godot setup and asset pipeline](docs/GODOT_MIGRATION.md)
- [Behavior ledger](docs/behavior_ledger/README.md) — recovered evidence vs. authored choices
- [Controls](docs/Controls.md) · [Main menu](docs/MAIN_MENU.md) · [Dialogue architecture](docs/DIALOGUE_ARCHITECTURE.md)
- [Third-party tools and licences](docs/THIRD_PARTY.md)
- Historical: [Unity-era mission guide](docs/MISSION1.md), [Phase 2 report](docs/PHASE2_REPORT.md),
  [Unity purge report](docs/migration/PURGE_REPORT.md). The project began in Unity;
  that code is kept for reference in [docs/gameplay_references](docs/gameplay_references/README.md)
  and on the `archive/unity-pre-migration` branch.

## Contributing

Contributions to original gameplay code, tools, tests and documentation are
welcome — see [CONTRIBUTING.md](CONTRIBUTING.md). Report bugs and ideas through
GitHub Issues; report security problems as described in [SECURITY.md](SECURITY.md).

## Licence

Code and documentation written for this project are released under the
[MIT licence](LICENSE). The licence does **not** cover Spider-Man characters or
trademarks, original game assets, extracted or converted game data, or
third-party tools and code, which remain the property of their respective owners
and subject to their own terms (see [docs/THIRD_PARTY.md](docs/THIRD_PARTY.md)).
Nothing here grants any right to distribute original game content.
