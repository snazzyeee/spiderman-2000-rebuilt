# Mission 1 vertical slice: scripts, prefab setup and exact flow

The slice uses original PC Spider-Man, L1A1, henchman, web-dome and named SFX
assets locally. The route, encounter placement, entry marker and animation
bindings are authored integration data; they are not verified reconstructions
of the original mission script. Intro subtitles are newly authored, as requested.
No Stan Lee recording is fabricated or assigned an unidentified sample.

## Generate and run

Close the Game project in Unity before batch commands. From the repository root:

    .\Tools\Reference\setup_converter.ps1
    python Tools/Reference/prepare_phase1_assets.py
    python Tools/Reference/prepare_mission1_assets.py
    .\Tools\Build\build_mission1.ps1 -RunTests -SmokeTest

The source PC archive must be LocalReference/PC/app/data.pkr.
Both conversion recipes verify the pinned converter, member CRCs and lengths.
They read archive/model/audio data and never launch the original game code.

Open Game in Unity Hub with editor 6000.6.6f1. Run:

    Assets/_LocalReference/Mission1/BankHeistRooftops.unity

Editor assembly: Rebuild > Mission 1 > Assemble Vertical Slice.
Windows build: Rebuild > Mission 1 > Build Windows.
Executable: Builds/Mission1/SpiderManMission1.exe.

The builder preserves an existing mission scene and generated MissionLayout.
Edit those assets in the Inspector for art-directed layout changes. Generated
private assets are under Assets/_LocalReference/Mission1/Generated.

## Runtime ownership

| Component | Responsibility |
|---|---|
| TraversalInput | Samples keyboard/mouse and gamepad actions |
| TraversalSession | Owns pause, audio pause, cursor/menu mode and independent control-lock owners |
| SpiderManCharacterController | Swept box collision, ground locomotion, contact sliding, overlap recovery |
| PlayerStateMachine | Sole movement driver in the slice; swing, zip, crawl, mantle, damage and control transitions |
| WebAnchorSelector | Chooses visible raycast surfaces from surrounding original geometry |
| SwingSolver | Pendulum integration, damping, trough gravity boost, tangential steering and rope constraint |
| PlayerCombat | Web energy/cooldowns, traced web shots, three-hit melee buffer and dome stun/protection |
| Health | Team filtering, damage, invulnerability, one-shot defeat event and restore |
| MissionManager | Ordered progression, safe checkpoint recovery and completion gate |
| MissionCheckpoint | Spatial validation and ordered checkpoint requests |
| EncounterDirector | Tracks living enemies, owns arena lock and releases it on the last defeat |
| SkylightInteraction | Range/state-checked final interaction |
| ThugAI | Patrol, alert, telegraphed melee/ranged attacks, stun and defeat |
| SurfaceSteeringMotor | Ground-aware obstacle avoidance on the bank roof without a NavMesh dependency |
| CutsceneSequence | Owns PlayableDirector lifetime, skip, pause/resume and its control lock |
| DialogueCueTrack / Clip | Timeline subtitle/audio/camera cue data and evaluation |
| DialoguePresenter | Audio synchronization, subtitle events, exact camera-priority restoration |
| DialogueTrigger | Retry-on-busy trigger volume, with optional once-per-reset behavior |
| MissionHud | Objectives, target distance, health/web meter, subtitles, pause and replay UI |

All runtime source is under Game/Assets/_Game/Runtime. Assembly stays independent
of editor APIs. The editor builder is Game/Assets/_Game/Editor/Mission1SceneBuilder.cs.
The old five-slot PlayableGraph animation driver is removed from the mission
instance so it cannot compete with the mission's Mecanim controller.

## Player prefab / Inspector setup

Player root requirements: identity world rotation/scale, layer 2, kinematic
Rigidbody with gravity off and frozen rotation, and a BoxCollider centered at
(0,0.9,0) with size (0.55,1.8,0.45). Do not add a CapsuleCollider or Unity
CharacterController.

The generated private scene already supplies these references:

1. SpiderManCharacterController: assign Movement.asset, the imported Visual,
   TraversalSession and SpiderManOrbitCamera. WorldMask includes static world
   layer 0 and enemy layer 8.
2. Health: Team=Player, Maximum=100.
3. PlayerStateMachine: assign traversal settings, WebAnchorSelector, PlayerCombat,
   the Visual's Generic Animator, and the textured two-point rope LineRenderer.
   It sets Motor.Automation=true and becomes the only movement owner.
4. WebAnchorSelector: Main Camera, static-world layer mask 1, motor and settings.
   Trigger volumes and enemies cannot become building anchors.
