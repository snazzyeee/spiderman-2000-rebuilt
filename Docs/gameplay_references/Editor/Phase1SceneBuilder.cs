using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SpiderManRebuild.Editor
{
    public static class Phase1SceneBuilder
    {
        public const string PrivateRoot = "Assets/_LocalReference/Phase1";
        public const string ScenePath = PrivateRoot + "/SpiderManPhase1.unity";
        const string CharacterPath = PrivateRoot + "/SpiderMan/spidey.gltf";
        const string LevelPath = PrivateRoot + "/L1A1/L1A1_G.gltf";

        [MenuItem("Rebuild/Phase 1/Assemble Authentic Assets")]
        public static void Create()
        {
            if (!File.Exists(CharacterPath) || !File.Exists(LevelPath))
                throw new InvalidOperationException("Run python Tools/Reference/prepare_phase1_assets.py from the repository root first.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (File.Exists(ScenePath))
            {
                if (File.Exists(PrivateRoot + "/SpiderManGeneric.avatar"))
                    AssetDatabase.MoveAsset(PrivateRoot + "/SpiderManGeneric.avatar", PrivateRoot + "/SpiderManGeneric.asset");
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                Debug.Log("Preserving existing private Phase 1 scene."); return;
            }
            Directory.CreateDirectory(PrivateRoot + "/Materials");
            Directory.CreateDirectory(PrivateRoot + "/Clips");
            Directory.CreateDirectory("Assets/_Game/Settings");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            if (imported == null) throw new InvalidOperationException("glTFast did not import Spider-Man.");
            var root = new GameObject("SpiderManPlayer");
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(imported);
            visual.name = "OriginalSpiderMan"; visual.transform.SetParent(root.transform, false);
            foreach (var legacy in visual.GetComponentsInChildren<Animation>())
            { legacy.Stop(); legacy.playAutomatically = false; Object.DestroyImmediate(legacy); }
            Bounds characterBounds = BoundsOf(visual);
            float scale = 1.8f / characterBounds.size.y;
            visual.transform.localScale = Vector3.one * scale;
            visual.transform.localPosition = new Vector3(-characterBounds.center.x, -characterBounds.min.y, -characterBounds.center.z) * scale;
            foreach (var transform in visual.GetComponentsInChildren<Transform>()) transform.gameObject.layer = 2;
            ConvertMaterials(visual, "Character");
            var skin = visual.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null || skin.bones.Length != 18)
                throw new InvalidOperationException("Expected the PC Spider-Man's 18-joint skinned rig.");
            var clips = AssetDatabase.LoadAllAssetsAtPath(CharacterPath).OfType<AnimationClip>().ToArray();
            if (clips.Length != 300) throw new InvalidOperationException("Expected 300 original animation slots, got " + clips.Length);
            foreach (var clip in clips)
            {
                var copy = Object.Instantiate(clip); copy.name = clip.name; copy.legacy = false;
                AssetDatabase.CreateAsset(copy, PrivateRoot + "/Clips/" + copy.name + ".anim");
            }
            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            var avatar = AvatarBuilder.BuildGenericAvatar(visual, "");
            avatar.name = "OriginalSpiderManGeneric";
            AssetDatabase.CreateAsset(avatar, PrivateRoot + "/SpiderManGeneric.asset");
            animator.avatar = avatar; animator.applyRootMotion = false;
            if (!avatar.isValid) throw new InvalidOperationException("Imported skeleton produced an invalid Generic avatar.");
            var tuning = AssetDatabase.LoadAssetAtPath<MovementTuning>("Assets/_Game/Settings/Movement.asset");
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<MovementTuning>();
                AssetDatabase.CreateAsset(tuning, "Assets/_Game/Settings/Movement.asset");
            }
            var motor = SpiderManRigSetup.Install(root, visual.transform, tuning);
            var animation = visual.AddComponent<SpiderManAnimationDriver>(); animation.Motor = motor;

            var levelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPath);
            if (levelAsset == null) throw new InvalidOperationException("glTFast did not import L1A1.");
            var level = (GameObject)PrefabUtility.InstantiatePrefab(levelAsset);
            level.name = "OriginalL1A1"; level.transform.localScale = Vector3.one * scale;
            ConvertMaterials(level, "Level");
            foreach (var filter in level.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh; collider.convex = false;
            }
            Physics.SyncTransforms();
            Bounds levelBounds = BoundsOf(level);
            Vector3 spawn = FindRoofSpawn(levelBounds);
            root.transform.position = spawn;
            motor.SpawnPosition = spawn; motor.KillHeight = levelBounds.min.y - 10;
            var camera = UnityEngine.Camera.main;
            camera.backgroundColor = new Color(.12f, .17f, .24f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            motor.Orbit.transform.position = spawn + Vector3.up * motor.Orbit.TargetHeight;
            var mixer = CreateMixer();
            var audio = new GameObject("SpatialAudio").AddComponent<TraversalAudioRig>();
            audio.Player = motor; audio.AltitudeReference = spawn.y;
            audio.Sfx = mixer.FindMatchingGroups("SFX").Single();
            audio.Music = mixer.FindMatchingGroups("Music").Single();
            audio.Dialogue = mixer.FindMatchingGroups("Dialogue").Single();
            CreateHud(motor);
            var sun = new GameObject("Directional Soft Light").AddComponent<Light>();
            sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.intensity = 1.2f;
            sun.color = new Color(.95f, .91f, .85f); sun.transform.rotation = Quaternion.Euler(45, -35, 0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.35f, .42f, .55f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .008f; RenderSettings.fogColor = camera.backgroundColor;
            ConfigureRendering();
            root.AddComponent<Phase1SmokeRunner>();
            ValidateNoScaffolding();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Spider-Man Phase 1";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            File.WriteAllText(PrivateRoot + "/unity-import-report.json", JsonUtility.ToJson(new ImportReport
            {
                bones = skin.bones.Length, clips = clips.Length, levelMeshes = level.GetComponentsInChildren<MeshFilter>().Length,
                metresPerExportUnit = scale, spawn = spawn, genericAvatarValid = avatar.isValid,
                collisionParityVerified = false, animationMappingVerified = false
            }, true));
            Debug.Log("PHASE1_SCENE_READY: bones=18 clips=300 scale=" + scale + " spawn=" + spawn);
        }
        [Serializable] sealed class ImportReport
        {
            public int bones, clips, levelMeshes;
            public float metresPerExportUnit;
            public Vector3 spawn;
            public bool genericAvatarValid, collisionParityVerified, animationMappingVerified;
        }
        internal static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("No original mesh renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        internal static void ConvertMaterials(GameObject go, string prefix)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var originals = renderer.sharedMaterials;
                var remapped = new Material[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    var original = originals[i];
                    string path = PrivateRoot + "/Materials/" + prefix + "_" + original.name.Replace("/", "_") + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = original.name };
                        Texture texture = original.mainTexture;
                        foreach (string property in new[] { "_BaseMap", "baseColorTexture", "_BaseColorTexture", "_MainTex" })
                            if (texture == null && original.HasProperty(property)) texture = original.GetTexture(property);
                        material.SetTexture("_BaseMap", texture);
                        Color color = Color.white;
                        foreach (string property in new[] { "_BaseColor", "baseColorFactor", "_Color" })
                            if (original.HasProperty(property)) { color = original.GetColor(property); break; }
                        material.SetColor("_BaseColor", color);
                        material.SetFloat("_Smoothness", .15f); material.SetFloat("_Metallic", 0);
                        material.SetFloat("_Cull", 0);
                        // Keep extracted cut-out pixels without introducing blended depth sorting.
                        if (original.IsKeywordEnabled("_ALPHATEST_ON") || original.renderQueue == 2450)
                        {
                            material.SetFloat("_AlphaClip", 1); material.SetFloat("_Cutoff", .5f);
                            material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue = 2450;
                        }
                        AssetDatabase.CreateAsset(material, path);
                    }
                    remapped[i] = material;
                }
                renderer.sharedMaterials = remapped;
            }
        }
        static Vector3 FindRoofSpawn(Bounds bounds)
        {
            float score = float.NegativeInfinity; Vector3 result = default;
            for (int x = 1; x < 16; x++) for (int z = 1; z < 16; z++)
            {
                Vector3 origin = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, x / 16f),
                    bounds.max.y + 5, Mathf.Lerp(bounds.min.z, bounds.max.z, z / 16f));
                if (!Physics.Raycast(origin, Vector3.down, out var hit, bounds.size.y + 10, 1, QueryTriggerInteraction.Ignore)
                    || hit.normal.y < .9f) continue;
                Vector3 feet = hit.point + Vector3.up * .04f;
                if (Physics.CheckBox(feet + Vector3.up * .92f, new Vector3(.35f, .87f, .35f),
                    Quaternion.identity, 1, QueryTriggerInteraction.Ignore)) continue;
                float candidate = hit.point.y - Vector2.Distance(new Vector2(hit.point.x, hit.point.z),
                    new Vector2(bounds.center.x, bounds.center.z)) * .15f;
                if (candidate > score) { score = candidate; result = feet; }
            }
            if (float.IsNegativeInfinity(score)) throw new InvalidOperationException("No clear roof spawn found in original mesh.");
            return result;
        }
        static AudioMixer CreateMixer()
        {
            const string path = "Assets/_Game/Settings/Traversal.mixer";
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(path); if (existing != null) return existing;
            // Unity exposes mixer playback publicly, but its asset authoring API is editor-internal.
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Audio.AudioMixerController"))
                .First(t => t != null);
            var mixer = (AudioMixer)type.GetMethod("CreateMixerControllerAtPath").Invoke(null, new object[] { path });
            var master = type.GetProperty("masterGroup").GetValue(mixer);
            foreach (string name in new[] { "SFX", "Music", "Dialogue" })
            {
                var group = type.GetMethod("CreateNewGroup").Invoke(mixer, new object[] { name, false });
                type.GetMethod("AddChildToParent").Invoke(mixer, new[] { group, master });
            }
            EditorUtility.SetDirty(mixer); return mixer;
        }
        static void CreateHud(SpiderManCharacterController motor)
        {
            const string path = "Assets/_Game/Settings/Hud.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (panel == null) { panel = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(panel, path); }
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution = new Vector2Int(1280, 720);
            var go = new GameObject("Phase1 HUD"); var document = go.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Game/UI/PrototypeHud.uxml");
            var hud = go.AddComponent<Phase1Hud>(); hud.Motor = motor; hud.Session = motor.Session;
            hud.Timing = motor.GetComponent<FrameTimeRecorder>();
        }
        static void ConfigureRendering()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            if (!renderer.rendererFeatures.Any(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion"))
            {
                var type = typeof(UniversalRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
                var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
                feature.name = "Retro Modern SSAO";
                var serialized = new SerializedObject(feature);
                serialized.FindProperty("m_Settings.Downsample").boolValue = true;
                serialized.FindProperty("m_Settings.Intensity").floatValue = 1.2f;
                serialized.FindProperty("m_Settings.Radius").floatValue = .35f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                renderer.rendererFeatures.Add(feature); AssetDatabase.AddObjectToAsset(feature, renderer);
                EditorUtility.SetDirty(renderer);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            pipeline.shadowDistance = 70; pipeline.msaaSampleCount = 2;
            EditorUtility.SetDirty(pipeline); QualitySettings.SetQualityLevel(1, true);
        }
        public static void ValidateNoScaffolding()
        {
            if (Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None).Length != 0
                || Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None).Length != 0)
                throw new InvalidOperationException("Capsule-based colliders are forbidden in the Phase 1 scene.");
            foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if (!AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith("Assets/_LocalReference/", StringComparison.Ordinal))
                    throw new InvalidOperationException("Scene mesh is not from the private authentic asset conversion: " + filter.name);
        }
    }
}
