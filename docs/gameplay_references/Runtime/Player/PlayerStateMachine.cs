using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiderManRebuild
{
    public enum PlayerState { Grounded, Airborne, Swinging, Zipping, Crawling, HitStun, ControlLocked, Dead }
    public struct MissionInputCommand
    {
        public Vector2 Move;
        public bool Jump, Run, Swing, Zip, Crawl, Melee, Web, Dome, Interact, Reset;
        public static MissionInputCommand Read(TraversalInput i) => new MissionInputCommand {
            Move = i.Move, Jump = i.JumpPressed, Run = i.RunHeld, Swing = i.SwingHeld, Zip = i.ZipPressed, Crawl = i.CrawlHeld,
            Melee = i.MeleePressed, Web = i.WebHeld, Dome = i.DomePressed, Interact = i.InteractPressed, Reset = i.ResetPressed };
    }
    [DefaultExecutionOrder(0), RequireComponent(typeof(SpiderManCharacterController), typeof(Health))]
    public sealed class PlayerStateMachine : MonoBehaviour
    {
        public TraversalSettings Settings;
        public WebAnchorSelector Anchors;
        public PlayerCombat Combat;
        public Animator Animator;
        public LineRenderer Rope;
        public bool Automation;
        public PlayerState State { get; private set; } = PlayerState.Airborne;
        public SpiderManCharacterController Motor { get; private set; }
        public Health Health { get; private set; }
        public float SwingRopeLength => ropeLength;
        public Vector3 SwingAnchorPoint => anchor.Valid ? anchor.Point : Vector3.zero;
        public event Action<PlayerState> StateChanged;
        public event Action RecoveryRequested;
        public event Action InteractRequested;
        WebAnchor anchor;
        TraversalInput input;
        Vector3 velocity, wallNormal;
        float ropeLength, stateTime, detachUntil, zipUntil, hitUntil, impactVelocity;
        readonly HashSet<int> animatorParameters = new HashSet<int>();
        void Awake()
        {
            Motor = GetComponent<SpiderManCharacterController>(); Motor.Automation = true;
            Health = GetComponent<Health>(); input = GetComponent<TraversalInput>();
            Health.Damaged += OnDamaged; Health.Died += OnDied; Motor.ResetOccurred += ResetState;
        }
        void Start()
        {
            if (Animator != null && Animator.runtimeAnimatorController != null)
                foreach (var parameter in Animator.parameters) animatorParameters.Add(parameter.nameHash);
        }
        void OnDestroy()
        { Health.Damaged -= OnDamaged; Health.Died -= OnDied; Motor.ResetOccurred -= ResetState; }
        void Update()
        {
            if (!Automation) Tick(MissionInputCommand.Read(input), Time.deltaTime, Time.time);
            RefreshAnimator();
            if (Rope != null)
            {
                Rope.enabled = State == PlayerState.Swinging && anchor.Valid;
                if (Rope.enabled) { Rope.SetPosition(0, transform.position + Vector3.up * 1.1f); Rope.SetPosition(1, anchor.Point); }
            }
        }
        public void Tick(MissionInputCommand command, float dt, float now)
        {
            if (Health.Defeated) { SetState(PlayerState.Dead); return; }
            if (Motor.Session != null && Motor.Session.ControlBlocked)
            { if (!Motor.Session.Paused) { Release(); SetState(PlayerState.ControlLocked); } return; }
            if (dt <= 0 || Settings == null) return;
            if (command.Reset || transform.position.y < Motor.KillHeight)
            {
                if (RecoveryRequested != null) RecoveryRequested.Invoke(); else Motor.ResetPlayer();
                return;
            }
            if (State == PlayerState.ControlLocked) SetState(Motor.Grounded ? PlayerState.Grounded : PlayerState.Airborne);
            if (command.Interact) InteractRequested?.Invoke();
            Vector3 move = (Motor.Orbit != null ? Motor.Orbit.Heading : Quaternion.identity)
                * new Vector3(command.Move.x, 0, command.Move.y);
            move = Vector3.ClampMagnitude(move, 1);
            if (State != PlayerState.HitStun)
            {
                if (command.Zip && now >= zipUntil && Anchors.TrySelect(false, out var zipAnchor)) BeginZip(zipAnchor);
                else if (command.Swing && State != PlayerState.Swinging && now >= detachUntil
                    && Anchors.TrySelect(true, out var swingAnchor)) BeginSwing(swingAnchor);
                else if (command.Crawl && State != PlayerState.Crawling && now >= detachUntil && FindWall(out var wall))
                { wallNormal = wall.normal; velocity = Vector3.zero; SetState(PlayerState.Crawling); }
            }
            float previousFall = Motor.Velocity.y;
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / (1f / 120)), 1, 16);
            float sub = Mathf.Min(dt, .1333333f) / steps;
            if (State == PlayerState.Swinging)
            {
                if (!command.Swing || command.Jump || !anchor.Valid || RopeObstructed()) Release();
                else for (int n = 0; n < steps; n++)
                {
                    Vector3 before = transform.position, chest = before + Vector3.up * 1.1f;
                    SwingSolver.Integrate(ref chest, ref velocity, anchor.Point, ropeLength, move, sub, Settings);
                    Vector3 desired = chest - Vector3.up * 1.1f - before;
                    Vector3 actual = Motor.MoveExternal(desired / sub, sub);
                    if ((actual - desired).sqrMagnitude > .000004f)
                    { velocity = Motor.Velocity; Release(); break; }
                    Motor.SetVelocity(velocity);
                    if (Motor.Grounded) { Release(); break; }
                }
            }
            else if (State == PlayerState.Zipping)
            {
                if (!anchor.Valid) Release();
                else
                {
                    Vector3 destination = anchor.Point + anchor.Normal * .4f - Vector3.up * (anchor.Normal.y > .6f ? 0 : .9f);
                    Vector3 delta = destination - transform.position;
                    if (delta.magnitude < .7f || stateTime > 3)
                    { velocity = delta.normalized * Settings.ZipReleaseSpeed + Vector3.up * 2; zipUntil = now + Settings.ZipCooldown; Release(); }
                    else
                    {
                        velocity = delta.normalized * Mathf.Min(Settings.ZipSpeed, delta.magnitude / dt);
                        Vector3 actual = Motor.MoveExternal(velocity, dt);
                        if (actual.magnitude < velocity.magnitude * dt * .02f) { zipUntil = now + Settings.ZipCooldown; Release(); }
                    }
                }
            }
            else if (State == PlayerState.Crawling)
            {
                bool attached = ProbeAttachedWall(out var wall);
                if (command.Crawl && !command.Jump && !attached && TryMantle())
                { SetState(PlayerState.Grounded); }
                else if (!command.Crawl || command.Jump || !attached)
                {
                    velocity = command.Jump ? wallNormal * Settings.WallJumpOutward + Vector3.up * Settings.WallJumpUpward : Vector3.zero;
                    Release();
                }
                else
                {
                    wallNormal = wall.normal;
                    Vector3 up = Vector3.ProjectOnPlane(Vector3.up, wallNormal).normalized;
                    Vector3 right = Vector3.Cross(wallNormal, up).normalized;
                    velocity = (up * command.Move.y + right * command.Move.x) * Settings.CrawlSpeed - wallNormal * 2;
                    Motor.MoveExternal(velocity, dt);
                    if (Motor.Visual != null) Motor.Visual.rotation = Quaternion.Slerp(Motor.Visual.rotation,
                        Quaternion.LookRotation(-wallNormal, up), 1 - Mathf.Exp(-15 * dt));
                }
            }
            else if (State == PlayerState.HitStun)
            {
                velocity += Vector3.down * Motor.Tuning.Gravity * dt;
                Motor.MoveExternal(velocity, dt);
                velocity = Motor.Velocity;
                if (now >= hitUntil) SetState(Motor.Grounded ? PlayerState.Grounded : PlayerState.Airborne);
            }
            else
            {
                Motor.Simulate(move, command.Run, command.Jump, now, dt);
                SetState(Motor.Grounded ? PlayerState.Grounded : PlayerState.Airborne);
            }
            if (Motor.Grounded && previousFall < -3) impactVelocity = -previousFall;
            else impactVelocity = Mathf.MoveTowards(impactVelocity, 0, dt * 10);
            stateTime += dt;
            if (Combat != null && State != PlayerState.HitStun) Combat.Tick(command, dt, now);
        }
        public bool BeginSwing(WebAnchor target)
        {
            if (!target.Valid || Settings == null || (Motor.Session != null && Motor.Session.ControlBlocked)) return false;
            Vector3 chest = transform.position + Vector3.up * 1.1f;
            Vector3 offset = target.Point - chest;
            if (offset.magnitude < 2 || offset.magnitude > Settings.AnchorRange
                || offset.y < Settings.AnchorMinimumHeight || Anchors == null) return false;
            if (Physics.Raycast(chest, offset.normalized, out var obstruction, offset.magnitude - .12f,
                Anchors.Geometry, QueryTriggerInteraction.Ignore) && Vector3.Distance(obstruction.point, target.Point) > .4f) return false;
            anchor = target; ropeLength = Mathf.Max(2, Vector3.Distance(transform.position + Vector3.up * 1.1f, target.Point));
            velocity = Motor.Velocity; if (Motor.Grounded) velocity += Vector3.up * 5;
            velocity = Vector3.ProjectOnPlane(velocity, (chest - target.Point).normalized);
            SetState(PlayerState.Swinging); return true;
        }
        public bool BeginZip(WebAnchor target)
        {
            if (!target.Valid || (Motor.Session != null && Motor.Session.ControlBlocked)) return false;
            anchor = target; velocity = Motor.Velocity; SetState(PlayerState.Zipping); return true;
        }
        public void Release()
        {
            Motor.SetVelocity(velocity); anchor = default;
            detachUntil = Time.time + .2f;
            if (!Health.Defeated) SetState(PlayerState.Airborne);
        }
        bool RopeObstructed()
        {
            Vector3 origin = transform.position + Vector3.up * 1.1f, delta = anchor.Point - origin;
            return Physics.Raycast(origin, delta.normalized, out var hit, Mathf.Max(0, delta.magnitude - .12f),
                Anchors.Geometry, QueryTriggerInteraction.Ignore) && Vector3.Distance(hit.point, anchor.Point) > .4f;
        }
        bool FindWall(out RaycastHit hit)
        {
            Vector3 forward = Motor.Orbit != null ? Motor.Orbit.Heading * Vector3.forward : transform.forward;
            return ProbeWall(forward, out hit);
        }
        bool ProbeAttachedWall(out RaycastHit hit) => ProbeWall(-wallNormal, out hit);
        bool ProbeWall(Vector3 direction, out RaycastHit hit)
        {
            // Lower probes preserve latch acquisition when the chest has already cleared a roof lip.
            for (int i = 0; i < 3; i++)
                if (Physics.SphereCast(transform.position + Vector3.up * (1.1f - i * .425f), .12f, direction,
                    out hit, Settings.WallProbe, Anchors.Geometry, QueryTriggerInteraction.Ignore)
                    && Mathf.Abs(hit.normal.y) < .65f) return true;
            hit = default; return false;
        }
        bool TryMantle()
        {
            Vector3 above = transform.position + Vector3.up * 1.9f - wallNormal * .75f;
            if (!Physics.Raycast(above, Vector3.down, out var roof, 2.1f, Anchors.Geometry, QueryTriggerInteraction.Ignore)
                || roof.normal.y < .8f) return false;
            Vector3 destination = roof.point + Vector3.up * .05f;
            if (Physics.CheckBox(destination + Vector3.up * (Motor.BodySize.y * .5f),
                Motor.BodySize * .5f - Vector3.one * .025f, Quaternion.identity, Anchors.Geometry, QueryTriggerInteraction.Ignore)) return false;
            float rise = Mathf.Max(0, destination.y - transform.position.y);
            Motor.MoveExternal(Vector3.up * rise, 1);
            if (transform.position.y < destination.y - .08f) return false;
            Vector3 horizontal = destination - transform.position; horizontal.y = 0;
            Motor.MoveExternal(horizontal, 1);
            velocity = Vector3.zero; Motor.SetVelocity(Vector3.zero);
            return Vector3.Distance(transform.position, destination) < .3f;
        }
        void OnDamaged(DamageHit hit)
        {
            Combat?.CancelAttack();
            velocity = Motor.Velocity + hit.Impulse; anchor = default;
            hitUntil = Time.time + .22f; SetState(PlayerState.HitStun);
        }
        void OnDied() { anchor = default; Combat?.CancelAttack(); SetState(PlayerState.Dead); }
        void ResetState()
        { anchor = default; velocity = Vector3.zero; stateTime = 0; detachUntil = Time.time + .2f; SetState(PlayerState.Airborne); }
        void SetState(PlayerState state)
        { if (State == state) return; State = state; stateTime = 0; StateChanged?.Invoke(state); }
        void RefreshAnimator()
        {
            if (Animator == null || Animator.runtimeAnimatorController == null) return;
            SetBool("isSwinging", State == PlayerState.Swinging); SetBool("isCrawling", State == PlayerState.Crawling);
            SetBool("isGrounded", Motor.Grounded);
            SetFloat("speed", new Vector2(Motor.Velocity.x, Motor.Velocity.z).magnitude);
            SetFloat("swingNormalizedTime", State == PlayerState.Swinging ?
                Mathf.Repeat(stateTime / (2 * Mathf.PI * Mathf.Sqrt(ropeLength / Settings.SwingGravity)), 1) : 0);
            SetFloat("impactVelocity", impactVelocity);
        }
        void SetBool(string name, bool value) { int hash = UnityEngine.Animator.StringToHash(name); if (animatorParameters.Contains(hash)) Animator.SetBool(hash, value); }
        void SetFloat(string name, float value) { int hash = UnityEngine.Animator.StringToHash(name); if (animatorParameters.Contains(hash)) Animator.SetFloat(hash, value); }
    }
}
