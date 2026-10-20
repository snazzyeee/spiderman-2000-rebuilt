using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpiderManRebuild
{
    public sealed class MainMenuSmokeRunner : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed;
            public string gpu;
            public List<string> checks = new List<string>(), errors = new List<string>();
        }
        public MainMenuManager Menu;
        static bool running;
        readonly Report report = new Report();
        bool active;
        string output, preview;
        Gamepad pad;
        double began;
        void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();
            active = Array.IndexOf(args, "--menu-smoke") >= 0;
            if (!active) { enabled = false; return; }
            if (running) { active = false; Destroy(gameObject); return; }
            running = true; DontDestroyOnLoad(gameObject); began = Time.realtimeSinceStartupAsDouble;
            output = Arg(args, "--menu-output") ?? Path.Combine(Application.persistentDataPath, "menu-smoke.json");
            preview = Arg(args, "--menu-preview");
            SaveGameStore.OverridePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)), "menu-smoke-save.json");
            pad = InputSystem.AddDevice<Gamepad>("MenuSmokePad");
            report.gpu = SystemInfo.graphicsDeviceName; Application.logMessageReceived += Log;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var mission = FindAnyObjectByType<MissionManager>();
            if (mission == null) return;
            mission.Player.Automation = true; mission.Player.GetComponent<TraversalInput>().Automation = true;
            mission.Player.Motor.Session.SetPaused(false);
        }
        static string Arg(string[] args, string key)
        { int i = Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        IEnumerator Press(GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button));
            yield return null; yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null; yield return null;
        }
        IEnumerator Capture(string page)
        {
            if (string.IsNullOrEmpty(preview)) yield break;
            for (int frame = 0; frame < 3; frame++) yield return null;
            string path = page == "main" ? preview : Path.Combine(Path.GetDirectoryName(preview),
                Path.GetFileNameWithoutExtension(preview) + "-" + page + ".png");
            if (File.Exists(path)) File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
            for (int frame = 0; frame < 15; frame++) yield return null;
            Check("rendered " + page + " preview", File.Exists(path));
        }
        IEnumerator Start()
        {
            if (!active) yield break;
            for (int frame = 0; frame < 30; frame++) yield return null;
            Check("five requested main selections", Menu.MainButtons.Length == 5);
            Check("imported PCM audio registry", Menu.Audio != null && Menu.Audio.Database.Count > 100);
            Check("dedicated mixer buses", Menu.Audio.DialogueBus != null && Menu.Audio.SfxBus != null && Menu.Audio.MusicBus != null);
            Check("original menu bank shift and confirmation", Menu.Audio.Database.TryResolve(Menu.MenuShiftId, out _, out _)
                && Menu.Audio.Database.TryResolve(Menu.MenuConfirmId, out _, out _));
            Check("textured original menu character", FindAnyObjectByType<MainMenuBackdrop>().Hero.GetComponentInChildren<SkinnedMeshRenderer>() != null);
            Check("no capsule controller scaffolding", FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None).Length == 0
                && FindObjectsByType<CharacterController>(FindObjectsSortMode.None).Length == 0);
            yield return Capture("main");
            yield return Press(GamepadButton.DpadDown);
            Check("gamepad navigation selects Options", EventSystem.current.currentSelectedGameObject == Menu.MainButtons[1].gameObject);
            yield return Press(GamepadButton.South);
            Check("gamepad submit opens Options", Menu.CurrentPage == "Options");
            float original = Menu.Audio.GetVolume("Master");
            var slider = Menu.Rows.GetComponentInChildren<Slider>(); slider.value = .6f;
            Check("volume slider reaches mixer preference", Mathf.Abs(Menu.Audio.GetVolume("Master") - .6f) < .01f);
            slider.value = original;
            yield return Capture("options");
            yield return Press(GamepadButton.East);
            Check("gamepad cancel restores main menu", Menu.CurrentPage == "Main");
            Menu.OpenPage("Gallery"); yield return null;
            Check("original gallery opens", Menu.GalleryArt.Length == 32 && Menu.GalleryImage.texture == Menu.GalleryArt[0]);
            yield return Capture("gallery");
            Menu.OpenPage("Credits"); yield return null;
            Check("credits use layout-aware pages and parsed RGB directives", Menu.CreditsPageCount > 1
                && !Menu.Body.text.Contains("(0,55,255)"));
            yield return Capture("credits");
            Menu.OpenPage("Controls"); yield return null;
            Menu.Body.ForceMeshUpdate();
            Check("controls fit reading panel", !Menu.Body.isTextOverflowing);
            yield return Capture("controls");
            Menu.OpenPage("Special"); Check("Special has functioning branches", Menu.Rows.GetComponentsInChildren<Button>().Length >= 3);
            Menu.OpenPage("Training"); Check("Training offers practice and controls", Menu.Rows.GetComponentsInChildren<Button>().Length >= 3);
            Menu.StartNewGame(false);
            MissionManager mission = null;
            for (int frame = 0; frame < 1200; frame++)
            { mission = FindAnyObjectByType<MissionManager>(); if (mission != null && mission.Stage == MissionStage.Intro) break; yield return null; }
            if (mission == null) { Fail("New Game did not load Mission 1"); yield break; }
            for (int frame = 0; frame < 30 && !DialogueManager.Instance.IsSpeaking; frame++) yield return null;
            Check("New Game loads mission intro", mission.Stage == MissionStage.Intro && mission.Player.Motor.Session.ControlBlocked);
            Check("original Stan stream routed from registry", DialogueManager.Instance.ActiveClipId != null
                && DialogueManager.Instance.Database.TryGet(DialogueManager.Instance.ActiveClipId, out var narration)
                && narration.source_path.EndsWith("/stan_00.bik", StringComparison.OrdinalIgnoreCase));
            yield return Capture("intro");
            mission.Intro.Skip(); yield return null; yield return null;
            Check("intro completion exclusively begins blocked briefing", mission.Lifecycle == MissionLifecycle.MissionBriefing
                && mission.Player.Motor.Session.ControlBlocked && DialogueManager.Instance.ActiveClipId == null
                && mission.Intro.Presenter.Subtitle == mission.BriefingText);
            yield return Capture("briefing");
            mission.CompleteBriefing(); yield return null;
            Check("briefing completion releases gameplay", mission.Stage == MissionStage.RooftopRoute
                && !mission.Player.Motor.Session.ControlBlocked && DialogueManager.Instance.ActiveClipId == null);
            Check("checkpoint persisted separately from original saves", SaveGameStore.TryRead(out _, out _));
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null; yield return null;
            Menu = FindAnyObjectByType<MainMenuManager>(); Menu.OpenPage("Game");
            Check("Load Game enabled after checkpoint save", Menu.Rows.GetComponentsInChildren<Button>()[1].interactable);
            Menu.StartContinue();
            for (int frame = 0; frame < 1200; frame++)
            { mission = FindAnyObjectByType<MissionManager>(); if (mission != null && mission.Stage == MissionStage.RooftopRoute) break; yield return null; }
            Check("Load Game resumes checkpoint without replaying intro", mission != null && mission.Stage == MissionStage.RooftopRoute
                && DialogueManager.Instance.ActiveClipId == null);
            Finish();
        }
        void Check(string label, bool passed) { if (passed) report.checks.Add(label); else report.errors.Add(label); }
        void Fail(string message) { report.errors.Add(message); Finish(); }
        void Log(string message, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message); }
        void Update() { if (active && Time.realtimeSinceStartupAsDouble - began > 100) Fail("Menu verification timed out."); }
        void Finish()
        {
            if (!active) return;
            active = false; report.passed = report.errors.Count == 0;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output, JsonUtility.ToJson(report, true)); Application.Quit(report.passed ? 0 : 1);
        }
        void OnDestroy()
        {
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            Application.logMessageReceived -= Log;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