5. PlayerCombat: player, Main Camera, enemy mask 256, shot mask 257, SFX source,
   TraversalAudioRig, native WebShot3/PunchBig/webdome clips, web tracer, and
   original converted WebDome prefab.
6. Animator: original Generic avatar, root motion disabled, generated
   Generated/Player.controller.

Mecanim parameters:

| Name | Type | Runtime meaning |
|---|---|---|
| isSwinging | Bool | Swing state active |
| isCrawling | Bool | Surface latch/crawl active |
| swingNormalizedTime | Float | Repeating pendulum-period phase |
| impactVelocity | Float | Landing speed, decaying after impact |
| isGrounded | Bool | Motor ground contact |
| speed | Float | Horizontal world speed |
| comboIndex | Int | Zero-based current melee stage |
| melee | Trigger | Starts a combo-stage animation |

The generated controller contains functioning transitions and original clips.
The numeric clip selections are provisional bindings, not identified original
idle/run/swing/crawl meanings. Inspect and replace motions in the private
controller without changing gameplay timing. Combat hit windows are owned by
PlayerCombat, so an animation import or clip replacement cannot silently alter
damage timing.

## Exact traversal flow

    Grounded <-> Airborne
      | F/RT held + visible elevated geometry -> Swinging
      | Q/LT + valid geometry -> Zipping
      | C/B held + wall probe -> Crawling

    Swinging -> Airborne:
      release F/RT, press Jump, lose anchor, or occlude the rope
      (retains the world-space release velocity)

    Zipping -> Airborne:
      reach the surface stand-off, get fully blocked, time out, or lose anchor
      (applies the forward release impulse)

    Zipping / Airborne -> Crawling:
      hold C/B while a wall is within probe reach

    Crawling -> Grounded:
      lower/chest probes clear a roof lip and a swept, clearance-checked
      mantle finds a walkable roof

    Crawling -> Airborne:
      release latch, lose the surface without a safe mantle, or jump outward

    Any live state -> HitStun -> Grounded/Airborne:
      valid damage; cancels queued melee and applies collision-aware knockback

    Any live state -> ControlLocked:
      narrative/mission control owner; pause preserves the current state

    Any live state -> Dead -> checkpoint recovery:
      health reaches zero; MissionManager restores health, energy and encounter

The swing anchor stores a collider-local position/normal and tracks its transform.
The pendulum uses bounded simulation steps and a maximum speed. The line is
constrained only when taut, allowing slack. A descending trough boosts gravity;
steering pumps along the tangent. Buildings block both movement and rope LOS.

Zip follows the target directly with swept collision; it never disables world
collision. Hold latch at a façade to climb and mantle onto the next roof.
The crawl root retains identity rotation for the box motor while the imported
Visual aligns with the sampled surface normal. Horizontal crawl input uses the
surface's right vector.

## Mission progression and checkpoint recovery

    Intro
      -> RooftopRoute: Timeline finishes or is skipped
      -> checkpoint 1
      -> checkpoint 2
      -> final bank rooftop checkpoint
      -> BankEncounter: arena constraint enabled; guards become active
      -> Skylight: last registered guard defeated; arena constraint released
      -> Complete: player is in range and presses E/Y

Out-of-order checkpoints, distant interactions and a skylight request while any
guard is alive are rejected. No timed counter substitutes for enemy defeat.

Falling below KillHeight, pressing R/Back or losing all health starts recovery.
During the fight, recovery returns to the preceding approach roof and resets
all guards. After the fight, recovery preserves the cleared encounter and
returns to the bank entry stage. Health, web energy, motion, rope and queued
attacks are reset. Replay resets the entire mission and starts the intro again.

MissionCheckpoint requires a BoxCollider with IsTrigger enabled, its exact
ordered Index, Mission reference, label and SafeSpawn transform on actual ground.
Checkpoint acceptance is also checked against the player's current position.

EncounterDirector requires Player, all encounter ThugAI references and the
bank Bounds. The lock constrains player X/Z movement to the authored arena;
it introduces no fake wall meshes. The final event releases that constraint.

SkylightInteraction requires Mission and a world-space entry point, with a 2 m
range by default. The generated entry uses an original L1A1 object surface.
Its original script/artist semantics are not established; the entry role and
checkpoint layout are authored for this slice.

## Bank thug prefab

Generated/BankThug.prefab contains:

