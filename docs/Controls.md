# Active Godot controls

Use tools/pipeline/run_godot.ps1 to launch the root Godot project. Main menu:
arrows/D-pad navigate, Enter/A confirms, mouse clicks select. Options includes
Dialogue/Music/SFX levels and **Font variant: Faithful / Enhanced**. Enhanced
is the default; selection persists in user://settings.cfg and applies live to
menu, HUD and subtitles. Missing private fonts retain the default font.

| Action | Keyboard/mouse | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Jump / swing release | Space | A / Cross |
| Swing / release | Right mouse press/hold/release | No dedicated binding currently |
| ZIP_TARGET | Q | RB / R1 |
| ZIP_FORWARD | T | RT / R2 |
| Explicit crawl attach / knock off | C | LB / L1 |
| Melee | F | X / Square |
| Web | R | Y / Triangle |
| Interact | E | B / Circle |
| Pause | Escape | Start |

Zip assignments and explicit wall attach are authored; original button mapping
is unknown. Training uses the same motor and bindings. Full control remapping,
video options and accessibility settings remain pending. The old in-game
controls panel is also pending expansion for ZIP_FORWARD; this document records
the actual current input map rather than claiming that panel has been updated.

## Historical Unity controls (superseded)

The following content documents the archived Unity slice and is not the current
Godot input map or launch procedure.

# Menu and Mission 1 controls

Launch Builds/Frontend/SpiderManRevamp.exe for the complete menu entry flow.
Arrow keys, D-pad or left stick navigate; Enter/A submits; Escape/B returns;
left/right adjusts sliders. Mouse hover selects and click submits. Game offers
New Game and checkpoint Load Game. Training offers an invulnerable rooftop
practice mode that does not overwrite the normal save. Options persists mixer
levels, mouse sensitivity and invert Y. Pause/completion also offers Main Menu
when that scene is included in the build.

The standalone mission-only entry below remains available for development.

Open Assets/_LocalReference/Mission1/BankHeistRooftops.unity in Unity 6000.6.6f1,
or run Builds/Mission1/SpiderManMission1.exe.

| Action | Keyboard/mouse | Gamepad |
|---|---|---|
| Move / orbit | WASD / mouse | Left / right stick |
| Jump / swing release | Space | A / Cross |
| Run (hold) | Left Shift | LB / L1 |
| Swing (hold) | F | RT / R2 |
| Zip to visible geometry | Q | LT / L2 |
| Wall crawl / latch (hold) | C | B / Circle |
| Melee / queue next combo stage | Left mouse | X / Square |
| Web shot (hold) | Right mouse | RB / R1 |
| Web dome | V | D-pad down |
| Skylight interaction | E | Y / Triangle |
| Pause | Escape | Start |
| Recover checkpoint | R | Back / Select |
| Skip intro | Enter | A / Cross during intro |
| Advance separate mission briefing | Enter after 0.2 seconds | A / Cross |

Follow the rooftop objective markers. Aim toward building geometry for swing
and zip. Hold latch when approaching a wall, then move up to crawl and mantle
its lip. Swing requires a visible elevated surface within range.
The opening recording and the authored briefing are separate phases, both
blocking gameplay. The briefing advances automatically after five seconds or
with Enter/A. Movement, combat and objective UI begin afterward.

The final rooftop starts the encounter and constrains movement to its arena.
Defeat all three guards with web capture or melee to unlock the entry prompt.
Three accumulated web stacks capture a guard; a dome supplies two area stacks.

Losing window focus pauses normal interactive play. Pause provides Resume,
Restart Checkpoint and Quit. Completion provides Replay Mission. During combat,
recovery returns to the preceding approach roof and resets the encounter.
After clearing it, recovery preserves the cleared guards.

The old Phase 1 foundation scene supports move/orbit/jump/run/pause/reset only.
See [MISSION1.md](MISSION1.md) for the full component setup and scope limits.
