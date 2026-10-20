using UnityEngine;
using UnityEngine.UIElements;

namespace SpiderManRebuild
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] CharacterMotor motor;
        [SerializeField] GameSession session;
        [SerializeField] RespawnController respawn;
        [SerializeField] FrameTimeRecorder timing;
        Label state, speed, fps, objective;
        VisualElement pausePanel;
        double nextUpdate;
        public void Configure(CharacterMotor movement, GameSession game, RespawnController retry, FrameTimeRecorder stats)
        { motor = movement; session = game; respawn = retry; timing = stats; }
        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            state = root.Q<Label>("state"); speed = root.Q<Label>("speed");
            fps = root.Q<Label>("fps"); objective = root.Q<Label>("objective");
            pausePanel = root.Q("pause-panel");
            root.Q<Button>("resume").clicked += Resume;
            root.Q<Button>("restart").clicked += Restart;
            root.Q<Button>("quit").clicked += Quit;
            if (Application.isEditor) root.Q<Button>("quit").style.display = DisplayStyle.None;
        }
        void Resume() => session.SetPaused(false);
        void Restart() { respawn.ResetPlayer(); session.SetPaused(false); }
        void Quit() => Application.Quit();
        void OnDisable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            var resume = root.Q<Button>("resume"); if (resume != null) resume.clicked -= Resume;
            var restart = root.Q<Button>("restart"); if (restart != null) restart.clicked -= Restart;
            var quit = root.Q<Button>("quit"); if (quit != null) quit.clicked -= Quit;
        }
        void Update()
        {
            pausePanel.style.display = session.IsPaused ? DisplayStyle.Flex : DisplayStyle.None;
            if (Time.unscaledTimeAsDouble < nextUpdate) return;
            nextUpdate = Time.unscaledTimeAsDouble + .15;
            state.text = motor.State == LocomotionState.Grounded ? "GROUNDED" : "AIRBORNE";
            speed.text = $"{motor.Speed:0.0} m/s";
            fps.text = timing.FramesPerSecond > 0 ? $"{timing.FramesPerSecond:0} FPS  /  P95 {timing.P95Milliseconds:0.0} ms" : "WARMING UP";
            objective.text = session.GoalReached ? "COURSE COMPLETE  /  R to try again" : "Reach the cyan beacon on the far rooftop.";
        }
    }
}
