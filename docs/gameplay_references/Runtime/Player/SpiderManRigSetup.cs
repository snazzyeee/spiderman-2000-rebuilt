using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SpiderManRebuild
{
    public static class SpiderManRigSetup
    {
        // Call once on a root with identity scale/rotation. visual contains the imported skeleton.
        public static SpiderManCharacterController Install(GameObject root, Transform visual, MovementTuning tuning)
        {
            root.layer = 2;
            var input = root.AddComponent<TraversalInput>();
            var session = root.AddComponent<TraversalSession>();
            var motor = root.AddComponent<SpiderManCharacterController>();
            motor.Visual = visual; motor.Tuning = tuning; motor.Session = session;
            var target = new GameObject("OrbitTarget");
            var orbit = target.AddComponent<SpiderManOrbitCamera>();
            var camera = UnityEngine.Camera.main;
            var cameraObject = camera != null ? camera.gameObject : new GameObject("Main Camera") { tag = "MainCamera" };
            if (camera == null) camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.nearClipPlane = .05f; camera.farClipPlane = 500;
            if (cameraObject.GetComponent<AudioListener>() == null) cameraObject.AddComponent<AudioListener>();
            if (cameraObject.GetComponent<UniversalAdditionalCameraData>() == null) cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var brain = cameraObject.GetComponent<CinemachineBrain>();
            if (brain == null) brain = cameraObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
            var cmObject = new GameObject("Cinemachine Third Person");
            var cm = cmObject.AddComponent<CinemachineCamera>(); cm.Follow = target.transform;
            cm.Lens.FieldOfView = 65; cm.Lens.NearClipPlane = .05f; cm.Lens.FarClipPlane = 500;
            var follow = cmObject.AddComponent<CinemachineThirdPersonFollow>();
            follow.CameraDistance = 4.5f; follow.ShoulderOffset = new Vector3(.45f, 0, 0);
            follow.VerticalArmLength = .2f; follow.CameraSide = 1;
            follow.Damping = new Vector3(.06f, .1f, .06f);
            follow.AvoidObstacles = new CinemachineThirdPersonFollow.ObstacleSettings
            {
                Enabled = true, CollisionFilter = motor.WorldMask, IgnoreTag = "",
                CameraRadius = .24f, DampingIntoCollision = 0, DampingFromCollision = .25f
            };
            orbit.Player = root.transform; orbit.Input = input; orbit.Session = session; orbit.Camera = cm;
            motor.Orbit = orbit;
            root.AddComponent<FrameTimeRecorder>();
            return motor;
        }
    }
}
