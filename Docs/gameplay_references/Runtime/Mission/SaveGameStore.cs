using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SpiderManRebuild
{
    [Serializable] public sealed class MissionSaveData
    {
        public int version = 1, checkpoint;
        public string scene = "BankHeistRooftops", savedUtc;
        public bool bankCleared;
    }
    public static class SaveGameStore
    {
        public static string OverridePath { get; set; }
        public static string FilePath => OverridePath ?? Path.Combine(Application.persistentDataPath, "mission-save.json");
        public static bool TryRead(out MissionSaveData data, out string error)
        {
            data = null; error = "";
            try
            {
                if (!File.Exists(FilePath)) { error = "No saved game yet."; return false; }
                if (new FileInfo(FilePath).Length > 65536) throw new InvalidDataException("Save file is too large.");
                data = JsonUtility.FromJson<MissionSaveData>(File.ReadAllText(FilePath));
                if (data == null || data.version != 1 || data.scene != "BankHeistRooftops" || data.checkpoint < 0 || data.checkpoint > 100)
                    throw new InvalidDataException("Saved game is invalid or incompatible.");
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is UnauthorizedAccessException || exception is ArgumentException)
            { data = null; error = exception.Message; return false; }
        }
        public static bool TryWrite(MissionSaveData data, out string error)
        {
            error = "";
            try
            {
                data.savedUtc = DateTime.UtcNow.ToString("O");
                string path = Path.GetFullPath(FilePath), temporary = path + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            { error = exception.Message; return false; }
        }
    }
    public enum MissionLaunchMode { NewGame, Continue, Training }
    public static class MissionLaunchRequest
    {
        public static bool Pending { get; private set; }
        public static MissionLaunchMode Mode { get; private set; }
        public static MissionSaveData Save { get; private set; }
        public static void Set(MissionLaunchMode mode, MissionSaveData save = null)
        { Mode = mode; Save = save; Pending = true; }
        public static void Clear() { Pending = false; Save = null; }
    }
}
