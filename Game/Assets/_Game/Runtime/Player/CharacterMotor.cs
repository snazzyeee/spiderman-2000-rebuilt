using UnityEngine;

namespace SpiderManRebuild
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        [SerializeField] MovementTuning tuning;
        CharacterController capsule;
        readonly JumpBuffer jumpBuffer = new JumpBuffer();
        double lastGrounded = double.NegativeInfinity;
        Vector3 planarVelocity;
        float verticalVelocity;
        public LocomotionState State { get; private set; }
        public Vector3 Velocity => planarVelocity + Vector3.up * verticalVelocity;
        public float Speed => planarVelocity.magnitude;
        public MovementTuning Tuning => tuning;
        public void Configure(MovementTuning settings) => tuning = settings;
        void Awake() => capsule = GetComponent<CharacterController>();

        public void Step(Vector3 worldDirection, bool sprint, bool jumpPressed, double now, float dt)
        {
            if (dt <= 0 || tuning == null) return;
            if (capsule == null) capsule = GetComponent<CharacterController>();
            worldDirection.y = 0;
            worldDirection = Vector3.ClampMagnitude(worldDirection, 1);
            if (jumpPressed) jumpBuffer.Press(now, tuning.JumpBufferTime);
            bool grounded = capsule.isGrounded && verticalVelocity <= 0;
            if (grounded) { lastGrounded = now; verticalVelocity = -2f; }
            float acceleration = grounded ? tuning.GroundAcceleration : tuning.AirAcceleration;
            float speed = sprint ? tuning.RunSpeed : tuning.WalkSpeed;
            planarVelocity = Vector3.MoveTowards(planarVelocity, worldDirection * speed, acceleration * dt);
            if (now - lastGrounded <= tuning.CoyoteTime && jumpBuffer.TryConsume(now))
            {
                verticalVelocity = Mathf.Sqrt(2 * tuning.Gravity * tuning.JumpHeight);
                lastGrounded = double.NegativeInfinity; // One jump per ground/coyote opportunity.
            }
            verticalVelocity = Mathf.Max(verticalVelocity - tuning.Gravity * dt, -tuning.TerminalSpeed);
            CollisionFlags flags = capsule.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
            bool landed = (flags & CollisionFlags.Below) != 0 && verticalVelocity <= 0;
            if (landed) { verticalVelocity = -2; lastGrounded = now; }
            State = landed ? LocomotionState.Grounded : LocomotionState.Airborne;
        }

        public void ClearBufferedActions() => jumpBuffer.Clear();
        public void ResetTo(Vector3 position, Quaternion rotation)
        {
            if (capsule == null) capsule = GetComponent<CharacterController>();
            capsule.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            planarVelocity = Vector3.zero;
            verticalVelocity = 0;
            lastGrounded = double.NegativeInfinity;
            jumpBuffer.Clear();
            State = LocomotionState.Airborne;
            capsule.enabled = true;
            Physics.SyncTransforms();
        }
    }
}
