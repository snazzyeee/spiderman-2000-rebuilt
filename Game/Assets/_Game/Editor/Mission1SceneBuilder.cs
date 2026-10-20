using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SpiderManRebuild.Editor
{
    public static class Mission1SceneBuilder
    {
        public const string Root = "Assets/_LocalReference/Mission1";
        public const string ScenePath = Root + "/BankHeistRooftops.unity";
        [MenuItem("Rebuild/Mission 1/Assemble Vertical Slice")]
        public static void Create()
        {
            Phase1SceneBuilder.Create();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!File.Exists(Root + "/henchman/henchman.gltf"))
                throw new InvalidOperationException("Run python Tools/Reference/prepare_mission1_assets.py first.");
            if (File.Exists(ScenePath))
            {
                ConfigureWebEffectMaterials(); AssetDatabase.SaveAssets();
                EditorBuildSettings.scenes = TestScenes();
                Debug.Log("Preserving existing Mission 1 scene."); return;
            }
            EditorSceneManager.OpenScene(Phase1SceneBuilder.ScenePath);
            Directory.CreateDirectory(Root + "/Generated"); AssetDatabase.Refresh();
            var motor = Object.FindAnyObjectByType<SpiderManCharacterController>();
            Object.DestroyImmediate(motor.GetComponent<Phase1SmokeRunner>());
            var previousAnimation = motor.GetComponentInChildren<SpiderManAnimationDriver>();
            if (previousAnimation != null) Object.DestroyImmediate(previousAnimation);
            var oldHud = Object.FindAnyObjectByType<Phase1Hud>();
            if (oldHud != null) Object.DestroyImmediate(oldHud.gameObject);
            var player = motor.gameObject.AddComponent<PlayerStateMachine>();
            var playerHealth = motor.GetComponent<Health>(); playerHealth.Team = Team.Player; playerHealth.Maximum = 100;
            var settings = ScriptableObject.CreateInstance<TraversalSettings>();
            AssetDatabase.CreateAsset(settings, Root + "/Generated/Traversal.asset"); player.Settings = settings;
            player.Animator = motor.Visual.GetComponent<Animator>();
            player.Animator.runtimeAnimatorController = MakePlayerAnimator();
            var selector = motor.gameObject.AddComponent<WebAnchorSelector>();
            selector.AimCamera = Camera.main; selector.Settings = settings; selector.Motor = motor; player.Anchors = selector;
            var combat = motor.gameObject.AddComponent<PlayerCombat>(); player.Combat = combat; combat.Player = player;
            combat.AimCamera = Camera.main; combat.Audio = Object.FindAnyObjectByType<TraversalAudioRig>();
            combat.Audio.WebImpact = Sfx("WebHit4");
            var sfx = motor.gameObject.AddComponent<AudioSource>(); sfx.outputAudioMixerGroup = combat.Audio.Sfx;
            sfx.playOnAwake = false; combat.LocalSfx = sfx;
            combat.PunchSound = Sfx("PunchBig"); combat.WebShotSound = Sfx("WebShot3"); combat.DomeSound = Sfx("webdome");
            var effectAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/webdome2/webdome2.gltf");
            var effect = (GameObject)PrefabUtility.InstantiatePrefab(effectAsset);
            effect.name = "OriginalWebDome"; Phase1SceneBuilder.ConvertMaterials(effect, "WebEffect");
            PlayerCombat.FitOriginalEffect(effect, Vector3.zero, 6);
            var effectContainer = new GameObject("WebDome");
            effect.transform.SetParent(effectContainer.transform, true);
            combat.DomePrefab = PrefabUtility.SaveAsPrefabAsset(effectContainer, Root + "/Generated/WebDome.prefab");
            ConfigureWebEffectMaterials();
            Object.DestroyImmediate(effectContainer);
            Texture webTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/webdome2/Textures/image_000.png");
            var webMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Web Strand" };
            webMaterial.SetTexture("_BaseMap", webTexture); webMaterial.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(webMaterial, Root + "/Generated/WebStrand.mat");
            player.Rope = Line("Swing web", motor.transform, webMaterial, .035f);
            combat.WebTracer = Line("Web shooter", motor.transform, webMaterial, .04f);
            motor.WorldMask = 1 | (1 << 8);

            var coverAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/l1a1_o/L1A1_O.gltf");
            var cover = (GameObject)PrefabUtility.InstantiatePrefab(coverAsset); cover.name = "Original Bank Entry Surface";
            cover.transform.localScale = Vector3.one * .019792303442955017f;
            var coverFilters = cover.GetComponentsInChildren<MeshFilter>();
            bool retained = false;
            foreach (var filter in coverFilters)
            {
                if (!filter.sharedMesh.name.Contains("00000006")) { Object.DestroyImmediate(filter.gameObject); continue; }
                retained = true; filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
            if (!retained) throw new InvalidOperationException("Original object mesh 6 was not found.");
            Phase1SceneBuilder.ConvertMaterials(cover, "BankEntry");
            Physics.SyncTransforms();
            var layout = CreateLayout(cover);
            motor.transform.position = layout.StartingRoof; motor.SpawnPosition = layout.StartingRoof;
            Vector3 initialDirection = layout.CheckpointRoofs[0] - layout.StartingRoof;
            motor.Orbit.UseInitialHeading = true;
            motor.Orbit.InitialYaw = Mathf.Atan2(initialDirection.x, initialDirection.z) * Mathf.Rad2Deg;
            motor.Orbit.transform.position = layout.StartingRoof + Vector3.up * motor.Orbit.TargetHeight;
            var missionObject = new GameObject("Mission 1 Director"); var mission = missionObject.AddComponent<MissionManager>();
            mission.Player = player;
            var start = new GameObject("Starting rooftop").transform; start.position = layout.StartingRoof;
            start.SetParent(missionObject.transform); mission.StartingSpawn = start;
            mission.Checkpoints = new MissionCheckpoint[layout.CheckpointRoofs.Length];
            for (int i = 0; i < layout.CheckpointRoofs.Length; i++)
            {
                var go = new GameObject("Rooftop checkpoint " + (i + 1)); go.transform.SetParent(missionObject.transform);
                go.transform.position = layout.CheckpointRoofs[i] + Vector3.up * 2;
                var box = go.AddComponent<BoxCollider>(); box.size = new Vector3(7, 5, 7); box.isTrigger = true;
                var checkpoint = go.AddComponent<MissionCheckpoint>(); checkpoint.Mission = mission; checkpoint.Index = i;
                checkpoint.ObjectiveLabel = i == layout.CheckpointRoofs.Length - 1 ? "Reach the bank rooftop" : "Cross to rooftop checkpoint " + (i + 1);
                var safe = new GameObject("Safe respawn").transform; safe.SetParent(go.transform); safe.position = layout.CheckpointRoofs[i];
                checkpoint.SafeSpawn = safe; mission.Checkpoints[i] = checkpoint;
            }
            var encounter = missionObject.AddComponent<EncounterDirector>();
            encounter.Player = player; encounter.Arena = layout.BankArena; mission.Encounter = encounter;
            var enemyPrefab = MakeEnemyPrefab(); encounter.Enemies = new ThugAI[3];
            Vector3[] enemyOffsets = { new Vector3(-2, 0, -1), new Vector3(2, 0, -1), new Vector3(0, 0, 1.4f) };
            for (int i = 0; i < 3; i++)
            {
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
                enemy.name = "Bank thug " + (i + 1); enemy.transform.position = layout.BankArena.center + enemyOffsets[i];
                var ai = enemy.GetComponent<ThugAI>(); ai.Player = player; ai.Ranged = i == 2;
                var steering = enemy.GetComponent<SurfaceSteeringMotor>(); steering.LimitToArea = true; steering.WalkArea = layout.BankArena;
                ai.PatrolPoints = new Transform[2];
                for (int p = 0; p < 2; p++)
                {
                    var point = new GameObject(enemy.name + " patrol " + p).transform;
                    point.SetParent(missionObject.transform); point.position = enemy.transform.position + Vector3.forward * (p == 0 ? -.6f : .6f);
                    ai.PatrolPoints[p] = point;
                }
                encounter.Enemies[i] = ai;
            }
            var entry = new GameObject("Skylight interaction").AddComponent<SkylightInteraction>();
            entry.transform.position = layout.Skylight; entry.Mission = mission; mission.Skylight = entry;
            mission.Intro = CreateIntro(mission, combat.Audio);
            var hudObject = new GameObject("Mission HUD"); var document = hudObject.AddComponent<UIDocument>();
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/_Game/Settings/Hud.asset");
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Game/UI/MissionHud.uxml");
            var hud = hudObject.AddComponent<MissionHud>(); hud.Mission = mission; hud.Dialogue = mission.Intro.Presenter;
            var brain = Camera.main.GetComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, .5f);
            missionObject.AddComponent<MissionSmokeRunner>().Mission = mission;
            Phase1SceneBuilder.ValidateNoScaffolding();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = TestScenes();
            PlayerSettings.productName = "Spider-Man Mission 1"; AssetDatabase.SaveAssets();
            Debug.Log("MISSION1_READY: " + layout.CheckpointRoofs.Length + " ordered checkpoints / 3 bank thugs");
        }
        static LineRenderer Line(string name, Transform parent, Material material, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material;
            line.useWorldSpace = true; line.positionCount = 2; line.widthMultiplier = width;
            line.textureMode = LineTextureMode.Tile; line.enabled = false; line.numCapVertices = 3; return line;
        }
        static void ConfigureWebEffectMaterials()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Generated/WebDome.prefab");
            if (prefab == null) return;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null) continue;
                // The exported web texture carries original half-alpha pixels.
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0); material.SetFloat("_AlphaClip", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHATEST_ON"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
                material.SetShaderPassEnabled("ShadowCaster", false); EditorUtility.SetDirty(material);
            }
        }
        static EditorBuildSettingsScene[] TestScenes() => new[] {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(Phase1SceneBuilder.ScenePath, true) };
        [MenuItem("Rebuild/Mission 1/Build Windows")]
        public static void Build()
        {
            Create();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Mission1/SpiderManMission1.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Mission build failed: " + report.summary.result);
            Debug.Log("MISSION1_BUILD_SUCCEEDED: " + output + " " + report.summary.totalSize);
        }
        static AudioClip Sfx(string name)
        {
            var path = Directory.GetFiles(Root + "/Audio", "*.wav").First(p => Path.GetFileNameWithoutExtension(p).Equals(name, StringComparison.OrdinalIgnoreCase));
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path.Replace('\\', '/'));
        }
        static MissionLayout CreateLayout(GameObject cover)
        {
            string path = Root + "/Generated/MissionLayout.asset";
            var existing = AssetDatabase.LoadAssetAtPath<MissionLayout>(path); if (existing != null) return existing;
            var layout = ScriptableObject.CreateInstance<MissionLayout>();
            Bounds bank = Phase1SceneBuilder.BoundsOf(cover);
            Vector3 center = bank.center + Vector3.up * .06f;
            if (Physics.Raycast(center + Vector3.up, Vector3.down, out var floor, 3, 1, QueryTriggerInteraction.Ignore))
                center.y = floor.point.y + .04f;
            layout.BankArena = new Bounds(center, new Vector3(Mathf.Max(3, bank.size.x - 1.4f), 10, Mathf.Max(3, bank.size.z - 1.4f)));
            layout.Skylight = center + Vector3.forward * .7f;
            var nodes = new List<Vector3>();
            foreach (var collider in GameObject.Find("OriginalL1A1").GetComponentsInChildren<MeshCollider>())
            {
                Bounds b = collider.bounds;
                for (int x = 0; x < 3; x++) for (int z = 0; z < 3; z++)
                {
                    Vector3 ray = new Vector3(Mathf.Lerp(b.min.x, b.max.x, .25f + x * .25f), b.max.y + 1,
                        Mathf.Lerp(b.min.z, b.max.z, .25f + z * .25f));
                    if (!Physics.Raycast(ray, Vector3.down, out var hit, b.size.y + 3, 1, QueryTriggerInteraction.Ignore)
                        || hit.normal.y < .9f) continue;
                    Vector3 point = hit.point + Vector3.up * .04f;
                    if (!ClearRoof(point) || nodes.Any(p => Vector3.Distance(p, point) < 6)) continue;
                    nodes.Add(point);
                }
            }
            Debug.Log("ROUTE_CANDIDATES: " + nodes.Count + " bank=" + center);
            var reverse = new List<Vector3> { center };
            for (int stage = 0; stage < 3; stage++)
            {
                Vector3 target = reverse[reverse.Count - 1];
                var choices = nodes.Where(p => !reverse.Any(r => Vector3.Distance(p, r) < 10))
                    .Where(p => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(target.x, target.z)) >= 12
                        && Vector2.Distance(new Vector2(p.x, p.z), new Vector2(target.x, target.z)) <= 58
                        && Mathf.Abs(p.y - target.y) < 14 && HasGap(p, target))
                    .OrderBy(p => Mathf.Abs(Vector3.Distance(p, target) - 27) + Mathf.Max(0, p.y - target.y) * 2).ToArray();
                if (choices.Length == 0) throw new InvalidOperationException("No safe next rooftop; author MissionLayout using the inspected original geometry.");
                reverse.Add(choices[0]);
            }
            reverse.Reverse(); layout.StartingRoof = reverse[0]; layout.CheckpointRoofs = reverse.Skip(1).ToArray();
            AssetDatabase.CreateAsset(layout, path);
            return layout;
        }
        static bool ClearRoof(Vector3 point)
        {
            if (Physics.CheckBox(point + Vector3.up * .95f, new Vector3(.5f, .85f, .5f), Quaternion.identity, 1, QueryTriggerInteraction.Ignore)) return false;
            foreach (Vector3 offset in new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
                if (!Physics.Raycast(point + offset + Vector3.up * .3f, Vector3.down, out var hit, .7f, 1,
                    QueryTriggerInteraction.Ignore) || hit.normal.y < .85f) return false;
            return true;
        }
        static bool HasGap(Vector3 a, Vector3 b)
        {
            for (int i = 1; i < 6; i++)
                if (!Physics.Raycast(Vector3.Lerp(a, b, i / 6f) + Vector3.up * .6f, Vector3.down, 2, 1,
                    QueryTriggerInteraction.Ignore)) return true;
            return false;
        }
        static AnimatorController MakePlayerAnimator()
        {
            var c = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Generated/Player.controller");
            foreach (string name in new[] { "isSwinging", "isCrawling", "isGrounded" }) c.AddParameter(name, AnimatorControllerParameterType.Bool);
            foreach (string name in new[] { "speed", "swingNormalizedTime", "impactVelocity" }) c.AddParameter(name, AnimatorControllerParameterType.Float);
            c.AddParameter("comboIndex", AnimatorControllerParameterType.Int); c.AddParameter("melee", AnimatorControllerParameterType.Trigger);
            var sm = c.layers[0].stateMachine;
            AnimationClip Clip(int i) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Phase1SceneBuilder.PrivateRoot + "/Clips/spidey_anim_" + i + ".anim");
            var ground = sm.AddState("Locomotion"); ground.motion = Clip(0); sm.defaultState = ground;
            var air = sm.AddState("Airborne"); air.motion = Clip(1);
            var swing = sm.AddState("Swinging"); swing.motion = Clip(1); swing.timeParameter = "swingNormalizedTime"; swing.timeParameterActive = true;
            var crawl = sm.AddState("Crawling"); crawl.motion = Clip(0);
            var attack = sm.AddState("Melee"); attack.motion = Clip(3);
            Transition(ground, air, "isGrounded", false); Transition(air, ground, "isGrounded", true);
            var toSwing = sm.AddAnyStateTransition(swing); toSwing.AddCondition(AnimatorConditionMode.If, 0, "isSwinging"); Setup(toSwing);
            var toCrawl = sm.AddAnyStateTransition(crawl); toCrawl.AddCondition(AnimatorConditionMode.If, 0, "isCrawling"); Setup(toCrawl);
            Transition(swing, air, "isSwinging", false); Transition(crawl, air, "isCrawling", false);
            var punch = sm.AddAnyStateTransition(attack); punch.AddCondition(AnimatorConditionMode.If, 0, "melee"); Setup(punch);
            var done = attack.AddTransition(ground); done.hasExitTime = true; done.exitTime = .95f; done.duration = .1f;
            return c;
        }
        static void Setup(AnimatorStateTransition t) { t.hasExitTime = false; t.duration = .1f; t.canTransitionToSelf = false; }
        static void Transition(AnimatorState from, AnimatorState to, string condition, bool value)
        { var t = from.AddTransition(to); t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, condition); Setup(t); }
        static GameObject MakeEnemyPrefab()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/henchman/henchman.gltf");
            var root = new GameObject("BankThug"); root.layer = 8;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(asset); visual.transform.SetParent(root.transform, false);
            foreach (var old in visual.GetComponentsInChildren<Animation>()) Object.DestroyImmediate(old);
            Bounds b = Phase1SceneBuilder.BoundsOf(visual); float scale = 1.8f / b.size.y;
            visual.transform.localScale = Vector3.one * scale;
            visual.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z) * scale;
            foreach (Transform child in visual.GetComponentsInChildren<Transform>()) child.gameObject.layer = 8;
            Phase1SceneBuilder.ConvertMaterials(visual, "Henchman");
            var health = root.AddComponent<Health>(); health.Team = Team.Enemy; health.Maximum = 90;
            root.AddComponent<SurfaceSteeringMotor>(); var ai = root.AddComponent<ThugAI>();
            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            ai.Animator = animator;
            var avatar = AvatarBuilder.BuildGenericAvatar(visual, ""); avatar.name = "Henchman Generic";
            AssetDatabase.CreateAsset(avatar, Root + "/Generated/HenchmanAvatar.asset"); animator.avatar = avatar; animator.applyRootMotion = false;
            var clips = AssetDatabase.LoadAllAssetsAtPath(Root + "/henchman/henchman.gltf").OfType<AnimationClip>().ToArray();
            var motion = Object.Instantiate(clips.OrderBy(c => c.name).First()); motion.legacy = false;
            AssetDatabase.CreateAsset(motion, Root + "/Generated/HenchmanMotion.anim");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Generated/Henchman.controller");
            controller.AddParameter("state", AnimatorControllerParameterType.Int); controller.AddParameter("webbed", AnimatorControllerParameterType.Bool);
            for (int i = 0; i < 6; i++)
            {
                var state = controller.layers[0].stateMachine.AddState(((ThugState)i).ToString()); state.motion = motion;
                var transition = controller.layers[0].stateMachine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.Equals, i, "state"); Setup(transition);
            }
            animator.runtimeAnimatorController = controller;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Generated/BankThug.prefab"); Object.DestroyImmediate(root); return prefab;
        }
        static CutsceneSequence CreateIntro(MissionManager mission, TraversalAudioRig audio)
        {
            var presenter = new GameObject("Dialogue Presenter").AddComponent<DialoguePresenter>(); presenter.DialogueBus = audio.Dialogue;
            var go = new GameObject("Rooftop intro Timeline"); var director = go.AddComponent<PlayableDirector>();
            var sequence = go.AddComponent<CutsceneSequence>(); sequence.Session = mission.Player.GetComponent<TraversalSession>();
            sequence.Input = mission.Player.GetComponent<TraversalInput>(); sequence.Presenter = presenter;
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>(); timeline.name = "Bank Rooftops Intro";
            AssetDatabase.CreateAsset(timeline, Root + "/Generated/Intro.playable");
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength; timeline.fixedDuration = 8;
            var cameras = timeline.CreateTrack<CinemachineTrack>(null, "Rooftop framing");
            director.SetGenericBinding(cameras, Camera.main.GetComponent<CinemachineBrain>());
            var dialogue = timeline.CreateTrack<DialogueCueTrack>(null, "Mission briefing subtitles");
            director.SetGenericBinding(dialogue, presenter);
            for (int i = 0; i < 2; i++)
            {
                Vector3 focus = i == 0 ? mission.StartingSpawn.position : mission.Encounter.Arena.center;
                var cm = new GameObject(i == 0 ? "Intro exterior camera" : "Bank rooftop camera").AddComponent<CinemachineCamera>();
                cm.transform.position = focus + new Vector3(-7, 5, -9); cm.transform.LookAt(focus + Vector3.up);
                cm.Lens.FieldOfView = 55; cm.Priority = -10;
                string key = "intro_camera_" + i; director.SetReferenceValue(key, cm);
                var shot = cameras.CreateClip<CinemachineShot>(); shot.start = i * 4; shot.duration = 4;
                ((CinemachineShot)shot.asset).VirtualCamera = new ExposedReference<CinemachineVirtualCameraBase> { exposedName = key };
                var line = dialogue.CreateClip<DialogueCueClip>(); line.start = i * 4; line.duration = 4;
                var cue = (DialogueCueClip)line.asset; cue.Speaker = "MISSION BRIEFING";
                cue.Subtitle = i == 0 ? "A bank robbery is underway. Cross the rooftops and reach the bank." :
                    "Clear the rooftop guards, then enter through the skylight.";
            }
            director.playableAsset = timeline; return sequence;
        }
    }
}
