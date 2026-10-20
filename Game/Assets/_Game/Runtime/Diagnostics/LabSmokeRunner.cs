using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpiderManRebuild
{
    // Runs only when explicitly requested on our own test build.
    public sealed class LabSmokeRunner : MonoBehaviour
    {
        [Serializable] public sealed class Result
        {
            public bool passed;
            public string unityVersion;
            public string graphicsDevice;
            public string utc;
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
        }
        readonly Result result = new Result();
        string output, preview;
        bool active;
        PlayerInputReader input;
        CharacterMotor motor;
        GameSession session;
        RespawnController respawn;
        double began;

        void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();
            active = Array.IndexOf(args, "--smoke-test") >= 0;
            if (!active) { enabled = false; return; }
            input = FindAnyObjectByType<PlayerInputReader>();
            input.Automation = true; // Before session Start: never capture the user's cursor during automation.
            output = Argument(args, "--smoke-output");
            preview = Argument(args, "--capture-preview");
            if (string.IsNullOrEmpty(output))
                output = Path.Combine(Application.persistentDataPath, "smoke-result.json");
            result.unityVersion = Application.unityVersion;
            result.graphicsDevice = SystemInfo.graphicsDeviceName;
            result.utc = DateTime.UtcNow.ToString("O");
            Application.logMessageReceived += Log;
            began = Time.realtimeSinceStartupAsDouble;
        }
        static string Argument(string[] args, string key)
        {
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
        IEnumerator Start()
        {
            if (!active) yield break;
            input = FindAnyObjectByType<PlayerInputReader>();
            motor = FindAnyObjectByType<CharacterMotor>();
            session = FindAnyObjectByType<GameSession>();
            respawn = FindAnyObjectByType<RespawnController>();
            input.Automation = true;
            yield return null;
            session.SetPaused(false);
            yield return new WaitForSeconds(.25f);
            Check("grounded at spawn", motor.State == LocomotionState.Grounded);
            Vector3 start = motor.transform.position;
            input.Inject(new PlayerCommand { Move = Vector2.up });
            yield return new WaitForSeconds(.55f);
            Check("command-to-motor forward movement", motor.transform.position.z - start.z > 1.5f);
            float height = motor.transform.position.y;
            input.Inject(new PlayerCommand { JumpPressed = true });
            yield return null;
            input.Inject(default);
            yield return new WaitForSeconds(.22f);
            Check("jump leaves roof", motor.transform.position.y > height + .8f);
            float riseSpeed = motor.Velocity.y;
            input.Inject(new PlayerCommand { JumpPressed = true });
            yield return null;
            input.Inject(default);
            Check("no second jump in air", motor.Velocity.y <= riseSpeed + .01f);
            respawn.ResetPlayer();
            Check("reset returns to spawn", Vector3.Distance(motor.transform.position, respawn.SpawnPosition) < .01f);
            Check("reset clears velocity", motor.Velocity.sqrMagnitude < .001f);
            yield return new WaitForSeconds(.15f);
            input.Inject(new PlayerCommand { PausePressed = true });
            yield return null; yield return null;
            input.Inject(new PlayerCommand { Move = Vector2.up, JumpPressed = true });
            Vector3 pausedPosition = motor.transform.position;
            yield return new WaitForSecondsRealtime(.2f);
            Check("pause freezes movement", session.IsPaused && Time.timeScale == 0
                && Vector3.Distance(pausedPosition, motor.transform.position) < .001f);
            input.Inject(new PlayerCommand { PausePressed = true });
            yield return null; yield return null;
            input.Inject(default);
            Check("resume restores clock", !session.IsPaused && Time.timeScale == 1);
            respawn.ResetPlayer();
            var cameraRig = FindAnyObjectByType<ThirdPersonCameraRig>();
            Quaternion original = cameraRig.YawRotation;
            input.Inject(new PlayerCommand { Look = new Vector2(50, 0) });
            yield return null; yield return null;
            input.Inject(default);
            Check("look rotates camera target", Quaternion.Angle(original, cameraRig.YawRotation) > 1);
            respawn.ResetPlayer();
            // Settle the camera before capturing an image from our generated scene.
            yield return new WaitForSeconds(.6f);
            if (!string.IsNullOrEmpty(preview) && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(preview)));
                ScreenCapture.CaptureScreenshot(preview);
                for (int i = 0; i < 30; i++) yield return null;
                Check("rendered preview written", File.Exists(preview));
            }
            Check("HUD created", FindAnyObjectByType<PrototypeHud>() != null);
            Finish();
        }
        void Update()
        {
            if (active && Time.realtimeSinceStartupAsDouble - began > 45)
            {
                result.errors.Add("Smoke route timed out.");
                Finish();
            }
        }
        void Check(string name, bool passed)
        {
            if (passed) result.checks.Add(name);
            else result.errors.Add("Failed: " + name);
        }
        void Log(string message, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                result.errors.Add(message);
        }
        void Finish()
        {
            active = false;
            result.passed = result.errors.Count == 0;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output, JsonUtility.ToJson(result, true));
            Debug.Log("LAB_SMOKE_RESULT: " + (result.passed ? "PASS" : "FAIL") + " " + output);
            Application.Quit(result.passed ? 0 : 1);
        }
        void OnDestroy() => Application.logMessageReceived -= Log;
    }
}
