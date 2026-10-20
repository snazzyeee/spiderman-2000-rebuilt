using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpiderManRebuild
{
    [Serializable] public sealed class DialogueMapping
    {
        public string scene_or_mission_id, playback_trigger, speaker, subtitle_text, status, evidence;
    }
    [Serializable] public sealed class DialogueEntry
    {
        public string clip_id, file_name, source_path, speaker, scene_or_mission_id, subtitle_text;
        public string playback_trigger, category, mapping_status, subtitle_status;
        public string level_id, transcript, transcript_status, trigger_condition, trigger_condition_status;
        public float duration_seconds;
        public DialogueMapping[] mappings;
        public bool VerifiedSubtitle => subtitle_status == "verified";
    }
    [Serializable] public struct DialogueAudioBinding
    {
        public string ClipId;
        public AudioClip Clip;
        public DialogueAudioBinding(string id, AudioClip clip) { ClipId = id; Clip = clip; }
    }
    [CreateAssetMenu(menuName = "Spider-Man/Dialogue Database")]
    public sealed class DialogueDatabase : ScriptableObject
    {
        [Serializable] sealed class ManifestDocument { public int schema_version; public DialogueEntry[] clips; }
        [SerializeField] TextAsset manifest;
        [SerializeField] DialogueAudioBinding[] audio = Array.Empty<DialogueAudioBinding>();
        readonly Dictionary<string, DialogueEntry> entries = new Dictionary<string, DialogueEntry>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyCollection<DialogueEntry> Entries => entries.Values;
        public int Count => entries.Count;
        void OnEnable() { if (manifest != null) Rebuild(); }
        public void Initialize(TextAsset source, DialogueAudioBinding[] bindings)
        { manifest = source; audio = bindings ?? Array.Empty<DialogueAudioBinding>(); Rebuild(); }
        public void Rebuild()
        {
            entries.Clear(); clips.Clear();
            if (manifest == null) return;
            ManifestDocument document = JsonUtility.FromJson<ManifestDocument>(manifest.text);
            if (document == null || document.schema_version != 1 || document.clips == null)
                throw new InvalidDataException("Unsupported dialogue manifest schema.");
            foreach (DialogueEntry entry in document.clips)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.clip_id) || !entries.TryAdd(entry.clip_id, entry))
                    throw new InvalidDataException("Empty or duplicate dialogue clip identity.");
                string path = (entry.file_name ?? "").Replace('\\', '/');
                if (string.IsNullOrEmpty(path) || path.StartsWith("/") || path.Contains(":") || Array.IndexOf(path.Split('/'), "..") >= 0)
                    throw new InvalidDataException("Unsafe dialogue audio path: " + entry.clip_id);
            }
            foreach (DialogueAudioBinding binding in audio)
            {
                if (string.IsNullOrEmpty(binding.ClipId) || !entries.ContainsKey(binding.ClipId))
                    throw new InvalidDataException("Audio binding has no manifest entry.");
                if (binding.Clip == null || !clips.TryAdd(binding.ClipId, binding.Clip))
                    throw new InvalidDataException("Missing or duplicate audio binding: " + binding.ClipId);
            }
        }
        public bool TryGet(string id, out DialogueEntry entry)
        { entry = null; return !string.IsNullOrEmpty(id) && entries.TryGetValue(id, out entry); }
        public bool TryResolve(string id, out DialogueEntry entry, out AudioClip clip)
        { clip = null; return TryGet(id, out entry) && clips.TryGetValue(id, out clip) && clip != null; }
        public bool TryFindSource(string fileName, out DialogueEntry entry)
        {
            entry = null;
            foreach (DialogueEntry candidate in entries.Values)
            {
                if (!string.Equals(Path.GetFileName(candidate.source_path), fileName, StringComparison.OrdinalIgnoreCase)) continue;
                if (entry != null) { entry = null; return false; }
                entry = candidate;
            }
            return entry != null;
        }
        public List<DialogueEntry> FindEvent(string scene, string trigger, bool includeAuthored = false)
        {
            var result = new List<DialogueEntry>();
            foreach (DialogueEntry entry in entries.Values)
            {
                if (entry.mappings == null) continue;
                foreach (DialogueMapping mapping in entry.mappings)
                    if ((mapping.status == "verified" || (includeAuthored && mapping.status == "authored"))
                        && string.Equals(mapping.scene_or_mission_id, scene, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(mapping.playback_trigger, trigger, StringComparison.OrdinalIgnoreCase))
                    { result.Add(entry); break; }
            }
            result.Sort((a, b) => string.CompareOrdinal(a.clip_id, b.clip_id));
            return result;
        }
    }
}
