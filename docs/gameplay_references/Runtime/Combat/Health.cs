using System;
using UnityEngine;

namespace SpiderManRebuild
{
    public enum Team { Player, Enemy }
    public enum DamageKind { Melee, Web, Bullet, Fall }
    public readonly struct DamageHit
    {
        public readonly float Amount;
        public readonly Team SourceTeam;
        public readonly DamageKind Kind;
        public readonly Vector3 Impulse;
        public DamageHit(float amount, Team team, DamageKind kind, Vector3 impulse = default)
        { Amount = amount; SourceTeam = team; Kind = kind; Impulse = impulse; }
    }
    public sealed class Health : MonoBehaviour
    {
        public Team Team;
        [Min(1)] public float Maximum = 100;
        public float Current { get; private set; }
        public bool Defeated => Current <= 0;
        public float InvulnerableUntil { get; set; }
        public event Action<DamageHit> Damaged;
        public event Action Died;
        public event Action Restored;
        void Awake() => Current = Maximum;
        public bool Apply(DamageHit hit)
        {
            if (Defeated || hit.SourceTeam == Team || hit.Amount <= 0 || Time.time < InvulnerableUntil) return false;
            Current = Mathf.Max(0, Current - hit.Amount);
            Damaged?.Invoke(hit);
            if (Defeated) Died?.Invoke();
            return true;
        }
        public void Restore()
        { Current = Maximum; InvulnerableUntil = 0; Restored?.Invoke(); }
    }
}
