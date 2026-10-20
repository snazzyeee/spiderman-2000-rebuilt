using UnityEngine;

namespace SpiderManRebuild
{
    [DefaultExecutionOrder(-250), RequireComponent(typeof(TraversalInput))]
    public sealed class TraversalSession : MonoBehaviour
    {
        TraversalInput input;
        public bool Paused { get; private set; }
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
            Paused = paused; Time.timeScale = paused ? 0 : 1;
            if (input != null && !input.Automation && !Application.isBatchMode)
            {
                Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = paused;
            }
        }
        void OnApplicationFocus(bool focused)
        { if (!focused && input != null && !input.Automation) SetPaused(true); }
        void OnDestroy()
        {
            Time.timeScale = 1;
            if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
    }
}
