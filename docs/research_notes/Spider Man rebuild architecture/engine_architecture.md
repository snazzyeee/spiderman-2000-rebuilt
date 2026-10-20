# Engine and runtime architecture

## Which engine and project shape should we choose?

### Takeaway
Updated recommendation after user answers: Unity 6 in an evaluated stable, supported release, C#, and URP. The user is a solo experienced programmer with no game-development experience, no fixed budget/timeline, a faithful complete Windows PC version as the target, keyboard/mouse first and eventual public/open-source intent. Development hardware: Ryzen 5 5500, RX 580 8 GB 2048SP variant, 16 GB DDR4-3000 and 512 GB NVMe SSD. Prefer learning/productivity and a native movement prototype before expensive graphics. Treat this PC at 1080p60 as a provisional test target requiring measurement; the 2048SP GPU must not be assumed to match full 2304SP RX 580 benchmarks. Exact visual style remains TBD. This is a project-fit judgment, not a claim that Unity is universally best.

### Cited Findings
- Unreal's CharacterMovementComponent exposes built-in walking/falling movement and a CustomMovementMode plus PhysCustom extension point. This is a useful base, but does not implement Spider-Man wall or web traversal automatically. — [Epic API](https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/UCharacterMovementComponent)
- Enhanced Input supports actions, mapping contexts, modifiers, triggers, dead zones and runtime context prioritization. — [Epic Enhanced Input](https://dev.epicgames.com/documentation/unreal-engine/enhanced-input-in-unreal-engine)
- Animation Blueprints blend skeletal poses and control animation behavior. Gameplay remains a separate responsibility. — [Epic Animation Blueprints](https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-blueprints-in-unreal-engine)
- Unity's CharacterController moves with collision constraints without needing a Rigidbody; it is not driven by physical forces. Unity also provides a configurable Input System. — [Unity CharacterController](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.html), [Unity Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/manual/index.html)
- Unity HDRP targets high graphical fidelity on modern compute-shader-capable platforms; HDRP and URP features are not cross-compatible. Rendering pipeline choice matters early. — [Unity HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/index.html)
- URP is Unity's prebuilt rendering pipeline for optimized graphics across a wide range of platforms, including high-end PCs. — [Unity URP](https://docs.unity3d.com/ja/current/Manual/com.unity.render-pipelines.universal.html)
- Cinemachine supplies procedural target tracking, framing and shot blending and can be extended with custom camera scripts. It is a separately installed package. — [Unity Cinemachine](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html)
- Godot offers CharacterBody3D for script-controlled characters and AnimationTree blending/state-machine tools. — [Godot CharacterBody3D](https://docs.godotengine.org/en/stable/classes/class_characterbody3d.html), [Godot AnimationTree](https://docs.godotengine.org/en/stable/tutorials/animation/animation_tree.html)

### Inferences
- Unity: first choice for this user. C# gameplay/components, ScriptableObject tuning data, Prefabs and scenes form a direct working model for an experienced programmer learning games. Start with URP and a deliberate modern stylized or restrained realistic art target; good meshes, animation, materials, lighting and art direction matter more than a feature-heavy pipeline. If the desired look truly requires HDRP/Unreal features, evaluate a representative scene before committing art production. Do not assume changing pipelines later is cheap. Custom traversal is substantial work in every engine.
- Unreal: strong alternative if the user values its rendering/animation workflow enough to learn C++ plus Blueprints and can run the tools comfortably. It is not required to achieve modern visuals. Start from the ordinary third-person template and replace/tune systems deliberately; do not adopt a huge sample framework merely for fashionable architecture.
- Godot: credible for a deliberately scoped stylized interpretation and a team already comfortable with it. Evaluate the actual visual target and animation/content pipeline in a prototype; avoid promising AAA fidelity or better performance solely because the editor is lighter.
- Pin an evaluated stable engine release, plugin list and build toolchain after a short spike. Do not lock to the documentation's current/latest release automatically.
- The final scope is the complete original PC game. Windows single-player and one bounded first-level milestone are the production approach, not a reduction of that goal. Prefer ordinary Unity GameObjects/MonoBehaviours and direct C# interfaces over custom ECS/DOTS, networking, microservices, engine forks or huge third-party gameplay frameworks.
- Begin with Runtime, Editor and Tests assembly definitions/folders (EditMode and PlayMode tests as needed); keep logical subsystems as folders until another assembly offers a concrete benefit. Editor import code must not become a runtime dependency. Use normal C# logic for action rules/data and thin MonoBehaviours for Unity lifecycle/physics integration. A full dependency-injection container is unnecessary.
- Suggested Unity package baseline: Input System, Cinemachine, URP and Test Framework. Add Addressables once the level/content loading requirement justifies it; local content only is sufficient. Pin compatible stable package versions rather than the exact documentation versions cited here.

### Gaps
- Public-release minimum target PC and artistic reference remain unknown. Use the supplied PC as the initial measured baseline and validate the provisional 1080p60 target; do not present it as guaranteed.
- No benchmark comparing equivalent Spider-Man prototypes across engines was found or performed. No engine is guaranteed to eliminate bugs or hitches.

## How should runtime systems interact without becoming tightly coupled?

### Takeaway
Use an explicit input-to-gameplay-to-presentation flow with one authority for movement and small orthogonal state machines. Reconstruct the original behavior from observations and data, while building modern rendering, controls, camera and accessibility as replaceable systems.

### Cited Findings
- Unreal provides a custom movement-mode hook and post-movement notifications; it also exposes collision/penetration helpers. These are extensions to a movement framework, not a complete wall-crawling solution. — [Epic movement API](https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/UCharacterMovementComponent)
- Input Actions represent what the player wants to do independently of a raw hardware binding; mapping contexts can be added and prioritized at runtime. — [Epic Enhanced Input](https://dev.epicgames.com/documentation/unreal-engine/enhanced-input-in-unreal-engine)
- Animation Blueprints produce skeletal poses and support blended animation behavior. — [Epic Animation Blueprints](https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-blueprints-in-unreal-engine)
- Soft asset references avoid automatically loading referenced assets; RequestAsyncLoad supports deferred asset loading, while synchronous loads can stall the main thread. — [Epic asynchronous loading](https://dev.epicgames.com/documentation/unreal-engine/asynchronous-asset-loading-in-unreal-engine)

### Inferences
The following are proposed architecture and design decisions, not claims about the original implementation:

Unity implementation: PlayerInputAdapter (Input System) -> PlayerCoordinator -> CharacterMotor + CombatController; motor/action snapshots -> Animator and CameraRig (Cinemachine); typed C# events -> MissionController/HUD/Audio. Use ScriptableObjects for immutable tuning/configuration, not shared mutable per-session save state. Use Unity Animator states, layers and authored clips in the selected stack.

| System | Owns | Depends on / publishes |
|---|---|---|
| Input adapter / local player | Device mappings, remaps, sensitivity, dead zones, press/hold intent and short input buffer | Emits intent; knows no enemy or level implementation |
| Player coordinator | Action acceptance, priorities, cancellation and capability rules | Intent + traversal/action/life state; requests operations |
| Character motor | Authoritative capsule position/velocity, collision, floor and wall contact, jump, fall, swing and recovery | World-query interface + tuning; emits movement snapshot/contact events |
| Traversal query | Wall/ceiling/ledge probes, attachable surfaces, web target candidates | Collision channels/tags and bounded traces |
| Combat / web actions | Combo windows, hit queries, damage, web resource and attack interruption | Accepted actions + motor request interface; emits hits/results |
| Health / life state | Damage, invulnerability policy, death and respawn | Receives damage; emits life-state changes |
| AI | Small perception/behavior state, navigation and attack requests | Same damage/action contracts as player; no UI calls |
| Level / mission | Objectives, triggers, encounters, collectibles and completion | Stable IDs and typed gameplay events |
| Checkpoint / save | Versioned save payload, objective state, spawn point, settings | Stable IDs; serializes state rather than raw actor pointers |
| Camera | Look input, occlusion, target framing, recenter and camera profiles | Reads motor/target snapshots, never secretly moves player |
| Presentation | Animation pose, VFX, sound and HUD | Reads state and typed events; gameplay does not depend on VFX success |
| Content loading | Asset bundles, preparation/readiness, unload policy | Async handles + authored content manifests |
| Diagnostics | Trace markers, counters, logs and reproducible bug capture | Observes systems; development-only commands stay gated |

Suggested dependency direction: input -> coordinator -> motor/actions -> gameplay events -> mission/save and presentation. Share small types/interfaces, tuning ScriptableObjects and stable identifiers. Use a small typed event/delegate set; a global stringly event bus would hide ownership. Use explicit initialization and teardown; avoid cyclic scene/prefab reference graphs.

Use three coordinated state axes, not every combination encoded in a single enum:

- Traversal: Grounded, Airborne, SurfaceAttached, Swinging, ScriptedTraversal. Surface attachment carries its normal, tangent basis, contact ID and edge-transition information; climbing a wall and crossing onto a ceiling need explicit acceptance/collision rules.
- Action: Free, Attacking, WebAction, Interacting, Recovery. Timed substates/window data describe attacks instead of multiplying locomotion enums.
- Life/control: Alive, Stunned, Dead, Cinematic, Paused. Central priority/cancellation rules decide which operations are legal. Example: death cancels a web action; a permitted aerial attack retains Airborne traversal.

The motor is the only normal writer of capsule transform/velocity. Traversal requests movement; attacks request a bounded impulse or authored root-motion segment; animation and camera consume the result. Root motion is useful for selected finishers/ledge moves, but ubiquitous animation-driven traversal can fight responsive input. Keep gameplay hit windows/timers authoritative even if animation events assist presentation.

Implement a motor interface first. Unity CharacterController can accelerate a ground/air prototype, but immediately spike wall-to-ceiling/corner transitions and collision orientation; do not promise the stock controller solves these. If the required collision shape/contact rules fail the spike, replace the backend with a controlled capsule-sweep/slide motor. A dynamic Rigidbody rope simulation is a different movement design and should not accidentally become the core controller. Sample/buffer input in Update; choose and document the motor simulation phase; camera/presentation consume the final pose consistently (commonly LateUpdate). Physics queries, collider synchronization and interpolation must be tested together rather than mixing transform writers.

Prototype web swinging as a controllable movement mode with tuned acceleration, steering, gravity arc, release impulse, speed cap and optional author-placed/queried anchors. Render the web strand as presentation. Full simulated ropes, wrapping and physical building attachment are a separate design choice: add them only if wanted and validated against feel. Do not assume a modern physically realistic swing reproduces the 2000 game's feel. Measure observed behavior before claiming any original rule.

Input/UX defaults to validate: full keyboard/mouse and gamepad navigation, saved remaps and prompt updates; camera-relative movement; configurable sensitivity/inversion/dead zones; jump buffering and forgiving edge timing; toggle/hold options where relevant; subtitles; camera shake/motion blur toggles; quick retry and checkpoint restart. Provide a separately tuned classic-controls preset only if requested/valuable, rather than letting historical limitations dictate the default.

Keep original files, research and converters offline: local original files -> inventory/hash -> read-only extractor -> normalized intermediate files plus provenance manifest -> editor importer -> cooked game assets. Runtime reads only cooked/new content. Intermediate schema should record source edition/hash, units/axis conversion, asset IDs, mesh/material links, skeleton/animation timing, collision and trigger annotations. Unsupported fields are flagged rather than silently invented. Modern replacement art can map to stable asset IDs without changing mission code. Private proprietary source files and extraction outputs should be excluded from ordinary public version control; rights/distribution policy is covered by the companion research.

### Gaps
- Exact original movement equations, collision rules, camera behaviors, game-data formats and event scripts are unverified until a specific edition and gameplay/reference data are examined.
- Wall/ceiling collision geometry, high-speed sweep behavior and corner transitions require a prototype; extending stock CharacterMovement does not make them automatically correct.

## What measurable quality gates and first-level phases make this achievable?

### Takeaway
Replace “bug-less/no lag spikes” with a defined hardware/build/test matrix, visible frame-time budgets and reproducible acceptance scenarios. Finish a first-level vertical slice before scheduling a full recreation; controls and camera should earn approval in graybox before expensive art.

### Cited Findings
- Unity's Profiler can capture standalone build data; instrumentation affects performance and much of its API requires Development Build. Use diagnostic development captures and final player measurements for their different purposes. — [Unity Profiler API](https://docs.unity3d.com/cn/6000.0/ScriptReference/Profiling.Profiler.html), [Collecting data](https://docs.unity3d.com/6000.0/Documentation/Manual/profiler-profiling-applications.html)
- Unity Test Framework supports Edit Mode and Play Mode; Play Mode tests can run in a standalone Player. — [Unity test modes](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/edit-mode-vs-play-mode-tests.html)
- Addressables provides async loading operations and explicit release responsibilities. Async loading does not imply all scene activation/instantiation work is free. — [Unity Addressables](https://docs.unity3d.com/ja/Packages/com.unity.addressables@1.20/manual/LoadingAddressableAssets.html)
- Unity documents runtime PSO warmup; GraphicsStateCollection is explicitly experimental in the cited documentation. Do not build the initial architecture around that API as a stable guarantee. — [Unity PSO warmup](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-prewarm.html)
- Timing Insights displays per-frame data and CPU/GPU tracks to inspect spikes and thread work. — [Epic Timing Insights](https://dev.epicgames.com/documentation/unreal-engine/timing-insights-in-unreal-engine)
- PSO precaching asynchronously collects/compiles graphics pipelines; loading should wait for relevant outstanding preparation. Missing or late PSOs and driver-cache behavior can still hitch. Epic documents validation counters and cold-driver-cache test support. — [Epic PSO precaching](https://dev.epicgames.com/documentation/unreal-engine/pso-precaching-for-unreal-engine)
- Godot also documents shader/pipeline compilation mitigation and remaining edge cases, so shader hitch management is not solely an Unreal issue. Forward+/Mobile have precompilation support; Compatibility needs a different preload approach. — [Godot pipelines](https://docs.godotengine.org/en/stable/tutorials/performance/pipeline_compilations.html)
- Unreal Automation provides feature/content stress testing, input simulation and functional level testing interfaces. — [Epic automation framework](https://dev.epicgames.com/documentation/en-us/unreal-engine/automation-test-framework-in-unreal-engine)

### Inferences
Provisional acceptance gates below are project targets, not measured results, engine guarantees or final commitments:

- Agree a minimum PC, resolution and visual preset first. For a 60 FPS target the frame interval is 16.67 ms. Leave CPU/GPU headroom rather than filling it with visual features during initial prototypes. Do not add independent CPU and GPU budgets as though they were always serial stages.
- Capture a repeatable packaged-build route through the first level: rooftop movement -> wall/corner/ceiling -> swing/release -> enemy combat/web effects -> pickup -> checkpoint -> death/retry -> completion. Log run ID, build/engine/settings, hardware/driver, resolution/upscaler and cache condition. Test three warm runs plus a separately measured first-run/cold-cache run. Editor play is useful for iteration, not final performance acceptance.
- Example initial 60 FPS gate after loading/preparation: frame-time p95 <= 16.67 ms, p99 <= 20 ms, maximum <= 33.3 ms on the agreed route; record every frame over 33.3 ms with cause. The strict maximum requires controlled repeated runs and may need adjustment for external OS events. Never report only average FPS or hide exceptions by averaging. Measure uncapped headroom and actual capped/frame-paced behavior separately.
- Separate initial startup/loading duration, shader preparation duration and checkpoint transition duration from playable runtime hitch statistics; report them all. Preparation is real user waiting and needs a truthful progress/readiness UI. A loading screen must not conceal failed loads indefinitely.
- Audit synchronous loads and prefab/scene dependency chains; preload the bounded level and common combat/audio effects; defer optional content; spread expensive spawning/activation work; pool only measured frequently reused transient objects. Avoid steady per-frame C# allocations in gameplay hot paths; inspect GC captures rather than assuming no allocations means no hitches. Bound texture/mesh/VFX memory to target VRAM/RAM with headroom; set numeric budgets after hardware is known.
- For Unity, test first-use materials/particles/UI/render modes and capture CPU/GPU/shader behavior; evaluate supported variant/PSO warmup for the chosen stable editor and graphics API without assuming one generic warmup call solves every pipeline. If choosing Unreal instead, use PSO validation and traces to diagnose misses/late work. Test clean first-run conditions separately from warmed caches. Preloading alone does not eliminate object creation, garbage collection, GPU upload, driver or asset-registration stalls.
- Tick/render policy: use engine-supported movement integration, bounded substeps where needed, and one authority for motion. Timestamp input and compare movement/jump/swing behavior at 30/60/120+ FPS. A fixed time step alone does not guarantee determinism or remove hitches; do not bolt a second independent game loop onto the engine without a demonstrated need.
- Bug gate for each milestone: zero known crashes, save corruption, progression blockers or unrecoverable softlocks in the slice; every must-pass traversal/combat/checkpoint scenario passes; every found defect has reproduction steps, severity, build ID and owner. Include out-of-bounds recovery, wall seams, pause during actions, input disconnect/reconnect, alt-tab, remapping conflicts, reload during an encounter and repeated retry.
- Input gate: accepted input changes gameplay state in the next scheduled gameplay update under normal load; log intended buffering behavior. Measure physical end-to-end input-to-photon separately if claiming a latency number, since UI/event timestamps alone do not measure the display pipeline.
- Soak gate example: a 60-minute repeated traversal/combat/retry session has no crashes, blockers or continuing memory growth after caches reach steady state. CPU/GPU/VRAM counters remain captured. The pass is evidence for this build/scenario, not proof of no bugs.

Phases and exit artifacts (scope is one short, bounded first-level slice):

1. Scope/reference and build baseline: choose one original edition and visual target; list observable behaviors; agree min spec; create reproducible empty packaged build, source control/LFS rules, dependency pin and trace capture. Exit: a small executable and explicit slice acceptance checklist.
2. Movement/camera laboratory: original or placeholder shapes only. Ground, jump, fall, wall/ceiling contact, corners, swing/release, landing and camera occlusion; tune against clips/measurements. Exit: enjoyable graybox controls plus transition/regression map, including high/low render-rate checks.
3. One playable mission: block out first level flow, one enemy type, minimal combat/web interactions, objective, collectible, checkpoint/death/retry and completion. Exit: full repeatable playthrough before visual production.
4. Presentation slice: one hero, enemy and environment kit at target style; animation/IK, audio, effects, subtitles, menus/remaps and accessibility settings. Exit: representative visual density, readable action and saved preferences.
5. Performance and release hardening: final cook/load/PSO behavior, frame-time route, memory soak and bug regression on minimum and representative higher PC. Exit: packaged build meeting declared gates plus a known-issues list.
6. Complete the full game through a level-and-feature inventory: every original level/boss, enemy, mechanic, collectible/unlock, cutscene, audio cue, menu, save state and ending has a tracked implementation/fidelity/QA status. Expand one level/mechanic at a time after first-level acceptance; shared systems remain reusable and exceptional level scripts remain explicit. No full-game calendar estimate until the slice demonstrates production speed and asset/animation costs. Eventual open source can cover newly written code and rights-cleared material; Unity's engine implementation is not automatically part of the game's repository or license.

### Gaps
- No build, test route or hardware benchmark exists yet. All numeric gates are suggested starting points and require hardware evaluation. On the supplied RX 580 2048SP/16 GB PC, start with lean URP lighting/materials, baked lighting where practical, bounded real-time shadows/VFX, modest texture sizes and controlled visual density. New art, animation, coherent lighting and readability can deliver a modern look without ray-traced/global-illumination-heavy targets. Leave RAM/disk headroom for the editor/imports and profile actual builds before increasing quality.
- No calendar estimate is defensible without knowing skill, weekly time, art sourcing and team. A modern complete game recreation includes substantial animation, level, audio, UI and QA work beyond engine programming.
