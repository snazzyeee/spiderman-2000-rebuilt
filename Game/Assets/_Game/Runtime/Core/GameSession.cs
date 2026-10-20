using System;
using UnityEngine;

namespace SpiderManRebuild
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        public bool IsPaused { get; private set; }
        public bool GoalReached { get; private set; }
        public event Action<bool> PauseChanged;
        public void Configure(PlayerInputReader reader) => input = reader;
        void Start() => SetPaused(false);
        void Update()
        {
            if (input.Command.PausePressed) SetPaused(!IsPaused);
        }
        public void SetPaused(bool paused)
        {
            bool changed = IsPaused != paused;
            IsPaused = paused;
            Time.timeScale = paused ? 0 : 1;
            if (!Application.isBatchMode && (input == null || !input.Automation))
            {
                Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = paused;
            }
            input?.Clear();
            if (changed) PauseChanged?.Invoke(paused);
        }
        public void ReachGoal() => GoalReached = true;
        public void ResetCourse() => GoalReached = false;
        void OnApplicationFocus(bool focused)
        {
            if (!focused && !Application.isBatchMode && (input == null || !input.Automation)) SetPaused(true);
        }
        void OnDestroy()
        {
            Time.timeScale = 1;
            if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
    }
}
