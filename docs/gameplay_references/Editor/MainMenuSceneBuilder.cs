using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Playables;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Timeline;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpiderManRebuild.Editor
{
    public static class MainMenuSceneBuilder
    {
        public const string Root = "Assets/_LocalReference/Frontend";
        public const string ScenePath = Root + "/MainMenu.unity";
        static TMP_FontAsset font;
        static Sprite underline;
        [MenuItem("Rebuild/Main Menu/Assemble Menu and Connect Mission")]
        public static void Create()
        {
            if (!File.Exists(Root + "/Art/Title.bmp")) throw new FileNotFoundException("Run prepare_frontend_assets.py first.");
            if (!File.Exists(DialogueAssetBuilder.DatabasePath)) DialogueAssetBuilder.BuildDatabase();
            EnsureTmpResources();
            Directory.CreateDirectory(Root + "/Generated"); AssetDatabase.Refresh(); font = CreateFont();
            var database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DialogueAssetBuilder.DatabasePath);
            ConnectMission(database);
            if (File.Exists(ScenePath))
            { EditorSceneManager.OpenScene(ScenePath); ConfigureBuildScenes(); Debug.Log("Preserving existing main menu scene."); return; }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.25f, .3f, .45f); RenderSettings.fog = false;
            var camera = new GameObject("Menu Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 1.55f;
            camera.transform.SetPositionAndRotation(new Vector3(0, .05f, 6), Quaternion.Euler(0, 180, 0));
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.012f, .018f, .065f);
            camera.nearClipPlane = .05f; camera.farClipPlane = 50; camera.gameObject.AddComponent<AudioListener>();
            var key = new GameObject("Warm comic key").AddComponent<Light>(); key.type = LightType.Directional;
            key.transform.rotation = Quaternion.Euler(30, 150, 0); key.color = new Color(1, .88f, .7f); key.intensity = 2;
            key.shadows = LightShadows.Soft;
            var rim = new GameObject("Blue rim").AddComponent<Light>(); rim.type = LightType.Point;
            rim.transform.position = new Vector3(-2, 1, -1); rim.color = new Color(.15f, .45f, 1); rim.intensity = 8; rim.range = 7;
            var service = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueAssetBuilder.ServicePath);
            PrefabUtility.InstantiatePrefab(service);
            var hero = CreateHero();
            Texture2D web = WebTexture();
            var background = MakeCanvas("Background layers", RenderMode.ScreenSpaceCamera, camera);
            background.planeDistance = 10; background.sortingOrder = -10;
            var webRect = FullRect("High resolution web", background.transform);
            webRect.gameObject.AddComponent<RawImage>().texture = web;
            var emblem = Rect("Original shell emblem", background.transform, Vector2.zero, new Vector2(1250, 940));
            var shell = emblem.gameObject.AddComponent<RawImage>(); shell.texture = Texture("shell_bg.bmp"); shell.color = new Color(1, 1, 1, .28f);
            var canvas = MakeCanvas("Main Menu UI", RenderMode.ScreenSpaceOverlay, null);
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            var menu = canvas.gameObject.AddComponent<MainMenuManager>();
            menu.MenuShiftId = Find(database, "/menu.kat/000.wav"); menu.MenuConfirmId = Find(database, "/menu.kat/001.wav");
            menu.MenuMusicId = Find(database, "/song_full.bik");
            Label("SPIDER-MAN", canvas.transform, new Vector2(0, 430), new Vector2(1500, 140), 120, TextAlignmentOptions.Center);
            var edition = Label("2000  /  REVAMP", canvas.transform, new Vector2(0, 330), new Vector2(1000, 50), 26, TextAlignmentOptions.Center);
            edition.color = new Color(1, .8f, .12f);
            menu.Description = Label("", canvas.transform, new Vector2(0, -360), new Vector2(1400, 80), 28, TextAlignmentOptions.Center);
            var hint = Label("ARROWS / D-PAD   NAVIGATE     ENTER / A   SELECT     ESC / B   BACK", canvas.transform,
                new Vector2(0, -475), new Vector2(1600, 50), 22, TextAlignmentOptions.Center); hint.color = new Color(.7f, .76f, .88f);
            menu.MainRoot = Rect("Main selections", canvas.transform, Vector2.zero, new Vector2(1920, 1080)).gameObject;
            string[] titles = { "GAME", "OPTIONS", "TRAINING", "SPECIAL", "GALLERY" };
            string[] descriptions = { "New game or load a saved rooftop checkpoint.", "Audio, camera and display settings.",
                "Rooftop practice and modern controls.", "Original music archive and credits.", "Browse the original comic collection." };
            Vector2[] positions = { new Vector2(-600, 190), new Vector2(-670, 20), new Vector2(-585, -150), new Vector2(520, -135), new Vector2(610, 65) };
            menu.MainButtons = new Button[titles.Length];
            for (int i = 0; i < titles.Length; i++)
            {
                menu.MainButtons[i] = Button(titles[i], menu.MainRoot.transform, positions[i], new Vector2(350, 85), 48);
                menu.MainButtons[i].GetComponent<MenuSelectionEffect>().Description = descriptions[i];
            }
            var page = Rect("Submenu", canvas.transform, Vector2.zero, new Vector2(1920, 1080)); menu.PageRoot = page.gameObject;
            var panel = Rect("Comic panel", page, new Vector2(545, 0), new Vector2(690, 780));
            panel.gameObject.AddComponent<Image>().color = new Color(.018f, .025f, .07f, .96f);
            var border = panel.gameObject.AddComponent<Outline>(); border.effectColor = new Color(.75f, .045f, .08f); border.effectDistance = new Vector2(6, -6);
            menu.PageTitle = Label("GAME", panel, new Vector2(0, 325), new Vector2(610, 90), 66, TextAlignmentOptions.Left);
            menu.Rows = Rect("Rows", panel, new Vector2(0, -10), new Vector2(590, 570));
            var layout = menu.Rows.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 9;
            layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;
            menu.Status = Label("", panel, new Vector2(0, -340), new Vector2(610, 70), 22, TextAlignmentOptions.Center);
            var textPanel = Rect("Reading panel", page, new Vector2(-460, 0), new Vector2(790, 730));
            textPanel.gameObject.AddComponent<Image>().color = new Color(.015f, .025f, .065f, .97f);
            menu.Body = Label("", textPanel, Vector2.zero, new Vector2(730, 680), 24, TextAlignmentOptions.TopLeft);
            // Body visibility controls its containing reading panel too.
            textPanel.gameObject.SetActive(false);
            var galleryFrame = Rect("Gallery frame", page, new Vector2(-460, 0), new Vector2(780, 650));
            var gallery = FullRect("Comic art", galleryFrame);
            menu.GalleryImage = gallery.gameObject.AddComponent<RawImage>();
            gallery.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            menu.GalleryArt = Enumerable.Range(0, 32).Select(index => Texture("cb" + index.ToString("00") + ".bmp")).ToArray();
            menu.Credits = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Art/credits.txt");
            var templateRoot = new GameObject("Templates");
            var row = Button("ROW", templateRoot.transform, Vector2.zero, new Vector2(590, 55), 32);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 55;
            menu.RowPrefab = PrefabUtility.SaveAsPrefabAsset(row.gameObject, Root + "/Generated/MenuRow.prefab").GetComponent<Button>();
            var slider = MakeSlider(templateRoot.transform);
            menu.SliderPrefab = PrefabUtility.SaveAsPrefabAsset(slider.gameObject, Root + "/Generated/MenuSlider.prefab").GetComponent<Slider>();
            Object.DestroyImmediate(templateRoot);
            var fade = FullRect("Transition", canvas.transform); fade.gameObject.AddComponent<Image>().color = Color.black;
            menu.Fade = fade.gameObject.AddComponent<CanvasGroup>(); menu.Fade.alpha = 0; menu.Fade.blocksRaycasts = false;
            var eventSystem = new GameObject("Menu Event System").AddComponent<EventSystem>();
            var input = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>(); input.AssignDefaultActions();
            input.moveRepeatDelay = .32f; input.moveRepeatRate = .09f;
            var backdrop = new GameObject("Menu atmosphere").AddComponent<MainMenuBackdrop>(); backdrop.Hero = hero.transform; backdrop.WebLayer = webRect;
            AddParticles();
            new GameObject("Menu verification").AddComponent<MainMenuSmokeRunner>().Menu = menu;
            menu.PageRoot.SetActive(false);
            Phase1SceneBuilder.ValidateNoScaffolding();
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets(); ConfigureBuildScenes();
            Debug.Log("MAIN_MENU_READY: 5 branches / 32 original gallery images / imported audio");
        }
        static void EnsureTmpResources()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return;
            throw new FileNotFoundException("Run prepare_frontend_assets.py to unpack the bundled TMP essentials before starting Unity.");
        }
        static TMP_FontAsset CreateFont()
        {
            string path = Root + "/Generated/Menu SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path); if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/MenuDisplay.ttf");
            if (source == null) throw new InvalidDataException("Private display font missing.");
            var asset = TMP_FontAsset.CreateFontAsset(source, 90, 10, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            asset.name = "Menu Display SDF"; AssetDatabase.CreateAsset(asset, path);
            asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,;:!?'-/()%+&=\n…—·");
            foreach (var atlas in asset.atlasTextures) if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, asset);
            asset.material.SetFloat("_OutlineWidth", .14f); asset.material.SetColor("_OutlineColor", new Color(.06f, .015f, .04f));
            AssetDatabase.AddObjectToAsset(asset.material, asset); EditorUtility.SetDirty(asset); return asset;
        }
        static void ConnectMission(DialogueDatabase database)
        {
            Mission1SceneBuilder.Create(); EditorSceneManager.OpenScene(Mission1SceneBuilder.ScenePath);
            var mission = Object.FindAnyObjectByType<MissionManager>();
            if (mission.GetComponent<MissionMenuBridge>() == null) mission.gameObject.AddComponent<MissionMenuBridge>();
            if (Object.FindAnyObjectByType<DialogueManager>() == null)
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DialogueAssetBuilder.ServicePath));
            var timeline = mission.Intro.GetComponent<PlayableDirector>().playableAsset as TimelineAsset;
            string opening = Find(database, "/stan_00.bik");
            if (!database.TryResolve(opening, out _, out AudioClip narration)) throw new InvalidDataException("Original narrator clip missing.");
            double duration = narration.length;
            foreach (var track in timeline.GetOutputTracks())
            {
                var clips = track.GetClips().ToArray();
                if (track is DialogueCueTrack)
                {
                    for (int i = 1; i < clips.Length; i++) timeline.DeleteClip(clips[i]);
                    var cue = (DialogueCueClip)clips[0].asset; cue.DatabaseClipId = opening;
                    cue.Speaker = ""; cue.Subtitle = "";
                    clips[0].start = 0; clips[0].duration = duration; EditorUtility.SetDirty(cue);
                }
                else for (int i = 0; i < clips.Length; i++) { clips[i].start = i * duration / clips.Length; clips[i].duration = duration / clips.Length; }
                EditorUtility.SetDirty(track);
            }
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength; timeline.fixedDuration = duration; EditorUtility.SetDirty(timeline);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        }
        public static string Find(DialogueDatabase database, string suffix)
        {
            var matches = database.Entries.Where(entry => entry.source_path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) throw new InvalidDataException("Missing or ambiguous original source: " + suffix);
            return matches[0].clip_id;
        }
        static Canvas MakeCanvas(string name, RenderMode mode, Camera camera)
        {
            var root = new GameObject(name, typeof(RectTransform)); var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = mode; canvas.worldCamera = camera;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f; return canvas;
        }
        static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.gameObject.layer = 5;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        static RectTransform FullRect(string name, Transform parent)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.zero); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        static TMP_Text Label(string text, Transform parent, Vector2 position, Vector2 size, int pointSize, TextAlignmentOptions alignment)
        {
            var label = Rect(text.Length > 0 ? text : "Text", parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = pointSize; label.text = text; label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal; label.raycastTarget = false; label.color = Color.white;
            return label;
        }
        static Button Button(string title, Transform parent, Vector2 position, Vector2 size, int pointSize)
        {
            var rect = Rect(title, parent, position, size); var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white; var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = new Color(.025f, .04f, .1f, .94f);
            colors.highlightedColor = colors.selectedColor = new Color(.5f, .025f, .055f, 1);
            colors.pressedColor = new Color(.8f, .04f, .045f); colors.disabledColor = new Color(.09f, .09f, .12f, .6f); button.colors = colors;
            var label = Label(title, rect, Vector2.zero, size - new Vector2(28, 8), pointSize, TextAlignmentOptions.Center);
            var accentRect = Rect("Web underline", rect, new Vector2(0, -size.y * .5f + 2), new Vector2(size.x, 4));
            var accent = accentRect.gameObject.AddComponent<Image>(); accent.color = new Color(.8f, .1f, .1f, .6f);
            if (underline == null)
            {
                underline = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Generated/WebUnderline.asset");
            }
            if (underline == null)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_LocalReference/Mission1/webdome2/Textures/image_000.png");
                underline = Sprite.Create(texture, new UnityEngine.Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                AssetDatabase.CreateAsset(underline, Root + "/Generated/WebUnderline.asset");
            }
            accent.sprite = underline;
            var effect = rect.gameObject.AddComponent<MenuSelectionEffect>(); effect.Label = label; effect.Accent = accent;
            return button;
        }
        static Slider MakeSlider(Transform parent)
        {
            var rect = Rect("Audio slider", parent, Vector2.zero, new Vector2(590, 58));
            var label = Label("VOLUME", rect, new Vector2(-150, 0), new Vector2(270, 54), 26, TextAlignmentOptions.Left);
            var background = Rect("Track", rect, new Vector2(145, 0), new Vector2(270, 10)); background.gameObject.AddComponent<Image>().color = new Color(.2f, .24f, .35f);
            var fillArea = Rect("Fill Area", rect, new Vector2(145, 0), new Vector2(250, 10));
            var fill = FullRect("Fill", fillArea); fill.gameObject.AddComponent<Image>().color = new Color(.9f, .12f, .12f);
            var handleArea = Rect("Handle Area", rect, new Vector2(145, 0), new Vector2(250, 25));
            var handle = Rect("Handle", handleArea, Vector2.zero, new Vector2(15, 28)); var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(1, .8f, .1f);
            var slider = rect.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handleImage;
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
            var effect = rect.gameObject.AddComponent<MenuSelectionEffect>(); effect.Label = label; return slider;
        }
        static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/" + name);
        static GameObject CreateHero()
        {
            var root = new GameObject("Original Spider-Man menu pose");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LocalReference/Phase1/SpiderMan/spidey.gltf"));
            model.transform.SetParent(root.transform, false);
            foreach (var animation in model.GetComponentsInChildren<Animation>()) Object.DestroyImmediate(animation);
            foreach (var animator in model.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(animator);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_LocalReference/Phase1/Clips/spidey_anim_0.anim");
            if (clip != null) clip.SampleAnimation(model, .15f);
            Bounds bounds = Phase1SceneBuilder.BoundsOf(model);
            model.transform.localScale *= 1.8f / bounds.size.y;
            bounds = Phase1SceneBuilder.BoundsOf(model); model.transform.position -= new Vector3(bounds.center.x, bounds.min.y + .9f, bounds.center.z);
            Phase1SceneBuilder.ConvertMaterials(model, "MenuHero");
            var detail = FabricNormal();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>()) foreach (var material in renderer.sharedMaterials)
            {
                material.SetFloat("_Smoothness", .3f); material.SetTexture("_BumpMap", detail); material.SetFloat("_BumpScale", .2f);
                material.EnableKeyword("_NORMALMAP"); EditorUtility.SetDirty(material);
            }
            return root;
        }
        static Texture2D FabricNormal()
        {
            string path = Root + "/Generated/FabricNormal.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false); var pixels = new Color32[1024 * 1024];
                for (int y = 0; y < 1024; y++) for (int x = 0; x < 1024; x++)
                { float nx = Mathf.Cos(x * Mathf.PI / 4) * .18f, ny = Mathf.Cos(y * Mathf.PI / 4) * .18f;
                    pixels[y * 1024 + x] = new Color(nx * .5f + .5f, ny * .5f + .5f, Mathf.Sqrt(1 - nx * nx - ny * ny), 1); }
                texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.NormalMap;
                importer.wrapMode = TextureWrapMode.Repeat; importer.maxTextureSize = 1024; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Texture2D WebTexture()
        {
            string path = Root + "/Generated/ComicWeb.png";
            if (!File.Exists(path))
            {
                const int width = 2048, height = 1024;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false); var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    float px = (x - width * .5f) / height, py = (y - height * .5f) / height;
                    float radius = Mathf.Sqrt(px * px + py * py), angle = Mathf.Atan2(py, px);
                    float spoke = Mathf.Abs(Mathf.Sin(angle * 12)) * Mathf.Max(.03f, radius);
                    float ring = Mathf.Abs(Mathf.Sin((radius + .009f * Mathf.Cos(angle * 24)) * 48));
                    float line = Mathf.Max(1 - Mathf.SmoothStep(.001f, .004f, spoke), 1 - Mathf.SmoothStep(.025f, .06f, ring));
                    float dot = x % 8 < 2 && y % 8 < 2 ? .016f : 0;
                    Color color = Color.Lerp(new Color(.018f + dot, .027f + dot, .11f + dot), new Color(.08f, .17f, .34f), line * .65f);
                    color *= Mathf.Lerp(1.3f, .4f, Mathf.Clamp01(radius)); color.a = 1; pixels[y * width + x] = color;
                }
                texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static void AddParticles()
        {
            var particles = new GameObject("Ambient web motes").AddComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.startLifetime = 8; main.startSpeed = .025f; main.startSize = .015f; main.maxParticles = 50;
            main.startColor = new Color(.7f, .8f, 1, .35f); var emission = particles.emission; emission.rateOverTime = 5;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 3;
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_LocalReference/Mission1/webdome2/Textures/image_000.png"));
            AssetDatabase.CreateAsset(material, Root + "/Generated/AmbientWeb.mat"); particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            particles.Play();
        }
        static void ConfigureBuildScenes()
        { EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(Mission1SceneBuilder.ScenePath, true), new EditorBuildSettingsScene(Phase1SceneBuilder.ScenePath, true) }; }
        [MenuItem("Rebuild/Main Menu/Build Windows")]
        public static void Build()
        {
            Create(); string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Frontend/SpiderManRevamp.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output)); PlayerSettings.productName = "Spider-Man 2000 Revamp";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath, Mission1SceneBuilder.ScenePath },
                target = BuildTarget.StandaloneWindows64, locationPathName = output, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("Frontend build failed.");
            Debug.Log("FRONTEND_BUILD_SUCCEEDED: " + report.summary.totalSize);
        }
    }
}
