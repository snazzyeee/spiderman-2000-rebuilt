using System;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace SpiderManRebuild.Editor
{
    public static class LabSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/TraversalLab.unity";
        const string Root = "Assets/_Game/";

        [MenuItem("Rebuild/Create Missing Traversal Laboratory")]
        public static void Create()
        {
            if (File.Exists(ScenePath))
            {
                Debug.Log("TraversalLab already exists; preserving authored scene.");
                return;
            }
            foreach (string folder in new[] { "Scenes", "Prefabs", "Materials", "Settings" })
                Directory.CreateDirectory(Root + folder);
            AssetDatabase.Refresh();
            MovementTuning movement = Settings<MovementTuning>("MovementTuning");
            CameraTuning cameraTuning = Settings<CameraTuning>("CameraTuning");
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root + "Runtime/Input/GameControls.inputactions");
            if (actions == null) throw new InvalidOperationException("GameControls input asset did not import.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rig = new GameObject("TraversalRig");
            var systems = Child("Systems", rig.transform);
            var input = systems.AddComponent<PlayerInputReader>(); input.Configure(actions);
            var session = systems.AddComponent<GameSession>(); session.Configure(input);
            var timing = systems.AddComponent<FrameTimeRecorder>();
            systems.AddComponent<GameBootstrap>();
            systems.AddComponent<LabSmokeRunner>();

            var spawn = Child("Spawn", rig.transform).AddComponent<SpawnPoint>();
            spawn.transform.position = new Vector3(0, .08f, -3);
            var player = Child("Player", rig.transform);
            player.layer = 2;
            player.transform.position = spawn.transform.position;
            var capsule = player.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = .35f;
            capsule.center = Vector3.up * .9f; capsule.skinWidth = .03f;
            capsule.stepOffset = .3f; capsule.slopeLimit = 50;
            var motor = player.AddComponent<CharacterMotor>(); motor.Configure(movement);
            var controller = player.AddComponent<PlayerController>();
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "PlaceholderBody"; body.layer = 2;
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = Vector3.up * .9f;
            body.transform.localScale = new Vector3(.7f, .9f, .7f);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = Material("PlayerRed", new Color(.78f, .12f, .17f));
            Box("Facing", body.transform, new Vector3(0, .15f, .48f), new Vector3(.24f, .12f, .08f),
                Material("White", new Color(.85f, .92f, .95f)), false).layer = 2;

            var cameraObject = Child("Main Camera", rig.transform);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.nearClipPlane = .1f; camera.farClipPlane = 250;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.18f, .28f, .38f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var brain = cameraObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
            var target = Child("CameraTarget", rig.transform);
            var cameraRigObject = Child("CameraRig", rig.transform);
            var cameraRig = cameraRigObject.AddComponent<ThirdPersonCameraRig>();
            var cm = cameraRigObject.AddComponent<CinemachineCamera>();
            cm.Follow = target.transform;
            cm.Lens.FieldOfView = 65;
            cm.Lens.NearClipPlane = .1f; cm.Lens.FarClipPlane = 250;
            var follow = cameraRigObject.AddComponent<CinemachineThirdPersonFollow>();
            follow.CameraDistance = 6;
            follow.ShoulderOffset = new Vector3(.65f, 0, 0);
            follow.VerticalArmLength = .25f; follow.CameraSide = 1;
            follow.Damping = new Vector3(.08f, .12f, .08f);
            follow.AvoidObstacles = new CinemachineThirdPersonFollow.ObstacleSettings
            {
                Enabled = true, CollisionFilter = 1, IgnoreTag = "",
                CameraRadius = .2f, DampingIntoCollision = 0, DampingFromCollision = .3f
            };
            cameraRig.Configure(player.transform, target.transform, cm, input, session, cameraTuning);
            var respawn = systems.AddComponent<RespawnController>();
            respawn.Configure(motor, cameraRig, input, session, spawn);
            controller.Configure(input, motor, cameraRig, session, body.transform);

            var panel = Settings<PanelSettings>("HudPanel");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.match = .5f;
            EditorUtility.SetDirty(panel);
            var hudObject = Child("HUD", rig.transform);
            var document = hudObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Root + "UI/PrototypeHud.uxml");
            hudObject.AddComponent<PrototypeHud>().Configure(motor, session, respawn, timing);
            PrefabUtility.SaveAsPrefabAssetAndConnect(rig, Root + "Prefabs/TraversalRig.prefab", InteractionMode.AutomatedAction);

            var world = new GameObject("RooftopCourse").transform;
            var concrete = Material("Concrete", new Color(.25f, .32f, .39f));
            var trim = Material("RoofTrim", new Color(.43f, .51f, .57f));
            var dark = Material("City", new Color(.13f, .20f, .28f));
            var cyan = Material("Cyan", new Color(.05f, .85f, .8f));
            var yellow = Material("SafetyYellow", new Color(.95f, .58f, .16f));
            Roof("StartRoof", world, new Vector3(0, 0, 0), new Vector2(18, 14), concrete, trim);
            Roof("JumpRoof", world, new Vector3(0, 1, 17), new Vector2(14, 12), concrete, trim);
            Roof("TurnRoof", world, new Vector3(16, 2, 17), new Vector2(10, 12), concrete, trim);
            Roof("GoalRoof", world, new Vector3(16, 2, 34), new Vector2(12, 12), concrete, trim);
            // Mark jump take-off and landing zones; gaps require running jumps.
            foreach (Vector3 mark in new[] { new Vector3(0, .03f, 5.5f), new Vector3(0, 1.03f, 12),
                new Vector3(5.5f, 1.03f, 17), new Vector3(12, 2.03f, 17), new Vector3(16, 2.03f, 21.5f),
                new Vector3(16, 2.03f, 29) })
                Box("RouteMarker", world, mark, new Vector3(2.5f, .04f, .6f), yellow, false);
            Box("HVAC", world, new Vector3(-5, .8f, 1), new Vector3(2, 1.6f, 2.5f), trim);
            Box("Step", world, new Vector3(4, .12f, 0), new Vector3(2, .24f, 2), trim);
            // A close wall is available for testing camera occlusion at the starting roof.
            Box("CameraTestWall", world, new Vector3(-7.8f, 1.5f, -1), new Vector3(.4f, 3, 5), trim);
            for (int i = 0; i < 16; i++)
            {
                float x = i % 2 == 0 ? -28 - i * 2 : 37 + i;
                float z = -20 + (i / 2) * 13;
                float height = 8 + (i * 7 % 17);
                Box("Skyline", world, new Vector3(x, -17 + height * .5f, z),
                    new Vector3(8 + i % 4, height, 8), dark);
            }
            var beacon = Box("GoalBeacon", world, new Vector3(16, 3.5f, 35), new Vector3(.35f, 3, .35f), cyan, false);
            var goalTrigger = beacon.AddComponent<BoxCollider>();
            goalTrigger.size = new Vector3(8, 1.5f, 8); goalTrigger.isTrigger = true;
            beacon.AddComponent<LabGoal>().Configure(session);
            var kill = Child("KillVolume", world);
            kill.transform.position = new Vector3(8, -13, 12);
            var killBox = kill.AddComponent<BoxCollider>();
            killBox.size = new Vector3(160, 2, 160); killBox.isTrigger = true;
            kill.AddComponent<KillVolume>().Configure(respawn);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.8f;
            sun.color = new Color(1, .89f, .77f); sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(43, -32, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.5f, .61f, .74f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = camera.backgroundColor;
            RenderSettings.fogStartDistance = 55; RenderSettings.fogEndDistance = 160;
            QualitySettings.SetQualityLevel(1, true);
            QualitySettings.vSyncCount = 0;
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            pipeline.msaaSampleCount = 2; pipeline.shadowDistance = 60;
            EditorUtility.SetDirty(pipeline);
            PlayerSettings.companyName = "Independent Rebuild";
            PlayerSettings.productName = "Traversal Laboratory";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("TRAVERSAL_LAB_CREATED: " + ScenePath);
        }

        static T Settings<T>(string name) where T : ScriptableObject
        {
            string path = Root + "Settings/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        static Material Material(string name, Color color)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .18f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        static GameObject Child(string name, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go;
        }
        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size,
            Material material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static void Roof(string name, Transform parent, Vector3 top, Vector2 size, Material body, Material trim)
        {
            Box(name, parent, top - Vector3.up * 4, new Vector3(size.x, 8, size.y), body);
            Box(name + "Surface", parent, top - Vector3.up * .06f, new Vector3(size.x + .1f, .12f, size.y + .1f), trim);
        }
    }
}
