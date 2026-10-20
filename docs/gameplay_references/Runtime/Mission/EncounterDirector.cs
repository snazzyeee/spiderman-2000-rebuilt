using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiderManRebuild
{
    public sealed class EncounterDirector : MonoBehaviour
    {
        public PlayerStateMachine Player;
        public ThugAI[] Enemies;
        public Bounds Arena;
        public bool Active { get; private set; }
        public int Remaining => living.Count;
        public event Action Cleared;
        readonly HashSet<ThugAI> living = new HashSet<ThugAI>();
        void Awake()
        { foreach (var enemy in Enemies) if (enemy != null) enemy.Defeated += OnDefeated; }
        void OnDestroy()
        {
            if (Enemies != null) foreach (var enemy in Enemies) if (enemy != null) enemy.Defeated -= OnDefeated;
            if (Player != null) Player.Motor.ArenaLocked = false;
        }
        public void Begin()
        {
            if (Active) return;
            living.Clear(); Active = true;
            Player.Motor.ArenaBounds = Arena; Player.Motor.ArenaLocked = true;
            foreach (var enemy in Enemies)
            {
                if (enemy == null || enemy.Health.Defeated) continue;
                enemy.ConfigureWebWrap(Player.Combat.DomePrefab);
                living.Add(enemy); enemy.Player = Player; enemy.EncounterActive = true;
            }
            if (living.Count == 0) Complete();
        }
        void OnDefeated(ThugAI enemy)
        { if (!Active || !living.Remove(enemy)) return; if (living.Count == 0) Complete(); }
        void Complete()
        {
            if (!Active) return;
            Active = false; Player.Motor.ArenaLocked = false; Cleared?.Invoke();
        }
        public void ResetEncounter()
        {
            Active = false; living.Clear();
            if (Player != null) Player.Motor.ArenaLocked = false;
            foreach (var enemy in Enemies) if (enemy != null) enemy.ResetEnemy();
        }
    }
}
