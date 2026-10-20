using Unity.Cinemachine;
using UnityEngine;

namespace SpiderManRebuild
{
    [DefaultExecutionOrder(-100)]
    public sealed class SpiderManOrbitCamera : MonoBehaviour
    {
        public Transform Player;
        public TraversalInput Input;
        public TraversalSession Session;
        public CinemachineCamera Camera;
        public float TargetHeight = 1.45f, MouseSensitivity = .12f, StickDegreesPerSecond = 160;
        public float MinPitch = -35, MaxPitch = 70, StartPitch = 15, LookDamping = .055f;
        public bool InvertY;
        float targetYaw, targetPitch, yaw, pitch, yawRate, pitchRate;
        public Quaternion Heading => Quaternion.Euler(0, yaw, 0);
        public void ApplyLook(Vector2 pixels, Vector2 stick, float dt)
        {
            targetYaw += pixels.x * MouseSensitivity + stick.x * StickDegreesPerSecond * dt;
            targetPitch += (InvertY ? 1 : -1) * (pixels.y * MouseSensitivity + stick.y * StickDegreesPerSecond * dt);
            targetPitch = Mathf.Clamp(targetPitch, MinPitch, MaxPitch);
            yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawRate, LookDamping, Mathf.Infinity, dt);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchRate, LookDamping, Mathf.Infinity, dt);
        }
        void Start() { targetPitch = pitch = StartPitch; targetYaw = yaw = Player.eulerAngles.y; }
        void Update()
        {
            if (Input.Automation || (Session != null && Session.Paused)) return;
            // Mouse input is pixels/frame; stick input is a rate. Only stick is multiplied by dt.
            ApplyLook(Input.MouseDelta, Input.LookStick, Time.unscaledDeltaTime);
        }
        void LateUpdate()
        {
            transform.SetPositionAndRotation(Player.position + Vector3.up * TargetHeight,
                Quaternion.Euler(pitch, yaw, 0));
        }
        public void Warp(Vector3 displacement)
        {
            transform.position = Player.position + Vector3.up * TargetHeight;
            Camera.OnTargetObjectWarped(transform, displacement);
            Camera.PreviousStateIsValid = false;
        }
    }
}
