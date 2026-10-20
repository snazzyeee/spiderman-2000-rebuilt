using UnityEngine;
using UnityEngine.UIElements;

namespace SpiderManRebuild
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class Phase1Hud : MonoBehaviour
    {
        public SpiderManCharacterController Motor;
        public TraversalSession Session;
        public FrameTimeRecorder Timing;
        Label state, speed, fps;
        VisualElement pausePanel;
        double nextUpdate;
        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            state = root.Q<Label>("state"); speed = root.Q<Label>("speed"); fps = root.Q<Label>("fps");
            pausePanel = root.Q("pause-panel");
            root.Q<Label>("objective").text = "L1A1 asset integration / mechanics under verification";
            root.Q<Button>("resume").clicked += Resume;
            root.Q<Button>("restart").clicked += Restart;
            root.Q<Button>("quit").clicked += Quit;
        }
        void Resume() => Session.SetPaused(false);
        void Restart() { Motor.ResetPlayer(); Session.SetPaused(false); }
        void Quit() => Application.Quit();
        void OnDisable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            var resume = root.Q<Button>("resume"); if (resume != null) resume.clicked -= Resume;
            var restart = root.Q<Button>("restart"); if (restart != null) restart.clicked -= Restart;
            var quit = root.Q<Button>("quit"); if (quit != null) quit.clicked -= Quit;
        }
        void Update()
        {
            pausePanel.style.display = Session.Paused ? DisplayStyle.Flex : DisplayStyle.None;
            if (Time.unscaledTimeAsDouble < nextUpdate) return;
            nextUpdate = Time.unscaledTimeAsDouble + .2;
            state.text = Motor.Grounded ? "GROUNDED" : "AIRBORNE";
            speed.text = new Vector2(Motor.Velocity.x, Motor.Velocity.z).magnitude.ToString("0.0") + " m/s";
            fps.text = Timing.FramesPerSecond > 0 ? $"{Timing.FramesPerSecond:0} FPS / P95 {Timing.P95Milliseconds:0.0} ms" : "WARMING UP";
        }
    }
}
