using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;

namespace SpiderManRebuild
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class DialoguePresenter : MonoBehaviour
    {
        public AudioMixerGroup DialogueBus;
        public bool Claimed => owner != null;
        public event Action<string, string> SubtitleChanged;
        public string Speaker { get; private set; }
        public string Subtitle { get; private set; }
        UnityEngine.Object owner;
        AudioSource source;
        DialogueCueClip current;
        CinemachineVirtualCameraBase camera;
        PrioritySettings previousPriority;
        bool audioPaused;
        void Awake()
        {
            source = GetComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0;
            source.outputAudioMixerGroup = DialogueBus;
        }
        public bool TryAcquire(UnityEngine.Object claimant)
        {
            if (owner != null && owner != claimant) return false;
            owner = claimant; return true;
        }
        public void Release(UnityEngine.Object claimant)
        { if (owner != claimant) return; ClearCue(); owner = null; }
        public void Evaluate(DialogueCueClip cue, CinemachineVirtualCameraBase shot, double time, bool playing)
        {
            if (!Claimed) return;
            bool changed = current != cue;
            if (changed)
            {
                ClearCue(); current = cue; Speaker = cue.Speaker; Subtitle = cue.Subtitle;
                SubtitleChanged?.Invoke(Speaker, Subtitle);
                camera = shot;
                if (camera != null) { previousPriority = camera.Priority; camera.Priority = 100; }
                source.clip = cue.Audio;
                if (source.clip != null && time < source.clip.length)
                { source.time = Mathf.Clamp((float)time, 0, source.clip.length - .001f); source.Play(); }
            }
            if (source.clip == null) return;
            if (!playing && !audioPaused) { source.Pause(); audioPaused = true; }
            else if (playing && audioPaused) { source.UnPause(); audioPaused = false; }
            if (playing && source.isPlaying && Math.Abs(source.time - time) > .12 && time < source.clip.length)
                source.time = (float)time;
        }
        public void ClearCue()
        {
            if (camera != null) { camera.Priority = previousPriority; camera = null; }
            source?.Stop(); current = null; audioPaused = false;
            if (!string.IsNullOrEmpty(Subtitle))
            { Speaker = Subtitle = ""; SubtitleChanged?.Invoke("", ""); }
        }
        void OnDisable() { ClearCue(); owner = null; }
    }
}
