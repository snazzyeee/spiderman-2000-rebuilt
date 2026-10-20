using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SpiderManRebuild.Tests
{
    public sealed class DialogueDatabaseTests
    {
        DialogueDatabase database;
        TextAsset manifest;
        AudioClip clip;
        [SetUp] public void Setup()
        { database = ScriptableObject.CreateInstance<DialogueDatabase>(); clip = AudioClip.Create("Test speech", 2205, 1, 22050, false); }
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(database); Object.DestroyImmediate(clip); if (manifest != null) Object.DestroyImmediate(manifest); }
        [Test] public void RegistryResolvesImportedClipAndFiltersUnverifiedEvents()
        {
            manifest = new TextAsset("{\"schema_version\":1,\"clips\":[{\"clip_id\":\"test\",\"file_name\":\"Wav/test.wav\",\"mappings\":[{\"scene_or_mission_id\":\"L1A1\",\"playback_trigger\":\"ON_LEVEL_START\",\"status\":\"authored\"}]}]}");
            database.Initialize(manifest, new[] { new DialogueAudioBinding("test", clip) });
            Assert.That(database.TryResolve("TEST", out _, out var resolved), Is.True); Assert.That(resolved, Is.SameAs(clip));
            Assert.That(database.FindEvent("L1A1", "ON_LEVEL_START"), Is.Empty);
            Assert.That(database.FindEvent("L1A1", "ON_LEVEL_START", true).Count, Is.EqualTo(1));
        }
        [Test] public void DuplicateIdentityIsRejected()
        {
            manifest = new TextAsset("{\"schema_version\":1,\"clips\":[{\"clip_id\":\"same\",\"file_name\":\"Wav/a.wav\"},{\"clip_id\":\"same\",\"file_name\":\"Wav/b.wav\"}]}");
            Assert.Throws<InvalidDataException>(() => database.Initialize(manifest, null));
        }
        [Test] public void TraversalPathIsRejected()
        {
            manifest = new TextAsset("{\"schema_version\":1,\"clips\":[{\"clip_id\":\"bad\",\"file_name\":\"../outside.wav\"}]}");
            Assert.Throws<InvalidDataException>(() => database.Initialize(manifest, null));
        }
    }
}
