# Traversal laboratory controls

Open Game/ as a project in Unity Hub using **6000.6.6f1**.
Open Assets/_Game/Scenes/TraversalLab.unity and press Play.
The standalone build is Builds/Windows/TraversalLab.exe.

| Input | Action |
|---|---|
| W / A / S / D | Move relative to the camera's horizontal heading |
| Mouse | Turn camera; vertical movement changes pitch |
| Left Shift | Run |
| Space | Jump |
| R | Return to the starting roof, clearing momentum |
| Escape | Pause/resume and release/capture the mouse |

Click **Resume**, **Restart Course**, or **Quit** in the pause menu.
Losing window focus pauses the standalone game. Click back and press Escape.
In the Unity editor, Quit is hidden; use the editor's Play button to stop.

Follow the yellow take-off and landing markers. Hold Shift and jump close to
the edge to cross each gap. On the second roof, turn right, then turn forward
again to reach the cyan beacon. Falling resets you to the first roof.

The red capsule is a placeholder. There is no wall crawling, swinging, combat,
Spider-Man animation, or original level gameplay in this milestone.

Movement and camera parameters live in Assets/_Game/Settings/. The initial
movement uses 5.5 m/s walk, 9 m/s run, 2.1 m nominal jump height, 120 ms coyote
time, and a 140 ms input buffer. These are prototype values, not measurements
from the original game.

The HUD displays a rolling average FPS and 95th-percentile frame time after
five seconds of warm-up. Use a standalone build for performance checks. A
simple placeholder course cannot establish performance for the complete game.
