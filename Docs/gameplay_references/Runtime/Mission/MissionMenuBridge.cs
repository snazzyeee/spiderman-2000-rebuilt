using UnityEngine;

namespace SpiderManRebuild
{
    [DefaultExecutionOrder(100), RequireComponent(typeof(MissionManager))]
    public sealed class MissionMenuBridge : MonoBehaviour
    {
        MissionManager mission;
        bool training;
        public bool Training => training;
        void Awake()
        {
            mission = GetComponent<MissionManager>();
            if (MissionLaunchRequest.Pending && MissionLaunchRequest.Mode != MissionLaunchMode.NewGame)
                mission.SkipIntroOnNextBegin = true;
        }
        void Start()
        {
            if (MissionLaunchRequest.Pending)
            {
                training = MissionLaunchRequest.Mode == MissionLaunchMode.Training;
                if (MissionLaunchRequest.Mode == MissionLaunchMode.Continue && MissionLaunchRequest.Save != null)
                    mission.RestoreCheckpoint(MissionLaunchRequest.Save.checkpoint, MissionLaunchRequest.Save.bankCleared);
                MissionLaunchRequest.Clear();
            }
            var orbit = mission.Player.Motor.Orbit;
            orbit.InvertY = PlayerPrefs.GetInt("Camera.InvertY", 0) != 0;
            orbit.MouseSensitivity = PlayerPrefs.GetFloat("Camera.Sensitivity", .12f);
            if (training) mission.Player.Health.InvulnerableUntil = float.PositiveInfinity;
            mission.ObjectiveChanged += SaveCheckpoint;
            mission.StageChanged += OnStage;
            SaveCheckpoint();
        }
        void OnStage(MissionStage stage)
        {
            if (training && stage == MissionStage.RooftopRoute) mission.Player.Health.InvulnerableUntil = float.PositiveInfinity;
            if (training && stage == MissionStage.BankEncounter)
                foreach (var enemy in mission.Encounter.Enemies)
                    enemy.Health.Apply(new DamageHit(enemy.Health.Maximum, Team.Player, DamageKind.Melee));
        }
        void SaveCheckpoint()
        {
            if (training || !mission.GameplayActive || mission.Stage == MissionStage.Recovering) return;
            var save = new MissionSaveData { checkpoint = mission.SavedCheckpoint,
                bankCleared = mission.Stage == MissionStage.Skylight || mission.Stage == MissionStage.Complete };
            if (!SaveGameStore.TryWrite(save, out string error)) Debug.LogWarning("Checkpoint save failed: " + error);
        }
        void OnDestroy()
        { if (mission != null) { mission.ObjectiveChanged -= SaveCheckpoint; mission.StageChanged -= OnStage; } }
    }
}
