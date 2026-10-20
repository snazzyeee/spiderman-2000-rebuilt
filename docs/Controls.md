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
