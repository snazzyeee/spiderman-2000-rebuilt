using Unity.Cinemachine;
using UnityEngine;

namespace SpiderManRebuild
{
    [DefaultExecutionOrder(-10)]
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] Transform trackingTarget;
        [SerializeField] CinemachineCamera virtualCamera;
        [SerializeField] PlayerInputReader input;
        [SerializeField] GameSession session;
        [SerializeField] CameraTuning tuning;
        float yaw, pitch;
        public Quaternion YawRotation => Quaternion.Euler(0, yaw, 0);
        public Transform TrackingTarget => trackingTarget;
        public void Configure(Transform subject, Transform target, CinemachineCamera camera,
            PlayerInputReader reader, GameSession game, CameraTuning settings)
        { player = subject; trackingTarget = target; virtualCamera = camera;
          input = reader; session = game; tuning = settings; }
        void Start() => ResetHeading(player.eulerAngles.y, Vector3.zero);
        void LateUpdate()
        {
            if (!session.IsPaused)
            {
                Vector2 delta = input.Command.Look;
                // Mouse delta is already per-frame displacement; do not multiply by dt again.
                yaw += delta.x * tuning.MouseDegreesPerPixel;
                pitch = Mathf.Clamp(pitch - delta.y * tuning.MouseDegreesPerPixel, tuning.MinPitch, tuning.MaxPitch);
                yaw = Mathf.Repeat(yaw + 180, 360) - 180;
            }
            UpdateTarget();
        }
        void UpdateTarget()
        {
            trackingTarget.SetPositionAndRotation(player.position + Vector3.up * tuning.TargetHeight,
                Quaternion.Euler(pitch, yaw, 0));
        }
        public void ResetHeading(float heading, Vector3 displacement)
        {
            yaw = heading;
            pitch = tuning.StartPitch;
            UpdateTarget();
            virtualCamera.OnTargetObjectWarped(trackingTarget, displacement);
            virtualCamera.PreviousStateIsValid = false;
        }
    }
}
