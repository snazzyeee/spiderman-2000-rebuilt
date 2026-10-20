using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SpiderManRebuild.Tests
{
    public sealed class SaveGameStoreTests
    {
        string previous, path;
        [SetUp] public void Setup()
        {
            previous = SaveGameStore.OverridePath;
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../TestResults/save-unit"));
            Directory.CreateDirectory(root); path = Path.Combine(root, Guid.NewGuid().ToString("N") + ".json");
            SaveGameStore.OverridePath = path;
        }
        [TearDown] public void Cleanup()
        { File.Delete(path); File.Delete(path + ".tmp"); SaveGameStore.OverridePath = previous; }
        [Test] public void CheckpointRoundTripAndAtomicReplacement()
        {
            Assert.That(SaveGameStore.TryWrite(new MissionSaveData { checkpoint = 1 }, out _), Is.True);
            Assert.That(SaveGameStore.TryWrite(new MissionSaveData { checkpoint = 3, bankCleared = true }, out _), Is.True);
            Assert.That(SaveGameStore.TryRead(out var loaded, out _), Is.True);
            Assert.That(loaded.checkpoint, Is.EqualTo(3)); Assert.That(loaded.bankCleared, Is.True);
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }
        [Test] public void CorruptOrIncompatibleSaveIsRejected()
        {
            File.WriteAllText(path, "invalid json"); Assert.That(SaveGameStore.TryRead(out _, out _), Is.False);
            File.WriteAllText(path, "{\"version\":99,\"scene\":\"BankHeistRooftops\",\"checkpoint\":0}");
            Assert.That(SaveGameStore.TryRead(out _, out _), Is.False);
        }
    }
}
