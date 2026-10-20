using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpiderManRebuild
{
    public sealed class MissionSmokeRunner : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed;
            public string gpu;
            public List<string> checks = new List<string>(), errors = new List<string>();
        }
        public MissionManager Mission;
        readonly Report report = new Report();
        string output, preview;
        string previousSavePath;
        bool active;
        double began;
        void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();
            active = Array.IndexOf(args, "--mission-smoke") >= 0;
            if (!active) { enabled = false; return; }
            output = Arg(args, "--smoke-output"); preview = Arg(args, "--capture-preview");
            if (string.IsNullOrEmpty(output)) output = Path.Combine(Application.persistentDataPath, "mission-smoke.json");
            previousSavePath = SaveGameStore.OverridePath;
            SaveGameStore.OverridePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)), "mission-route-smoke-save.json");
            Mission.Player.Automation = true;
            Mission.Player.GetComponent<TraversalInput>().Automation = true;
            began = Time.realtimeSinceStartupAsDouble;
            report.gpu = SystemInfo.graphicsDeviceName;
            Application.logMessageReceived += Log;
        }
        static string Arg(string[] args, string name)
        { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        IEnumerator Start()
        {
            if (!active) yield break;
            yield return null; yield return null; yield return null;
            Check("intro holds gameplay control", Mission.Stage == MissionStage.Intro && Mission.Player.Motor.Session.ControlBlocked);
            Check("intro does not show authored briefing", string.IsNullOrEmpty(Mission.Intro.Presenter.Subtitle));
            Mission.Player.Motor.Session.SetPaused(true);
            yield return new WaitForSecondsRealtime(.15f);
            Check("pause retains cinematic control lock", Mission.Player.Motor.Session.Paused && Mission.Player.Motor.Session.ControlBlocked);
            Mission.Player.Motor.Session.SetPaused(false); Mission.Intro.Skip();
            Check("intro completion starts separate briefing", Mission.Lifecycle == MissionLifecycle.MissionBriefing);
            Mission.CompleteBriefing();
            yield return null;
            Check("intro skip restores gameplay", Mission.Stage == MissionStage.RooftopRoute && !Mission.Player.Motor.Session.ControlBlocked);
            Mission.Player.Health.InvulnerableUntil = Time.time + 90;
            for (int index = 0; index < Mission.Checkpoints.Length; index++)
            {
                Vector3 target = Mission.Checkpoints[index].SafeSpawn.position;
                if (!Physics.Raycast(target + Vector3.up * 2, Vector3.down, out var hit, 4, 1, QueryTriggerInteraction.Ignore))
                { Fail("checkpoint " + index + " has no original ground"); yield break; }
                Vector3 heading = target - Mission.Player.transform.position;
                Mission.Player.Motor.Orbit.SetHeading(Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg);
                Mission.Player.BeginZip(new WebAnchor(hit));
                for (int frame = 0; frame < 480 && Mission.NextCheckpoint == index; frame++)
                {
                    Vector3 toward = target - Mission.Player.transform.position;
                    Vector3 local = Quaternion.Inverse(Mission.Player.Motor.Orbit.Heading)
                        * Vector3.ProjectOnPlane(toward, Vector3.up).normalized;
                    var command = new MissionInputCommand {
                        Crawl = true, Move = Mission.Player.State == PlayerState.Crawling ?
                            Vector2.up : new Vector2(local.x, local.z), Run = true };
                    if (frame > 0 && frame % 90 == 0 && Mission.Player.State != PlayerState.Crawling)
                        Mission.Player.BeginZip(new WebAnchor(hit));
                    Mission.Player.Tick(command, Time.deltaTime, Time.time);
                    yield return null;
                }
                Check("geometry zip reaches ordered checkpoint " + (index + 1), Mission.NextCheckpoint > index);
                if (Mission.NextCheckpoint <= index)
                {
                    report.errors.Add("Traversal stopped at " + Mission.Player.transform.position + " toward " + target
                        + " state=" + Mission.Player.State + " velocity=" + Mission.Player.Motor.Velocity);
                    Finish(); yield break;
                }
            }
            Check("bank encounter locks arena", Mission.Stage == MissionStage.BankEncounter && Mission.Player.Motor.ArenaLocked);
            Check("skylight cannot bypass living enemies", !Mission.Skylight.TryInteract());
            float energyBefore = Mission.Player.Combat.WebEnergy;
            Mission.Player.Combat.WebDome(Time.time);
            Check("web dome spends energy and protects", Mission.Player.Combat.WebEnergy < energyBefore &&
                Mission.Player.Health.InvulnerableUntil > Time.time);
            foreach (ThugAI enemy in Mission.Encounter.Enemies)
            {
                for (int shot = 0; shot < 12 && !enemy.Health.Defeated; shot++)
                {
                    Mission.Player.Combat.FireWebAt(enemy.transform.position + Vector3.up, Time.time);
                    for (int frame = 0; frame < 18; frame++)
                    { Mission.Player.Tick(default, Time.deltaTime, Time.time); yield return null; }
                }
            }
            Check("web shooter defeats encounter", Mission.Stage == MissionStage.Skylight && Mission.Encounter.Remaining == 0);
            Check("last defeat releases arena", !Mission.Player.Motor.ArenaLocked);
            if (Mission.Stage != MissionStage.Skylight) { Finish(); yield break; }
            foreach (ThugAI enemy in Mission.Encounter.Enemies)
                Check("captured guard displays web wrapping: " + enemy.name, enemy.WebWrap != null && enemy.WebWrap.activeInHierarchy);
            Vector3 goal = Mission.Skylight.transform.position;
            for (int frame = 0; frame < 240 && !Mission.Skylight.InRange; frame++)
            {
                Vector3 direction = goal - Mission.Player.transform.position; direction.y = 0;
                Vector3 local = Quaternion.Inverse(Mission.Player.Motor.Orbit.Heading) * direction.normalized;
                Mission.Player.Tick(new MissionInputCommand { Move = new Vector2(local.x, local.z) }, Time.deltaTime, Time.time);
                yield return null;
            }
            Mission.Player.Tick(new MissionInputCommand { Interact = true }, Time.deltaTime, Time.time);
            Check("skylight completes mission", Mission.Stage == MissionStage.Complete);
            // Let UI Toolkit and the cinematic brain present the new state before capture.
            for (int frame = 0; frame < 12; frame++) yield return null;
            if (!string.IsNullOrEmpty(preview))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(preview)));
                ScreenCapture.CaptureScreenshot(preview);
                for (int frame = 0; frame < 25; frame++) yield return null;
                Check("rendered mission preview", File.Exists(preview));
            }
            Finish();
        }
        void Fail(string message) { report.errors.Add(message); Finish(); }
        void Check(string name, bool pass) { if (pass) report.checks.Add(name); else report.errors.Add(name); }
        void Log(string message, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message); }
        void Update() { if (active && Time.realtimeSinceStartupAsDouble - began > 100) Fail("Mission smoke timeout"); }
        void Finish()
        {
            if (!active) return;
            active = false; report.passed = report.errors.Count == 0;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output, JsonUtility.ToJson(report, true)); Application.Quit(report.passed ? 0 : 1);
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= Log;
            if (active || previousSavePath != null || !string.IsNullOrEmpty(output)) SaveGameStore.OverridePath = previousSavePath;
        }
    }
}