- Layer 8 root; Health (Enemy, 90), kinematic Rigidbody, BoxCollider.
- SurfaceSteeringMotor: layer 0 ground, bank WalkArea and LimitToArea.
- ThugAI: Player, fixed world PatrolPoints, Ranged flag and imported Animator.
- Original henchman model, textures, Generic avatar and generated Mecanim graph.

Patrol points belong to the encounter hierarchy, not to the moving enemy.
The builder makes two patrol points per guard and enables ranged behavior on
one of the three guards.

    Patrol -> Alert: visible player within sight range
    Alert -> MeleeAttack: melee range, LOS and cooldown ready
    Alert -> RangedAttack: ranged guard, 3–14 m, LOS and cooldown ready
    Attack -> Alert: wind-up, one damage window, then recovery
    Alert -> Patrol: player unseen for four seconds
    Any live state -> WebbedStun: valid web hit
    WebbedStun -> Alert: stun expires
    Any live state -> Defeated: health reaches zero / full web capture

Melee has a 0.48 s wind-up and checks range/LOS again at impact. Ranged attacks
cache the aimed position at wind-up and raycast after 0.7 s, allowing movement
to evade the shot. Hits and web stun interrupt attacks. Three accumulated web
stacks capture a guard and raise the same one-shot defeat event as melee damage.
Dead guards' collision is disabled, and duplicate defeat calls cannot reduce
the living count again.

## Player combat

Web shot: seven energy, 0.22 s cooldown; camera aim plus muzzle-to-target
collision check. The first actual obstruction receives the hit. Walls block
both shots and melee. Energy regenerates while active, up to 100.

Melee: three stages with 20/25/35 damage, wind-ups 0.13/0.16/0.20 s and recovery
0.28/0.30/0.40 s. A 0.35 s buffered press queues the next stage. Each target can
receive a stage's damage once. Damage or control transitions cancel the queue.

Web dome: 30 energy, four-second cooldown, 3.4 m LOS-checked area stun, short
invulnerability and the actual original web-dome visual. Its gameplay timing
is authored slice tuning, not measured original behavior.

## Timeline and dialogue authoring

The intro is an eight-second private Timeline with two Cinemachine shots and
a DialogueCueTrack bound to DialoguePresenter. Each shot lasts four seconds.
The authored briefing is subtitle-only; Audio can be assigned later.

To add a new dialogue volume:

1. Create a PlayableDirector with a Timeline containing a DialogueCueTrack.
2. Bind that track to the shared DialoguePresenter.
3. Add DialogueCueClip entries; assign Speaker, Subtitle, optional Audio and
   an optional exposed Cinemachine ShotCamera reference.
4. Add CutsceneSequence beside the director. Assign Session, Input and Presenter.
   Set BlocksControl as appropriate; leave AllowSkip enabled when desired.
5. Add DialogueTrigger to an empty volume with a trigger BoxCollider and assign
   the sequence. It retries while occupied if the presenter is busy, and Once
   prevents duplicate starts. ResetTrigger allows deliberate re-arming.

The presenter owns audio/subtitle synchronization and snapshots/restores camera
priority. CutsceneSequence owns only its own control lock. Skip, stop, abort,
pause/resume and scene destruction release resources without blindly restoring
someone else's lock or rewriting the mission state.

## Controls

| Action | Keyboard/mouse | Gamepad |
|---|---|---|
| Move / look | WASD / mouse | Left / right stick |
| Jump / swing release | Space | A / Cross |
| Run | Left Shift | LB / L1 |
| Swing (hold) | F | RT / R2 |
| Zip | Q | LT / L2 |
| Crawl/latch (hold) | C | B / Circle |
| Melee | Left mouse | X / Square |
| Web shot (hold) | Right mouse | RB / R1 |
| Web dome | V | D-pad down |
| Interact | E | Y / Triangle |
| Pause | Escape | Start |
| Checkpoint recovery | R | Back / Select |
| Skip intro | Enter | A / Cross during the intro |

## Validation and scope limits

The build script runs source-only physics tests and private asset/mission
integration tests, then launches a rendered standalone smoke route.
See Mission1Validation.md for the final result and the route actually exercised.
Missing private assets explicitly skip private tests; they are not reported as
passing gameplay checks.

All original and converted assets, scenes, prefabs, Timeline/animation assets,
SFX, layouts, builds and private screenshots stay ignored. Public source contains
the implementations, tests and reproducible assembly/conversion procedures.

This slice does not establish 100% parity with the shipping mission, validate
the original bank/skylight identity, identify native animation semantics, or
prove production performance/reliability. The original narration remains
unassigned by the user's choice. It is an end-to-end authored mission on original
assets, with further fidelity and play-feel work still required.
