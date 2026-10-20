using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace SpiderManRebuild
{
    [DefaultExecutionOrder(-400)]
    public sealed class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }
        public DialogueDatabase Database;
        public AudioMixer Mixer;
        public AudioMixerGroup DialogueBus, SfxBus, MusicBus;
        public bool PersistBetweenScenes = true;
        public event Action<string, string> SubtitleChanged;
        public event Action<string> PlaybackFinished;
        public event Action<string> PlaybackFailed;
        public string ActiveClipId { get; private set; }
        public string Speaker { get; private set; } = "";
        public string Subtitle { get; private set; } = "";
        public bool IsSpeaking => ActiveClipId != null && (began || loading != null);
        public AudioSource DialogueSource => dialogue;
        AudioSource dialogue, music;
        AudioSource[] effects;
        int effectCursor, generation;
        Coroutine loading;
        UnityEngine.Object timelineOwner;
        string fallbackSpeaker, fallbackSubtitle;
        bool timelinePaused;
        bool began;
        float baseMusic = 1;
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (PersistBetweenScenes) { transform.SetParent(null); DontDestroyOnLoad(gameObject); }
            dialogue = MakeSource("Dialogue", DialogueBus, false);
            music = MakeSource("Music", MusicBus, false); music.loop = true;
            effects = new AudioSource[16];
            for (int i = 0; i < effects.Length; i++) effects[i] = MakeSource("SFX " + i, SfxBus, true);
        }
        AudioSource MakeSource(string name, AudioMixerGroup bus, bool spatial)
        {
            var child = new GameObject(name); child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.outputAudioMixerGroup = bus; source.dopplerLevel = 0; source.spatialBlend = spatial ? 1 : 0;
            source.minDistance = 1; source.maxDistance = 35; source.rolloffMode = AudioRolloffMode.Custom;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(
                new Keyframe(0, 1), new Keyframe(.1f, .65f), new Keyframe(.45f, .12f), new Keyframe(1, 0)));
            return source;
        }
        public bool Play(string id, Vector3? position = null, bool interrupt = true)
        {
            if (Database == null || !Database.TryResolve(id, out DialogueEntry entry, out AudioClip clip)) return false;
            if (!interrupt && IsSpeaking) return false;
            StopDialogue(); ActiveClipId = id;
            dialogue.spatialBlend = position.HasValue ? 1 : 0;
            if (position.HasValue) dialogue.transform.position = position.Value;
            dialogue.clip = clip; int ticket = ++generation;
            if (clip.loadState == AudioDataLoadState.Loaded) StartLoaded(entry);
            else loading = StartCoroutine(LoadAndPlay(entry, clip, ticket));
            return true;
        }
        IEnumerator LoadAndPlay(DialogueEntry entry, AudioClip clip, int ticket)
        {
            clip.LoadAudioData(); double deadline = Time.realtimeSinceStartupAsDouble + 10;
            while (clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            loading = null;
            if (generation != ticket) yield break;
            if (clip.loadState != AudioDataLoadState.Loaded)
            { string failed = ActiveClipId; StopDialogue(); PlaybackFailed?.Invoke(failed); yield break; }
            StartLoaded(entry);
        }
        void StartLoaded(DialogueEntry entry)
        {
            PublishSubtitle(entry);
            dialogue.Play(); began = true; SubtitleChanged?.Invoke(Speaker, Subtitle);
        }
        void PublishSubtitle(DialogueEntry entry)
        {
            // An authored mission instruction must never be attributed as a spoken transcript.
            bool useFallback = string.IsNullOrEmpty(entry.subtitle_text) && !string.IsNullOrEmpty(fallbackSubtitle);
            Speaker = useFallback ? fallbackSpeaker : entry.speaker == "Unknown" ? "" : entry.speaker ?? "";
            Subtitle = useFallback ? fallbackSubtitle : entry.subtitle_text ?? "";
        }
        public bool BeginTimeline(string id, UnityEngine.Object owner, string authoredSpeaker, string authoredSubtitle)
        {
            if (owner == null || !Play(id)) return false;
            timelineOwner = owner; fallbackSpeaker = authoredSpeaker; fallbackSubtitle = authoredSubtitle;
            if (began && Database.TryGet(id, out DialogueEntry entry))
            { PublishSubtitle(entry); SubtitleChanged?.Invoke(Speaker, Subtitle); }
            return true;
        }
        public void EvaluateTimeline(UnityEngine.Object owner, double seconds, bool playing)
        {
            if (timelineOwner != owner || dialogue.clip == null || dialogue.clip.loadState != AudioDataLoadState.Loaded) return;
            if (seconds >= dialogue.clip.length) { dialogue.Stop(); return; }
            if (Math.Abs(dialogue.time - seconds) > .12) dialogue.time = Mathf.Clamp((float)seconds, 0, dialogue.clip.length - .001f);
            if (!playing && !timelinePaused) { dialogue.Pause(); timelinePaused = true; }
            else if (playing && timelinePaused) { dialogue.UnPause(); timelinePaused = false; }
        }
        public void EndTimeline(UnityEngine.Object owner) { if (timelineOwner == owner) StopDialogue(); }
        public void StopDialogue()
        {
            generation++;
            if (loading != null) { StopCoroutine(loading); loading = null; }
            if (dialogue != null) dialogue.Stop();
            ActiveClipId = null; began = false; Speaker = Subtitle = "";
            timelineOwner = null; fallbackSpeaker = fallbackSubtitle = null; timelinePaused = false;
            SubtitleChanged?.Invoke("", "");
        }
        public bool PlayEvent(string scene, string trigger, bool includeAuthored = false)
        {
            if (Database == null) return false;
            var matches = Database.FindEvent(scene, trigger, includeAuthored);
            return matches.Count > 0 && Play(matches[0].clip_id);
        }
        public bool PlaySfx(string id, Vector3? position = null)
        {
            if (Database == null || !Database.TryResolve(id, out _, out AudioClip clip)) return false;
            AudioSource source = effects[effectCursor]; effectCursor = (effectCursor + 1) % effects.Length; source.Stop();
            source.spatialBlend = position.HasValue ? 1 : 0;
            if (position.HasValue) source.transform.position = position.Value;
            source.clip = clip; source.Play(); return true;
        }
        public bool PlayMusic(string id)
        {
            if (Database == null || !Database.TryResolve(id, out _, out AudioClip clip)) return false;
            if (music.clip == clip && music.isPlaying) return true;
            music.clip = clip; music.Play(); return true;
        }
        public void StopMusic() { if (music != null) music.Stop(); }
        public void SetVolume(string channel, float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (Mixer != null) Mixer.SetFloat(channel + "Volume", normalized <= .0001f ? -80 : 20 * Mathf.Log10(normalized));
            PlayerPrefs.SetFloat("Audio." + channel, normalized);
        }
        public float GetVolume(string channel) => PlayerPrefs.GetFloat("Audio." + channel, 1);
        void Start()
        { foreach (string bus in new[] { "Master", "Dialogue", "SFX", "Music" }) SetVolume(bus, GetVolume(bus)); }
        void Update()
        {
            float target = IsSpeaking ? .35f : baseMusic;
            music.volume = Mathf.Lerp(music.volume, target, 1 - Mathf.Exp(-5 * Time.unscaledDeltaTime));
            if (began && timelineOwner == null && !AudioListener.pause && Time.timeScale > 0 && !dialogue.isPlaying)
            { string finished = ActiveClipId; StopDialogue(); PlaybackFinished?.Invoke(finished); }
        }
        void OnDestroy()
        { if (Instance == this) { StopDialogue(); Instance = null; } }
    }
}
