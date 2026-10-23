# Contributing

This is an unofficial, community-driven reimplementation of Spider-Man (2000) in Godot 4. Contributions to the independently authored code, tools, documentation and synthetic tests are welcome.

## Before you start

- Read [README.md](README.md) (status, setup, tests) and, if you touch the asset pipeline, [the Godot setup guide](docs/GODOT_MIGRATION.md).
- Open an issue describing substantial changes before implementing them. Keep pull requests focused and explain the behaviour they change.
- Bugs and feature ideas go in GitHub Issues using the templates. Security problems go through [SECURITY.md](SECURITY.md), not public issues.
- Follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## What you may submit

By opening a pull request you confirm that you wrote the contribution yourself (or otherwise have the right to submit it) and that it is licensed under the project's [MIT licence](LICENSE).

Do **not** submit:

- original game files or anything extracted or converted from them — models, textures, audio, dialogue or transcripts, fonts, cinematics, level data, executables or installers;
- screenshots or captures that reproduce substantial original artwork;
- code copied from decompilations or other reverse-engineering projects without a compatible licence, or from any source whose licence you have not checked;
- files obtained from unauthorised sources, or instructions for obtaining them.

Behavioural facts recovered for the [behavior ledger](docs/behavior_ledger/README.md) must cite their evidence and be reimplemented in your own code. Document the source and licence of any external code or data you rely on.

## Setting up

No game files are needed for development and testing. See [Quick start](README.md#quick-start) and [Running the tests](README.md#running-the-tests); Windows and Linux (with PowerShell 7) are supported by the launcher scripts.

The asset-backed Mission 1 slice additionally needs files from your own legally obtained copy of the PC game; follow [the setup guide](docs/GODOT_MIGRATION.md). Nobody in this project can supply game data.

## Pull requests

1. Make the smallest practical change and include or update relevant tests.
2. Run the Python tests and Godot synthetic suites. Report what ran and what you could not test.
3. Update documentation when behaviour, setup or supported platforms change.
4. Do not commit generated artifacts, secrets, local caches, binaries or game data. Review the whole diff before pushing.
5. Explain tradeoffs and remaining limitations. Do not claim fidelity to the original game without evidence.

CI runs only publicly reproducible synthetic scenarios; a green run does not establish fidelity to the original game.

## Scope

Focus on reimplementation work, mechanics, accessibility, portability, documentation, validation and tooling. Maintainers may close contributions that create intellectual-property or security risks.
