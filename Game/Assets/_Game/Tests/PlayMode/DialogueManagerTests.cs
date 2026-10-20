using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpiderManRebuild.Tests
{
    public sealed class DialogueManagerTests
    {
        DialogueDatabase database;
        TextAsset manifest;
        AudioClip clip;
        GameObject root;
        DialogueManager manager;
        [UnitySetUp] public IEnumerator Setup()
        {
            if (DialogueManager.Instance != null) Object.Destroy(DialogueManager.Instance.gameObject);
            yield return null;
            database = ScriptableObject.CreateInstance<DialogueDatabase>();
            clip = AudioClip.Create("Synthetic speech", 44100, 1, 44100, false);
            manifest = new TextAsset("{\"schema_version\":1,\"clips\":[{\"clip_id\":\"test\",\"file_name\":\"Wav/test.wav\",\"speaker\":\"Test speaker\",\"subtitle_text\":\"Test subtitle\",\"subtitle_status\":\"verified\"}]}");
            database.Initialize(manifest, new[] { new DialogueAudioBinding("test", clip) });
            root = new GameObject("Audio service test"); root.SetActive(false);
            manager = root.AddComponent<DialogueManager>(); manager.Database = database;
            manager.PersistBetweenScenes = false; root.SetActive(true); yield return null;
        }
        [UnityTest] public IEnumerator PlaybackPublishesSubtitleAndStopsCleanly()
        {
            Assert.That(manager.Play("missing"), Is.False);
            Assert.That(manager.Play("test", new Vector3(4, 2, 1)), Is.True);
            yield return null;
            Assert.That(manager.Speaker, Is.EqualTo("Test speaker"));
            Assert.That(manager.Subtitle, Is.EqualTo("Test subtitle"));
            Assert.That(manager.DialogueSource.spatialBlend, Is.EqualTo(1));
            Assert.That(manager.DialogueSource.transform.position, Is.EqualTo(new Vector3(4, 2, 1)));
            manager.StopDialogue();
            Assert.That(manager.ActiveClipId, Is.Null); Assert.That(manager.Subtitle, Is.Empty);
        }
        [UnityTest] public IEnumerator PauseAndTimelineOwnershipPreserveNarration()
        {
            Assert.That(manager.BeginTimeline("test", root, "", ""), Is.True);
            yield return null;
            AudioListener.pause = true;
            yield return new WaitForSecondsRealtime(.06f);
            Assert.That(manager.ActiveClipId, Is.EqualTo("test"));
            AudioListener.pause = false;
            manager.EndTimeline(database); Assert.That(manager.ActiveClipId, Is.EqualTo("test"));
            manager.EndTimeline(root); Assert.That(manager.ActiveClipId, Is.Null);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            AudioListener.pause = false;
            Object.Destroy(root); Object.Destroy(database); Object.Destroy(manifest); Object.Destroy(clip);
            yield return null;
        }
    }
}
