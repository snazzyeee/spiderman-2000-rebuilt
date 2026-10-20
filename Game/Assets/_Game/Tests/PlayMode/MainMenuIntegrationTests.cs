using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SpiderManRebuild.Tests
{
    public sealed class MainMenuIntegrationTests
    {
        MainMenuManager menu;
        Gamepad pad;
        string savePath, previous;
        const string ScenePath = "Assets/_LocalReference/Frontend/MainMenu.unity";
        [UnitySetUp] public IEnumerator Load()
        {
            if (!File.Exists(ScenePath)) Assert.Ignore("Private menu art/audio assets are required.");
            previous = SaveGameStore.OverridePath;
            savePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../TestResults/menu-unit-save.json"));
            File.Delete(savePath); SaveGameStore.OverridePath = savePath;
            SceneManager.sceneLoaded += AutomateMission;
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null;
            menu = Object.FindAnyObjectByType<MainMenuManager>(); pad = InputSystem.AddDevice<Gamepad>("MenuTestPad");
        }
        void AutomateMission(Scene scene, LoadSceneMode mode)
        {
            var mission = Object.FindAnyObjectByType<MissionManager>();
            if (mission == null) return;
            mission.Player.Automation = true; mission.Player.GetComponent<TraversalInput>().Automation = true;
            mission.Player.Motor.Session.SetPaused(false);
        }
        IEnumerator Press(GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button)); yield return null; yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null;
        }
        [UnityTest] public IEnumerator GamepadNavigatesSubmitsCancelsAndAdjustsMixer()
        {
            yield return Press(GamepadButton.DpadDown);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(menu.MainButtons[1].gameObject));
            yield return Press(GamepadButton.South); Assert.That(menu.CurrentPage, Is.EqualTo("Options"));
            float previousVolume = menu.Audio.GetVolume("Master");
            var slider = menu.Rows.GetComponentInChildren<Slider>(); slider.value = .45f;
            Assert.That(menu.Audio.Mixer.GetFloat("MasterVolume", out float gain), Is.True);
            Assert.That(gain, Is.EqualTo(20 * Mathf.Log10(.45f)).Within(.01f)); slider.value = previousVolume;
            yield return Press(GamepadButton.East); Assert.That(menu.CurrentPage, Is.EqualTo("Main"));
        }
        [UnityTest] public IEnumerator GalleryAndControlsUseConfiguredArtAndContent()
        {
            menu.OpenPage("Gallery"); yield return null;
            Assert.That(menu.GalleryArt.Length, Is.EqualTo(32)); Assert.That(menu.GalleryImage.texture, Is.Not.Null);
            menu.Rows.GetComponentsInChildren<Button>()[1].onClick.Invoke();
            Assert.That(menu.GalleryIndex, Is.EqualTo(1));
            menu.OpenPage("Controls"); yield return null;
            Assert.That(menu.Body.gameObject.activeInHierarchy, Is.True); Assert.That(menu.Body.text, Does.Contain("WALL LATCH"));
        }
        [UnityTest] public IEnumerator NewGameLoadsOriginalNarrationAndSkipStopsItsOwner()
        {
            menu.StartNewGame(false); MissionManager mission = null;
            for (int i = 0; i < 300; i++) { mission = Object.FindAnyObjectByType<MissionManager>(); if (mission != null) break; yield return null; }
            Assert.That(mission, Is.Not.Null); yield return null; yield return null;
            Assert.That(mission.Stage, Is.EqualTo(MissionStage.Intro));
            Assert.That(DialogueManager.Instance.ActiveClipId, Is.Not.Null);
            Assert.That(DialogueManager.Instance.Database.TryGet(DialogueManager.Instance.ActiveClipId, out var entry), Is.True);
            Assert.That(entry.source_path, Does.EndWith("/stan_00.bik"));
            mission.Intro.Skip(); yield return null;
            Assert.That(DialogueManager.Instance.ActiveClipId, Is.Null);
            Assert.That(SaveGameStore.TryRead(out _, out _), Is.True);
        }
        [UnityTest] public IEnumerator LoadPreservesClearedEncounterAndSkipsIntro()
        {
            Assert.That(SaveGameStore.TryWrite(new MissionSaveData { checkpoint = 3, bankCleared = true }, out _), Is.True);
            menu.StartContinue(); MissionManager mission = null;
            for (int i = 0; i < 300; i++) { mission = Object.FindAnyObjectByType<MissionManager>(); if (mission != null) break; yield return null; }
            Assert.That(mission, Is.Not.Null); yield return null; yield return null;
            Assert.That(mission.Stage, Is.EqualTo(MissionStage.Skylight));
            foreach (var enemy in mission.Encounter.Enemies) Assert.That(enemy.Health.Defeated, Is.True);
            Assert.That(DialogueManager.Instance.ActiveClipId, Is.Null);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= AutomateMission;
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            var empty = SceneManager.CreateScene("AfterMenuTests"); SceneManager.SetActiveScene(empty);
            var current = SceneManager.GetSceneByPath(ScenePath);
            if (current.IsValid() && current.isLoaded) yield return SceneManager.UnloadSceneAsync(current);
            var mission = SceneManager.GetSceneByPath("Assets/_LocalReference/Mission1/BankHeistRooftops.unity");
            if (mission.IsValid() && mission.isLoaded) yield return SceneManager.UnloadSceneAsync(mission);
            if (DialogueManager.Instance != null) Object.Destroy(DialogueManager.Instance.gameObject);
            yield return null; SaveGameStore.OverridePath = previous; File.Delete(savePath);
            Time.timeScale = 1; AudioListener.pause = false; MissionLaunchRequest.Clear();
        }
    }
}
