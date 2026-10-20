using System.Collections.Generic;
using UnityEngine;

namespace SpiderManRebuild
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        public PlayerStateMachine Player;
        public Camera AimCamera;
        public TraversalAudioRig Audio;
        public AudioClip WebShotSound, PunchSound, DomeSound;
        public AudioSource LocalSfx;
        public LineRenderer WebTracer;
        public GameObject DomePrefab;
        public LayerMask EnemyMask = 1 << 8, ShotMask = (1 << 8) | 1;
        public float WebEnergy { get; private set; } = 100;
        public int ComboIndex { get; private set; }
        readonly Collider[] contacts = new Collider[32];
        readonly HashSet<Health> damaged = new HashSet<Health>();
        float shotAt, domeAt, tracerUntil, stageTime, queuedUntil;
        bool attacking, struck;
        GameObject dome;
        Vector3 domeOffset;
        float domeHideAt;
        readonly float[] windup = { .13f, .16f, .2f }, recovery = { .28f, .3f, .4f }, damage = { 20, 25, 35 };
        void Update()
        {
            if (WebTracer != null) WebTracer.enabled = Time.time < tracerUntil;
            if (dome != null && Time.time >= domeHideAt) dome.SetActive(false);
        }
        public void Tick(MissionInputCommand command, float dt, float now)
        {
            if (Player.Health.Defeated) { CancelAttack(); return; }
            WebEnergy = Mathf.Min(100, WebEnergy + 14 * dt);
            if (command.Web && now >= shotAt && WebEnergy >= 7) FireWeb(now);
            if (command.Dome && now >= domeAt && WebEnergy >= 30) WebDome(now);
            if (command.Melee)
            {
                if (!attacking) BeginAttack(0);
                else queuedUntil = now + .35f;
            }
            if (!attacking) return;
            stageTime += dt;
            if (!struck && stageTime >= windup[ComboIndex])
            { struck = true; StrikeMelee(); }
            if (stageTime < windup[ComboIndex] + recovery[ComboIndex]) return;
            if (queuedUntil >= now && ComboIndex < 2) BeginAttack(ComboIndex + 1);
            else CancelAttack();
        }
        void BeginAttack(int index)
        {
            ComboIndex = index; stageTime = 0; struck = false; attacking = true; damaged.Clear();
            queuedUntil = -1;
            if (Player.Animator != null && Player.Animator.runtimeAnimatorController != null)
            { Player.Animator.SetInteger("comboIndex", index); Player.Animator.SetTrigger("melee"); }
        }
        public void CancelAttack() { attacking = false; queuedUntil = -1; ComboIndex = 0; damaged.Clear(); }
        Vector3 Facing => Player.Motor.Visual != null ?
            Vector3.ProjectOnPlane(Player.Motor.Visual.forward, Vector3.up).normalized : transform.forward;
        void StrikeMelee()
        {
            Vector3 chest = transform.position + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(chest + Facing * .85f, 1.15f, contacts, EnemyMask, QueryTriggerInteraction.Ignore);
            Collider[] candidates = count == contacts.Length ?
                Physics.OverlapSphere(chest + Facing * .85f, 1.15f, EnemyMask, QueryTriggerInteraction.Ignore) : contacts;
            if (candidates != contacts) count = candidates.Length;
            bool landed = false;
            for (int i = 0; i < count; i++)
            {
                Health target = candidates[i].GetComponentInParent<Health>();
                if (target == null || target.Defeated || target.Team != Team.Enemy || !damaged.Add(target)) continue;
                Vector3 delta = target.transform.position + Vector3.up - chest;
                if (Vector3.Dot(Facing, delta.normalized) < .1f || Physics.Linecast(chest, chest + delta, 1,
                    QueryTriggerInteraction.Ignore)) continue;
                landed |= target.Apply(new DamageHit(damage[ComboIndex], Team.Player, DamageKind.Melee, Facing * (ComboIndex == 2 ? 5 : 2)));
            }
            if (landed && LocalSfx != null && PunchSound != null) LocalSfx.PlayOneShot(PunchSound);
        }
        public bool FireWeb(float now)
        {
            if (AimCamera == null || WebEnergy < 7 || now < shotAt) return false;
            WebEnergy -= 7; shotAt = now + .22f;
            Vector3 origin = transform.position + Vector3.up * 1.15f;
            Vector3 destination = AimCamera.transform.position + AimCamera.transform.forward * 65;
            if (Physics.SphereCast(AimCamera.transform.position, .18f, AimCamera.transform.forward, out var aim, 65, ShotMask,
                QueryTriggerInteraction.Ignore)) destination = aim.point;
            return Shoot(destination, origin, now);
        }
        public bool FireWebAt(Vector3 destination, float now)
        {
            if (WebEnergy < 7 || now < shotAt) return false;
            WebEnergy -= 7; shotAt = now + .22f;
            return Shoot(destination, transform.position + Vector3.up * 1.15f, now);
        }
        bool Shoot(Vector3 destination, Vector3 origin, float now)
        {
            Vector3 delta = destination - origin;
            if (Physics.SphereCast(origin, .1f, delta.normalized, out var hit, delta.magnitude, ShotMask, QueryTriggerInteraction.Ignore))
            {
                destination = hit.point;
                var thug = hit.collider.GetComponentInParent<ThugAI>();
                if (thug != null) thug.ApplyWeb(1, 2.5f);
                if (Audio != null) Audio.PlayWebImpact(hit.point);
            }
            if (WebTracer != null)
            { WebTracer.SetPosition(0, origin); WebTracer.SetPosition(1, destination); tracerUntil = now + .10f; WebTracer.enabled = true; }
            if (LocalSfx != null && WebShotSound != null) LocalSfx.PlayOneShot(WebShotSound);
            return true;
        }
        public bool WebDome(float now)
        {
            if (now < domeAt || WebEnergy < 30) return false;
            WebEnergy -= 30; domeAt = now + 4;
            Player.Health.InvulnerableUntil = Mathf.Max(Player.Health.InvulnerableUntil, now + .8f);
            int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, 3.4f, contacts, EnemyMask, QueryTriggerInteraction.Ignore);
            var unique = new HashSet<ThugAI>();
            for (int i = 0; i < count; i++)
            {
                var thug = contacts[i].GetComponentInParent<ThugAI>();
                if (thug != null && unique.Add(thug) && !Physics.Linecast(transform.position + Vector3.up,
                    thug.transform.position + Vector3.up, 1, QueryTriggerInteraction.Ignore)) thug.ApplyWeb(2, 3.5f);
            }
            if (DomePrefab != null)
            {
                if (dome == null)
                {
                    dome = Instantiate(DomePrefab); FitOriginalEffect(dome, transform.position, 6);
                    domeOffset = dome.transform.position - transform.position;
                }
                dome.transform.position = transform.position + domeOffset; dome.SetActive(true); domeHideAt = now + .8f;
            }
            if (LocalSfx != null && DomeSound != null) LocalSfx.PlayOneShot(DomeSound);
            return true;
        }
        public static void FitOriginalEffect(GameObject effect, Vector3 feet, float diameter, float height = 0)
        {
            // Exported effect roots can retain original level-space translation.
            // Fit measured renderer bounds, then recenter the rendered geometry at the new owner.
            var renderers = effect.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float horizontal = diameter / Mathf.Max(.001f, Mathf.Max(bounds.size.x, bounds.size.z));
            float vertical = height > 0 ? height / Mathf.Max(.001f, bounds.size.y) : horizontal;
            effect.transform.localScale = Vector3.Scale(effect.transform.localScale, new Vector3(horizontal, vertical, horizontal));
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            effect.transform.position += feet - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }
        public void Restore()
        { WebEnergy = 100; shotAt = domeAt = 0; CancelAttack(); if (dome != null) dome.SetActive(false); }
    }
}
