# Reverse engineering and reference-material strategy

## Which "reverse engineer anything" repository is relevant?

### Takeaway
The user has confirmed [morluto/rea](https://github.com/morluto/rea). Treat it as an optional investigation interface, not a game engine, automatic complete-game converter, or recovery of the original development project.

### Cited Findings
- The official README identifies `morluto/rea`, `rea.tools`, and the `rea-agents` npm package as official sources and warns that matching names/copied READMEs do not prove provenance. Native analysis returns pseudocode, assembly, strings, symbols, calls and references; deeper analysis requires a supported Hopper, Ghidra, or IDA installation. Analysis runs locally, but agent results still go to the model provider. — [Official REA README](https://github.com/morluto/rea)
- Windows native Ghidra support is explicitly experimental. Inspected documentation distinguishes repository-main support for native x86 (32-bit) PE from npm releases through 5.0.0, which admitted x86-64 PE on Windows. Verify the exact chosen release rather than projecting main's capabilities onto a downloaded package. Windows P0 does not imply general architecture/format parity; raw MIPS PS1 executables are not in its documented Windows admission boundary. — [Windows Ghidra P0](https://github.com/morluto/rea/blob/main/docs/windows-ghidra-p0.md)
- Current installation documentation describes exact-version persistent MCP registrations and separately supplied Ghidra/JDK installations; the agent integration is not the native analysis engine itself. — [REA installation](https://github.com/morluto/rea/blob/main/docs/installation.md)

### Inferences
- The user's confirmed repository matches the verified official project.
- First use existing game-specific research; add REA only for unanswered, bounded questions such as a movement constant, archive-loader branch, trigger opcode, or camera condition. Validate proposed explanations against observed gameplay and binary evidence.
- Pilot one small known function with a pinned REA/provider version before making it a project dependency. Do not install or execute unknown tooling merely because a repository name looks relevant. Nothing was installed or executed during this research.

### Gaps
- No Spider-Man (2000) binary was supplied or tested; no compatibility or analysis-quality claim for REA on this particular game has been established.
- Exact npm release applicability must be checked when installing; the repository documents a moving main-versus-release boundary.

## What existing game-specific work can reduce uncertainty?

### Takeaway
There is useful prior work, including a PC decompilation, extraction/modding tools, and a PS1 native recompilation. Their existence does not establish a complete, stable, modern remake foundation.

### Cited Findings
- `krystalgamer/spidey-decomp` describes itself specifically as a decompilation of the **PC port**. Its minimal README provides no completeness or ready-to-play release guarantee. — [PC decompilation repository](https://github.com/krystalgamer/spidey-decomp)
- Its Windows CMake configuration rejects non-32-bit builds, relevant when evaluating a native binary-analysis provider. — [CMakeLists.txt](https://raw.githubusercontent.com/krystalgamer/spidey-decomp/master/CMakeLists.txt)
- The author's March 2025 checkpoint reports 1,306 `@Ok`, 196 `@NotOk`, and additional TODO categories. These are dated status tags, not a present-day completion percentage. The same primary account identifies **LTI Gray Matter** as the PC port developer and describes Windows inlining versus the Mac build. — [March 2025 checkpoint](https://krystalgamer.github.io/spidey-decomp-status-march/)
- The author's May 2024 account says the Macintosh executable contains symbols, including function names and parameter information, and THPS2 PlayStation demos have related-engine symbols/structure offsets. The author explains why matching original behavior preserves bugs and why decompilation takes substantial manual work. — [May 2024 checkpoint](https://krystalgamer.github.io/spidey-decomp-status-may/)
- `spidey-tools` includes PKR extraction, extraction of some PSX files, texture repacking, save editing, texture loading, and experimental TRG disassembly/dumping. Its tree also contains exploit research, so tool selection must be deliberate rather than running the entire repository. — [spidey-tools README](https://github.com/krystalgamer/spidey-tools)
- `slfx77/neversoft-multitool` documents Spider-Man PC PKR3 archives, PSX-family geometry to glTF, Spider-Man PSX animations to animated glTF, and TRG scripts/triggers to JSON. Support is content/format gated; do not assume every platform, file or record is covered. These are author-documented capabilities, not results verified on the user's files. — [Neversoft Multitool](https://github.com/slfx77/neversoft-multitool)
- Neversoft Multitool source is MIT licensed, with a separate GPL notice for distributions bundling Blender. That licenses the tool's code; it is not evidence of permission to redistribute extracted game content. — [License](https://raw.githubusercontent.com/slfx77/neversoft-multitool/main/LICENSE.md)
- `GTTeancum/OpenSpideyPS1` redirects to `GTTeancum/SpideyPS1recomp`. The project documents PS1 native recompilations, USA disc-image setup, modernized rendering, and original PS1 environments. It explicitly says both games reach gameplay but targeted tests do not replace full playthroughs or long-duration stability checks. — [SpideyPS1recomp](https://github.com/GTTeancum/SpideyPS1recomp)
- A separate small patcher project identifies the PC executable as `SpideyPC.exe` and publishes hashes for its own retail and modified supported builds. This is evidence of build-specific compatibility; it does **not** establish which build the PC decomp targets. No recommendation to run this patcher is made. — [Spiderman2001Patcher](https://github.com/J0-o/Spiderman2001Patcher)

### Inferences
- A modern rebuild should take behavioral specifications and reference geometry from this work where appropriate, then implement redesigned controls, simulation and rendering in a modern engine. A matching decomp/recomp primarily preserves old behavior; new graphics/UX/performance requirements still require engineering.
- Recompilation could be a separate preservation/remaster route if the user ultimately wants original gameplay with rendering improvements. It should not silently be chosen for a remake with new locomotion, camera, collision and modern asset production.
- Review licenses at the exact pinned commit before copying code. Keep imported third-party code in an explicit dependency area with notices. A repository being public does not by itself establish a suitable reuse license. These notes do not establish a reuse license for the PC decomp or PS1 recomp.

### Gaps
- No independent build, playthrough, extraction test, fuzzing, stability test, or license audit of these projects was performed.
- No evidence found here establishes complete scene conversion, exact animation fidelity, complete trigger semantics, or production readiness for a modern rebuild.
- The PC decomp README, CMake configuration, build batch file, and inspected author progress accounts did not establish an exact executable SHA-256/region/release compatibility matrix. The batch file selects a Win32 Release build, not an input-game version. Do not assume another mod's supported hashes apply. — [build.bat](https://raw.githubusercontent.com/krystalgamer/spidey-decomp/master/build.bat)

## Which game copy/files are useful, and how should references enter the project?

### Takeaway
Source code is not required to start: a legitimately available playable copy and its local binary/data can support observation, format research and targeted analysis. Prefer the version the user remembers as the fidelity reference; use a PC installation as a convenient research candidate and a PS1 copy when PS1 behavior is the target.

### Cited Findings
- The PC decomp is specifically a PC-port effort, while the native recomp explicitly imports USA PlayStation BIN/CUE or ISO data. These are different implementation/reference paths and their inputs must not be conflated. — [PC decomp](https://github.com/krystalgamer/spidey-decomp); [PS1 recomp](https://github.com/GTTeancum/SpideyPS1recomp)
- Neversoft Multitool separately documents PC PKR3 archives and PS1/Dreamcast/Windows PSX geometry variants; sharing a filename extension is not proof of identical file semantics. — [Formats](https://github.com/slfx77/neversoft-multitool)
- Native REA analysis returns derived code representations and evidence, and the author's game-decomp accounts document work needed to infer structures, compiler effects and correctness. These do not imply original comments, source tree, art masters or design intent are recoverable. — [REA](https://github.com/morluto/rea); [decomp account](https://krystalgamer.github.io/spidey-decomp-status-may/)

### Inferences
- Given the confirmed full-game/faithful-gameplay objective and solo programmer with no game-development experience, use **one canonical behavior reference** rather than mixing convenient behavior from several ports. Ask which platform they played: PS1, Windows PC, Dreamcast, N64, or Mac. If they mean Neversoft's original PS1 experience, use that PS1 release for behavior and the PC installation plus existing research as supplementary analysis inputs. Request the PC copy first only for the extraction spike; do not silently redefine PS1 fidelity as PC-port fidelity.
- Mac is an optional advanced research aid because symbols survived; not a required purchase or prerequisite to start. Existing research already exposes much of that benefit. The author's first-hand account explicitly discusses PS1, N64, Dreamcast, Mac and Windows comparison. — [Mac-symbol investigation](https://krystalgamer.github.io/spidey-apple/)
- Begin with **inventory only**, not an upload of the whole game: platform; region/language; edition/patch; original versus modded status; directory listing with relative paths, byte sizes and SHA-256 hashes; filenames and hashes for the main executable, companion DLLs and data archives. Record PE machine/bitness after inspecting the actual PC binary. Do not invent an expected executable/archive basename before seeing the directory listing.
- For PC, inventory `*.exe`, `*.dll`, `*.pkr`, loose `*.psx`, `*.trg`, texture/audio/video files, and relevant configuration/save files **if present**. `binkw32.dll` is specifically named by the existing mod loader's instructions, but presence is not guaranteed across editions. — [Loader instructions](https://raw.githubusercontent.com/krystalgamer/spidey-tools/master/INSTRUCTIONS.txt)
- For PS1, inventory the local disc image filename(s), size(s), SHA-256, CUE track layout if present, region/disc ID and `SYSTEM.CNF` boot target where available. A CUE without its referenced BIN tracks is insufficient. Do not acquire BIOS or disc images from random sources for this task.
- Keep the untouched reference copy outside source control; make a read-only hashed working snapshot. Review/pin extraction tools, run only the narrow parser/converter needed, write outputs to a disposable directory, enforce file-size/bounds limits, and never let extraction overwrite the original installation. Runtime observation can follow in an isolated test environment after static inventory.
- Build a provenance manifest for every derived artifact: `source_build_id`, source-relative path, source SHA-256, tool name/version/commit, command/config, conversion schema version, output SHA-256, notes/confidence, and usage/rights category. Separate original assets, derived research outputs, third-party code and newly authored production content. Git should hold code/specifications/manifests, with original game content excluded.
- Extracted low-poly geometry and authored animation/level data are useful scale/layout/behavior references. They cannot supply missing high-detail sculpts, modern rigs, physically based materials, or contemporary animation polish; plan a separate modern art pipeline.
- Maintain a behavior ledger: observed behavior, reproduction steps/video, measured timing, evidence source, uncertainty, intended remake behavior and regression test. Preserve only behavior the remake intends; record deliberate fixes so original bugs are not accidentally treated as requirements.

### Gaps
- The user confirmed faithful original gameplay/levels with modern visuals, the whole original game, solo work, strong programming but no game-development experience, and no budget/timeline. Remembered/canonical platform, intended distribution and exact files remain unknown. No game copy is needed to choose the high-level architecture now; files become useful for the first reference/extraction spike.
- An exact expected-hash list or supported-disc edition cannot be given without selecting the relevant tool/version and user reference copy.
