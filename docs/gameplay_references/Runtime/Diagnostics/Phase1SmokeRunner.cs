using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpiderManRebuild
{
    public sealed class Phase1SmokeRunner : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed;
            public string unity, gpu;
            public List<string> checks = new List<string>(), errors = new List<string>();
        }
        readonly Report report = new Report();
        SpiderManCharacterController motor;
        TraversalInput input;
        string output, preview;
        double start;
        bool active;
        void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();
            active = Array.IndexOf(args, "--smoke-test") >= 0;
            if (!active) { enabled = false; return; }
            output = Arg(args, "--smoke-output");
            preview = Arg(args, "--capture-preview");
            if (string.IsNullOrEmpty(output)) output = Path.Combine(Application.persistentDataPath, "phase1-smoke.json");
            motor = GetComponent<SpiderManCharacterController>(); motor.Automation = true;
            input = GetComponent<TraversalInput>(); input.Automation = true;
            Application.logMessageReceived += Log;
            report.unity = Application.unityVersion; report.gpu = SystemInfo.graphicsDeviceName;
            start = Time.realtimeSinceStartupAsDouble;
        }
        static string Arg(string[] args, string key)
        { int i = Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        IEnumerator Start()
        {
            if (!active) yield break;
            yield return null;
            motor.Session.SetPaused(false);
            for (int i = 0; i < 20; i++) { Step(Vector3.zero); yield return null; }
            Check("grounded on original L1A1 mesh", motor.Grounded);
            Check("zero capsule colliders/controllers",
                FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None).Length == 0 &&
                FindObjectsByType<CharacterController>(FindObjectsSortMode.None).Length == 0);
            var skin = motor.GetComponentInChildren<SkinnedMeshRenderer>();
            Check("18 original joints bound to skinned mesh", skin != null && skin.bones.Length == 18);
            Check("textured URP Lit character materials", skin != null && Array.TrueForAll(skin.sharedMaterials,
                m => m != null && m.shader.name == "Universal Render Pipeline/Lit" && m.mainTexture != null));
            var animator = motor.GetComponentInChildren<Animator>();
            Check("valid Generic avatar", animator != null && animator.avatar != null && animator.avatar.isValid);
            Vector3 began = motor.transform.position;
            for (int i = 0; i < 15; i++) { Step(Vector3.forward); yield return null; }
            Check("box sweep movement", Vector3.Distance(began, motor.transform.position) > .2f);
            motor.ResetPlayer();
            for (int i = 0; i < 5; i++) { Step(Vector3.zero); yield return null; }
            float groundY = motor.transform.position.y;
            motor.Simulate(Vector3.zero, false, true, Time.timeAsDouble, 1f / 60);
            for (int i = 0; i < 12; i++) { Step(Vector3.zero); yield return null; }
            Check("jump in imported environment", motor.transform.position.y > groundY + .6f);
            motor.ResetPlayer();
            Check("reset clears velocity and pose", motor.Velocity.sqrMagnitude < .001f
                && Vector3.Distance(motor.transform.position, motor.SpawnPosition) < .001f);
            motor.Session.SetPaused(true);
            Check("pause clock", motor.Session.Paused && Time.timeScale == 0);
            motor.Session.SetPaused(false);
            var audio = FindAnyObjectByType<TraversalAudioRig>();
            Check("three mixer buses", audio != null && audio.Sfx != null && audio.Music != null && audio.Dialogue != null);
            Check("3D web impact pool", audio != null && audio.GetComponentsInChildren<AudioLowPassFilter>().Length == 12);
            for (int i = 0; i < 120; i++) { Step(Vector3.zero); yield return null; }
            if (!string.IsNullOrEmpty(preview))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(preview)));
                ScreenCapture.CaptureScreenshot(preview);
                for (int i = 0; i < 20; i++) yield return null;
                Check("rendered authentic scene", File.Exists(preview));
            }
            Finish();
        }
        void Step(Vector3 direction) => motor.Simulate(direction, false, false, Time.timeAsDouble, 1f / 60);
        void Check(string name, bool pass) { if (pass) report.checks.Add(name); else report.errors.Add(name); }
        void Log(string message, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message); }
        void Update()
        {
            if (active && Time.realtimeSinceStartupAsDouble - start > 50)
            { report.errors.Add("Smoke route timeout"); Finish(); }
        }
        void Finish()
        {
            active = false; report.passed = report.errors.Count == 0;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Application.Quit(report.passed ? 0 : 1);
        }
        void OnDestroy() => Application.logMessageReceived -= Log;
    }
}
