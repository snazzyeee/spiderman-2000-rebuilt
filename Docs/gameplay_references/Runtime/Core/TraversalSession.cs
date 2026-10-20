using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiderManRebuild
{
    [DefaultExecutionOrder(-250), RequireComponent(typeof(TraversalInput))]
    public sealed class TraversalSession : MonoBehaviour
    {
        TraversalInput input;
        public bool Paused { get; private set; }
        public bool MenuMode { get; private set; }
        public void SetMenuMode(bool enabled) { MenuMode = enabled; UpdateCursor(); }
        readonly HashSet<UnityEngine.Object> locks = new HashSet<UnityEngine.Object>();
        public bool ControlBlocked => Paused || locks.Count > 0;
        public event Action<bool> PauseChanged;
        public void SetControlLock(UnityEngine.Object owner, bool locked)
        { if (owner == null) return; if (locked) locks.Add(owner); else locks.Remove(owner); }
        void Awake() { input = GetComponent<TraversalInput>(); }
        void Start()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            SetPaused(false);
        }
        void Update() { if (!input.Automation && input.PausePressed) SetPaused(!Paused); }
        public void SetPaused(bool paused)
        {
            bool changed = Paused != paused;
            Paused = paused; Time.timeScale = paused ? 0 : 1;
            AudioListener.pause = paused;
            UpdateCursor();
            if (changed) PauseChanged?.Invoke(paused);
        }
        void UpdateCursor()
        {
            if (input != null && !input.Automation && !Application.isBatchMode)
            {
                Cursor.lockState = Paused || MenuMode ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Paused || MenuMode;
            }
        }
        void OnApplicationFocus(bool focused)
        { if (!focused && input != null && !input.Automation) SetPaused(true); }
        void OnDestroy()
        {
            Time.timeScale = 1;
            AudioListener.pause = false;
            if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
    }
}
