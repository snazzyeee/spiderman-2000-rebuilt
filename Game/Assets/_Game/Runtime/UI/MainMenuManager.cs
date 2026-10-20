using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpiderManRebuild
{
    public sealed class MainMenuManager : MonoBehaviour
    {
        public Button[] MainButtons;
        public GameObject MainRoot, PageRoot;
        public RectTransform Rows;
        public Button RowPrefab;
        public Slider SliderPrefab;
        public TMP_Text PageTitle, Description, Status, Body;
        public RawImage GalleryImage;
        public Texture2D[] GalleryArt;
        public TextAsset Credits;
        public CanvasGroup Fade;
        public string MissionScene = "BankHeistRooftops", MenuShiftId, MenuConfirmId, MenuMusicId;
        public string CurrentPage { get; private set; } = "Main";
        public bool Loading { get; private set; }
        public int GalleryIndex { get; private set; }
        readonly List<Selectable> selections = new List<Selectable>();
        readonly List<GameObject> generatedRows = new List<GameObject>();
        InputAction cancel;
        MenuSelectionEffect highlighted;
        int mainIndex, creditsPage;
        string[] creditPages;
        bool ready;
        public DialogueManager Audio => DialogueManager.Instance;
        void Awake()
        {
            cancel = new InputAction("MenuCancel", InputActionType.Button, "<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");
            creditPages = Paginate(Credits != null ? Credits.text : "Spider-Man PC remake", 850);
        }
        void OnEnable() => cancel.Enable();
        void OnDisable() => cancel.Disable();
        void OnDestroy() => cancel.Dispose();
        void Start()
        {
            Time.timeScale = 1; AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Application.targetFrameRate = 60;
            string[] pages = { "Game", "Options", "Training", "Special", "Gallery" };
            for (int i = 0; i < MainButtons.Length; i++)
            {
                int index = i; MainButtons[i].onClick.AddListener(() => { Confirm(); mainIndex = index; OpenPage(pages[index]); });
                var effect = MainButtons[i].GetComponent<MenuSelectionEffect>(); effect.Owner = this;
            }
            Connect(MainButtons); ready = true; OpenPage("Main");
            if (Audio != null && !string.IsNullOrEmpty(MenuMusicId)) Audio.PlayMusic(MenuMusicId);
        }
        void Update()
        { if (ready && !Loading && cancel.WasPressedThisFrame()) Back(); }
        public void Highlight(MenuSelectionEffect effect)
        {
            Description.text = effect.Description ?? "";
            if (ready && highlighted != effect && Audio != null) Audio.PlaySfx(MenuShiftId);
            highlighted = effect;
        }
        void Confirm() { if (Audio != null) Audio.PlaySfx(MenuConfirmId); }
        public void Back()
        {
            Confirm();
            if (CurrentPage == "Main") OpenPage("Exit");
            else if (CurrentPage == "Music" || CurrentPage == "Credits") OpenPage("Special");
            else if (CurrentPage == "Controls") OpenPage("Training");
            else OpenPage("Main");
        }
        public void OpenPage(string page)
        {
            if (Loading) return;
            foreach (GameObject row in generatedRows) { row.SetActive(false); Destroy(row); }
            generatedRows.Clear(); selections.Clear(); highlighted = null;
            CurrentPage = page; MainRoot.SetActive(page == "Main"); PageRoot.SetActive(page != "Main");
            GalleryImage.gameObject.SetActive(page == "Gallery"); Body.gameObject.SetActive(page == "Credits" || page == "Controls");
            Body.transform.parent.gameObject.SetActive(page == "Credits" || page == "Controls");
            PageTitle.text = page.ToUpperInvariant(); Status.text = "";
            if (page == "Main")
            { EventSystem.current.SetSelectedGameObject(MainButtons[Mathf.Clamp(mainIndex, 0, MainButtons.Length - 1)].gameObject); return; }
            if (page == "Game")
            {
                AddButton("NEW GAME", () => StartNewGame(false), "Start Bank Heist / Rooftops with the original narrated intro.");
                bool saved = SaveGameStore.TryRead(out var data, out string error);
                var load = AddButton("LOAD GAME", StartContinue, saved ? "Resume the last saved rooftop checkpoint." : error);
                load.interactable = saved; Status.text = saved ? "Checkpoint " + data.checkpoint + "  /  " + data.savedUtc : error;
            }
            else if (page == "Options")
            {
                foreach (string channel in new[] { "Master", "Music", "SFX", "Dialogue" })
                { string bus = channel; AddSlider(channel.ToUpperInvariant(), Audio != null ? Audio.GetVolume(bus) : 1,
                    value => { if (Audio != null) Audio.SetVolume(bus, value); }); }
                AddSlider("MOUSE LOOK", Mathf.InverseLerp(.04f, .3f, PlayerPrefs.GetFloat("Camera.Sensitivity", .12f)),
                    value => PlayerPrefs.SetFloat("Camera.Sensitivity", Mathf.Lerp(.04f, .3f, value)));
                AddButton("INVERT CAMERA Y: " + (PlayerPrefs.GetInt("Camera.InvertY", 0) == 0 ? "OFF" : "ON"), () => {
                    PlayerPrefs.SetInt("Camera.InvertY", 1 - PlayerPrefs.GetInt("Camera.InvertY", 0)); OpenPage("Options"); }, "Invert vertical camera look in gameplay.");
                AddButton("FULLSCREEN: " + (Screen.fullScreen ? "ON" : "OFF"), () => {
                    Screen.fullScreen = !Screen.fullScreen; StartCoroutine(RefreshDisplayOptions()); }, "Toggle windowed/fullscreen display.");
            }
            else if (page == "Training")
            {
                AddButton("ROOFTOP PRACTICE", () => StartNewGame(true), "Practice traversal on the slice rooftops with damage disabled.");
                AddButton("CONTROLS", () => OpenPage("Controls"), "Keyboard/mouse and dual-stick controls.");
            }
            else if (page == "Controls")
            {
                Body.text = "WASD / LEFT STICK   MOVE\nMOUSE / RIGHT STICK   LOOK\nSPACE / A   JUMP\nF / RT   SWING (HOLD)\nQ / LT   ZIP\nC / B   WALL LATCH (HOLD)\nLEFT CLICK / X   MELEE\nRIGHT CLICK / RB   WEB\nV / D-PAD DOWN   WEB DOME\nE / Y   INTERACT\nR / BACK   CHECKPOINT\nENTER / A   SKIP INTRO";
            }
            else if (page == "Special")
            {
                AddButton("MUSIC ARCHIVE", () => OpenPage("Music"), "Listen to the extracted original music streams.");
                AddButton("CREDITS", () => OpenPage("Credits"), "View the original PC credits.");
            }
            else if (page == "Music" && Audio != null && Audio.Database != null)
            {
                foreach (DialogueEntry entry in Audio.Database.Entries.Where(e => e.category == "Music").OrderBy(e => e.clip_id))
                {
                    string id = entry.clip_id;
                    string name = System.IO.Path.GetFileNameWithoutExtension(entry.source_path).Replace('_', ' ').ToUpperInvariant();
                    AddButton(name, () => { Audio.PlayMusic(id); Status.text = "NOW PLAYING: " + name; }, "Original audio / " + entry.duration_seconds.ToString("0.0") + " seconds");
                }
            }
            else if (page == "Gallery")
            {
                ShowGallery();
                AddButton("PREVIOUS COVER", () => { GalleryIndex = (GalleryIndex + GalleryArt.Length - 1) % GalleryArt.Length; ShowGallery(); }, "Browse original comic art.");
                AddButton("NEXT COVER", () => { GalleryIndex = (GalleryIndex + 1) % GalleryArt.Length; ShowGallery(); }, "Browse original comic art.");
            }
            else if (page == "Credits")
            {
                ShowCredits();
                AddButton("PREVIOUS PAGE", () => { creditsPage = (creditsPage + creditPages.Length - 1) % creditPages.Length; ShowCredits(); });
                AddButton("NEXT PAGE", () => { creditsPage = (creditsPage + 1) % creditPages.Length; ShowCredits(); });
            }
            else if (page == "Exit") AddButton("QUIT GAME", () => Application.Quit(), "Exit to desktop.");
            AddButton("BACK", () => { PlayerPrefs.Save(); Back(); });
            Connect(selections);
            var first = selections.FirstOrDefault(item => item.IsInteractable());
            EventSystem.current.SetSelectedGameObject(first != null ? first.gameObject : null);
        }
        IEnumerator RefreshDisplayOptions() { yield return null; yield return null; OpenPage("Options"); }
        void ShowGallery()
        {
            if (GalleryArt == null || GalleryArt.Length == 0) throw new InvalidOperationException("Menu gallery art was not configured.");
            GalleryIndex = Mathf.Clamp(GalleryIndex, 0, GalleryArt.Length - 1);
            GalleryImage.texture = GalleryArt[GalleryIndex];
            var fitter = GalleryImage.GetComponent<AspectRatioFitter>();
            if (fitter != null) fitter.aspectRatio = (float)GalleryArt[GalleryIndex].width / GalleryArt[GalleryIndex].height;
            Status.text = "ORIGINAL COMIC COVER " + (GalleryIndex + 1) + " / " + GalleryArt.Length;
        }
        void ShowCredits() { Body.text = creditPages[creditsPage]; Status.text = "ORIGINAL PC CREDITS " + (creditsPage + 1) + " / " + creditPages.Length; }
        Button AddButton(string label, Action action, string description = "")
        {
            Button button = Instantiate(RowPrefab, Rows); button.gameObject.SetActive(true);
            button.GetComponentInChildren<TMP_Text>().text = label;
            var effect = button.GetComponent<MenuSelectionEffect>(); effect.Owner = this; effect.Description = description;
            button.onClick.AddListener(() => { Confirm(); action(); });
            generatedRows.Add(button.gameObject); selections.Add(button); return button;
        }
        void AddSlider(string label, float value, Action<float> setter)
        {
            Slider slider = Instantiate(SliderPrefab, Rows); slider.gameObject.SetActive(true);
            var text = slider.GetComponentInChildren<TMP_Text>();
            slider.SetValueWithoutNotify(value); text.text = label + "  " + Mathf.RoundToInt(value * 100) + "%";
            slider.onValueChanged.AddListener(current => { setter(current); text.text = label + "  " + Mathf.RoundToInt(current * 100) + "%"; });
            var effect = slider.GetComponent<MenuSelectionEffect>(); effect.Owner = this;
            effect.Description = "LEFT / RIGHT or D-pad adjusts " + label.ToLowerInvariant() + ".";
            generatedRows.Add(slider.gameObject); selections.Add(slider);
        }
        static void Connect(IReadOnlyList<Selectable> controls)
        {
            for (int i = 0; i < controls.Count; i++)
            {
                var navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = controls[(i + controls.Count - 1) % controls.Count], selectOnDown = controls[(i + 1) % controls.Count] };
                if (!(controls[i] is Slider)) { navigation.selectOnLeft = navigation.selectOnUp; navigation.selectOnRight = navigation.selectOnDown; }
                controls[i].navigation = navigation;
            }
        }
        static string[] Paginate(string text, int length)
        {
            var pages = new List<string>(); int offset = 0;
            while (offset < text.Length)
            {
                int count = Mathf.Min(length, text.Length - offset);
                if (offset + count < text.Length)
                { int end = text.LastIndexOf('\n', offset + count - 1, count); if (end >= offset + length / 2) count = end - offset + 1; }
                pages.Add(text.Substring(offset, count)); offset += count;
            }
            return pages.Count == 0 ? new[] { "" } : pages.ToArray();
        }
        public void StartNewGame(bool practice)
        { if (!Loading) StartCoroutine(LoadMission(practice ? MissionLaunchMode.Training : MissionLaunchMode.NewGame, null)); }
        public void StartContinue()
        {
            if (SaveGameStore.TryRead(out var save, out string error))
            { if (!Loading) StartCoroutine(LoadMission(MissionLaunchMode.Continue, save)); }
            else Status.text = error;
        }
        IEnumerator LoadMission(MissionLaunchMode mode, MissionSaveData save)
        {
            if (!Application.CanStreamedLevelBeLoaded(MissionScene)) { Status.text = "Mission scene is missing from this build."; yield break; }
            Loading = true; Status.text = "LOADING ROOFTOPS…";
            Fade.blocksRaycasts = true; float opacity = 0;
            while (opacity < 1) { opacity += Time.unscaledDeltaTime * 3; Fade.alpha = Mathf.Min(1, opacity); yield return null; }
            PlayerPrefs.Save(); if (Audio != null) { Audio.StopDialogue(); Audio.StopMusic(); }
            MissionLaunchRequest.Set(mode, save);
            AsyncOperation operation = SceneManager.LoadSceneAsync(MissionScene, LoadSceneMode.Single);
            while (!operation.isDone) yield return null;
        }
    }
}
