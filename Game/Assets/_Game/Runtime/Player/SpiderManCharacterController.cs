using UnityEngine;

namespace SpiderManRebuild
{
    // Kinematic box sweeps: deliberately contains no Unity CharacterController/CapsuleCollider.
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody), typeof(TraversalInput))]
    public sealed class SpiderManCharacterController : MonoBehaviour
    {
        public MovementTuning Tuning;
        public SpiderManOrbitCamera Orbit;
        public TraversalSession Session;
        public Transform Visual;
        public LayerMask WorldMask = 1;
        public Vector3 BodySize = new Vector3(.55f, 1.8f, .45f);
        public float Skin = .02f, StepHeight = .28f, SlopeLimit = 50;
        public float KillHeight = -100;
        public Vector3 Velocity { get; private set; }
        public bool Grounded { get; private set; }
        public Vector3 SpawnPosition { get; set; }
        public bool Automation { get; set; }
        readonly RaycastHit[] hits = new RaycastHit[32];
        readonly Collider[] overlaps = new Collider[32];
        readonly JumpBuffer jump = new JumpBuffer();
        BoxCollider body;
        TraversalInput input;
        double lastGrounded = double.NegativeInfinity;
        Vector3 groundNormal = Vector3.up;
        float SlopeCos => Mathf.Cos(SlopeLimit * Mathf.Deg2Rad);
        Vector3 Center(Vector3 position) => position + Vector3.up * (BodySize.y * .5f);
        Vector3 Half => BodySize * .5f - Vector3.one * Skin;
        void Awake()
        {
            input = GetComponent<TraversalInput>(); body = GetComponent<BoxCollider>();
            body.center = Vector3.up * (BodySize.y * .5f); body.size = BodySize;
            var rigid = GetComponent<Rigidbody>(); rigid.isKinematic = true; rigid.useGravity = false;
            rigid.constraints = RigidbodyConstraints.FreezeRotation;
            gameObject.layer = 2; // Queries ignore this layer. Root scale/rotation must remain identity.
            SpawnPosition = transform.position;
        }
        void Update()
        {
            if (Automation) return;
            if (Session != null && Session.Paused) { jump.Clear(); return; }
            if (input.ResetPressed || transform.position.y < KillHeight) { ResetPlayer(); return; }
            Vector2 move = Vector2.ClampMagnitude(input.Move, 1);
            Quaternion heading = Orbit != null ? Orbit.Heading : Quaternion.identity;
            Simulate(heading * new Vector3(move.x, 0, move.y), input.RunHeld, input.JumpPressed,
                Time.timeAsDouble, Time.deltaTime);
        }
        public void Simulate(Vector3 direction, bool run, bool pressed, double now, float delta)
        {
            if (Tuning == null || delta <= 0) return;
            direction.y = 0; direction = Vector3.ClampMagnitude(direction, 1);
            if (pressed) jump.Press(now, Tuning.JumpBufferTime);
            // Bounded substeps prevent a slow render frame from creating one very large collision sweep.
            int steps = Mathf.Clamp(Mathf.CeilToInt(delta / (1f / 120)), 1, 16);
            float dt = Mathf.Min(delta, .1333333f) / steps;
            for (int i = 0; i < steps; i++) Integrate(direction, run, now - delta + (i + 1) * dt, dt);
            if (Visual != null && direction.sqrMagnitude > .001f)
                Visual.rotation = Quaternion.Slerp(Visual.rotation, Quaternion.LookRotation(direction),
                    1 - Mathf.Exp(-14 * delta));
        }
        void Integrate(Vector3 direction, bool run, double now, float dt)
        {
            Depenetrate();
            RaycastHit ground = default;
            Grounded = Velocity.y <= 0 && ProbeGround(transform.position, .10f, out ground);
            if (Grounded)
            {
                groundNormal = ground.normal; lastGrounded = now;
                transform.position += Vector3.down * Mathf.Max(0, ground.distance - Skin);
            }
            Vector3 planar = new Vector3(Velocity.x, 0, Velocity.z);
            planar = Vector3.MoveTowards(planar, direction * (run ? Tuning.RunSpeed : Tuning.WalkSpeed),
                (Grounded ? Tuning.GroundAcceleration : Tuning.AirAcceleration) * dt);
            float vertical = Grounded ? -2 : Velocity.y;
            if (now - lastGrounded <= Tuning.CoyoteTime && jump.TryConsume(now))
            {
                vertical = Mathf.Sqrt(2 * Tuning.Gravity * Tuning.JumpHeight);
                lastGrounded = double.NegativeInfinity; Grounded = false;
            }
            vertical = Mathf.Max(vertical - Tuning.Gravity * dt, -Tuning.TerminalSpeed);
            Velocity = planar + Vector3.up * vertical;
            Vector3 displacement = Velocity * dt;
            if (Grounded && groundNormal.y < .999f)
                displacement = Vector3.ProjectOnPlane(planar, groundNormal) * dt + Vector3.up * vertical * dt;
            Slide(displacement);
            Grounded = Velocity.y <= 0 && ProbeGround(transform.position, .07f, out ground);
            if (Grounded)
            {
                groundNormal = ground.normal; lastGrounded = now;
                Velocity = new Vector3(Velocity.x, -2, Velocity.z);
            }
            Physics.SyncTransforms();
        }
        bool Cast(Vector3 position, Vector3 direction, float distance, out RaycastHit nearest)
        {
            int count = Physics.BoxCastNonAlloc(Center(position), Half, direction, hits,
                Quaternion.identity, distance, WorldMask, QueryTriggerInteraction.Ignore);
            RaycastHit[] candidates = hits;
            if (count == hits.Length)
            {
                candidates = Physics.BoxCastAll(Center(position), Half, direction, Quaternion.identity,
                    distance, WorldMask, QueryTriggerInteraction.Ignore);
                count = candidates.Length;
            }
            nearest = default; float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
                if (candidates[i].collider != body && candidates[i].distance < best)
                { best = candidates[i].distance; nearest = candidates[i]; }
            return best < float.PositiveInfinity;
        }
        bool ProbeGround(Vector3 position, float distance, out RaycastHit hit)
        { return Cast(position, Vector3.down, distance + Skin, out hit) && hit.normal.y >= SlopeCos; }
        void Slide(Vector3 remaining)
        {
            for (int i = 0; i < 6 && remaining.sqrMagnitude > 1e-10f; i++)
            {
                float distance = remaining.magnitude; Vector3 direction = remaining / distance;
                if (!Cast(transform.position, direction, distance + Skin, out var hit))
                { transform.position += remaining; break; }
                float travel = Mathf.Clamp(hit.distance - Skin, 0, distance);
                transform.position += direction * travel;
                remaining -= direction * travel;
                if (Grounded && hit.normal.y < SlopeCos && TryStep(remaining)) break;
                Vector3 normal = hit.normal;
                // Steep faces must not convert horizontal movement into uphill climbing.
                if (normal.y > 0 && normal.y < SlopeCos)
                    normal = new Vector3(normal.x, 0, normal.z).normalized;
                remaining = Vector3.ProjectOnPlane(remaining, normal);
                if (Vector3.Dot(Velocity, normal) < 0) Velocity = Vector3.ProjectOnPlane(Velocity, normal);
            }
        }
        bool TryStep(Vector3 remaining)
        {
            Vector3 horizontal = new Vector3(remaining.x, 0, remaining.z);
            if (horizontal.sqrMagnitude < 1e-8f || StepHeight <= 0) return false;
            if (Cast(transform.position, Vector3.up, StepHeight + Skin, out _)) return false;
            Vector3 raised = transform.position + Vector3.up * StepHeight;
            if (Cast(raised, horizontal.normalized, horizontal.magnitude + Skin, out _)) return false;
            Vector3 ahead = raised + horizontal;
            if (!ProbeGround(ahead, StepHeight + .07f, out var ground)) return false;
            transform.position = ahead + Vector3.down * Mathf.Max(0, ground.distance - Skin);
            return true;
        }
        void Depenetrate()
        {
            for (int pass = 0; pass < 4; pass++)
            {
                int count = Physics.OverlapBoxNonAlloc(Center(transform.position), BodySize * .5f,
                    overlaps, Quaternion.identity, WorldMask, QueryTriggerInteraction.Ignore);
                Collider[] candidates = overlaps;
                if (count == overlaps.Length)
                {
                    candidates = Physics.OverlapBox(Center(transform.position), BodySize * .5f,
                        Quaternion.identity, WorldMask, QueryTriggerInteraction.Ignore);
                    count = candidates.Length;
                }
                bool moved = false;
                for (int i = 0; i < count; i++)
                    if (candidates[i] != body && Physics.ComputePenetration(body, transform.position, Quaternion.identity,
                        candidates[i], candidates[i].transform.position, candidates[i].transform.rotation,
                        out Vector3 direction, out float distance))
                    { transform.position += direction * (distance + .001f); moved = true; }
                if (!moved) break;
            }
        }
        public void ResetPlayer()
        {
            Vector3 delta = SpawnPosition - transform.position;
            transform.position = SpawnPosition; Velocity = Vector3.zero; Grounded = false;
            lastGrounded = double.NegativeInfinity; jump.Clear(); Physics.SyncTransforms();
            if (Orbit != null) Orbit.Warp(delta);
        }
    }
}
