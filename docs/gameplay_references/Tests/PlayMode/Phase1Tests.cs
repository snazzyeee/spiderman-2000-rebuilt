using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace SpiderManRebuild.Tests
{
    public sealed class Phase1Tests
    {
        GameObject actor, floor, wall;
        MovementTuning tuning;
        SpiderManCharacterController motor;
        double now;
        [SetUp]
        public void CreatePhysicsWorld()
        {
            floor = new GameObject("Test floor"); var surface = floor.AddComponent<BoxCollider>();
            surface.size = new Vector3(50, 1, 50); floor.transform.position = Vector3.down * .5f;
            actor = new GameObject("Test motor"); actor.transform.position = Vector3.up * .04f;
            tuning = ScriptableObject.CreateInstance<MovementTuning>();
            motor = actor.AddComponent<SpiderManCharacterController>(); motor.Tuning = tuning; motor.Automation = true;
            now = 0; Steps(Vector3.zero, false, 20);
        }
        void Steps(Vector3 direction, bool jump, int frames, bool run = false)
        {
            Physics.SyncTransforms();
            for (int i = 0; i < frames; i++)
            { now += 1.0 / 60; motor.Simulate(direction, run, jump && i == 0, now, 1f / 60); }
        }
        [Test] public void BoxSweepStopsAtWall()
        {
            wall = new GameObject("Test obstruction"); wall.transform.position = new Vector3(0, 1, 2);
            wall.AddComponent<BoxCollider>().size = new Vector3(8, 2, .2f);
            Steps(Vector3.forward, false, 90, true);
            Assert.That(actor.transform.position.z, Is.LessThan(1.7f));
            Assert.That(actor.GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(actor.GetComponent<CapsuleCollider>(), Is.Null);
        }
        [Test] public void BufferedJumpAndReset()
        {
            Assert.That(motor.Grounded, Is.True);
            Steps(Vector3.zero, true, 10);
            Assert.That(actor.transform.position.y, Is.GreaterThan(.8f));
            float speed = motor.Velocity.y;
            Steps(Vector3.zero, true, 1);
            Assert.That(motor.Velocity.y, Is.LessThan(speed));
            motor.ResetPlayer();
            Assert.That(motor.Velocity, Is.EqualTo(Vector3.zero));
            Assert.That(Vector3.Distance(actor.transform.position, motor.SpawnPosition), Is.LessThan(.001f));
        }
        [Test] public void DiagonalSpeedIsNormalized()
        {
            Steps(new Vector3(1, 0, 1), false, 30, true);
            Assert.That(new Vector2(motor.Velocity.x, motor.Velocity.z).magnitude, Is.EqualTo(tuning.RunSpeed).Within(.02));
        }
        [Test] public void LowStepIsTraversable()
        {
            wall = new GameObject("Test step"); wall.transform.position = new Vector3(0, .1f, 1.5f);
            wall.AddComponent<BoxCollider>().size = new Vector3(8, .2f, 2);
            Steps(Vector3.forward, false, 25);
            Assert.That(actor.transform.position.z, Is.GreaterThan(.9f));
            Assert.That(actor.transform.position.y, Is.GreaterThan(.15f));
        }
        [Test] public void CeilingStopsJump()
        {
            wall = new GameObject("Test ceiling"); wall.transform.position = new Vector3(0, 2.5f, 0);
            wall.AddComponent<BoxCollider>().size = new Vector3(8, .2f, 8);
            Steps(Vector3.zero, true, 15);
            Assert.That(actor.transform.position.y, Is.LessThan(.65f));
            Assert.That(motor.Velocity.y, Is.LessThanOrEqualTo(0));
        }
        [UnityTest] public IEnumerator GamepadMovementAndBothSticksResolve()
        {
            var old = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                InputSystem.QueueStateEvent(pad, new GamepadState
                { leftStick = new Vector2(.8f, .5f), rightStick = new Vector2(.6f, -.4f) }.WithButton(GamepadButton.LeftShoulder));
                InputSystem.Update(); yield return null;
                var input = actor.GetComponent<TraversalInput>();
                Assert.That(input.Move.x, Is.GreaterThan(.5f));
                Assert.That(input.LookStick.x, Is.GreaterThan(.3f));
                Assert.That(input.RunHeld, Is.True);
            }
            finally { InputSystem.RemoveDevice(pad); InputSystem.settings.backgroundBehavior = old; }
        }
        [Test] public void StickCameraRateIsIndependentOfRenderRate()
        {
            var a = new GameObject("60Hz camera").AddComponent<SpiderManOrbitCamera>(); a.enabled = false;
            var b = new GameObject("120Hz camera").AddComponent<SpiderManOrbitCamera>(); b.enabled = false;
            for (int i = 0; i < 60; i++) a.ApplyLook(Vector2.zero, Vector2.right, 1f / 60);
            for (int i = 0; i < 120; i++) b.ApplyLook(Vector2.zero, Vector2.right, 1f / 120);
            Assert.That(Quaternion.Angle(a.Heading, b.Heading), Is.LessThan(1));
            Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject);
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(actor); Object.DestroyImmediate(floor);
            if (wall != null) Object.DestroyImmediate(wall);
            Object.DestroyImmediate(tuning); Time.timeScale = 1;
        }
    }
}
