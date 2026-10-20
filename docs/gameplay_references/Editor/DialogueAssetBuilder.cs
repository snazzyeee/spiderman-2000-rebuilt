using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace SpiderManRebuild.Editor
{
    public static class DialogueAssetBuilder
    {
        public const string Root = "Assets/_LocalReference/Audio";
        public const string DatabasePath = Root + "/DialogueDatabase.asset";
        public const string ServicePath = Root + "/DialogueService.prefab";
        [Serializable] sealed class ImportDocument { public DialogueEntry[] clips; }
        [MenuItem("Rebuild/Audio/Import Manifest and Build Database")]
        public static void BuildDatabase()
        {
            string manifestPath = Root + "/dialogue_manifest.json";
            if (!File.Exists(manifestPath)) throw new FileNotFoundException("Run extract_audio_manifest.py first.", manifestPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var document = JsonUtility.FromJson<ImportDocument>(File.ReadAllText(manifestPath));
            if (document?.clips == null || document.clips.Length == 0) throw new InvalidDataException("Empty audio manifest.");
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (DialogueEntry entry in document.clips)
                {
                    string path = Root + "/" + entry.file_name;
                    if (!Path.GetFullPath(path).StartsWith(Path.GetFullPath(Root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Audio path escapes private import root.");
                    var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (importer == null) throw new InvalidDataException("Missing WAV importer: " + path);
                    var settings = importer.defaultSampleSettings;
                    AudioClipLoadType loadType = entry.category == "Music" ? AudioClipLoadType.Streaming :
                        entry.category == "Dialogue" ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                    if (settings.loadType == loadType && settings.compressionFormat == AudioCompressionFormat.Vorbis
                        && Mathf.Approximately(settings.quality, .7f) && importer.loadInBackground) continue;
                    settings.loadType = loadType; settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = .7f; settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                    importer.defaultSampleSettings = settings; importer.loadInBackground = true;
                    var serialized = new SerializedObject(importer);
                    var preload = serialized.FindProperty("m_PreloadAudioData");
                    if (preload != null) { preload.boolValue = entry.category == "SFX"; serialized.ApplyModifiedPropertiesWithoutUndo(); }
                    importer.SaveAndReimport();
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var bindings = new List<DialogueAudioBinding>();
            foreach (DialogueEntry entry in document.clips)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/" + entry.file_name);
                if (clip == null) throw new InvalidDataException("Missing imported clip: " + entry.clip_id);
                bindings.Add(new DialogueAudioBinding(entry.clip_id, clip));
            }
            var database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DatabasePath);
            if (database == null) { database = ScriptableObject.CreateInstance<DialogueDatabase>(); AssetDatabase.CreateAsset(database, DatabasePath); }
            database.Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>(manifestPath), bindings.ToArray());
            EditorUtility.SetDirty(database);
            var service = new GameObject("Dialogue Service");
            var manager = service.AddComponent<DialogueManager>(); manager.Database = database;
            manager.Mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/_Game/Settings/Traversal.mixer");
            manager.DialogueBus = Bus(manager.Mixer, "Dialogue"); manager.SfxBus = Bus(manager.Mixer, "SFX");
            manager.MusicBus = Bus(manager.Mixer, "Music");
            PrefabUtility.SaveAsPrefabAsset(service, ServicePath); UnityEngine.Object.DestroyImmediate(service);
            AssetDatabase.SaveAssets();
            Debug.Log("DIALOGUE_DATABASE_READY: " + database.Count + " clips");
        }
        public static AudioMixerGroup Bus(AudioMixer mixer, string name)
            => mixer.FindMatchingGroups(name).Single(group => group.name == name);
    }
}
