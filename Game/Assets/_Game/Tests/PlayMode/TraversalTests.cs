using System.Collections;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpiderManRebuild.Tests
{
    public sealed class TraversalTests
    {
        PlayerInputReader input;
        CharacterMotor motor;
        GameSession session;
        RespawnController respawn;
        double now;

        [UnitySetUp]
        public IEnumerator LoadLab()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Game/Scenes/TraversalLab.unity");
            input = Object.FindAnyObjectByType<PlayerInputReader>();
            motor = Object.FindAnyObjectByType<CharacterMotor>();
            session = Object.FindAnyObjectByType<GameSession>();
            respawn = Object.FindAnyObjectByType<RespawnController>();
            input.Automation = true;
            session.SetPaused(false);
            yield return null;
            // Direct simulation is deterministic and uses the actual CharacterController collision world.
            motor.GetComponent<PlayerController>().enabled = false;
            now = 0;
            Simulate(Vector3.zero, false, false, 12);
        }
        void Simulate(Vector3 move, bool run, bool jump, int frames)
        {
            Physics.SyncTransforms();
            for (int i = 0; i < frames; i++)
            {
                motor.Step(move, run, jump && i == 0, now, 1f / 60);
                now += 1.0 / 60;
            }
        }
        [Test]
        public void DiagonalMovementDoesNotExceedRunSpeed()
        {
            Simulate(new Vector3(1, 0, 1), true, false, 25);
            Assert.That(motor.Speed, Is.EqualTo(motor.Tuning.RunSpeed).Within(.02));
        }
        [Test]
        public void JumpCannotBeRepeatedInAirAndResetClearsMomentum()
        {
            Simulate(Vector3.zero, false, true, 8);
            Assert.That(motor.transform.position.y, Is.GreaterThan(.8f));
            float ascending = motor.Velocity.y;
            Simulate(Vector3.zero, false, true, 1);
            Assert.That(motor.Velocity.y, Is.LessThan(ascending));
            respawn.ResetPlayer();
            Assert.That(Vector3.Distance(motor.transform.position, respawn.SpawnPosition), Is.LessThan(.01f));
            Assert.That(motor.Velocity.sqrMagnitude, Is.LessThan(.001f));
        }
        [Test]
        public void CoyoteJumpWorksJustAfterWalkingOffRoof()
        {
            motor.ResetTo(new Vector3(0, .04f, 6.7f), Quaternion.identity);
            Simulate(Vector3.zero, false, false, 5);
            Simulate(Vector3.forward, true, false, 15);
            Assert.That(motor.State, Is.EqualTo(LocomotionState.Airborne));
            Simulate(Vector3.forward, true, true, 1);
            Assert.That(motor.Velocity.y, Is.GreaterThan(0));
        }
        [Test]
        public void JumpPressedShortlyBeforeLandingIsBuffered()
        {
            motor.ResetTo(new Vector3(0, .3f, 0), Quaternion.identity);
            Simulate(Vector3.zero, false, false, 6);
            Simulate(Vector3.zero, false, true, 12);
            Assert.That(motor.Velocity.y, Is.GreaterThan(0));
            Assert.That(motor.transform.position.y, Is.GreaterThan(.4f));
        }
        [Test]
        public void CeilingCollisionStopsUpwardVelocity()
        {
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.transform.position = motor.transform.position + Vector3.up * 2.5f;
            ceiling.transform.localScale = new Vector3(4, .3f, 4);
            Simulate(Vector3.zero, false, true, 12);
            Assert.That(motor.transform.position.y, Is.LessThan(.65f));
            Assert.That(motor.Velocity.y, Is.LessThanOrEqualTo(0));
            Object.Destroy(ceiling);
        }
        [UnityTest]
        public IEnumerator PauseFreezesControllerAndResumesWithoutStaleJump()
        {
            motor.GetComponent<PlayerController>().enabled = true;
            session.SetPaused(true);
            Vector3 position = motor.transform.position;
            input.Inject(new PlayerCommand { Move = Vector2.up, JumpPressed = true });
            yield return null; yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(0));
            Assert.That(Vector3.Distance(position, motor.transform.position), Is.LessThan(.001f));
            session.SetPaused(false);
            yield return null;
            Assert.That(motor.Velocity.y, Is.LessThanOrEqualTo(0));
        }
        [UnityTest]
        public IEnumerator CameraPullsInAheadOfObstruction()
        {
            var cm = Object.FindAnyObjectByType<CinemachineCamera>();
            var follow = cm.GetComponent<CinemachineThirdPersonFollow>();
            yield return null; yield return null;
            Vector3 target = Object.FindAnyObjectByType<ThirdPersonCameraRig>().TrackingTarget.position;
            float clearDistance = Vector3.Distance(UnityEngine.Camera.main.transform.position, target);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = target + new Vector3(0, 0, -2.4f);
            wall.transform.localScale = new Vector3(8, 8, .3f);
            Physics.SyncTransforms();
            yield return null; yield return null; yield return null;
            Assert.That(follow.CurrentObstacle, Is.Not.Null);
            Assert.That(Vector3.Distance(UnityEngine.Camera.main.transform.position, target), Is.LessThan(clearDistance - 1));
            Object.Destroy(wall);
        }
        [UnityTest]
        public IEnumerator KeyboardBindingReachesCommandReader()
        {
            var previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                input.Automation = false;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
                InputSystem.Update();
                yield return null; yield return null;
                Assert.That(keyboard.wKey.isPressed, Is.True, "Synthetic keyboard state was reset.");
                Assert.That(input.Command.Move.y, Is.EqualTo(1));
                Assert.That(input.Command.SprintHeld, Is.True);
            }
            finally
            {
                input.Automation = true;
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
            }
        }
        [UnityTearDown]
        public IEnumerator RestoreClock()
        {
            session.SetPaused(false);
            input.Inject(default);
            yield return null;
        }
    }
}
