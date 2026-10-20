# Spider-Man PC rebuild

Solo Unity/C#/URP rebuild targeting the complete original Windows PC game,
faithful levels/gameplay, improved input/UX, and modern replacement art.

## Current state

- Architecture blueprint: `reports/Spider Man rebuild architecture.md`.
- Original installer/game have not been executed by this project workflow.
- Two PKR3 archive indexes generated: 2,448 data members and 25 media members.
- `data.pkr` extracted; all 2,448 members match source byte lengths and CRC32.
- Spider-Man and `L1A1_G` static geometry exported to GLB and rendered to PNG.
- `L1A1_T` parsed to JSON (292 reported nodes). Trigger semantics are unverified.
- Character GLB contains a skin, but no animation clips were exported yet.
- Unity 6000.6.6f1 is installed, signature/version checked, and licensed.
- Local URP project: `Game/`; laboratory scene:
  `Game/Assets/_Game/Scenes/TraversalLab.unity`.
- Placeholder keyboard/mouse movement, running, buffered/coyote jumps, camera
  obstruction handling, pause menu, fall/reset, goal beacon, and timing HUD exist.
- Three EditMode and eight PlayMode tests pass. Windows build validation is
  recorded in [Docs/Validation.md](Docs/Validation.md).
- The abandoned 6000.3.26f1 download, setup scripts, and follow-up automation
  have been removed. No 6.3 editor installation was found.

## Local reference boundary

`LocalReference/` holds proprietary originals and derived research artifacts;
it is ignored by Git. Downloaded installers and portable tool binaries are also
ignored. Keep these separate from newly authored production assets/code.
No-detection antivirus results do not establish that the original package is safe.
Do not execute the installer or game as part of asset conversion.

```text
LocalReference/
  PC/app/                 Original selected executable and archives
  Extracted/data/data/    Decoded data.pkr members
  Converted/Spidey/       spidey.glb (reference skin/static pose)
  Converted/L1A1/         Level GLB and parsed trigger JSON
  Previews/               Rendered reference PNGs
```

## Reproduce the reference checks

From this directory, with Python 3.11+:

```powershell
python Tools/Reference/pkr_index.py LocalReference/PC/app/data.pkr LocalReference/PC/app/media.pkr --output "research_notes/Spider Man rebuild architecture/pkr-index.json"
python Tools/Reference/verify_extraction.py --index "research_notes/Spider Man rebuild architecture/pkr-index.json" --archive data.pkr --root LocalReference/Extracted/data --output "research_notes/Spider Man rebuild architecture/extraction-verification.json"
```

The inventory parser reads metadata only. The verifier compares extracted bytes
to source lengths/CRC and records SHA-256. They never execute game content.

Asset conversion used [Neversoft Multitool v1.3.4](https://github.com/slfx77/neversoft-multitool/releases/tag/v1.3.4),
Windows CLI release SHA-256:
`062c89e00382e7b7c4b73015f228c88e7be34e50a4c48654d39e3142bd633d35`.
The downloaded package matched GitHub's release digest and Defender reported no
threats. These are provenance checks, not a comprehensive audit of that tool.

Logs and manifests are under `research_notes/Spider Man rebuild architecture/`.
PKR structure references: [spidey-tools](https://github.com/krystalgamer/spidey-tools/tree/master/pkr_extractor).

## Play the traversal laboratory

Add the **Game** subfolder to Unity Hub as an existing project, using 6000.6.6f1.
Open `Assets/_Game/Scenes/TraversalLab.unity` and press Play.
The Windows build location is `Builds/Windows/TraversalLab.exe`.
Keep the executable beside its generated data, DLLs and runtime folders.

WASD moves, mouse looks, Left Shift runs, Space jumps, R resets, and Escape
pauses/releases the mouse. Follow yellow markers to the cyan goal.
Full controls and limits: [Docs/Controls.md](Docs/Controls.md).

This is the first original placeholder course, not a completed Spider-Man level.
No proprietary game content is imported into its production assets.

## Build and architecture

Pinned editor: 6000.6.6f1 (d20c9ac777da). Direct package pins include URP 17.6.0,
Input System 1.20.0, Cinemachine 6.6.0 (bundled), Test Framework 1.8.0 and
UGUI 2.6.0. Commit the manifest, lockfile, project settings and authored .meta
files together when version control is initialized.

Close this project in the editor before running batch commands:

```powershell
.\Tools\Build\build_windows.ps1 -RunTests -SmokeTest
```

The script builds locally and can run the unit/integration tests and a short
standalone smoke route. It never starts the original game's executables.
Builds, Unity caches, test artifacts and logs are ignored.

One runtime assembly owns input, player movement, camera, session, world retry,
HUD and diagnostics; one editor assembly generates missing laboratory assets
and builds Windows. Two test assemblies cover unit and scene integration.
The composed TraversalRig prefab keeps its internal object references together.

Detailed file/module plan and acceptance gates: [Docs/IMPLEMENTATION_PLAN.md](Docs/IMPLEMENTATION_PLAN.md).

Next: evaluate the laboratory controls, then add wall attachment and swinging
with placeholders. Follow with a single faithful level slice. Reference GLB
import, scale/orientation checks, separate collision authoring, mission logic,
combat, animation, and replacement art remain future work.
The eventual performance target is 1080p60 on the reference PC; the complete
game's performance and absence of hitches are not established.

REA is optional later for bounded behavior questions; it is unnecessary for PKR
extraction and is not installed or configured yet.
