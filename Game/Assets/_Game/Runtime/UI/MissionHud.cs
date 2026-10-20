using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace SpiderManRebuild
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MissionHud : MonoBehaviour
    {
        public MissionManager Mission;
        public DialoguePresenter Dialogue;
        Label objective, state, vitals, subtitle, speaker, marker;
        VisualElement pausePanel, completePanel, subtitlePanel;
        double refresh;
        Button returnMenuButton, completeMenuButton;
        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            objective = root.Q<Label>("objective"); state = root.Q<Label>("state");
            vitals = root.Q<Label>("vitals"); subtitle = root.Q<Label>("subtitle"); speaker = root.Q<Label>("speaker");
            marker = root.Q<Label>("marker"); pausePanel = root.Q("pause-panel");
            completePanel = root.Q("complete-panel"); subtitlePanel = root.Q("subtitle-panel");
            root.Q<Button>("resume").clicked += Resume;
            root.Q<Button>("restart").clicked += Recover;
            root.Q<Button>("replay").clicked += Replay;
            root.Q<Button>("quit").clicked += Quit;
            returnMenuButton = root.Q<Button>("return-menu"); completeMenuButton = root.Q<Button>("complete-menu");
            bool canReturn = Application.CanStreamedLevelBeLoaded("MainMenu");
            if (returnMenuButton != null) { returnMenuButton.style.display = canReturn ? DisplayStyle.Flex : DisplayStyle.None; returnMenuButton.clicked += ReturnToMenu; }
            if (completeMenuButton != null) { completeMenuButton.style.display = canReturn ? DisplayStyle.Flex : DisplayStyle.None; completeMenuButton.clicked += ReturnToMenu; }
        }
        void Start()
        { Dialogue.SubtitleChanged += OnSubtitle; Mission.ObjectiveChanged += RefreshObjective; }
        void OnSubtitle(string name, string text)
        { speaker.text = name; subtitle.text = text; subtitlePanel.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex; }
        void RefreshObjective() => objective.text = Mission.Objective;
        void Resume() => Mission.Player.Motor.Session.SetPaused(false);
        void Recover() { Mission.RequestRecovery(); Resume(); }
        void Replay() { Mission.BeginMission(); Resume(); }
        void Quit() => Application.Quit();
        void ReturnToMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded("MainMenu")) return;
            returnMenuButton?.SetEnabled(false); completeMenuButton?.SetEnabled(false);
            Mission.Player.Motor.Session.SetPaused(false);
            if (DialogueManager.Instance != null) DialogueManager.Instance.StopDialogue();
            SceneManager.LoadSceneAsync("MainMenu");
        }
        void Update()
        {
            if (Mission == null) return;
            pausePanel.style.display = Mission.Player.Motor.Session.Paused ? DisplayStyle.Flex : DisplayStyle.None;
            completePanel.style.display = Mission.Stage == MissionStage.Complete ? DisplayStyle.Flex : DisplayStyle.None;
            if (Time.unscaledTimeAsDouble >= refresh)
            {
                refresh = Time.unscaledTimeAsDouble + .1; RefreshObjective();
                state.text = Mission.Player.State.ToString().ToUpperInvariant();
                vitals.text = $"HEALTH {Mission.Player.Health.Current:0} / {Mission.Player.Health.Maximum:0}    WEB {Mission.Player.Combat.WebEnergy:0}";
            }
            bool show = Mission.Stage == MissionStage.RooftopRoute || Mission.Stage == MissionStage.Skylight;
            marker.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            Vector3 screen = Camera.main.WorldToScreenPoint(Mission.ObjectivePosition + Vector3.up * 2);
            var root = GetComponent<UIDocument>().rootVisualElement;
            float width = root.resolvedStyle.width, height = root.resolvedStyle.height;
            float x = screen.x / Screen.width * width, y = (1 - screen.y / Screen.height) * height;
            if (screen.z < 0) { x = width - x; y = height - 70; }
            marker.style.left = Mathf.Clamp(x, 25, width - 170); marker.style.top = Mathf.Clamp(y, 210, height - 130);
            marker.text = $"◆ {Vector3.Distance(Mission.Player.transform.position, Mission.ObjectivePosition):0} m";
        }
        void OnDisable()
        {
            if (returnMenuButton != null) returnMenuButton.clicked -= ReturnToMenu;
            if (completeMenuButton != null) completeMenuButton.clicked -= ReturnToMenu;
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            root.Q<Button>("resume").clicked -= Resume; root.Q<Button>("restart").clicked -= Recover;
            root.Q<Button>("replay").clicked -= Replay; root.Q<Button>("quit").clicked -= Quit;
        }
        void OnDestroy()
        {
            if (Dialogue != null) Dialogue.SubtitleChanged -= OnSubtitle;
            if (Mission != null) Mission.ObjectiveChanged -= RefreshObjective;
        }
    }
}
