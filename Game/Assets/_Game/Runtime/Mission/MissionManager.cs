using System;
using System.Collections;
using UnityEngine;

namespace SpiderManRebuild
{
    public enum MissionStage { Intro, RooftopRoute, BankEncounter, Skylight, Recovering, Complete }
    public sealed class MissionManager : MonoBehaviour
    {
        public PlayerStateMachine Player;
        public Transform StartingSpawn;
        public MissionCheckpoint[] Checkpoints;
        public EncounterDirector Encounter;
        public SkylightInteraction Skylight;
        public CutsceneSequence Intro;
        public MissionStage Stage { get; private set; }
        public int NextCheckpoint { get; private set; }
        public int SavedCheckpoint => checkpointProgress;
        public bool SkipIntroOnNextBegin { get; set; }
        public event Action<MissionStage> StageChanged;
        public event Action ObjectiveChanged;
        Vector3 checkpointPosition;
        int checkpointProgress;
        bool subscriptionsReady;
        Coroutine recovery;
        public string Objective => Stage switch
        {
            MissionStage.Intro => "Bank Heist / Rooftops — Enter / A to skip",
            MissionStage.RooftopRoute => NextCheckpoint < Checkpoints.Length ? Checkpoints[NextCheckpoint].ObjectiveLabel : "Reach the bank rooftop",
            MissionStage.BankEncounter => "Clear the bank rooftop — " + Encounter.Remaining + " thugs remaining",
            MissionStage.Skylight => Skylight.InRange ? "E / Y — enter the skylight" : "Find the bank skylight",
            MissionStage.Recovering => "Returning to the last rooftop checkpoint…",
            _ => "MISSION COMPLETE — bank rooftop secured"
        };
        public Vector3 ObjectivePosition => Stage == MissionStage.RooftopRoute && NextCheckpoint < Checkpoints.Length ?
            Checkpoints[NextCheckpoint].transform.position : Skylight.transform.position;
        void Start()
        {
            Player.RecoveryRequested += RequestRecovery; Player.InteractRequested += Interact;
            Player.Health.Died += RequestRecovery; Encounter.Cleared += OnEncounterCleared;
            foreach (var enemy in Encounter.Enemies) enemy.Defeated += EnemyCountChanged;
            if (Intro != null) Intro.Finished += IntroFinished;
            subscriptionsReady = true; BeginMission();
        }
        public void BeginMission()
        {
            if (recovery != null) { StopCoroutine(recovery); recovery = null; }
            if (Intro != null && Intro.Playing) Intro.Abort();
            Player.Motor.Session.SetControlLock(this, false);
            Player.Motor.Session.SetMenuMode(false);
            Encounter.ResetEncounter();
            NextCheckpoint = checkpointProgress = 0; checkpointPosition = StartingSpawn.position;
            Player.Health.Restore(); Player.Combat.Restore();
            Player.Motor.SpawnPosition = checkpointPosition; Player.Motor.ResetPlayer();
            Player.Motor.Simulate(Vector3.zero, false, false, Time.time, .05f);
            FaceObjective();
            SetStage(MissionStage.Intro);
            bool skipIntro = SkipIntroOnNextBegin; SkipIntroOnNextBegin = false;
            if (skipIntro || Intro == null || !Intro.TryPlay()) SetStage(MissionStage.RooftopRoute);
        }
        public void RestoreCheckpoint(int progress, bool bankCleared)
        {
            if (recovery != null) { StopCoroutine(recovery); recovery = null; }
            if (Intro != null) Intro.Abort();
            Encounter.ResetEncounter();
            checkpointProgress = bankCleared ? Checkpoints.Length : Mathf.Clamp(progress, 0, Checkpoints.Length - 1);
            NextCheckpoint = checkpointProgress;
            checkpointPosition = checkpointProgress == 0 ? StartingSpawn.position : Checkpoints[checkpointProgress - 1].SafeSpawn.position;
            if (bankCleared) foreach (var enemy in Encounter.Enemies)
                enemy.Health.Apply(new DamageHit(enemy.Health.Maximum, Team.Player, DamageKind.Melee));
            Player.Health.Restore(); Player.Combat.Restore();
            Player.Motor.SpawnPosition = checkpointPosition; Player.Motor.ResetPlayer();
            Player.Motor.Simulate(Vector3.zero, false, false, Time.time, .05f);
            Player.Motor.Session.SetControlLock(this, false); Player.Motor.Session.SetMenuMode(false);
            FaceObjective(); SetStage(bankCleared ? MissionStage.Skylight : MissionStage.RooftopRoute);
        }
        void IntroFinished(bool completed)
        { if (completed && Stage == MissionStage.Intro) SetStage(MissionStage.RooftopRoute); }
        public bool TryCheckpoint(MissionCheckpoint checkpoint)
        {
            if (Stage != MissionStage.RooftopRoute || checkpoint == null || checkpoint.Index != NextCheckpoint
                || NextCheckpoint >= Checkpoints.Length || Checkpoints[NextCheckpoint] != checkpoint
                || !checkpoint.ContainsPlayer() || Player.Health.Defeated) return false;
            NextCheckpoint++; checkpointProgress = NextCheckpoint;
            checkpointPosition = checkpoint.SafeSpawn.position;
            Player.Motor.SpawnPosition = checkpointPosition;
            if (NextCheckpoint == Checkpoints.Length)
            {
                // Keep the recovery spawn on the previous safe approach until the fight is cleared.
                if (NextCheckpoint > 1) checkpointPosition = Checkpoints[NextCheckpoint - 2].SafeSpawn.position;
                checkpointProgress = Mathf.Max(0, NextCheckpoint - 1);
                SetStage(MissionStage.BankEncounter); Encounter.Begin();
            }
            ObjectiveChanged?.Invoke(); return true;
        }
        void EnemyCountChanged(ThugAI enemy) => ObjectiveChanged?.Invoke();
        void OnEncounterCleared()
        {
            if (Stage != MissionStage.BankEncounter) return;
            checkpointProgress = Checkpoints.Length;
            checkpointPosition = Checkpoints[Checkpoints.Length - 1].SafeSpawn.position;
            Player.Motor.SpawnPosition = checkpointPosition;
            SetStage(MissionStage.Skylight);
        }
        void Interact() => Skylight.TryInteract();
        public bool CompleteAtSkylight(SkylightInteraction source)
        {
            if (Stage != MissionStage.Skylight || source != Skylight || !source.InRange) return false;
            SetStage(MissionStage.Complete); Player.Motor.Session.SetControlLock(this, true);
            Player.Motor.Session.SetMenuMode(true);
            Player.Combat.CancelAttack(); return true;
        }
        public void RequestRecovery()
        {
            if (Stage == MissionStage.Recovering || Stage == MissionStage.Complete) return;
            if (Intro != null) Intro.Abort();
            Player.Motor.Session.SetControlLock(this, true); Player.Combat.CancelAttack();
            SetStage(MissionStage.Recovering);
            recovery = StartCoroutine(Recover());
        }
        IEnumerator Recover()
        {
            yield return new WaitForSeconds(1.2f);
            if (checkpointProgress < Checkpoints.Length) Encounter.ResetEncounter();
            Player.Health.Restore(); Player.Combat.Restore();
            NextCheckpoint = checkpointProgress; Player.Motor.SpawnPosition = checkpointPosition; Player.Motor.ResetPlayer();
            FaceObjective();
            Player.Health.InvulnerableUntil = Time.time + 1;
            Player.Motor.Session.SetControlLock(this, false);
            SetStage(NextCheckpoint == Checkpoints.Length ? MissionStage.Skylight : MissionStage.RooftopRoute);
            recovery = null;
        }
        void SetStage(MissionStage stage)
        { Stage = stage; StageChanged?.Invoke(stage); ObjectiveChanged?.Invoke(); }
        void FaceObjective()
        {
            if (Player.Motor.Orbit == null || Checkpoints.Length == 0) return;
            Vector3 destination = NextCheckpoint < Checkpoints.Length ? Checkpoints[NextCheckpoint].transform.position : Skylight.transform.position;
            Vector3 direction = destination - Player.transform.position;
            Player.Motor.Orbit.SetHeading(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
        }
        void OnDestroy()
        {
            if (!subscriptionsReady) return;
            Player.RecoveryRequested -= RequestRecovery; Player.InteractRequested -= Interact; Player.Health.Died -= RequestRecovery;
            Encounter.Cleared -= OnEncounterCleared;
            foreach (var enemy in Encounter.Enemies) if (enemy != null) enemy.Defeated -= EnemyCountChanged;
            if (Intro != null) Intro.Finished -= IntroFinished;
            if (Player.Motor.Session != null) Player.Motor.Session.SetControlLock(this, false);
        }
    }
}
