using System;
using UnityEngine;

namespace SpiderManRebuild
{
    public enum ThugState { Patrol, Alert, MeleeAttack, RangedAttack, WebbedStun, Defeated }
    [RequireComponent(typeof(Health), typeof(SurfaceSteeringMotor))]
    public sealed class ThugAI : MonoBehaviour
    {
        public PlayerStateMachine Player;
        public Transform[] PatrolPoints;
        public bool Ranged;
        public Animator Animator;
        public GameObject WebWrap;
        public float SightRange = 18, MeleeRange = 1.8f;
        public Health Health { get; private set; }
        public ThugState State { get; private set; }
        public bool EncounterActive { get; set; }
        public event Action<ThugAI> Defeated;
        SurfaceSteeringMotor steering;
        Collider body;
        Vector3 spawn, shotTarget, knockback;
        Quaternion spawnRotation;
        float stateTime, cooldown, stunUntil, reactUntil, unseen;
        int patrolIndex, webStacks;
        bool applied;
        void Awake()
        {
            Health = GetComponent<Health>(); steering = GetComponent<SurfaceSteeringMotor>(); body = GetComponent<Collider>();
            spawn = transform.position; spawnRotation = transform.rotation;
            Health.Damaged += OnDamaged; Health.Died += OnDefeated;
        }
        void OnDestroy() { Health.Damaged -= OnDamaged; Health.Died -= OnDefeated; }
        void Update() => Tick(Time.deltaTime);
        public void Tick(float dt)
        {
            if (Health.Defeated || !EncounterActive || Player == null || dt <= 0
                || (Player.Motor.Session != null && Player.Motor.Session.ControlBlocked)) return;
            if (Player.Health.Defeated) return;
            stateTime += dt; cooldown = Mathf.Max(0, cooldown - dt);
            if (knockback.sqrMagnitude > .01f)
            { steering.MoveToward(transform.position + knockback, knockback.magnitude, dt); knockback *= Mathf.Exp(-10 * dt); }
            if (State == ThugState.WebbedStun)
            { if (Time.time >= stunUntil) { webStacks = 0; SetState(ThugState.Alert); } return; }
            if (Time.time < reactUntil) return;
            float distance = Vector3.Distance(transform.position, Player.transform.position);
            bool visible = distance < SightRange && HasSight();
            unseen = visible ? 0 : unseen + dt;
            if (State == ThugState.Patrol)
            {
                if (visible) SetState(ThugState.Alert);
                else if (PatrolPoints != null && PatrolPoints.Length > 0)
                {
                    Transform point = PatrolPoints[patrolIndex % PatrolPoints.Length];
                    if (point != null && !steering.MoveToward(point.position, 1.5f, dt)
                        && Vector3.Distance(point.position, transform.position) < .7f) patrolIndex++;
                }
            }
            else if (State == ThugState.Alert)
            {
                if (unseen > 4) SetState(ThugState.Patrol);
                else if (visible && cooldown <= 0 && distance < MeleeRange) SetState(ThugState.MeleeAttack);
                else if (Ranged && visible && cooldown <= 0 && distance > 3 && distance < 14)
                { shotTarget = Player.transform.position + Vector3.up; SetState(ThugState.RangedAttack); }
                else steering.MoveToward(Player.transform.position, 2.8f, dt);
            }
            else
            {
                Vector3 face = Player.transform.position - transform.position; face.y = 0;
                if (face.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(face);
                float windup = State == ThugState.MeleeAttack ? .48f : .7f;
                if (!applied && stateTime >= windup)
                {
                    applied = true;
                    if (State == ThugState.MeleeAttack && distance < MeleeRange + .2f && HasSight())
                        Player.Health.Apply(new DamageHit(12, Team.Enemy, DamageKind.Melee, face.normalized * 4));
                    else if (State == ThugState.RangedAttack)
                    {
                        Vector3 origin = transform.position + Vector3.up * 1.3f, direction = shotTarget - origin;
                        if (Physics.Raycast(origin, direction.normalized, out var hit, 25, 1 | (1 << 2), QueryTriggerInteraction.Ignore)
                            && hit.collider.GetComponentInParent<PlayerStateMachine>() == Player)
                            Player.Health.Apply(new DamageHit(9, Team.Enemy, DamageKind.Bullet, direction.normalized * 2));
                    }
                }
                if (stateTime > windup + .35f) { cooldown = Ranged ? 1.2f : .8f; SetState(ThugState.Alert); }
            }
            if (Animator != null && Animator.runtimeAnimatorController != null)
            { Animator.SetBool("webbed", State == ThugState.WebbedStun); Animator.SetInteger("state", (int)State); }
        }
        bool HasSight() => !Physics.Linecast(transform.position + Vector3.up * 1.3f,
            Player.transform.position + Vector3.up, 1, QueryTriggerInteraction.Ignore);
        public void ApplyWeb(int stacks, float duration)
        {
            if (Health.Defeated || !EncounterActive) return;
            webStacks = Mathf.Clamp(webStacks + Mathf.Max(0, stacks), 0, 3);
            stunUntil = Mathf.Max(stunUntil, Time.time + duration);
            SetState(ThugState.WebbedStun);
            Health.Apply(new DamageHit(webStacks >= 3 ? Health.Current : 10, Team.Player, DamageKind.Web));
        }
        void OnDamaged(DamageHit hit)
        {
            applied = true; knockback += hit.Impulse;
            if (hit.Kind != DamageKind.Web && State != ThugState.WebbedStun) { reactUntil = Time.time + .18f; SetState(ThugState.Alert); }
        }
        void OnDefeated()
        { SetState(ThugState.Defeated); EncounterActive = false; body.enabled = false; Defeated?.Invoke(this); }
        void SetState(ThugState next)
        {
            State = next; stateTime = 0; applied = false;
            if (Animator != null && Animator.runtimeAnimatorController != null)
            { Animator.SetBool("webbed", next == ThugState.WebbedStun); Animator.SetInteger("state", (int)next); }
            if (WebWrap != null) WebWrap.SetActive(next == ThugState.WebbedStun || (next == ThugState.Defeated && webStacks >= 3));
        }
        public void ConfigureWebWrap(GameObject prefab)
        {
            if (WebWrap != null || prefab == null) return;
            WebWrap = Instantiate(prefab, transform); WebWrap.name = "Captured web wrapping";
            PlayerCombat.FitOriginalEffect(WebWrap, transform.position, 1.4f, 1.9f);
            WebWrap.SetActive(false);
        }
        public void ResetEnemy()
        {
            transform.SetPositionAndRotation(spawn, spawnRotation); Health.Restore(); body.enabled = true;
            webStacks = 0; stunUntil = cooldown = reactUntil = 0; knockback = Vector3.zero;
            SetState(ThugState.Patrol); EncounterActive = false; Physics.SyncTransforms();
        }
    }
}
