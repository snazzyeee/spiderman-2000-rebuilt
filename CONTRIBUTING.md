# Contributing

This is an unofficial, community-driven reimplementation project for Spider-Man (2000) using Godot 4. Contributions to the independently authored code, tools, documentation, and synthetic tests are welcome.

## Before you start

- Read [README.md](README.md), [the Godot migration guide](docs/GODOT_MIGRATION.md), and [the repository workflow](docs/RepositoryWorkflow.md).
- Open an issue describing substantial changes before implementing them. Keep pull requests focused and explain the behavior they change.
- For bug reports, include steps to reproduce, your Godot version, renderer, and relevant logs without personal paths, tokens, or proprietary data.
- Do not submit original game files, extracted or converted assets, copyrighted audio, textures, models, dialogue, game binaries, proprietary code, or files obtained from unauthorized sources.
- Do not submit verbatim code from unlicensed third-party reverse-engineering repositories. Document sources and license compatibility for all external code.

## Setting up

This repository contains the Godot project, source code, conversion tools, and synthetic tests. The source tree can be inspected and the synthetic test suite can be run without original game assets.

On Windows, with the documented tooling installed:

```powershell
./tools/pipeline/run_godot.ps1 -Editor
./tools/pipeline/run_godot.ps1 -Headless -TraversalTests
```

To run the playable asset-backed slice locally, you must supply your own legally obtained compatible game files and follow [the migration guide](docs/GODOT_MIGRATION.md). We cannot supply original game data.

## Pull requests

1. Make the smallest practical change and include or update relevant tests.
2. Run the synthetic regression checks described in [README.md](README.md). Report what ran and what you could not test.
3. Update documentation when behavior, setup, or supported platforms change.
4. Avoid committing generated artifacts, secrets, local caches, binaries, and source-game data. Review the entire diff before pushing.
5. Explain any tradeoffs and remaining limitations. Do not claim mechanical fidelity without evidence.

CI exercises only publicly reproducible synthetic scenarios; a green CI run does not establish fidelity to the original game.

## Scope and conduct

Focus on original reimplementation work, mechanics, accessibility, portability, documentation, validation, and tooling. Be respectful in code reviews and issues. Maintainers may close contributions that create intellectual-property or security risks.
