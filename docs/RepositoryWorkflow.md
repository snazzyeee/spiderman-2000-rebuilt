# Repository workflow

Work on a branch and open a pull request against `main`; CI must pass before
merging. Keep commits focused, record validation and remaining limits in the PR,
and describe failed or skipped checks accurately — never label them as passed.
Do not force-push shared branches.

Track authored source: Godot scripts, scenes, resources and project settings,
Python tools and tests, and reviewed documentation.

Original game packages and executables, proprietary or converted assets,
extracted content, private reference indexes and logs, downloaded tools, Godot
caches, builds and test artifacts must remain untracked. `.gitignore` covers the
standard locations (`assets/`, `staging/`, `tools/third_party/`), but review the
staged paths and diff before every push.

The project opens and its synthetic tests run without game files; the
asset-backed slice requires the local conversion described in
[GODOT_MIGRATION.md](GODOT_MIGRATION.md).
