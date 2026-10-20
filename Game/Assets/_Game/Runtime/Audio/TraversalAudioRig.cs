using UnityEngine;
using UnityEngine.Audio;

namespace SpiderManRebuild
{
    public sealed class TraversalAudioRig : MonoBehaviour
    {
        public AudioMixerGroup Sfx, Music, Dialogue;
        public SpiderManCharacterController Player;
        public float AltitudeReference, AltitudeRange = 60, FilterSpeed = 18;
        public AudioClip WebImpact;
        AudioSource musicSource, dialogueSource;
        AudioSource[] impacts;
        AudioLowPassFilter[] filters;
        int cursor;
        float cutoff = 22000;
        public float CutoffHz => cutoff;
        void Awake()
        {
            impacts = new AudioSource[12]; filters = new AudioLowPassFilter[12];
            for (int i = 0; i < impacts.Length; i++)
            {
                var emitter = new GameObject("Web Impact " + i); emitter.transform.SetParent(transform, false);
                var source = emitter.AddComponent<AudioSource>();
                source.playOnAwake = false; source.spatialBlend = 1; source.dopplerLevel = 0;
                source.minDistance = 1; source.maxDistance = 35; source.outputAudioMixerGroup = Sfx;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
                    new AnimationCurve(new Keyframe(0, 1), new Keyframe(.08f, .65f),
                        new Keyframe(.35f, .15f), new Keyframe(1, 0)));
                impacts[i] = source; filters[i] = emitter.AddComponent<AudioLowPassFilter>();
                filters[i].lowpassResonanceQ = 1;
            }
            musicSource = Bus("Music", Music); dialogueSource = Bus("Dialogue", Dialogue);
            musicSource.loop = true;
        }
        AudioSource Bus(string name, AudioMixerGroup bus)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = bus; source.spatialBlend = 0; source.playOnAwake = false;
            return source;
        }
        void Update()
        {
            if (Player == null) return;
            float altitude = Mathf.Clamp01((Player.transform.position.y - AltitudeReference) / Mathf.Max(1, AltitudeRange));
            float speed = Mathf.Clamp01(Player.Velocity.magnitude / Mathf.Max(1, FilterSpeed));
            float desired = Mathf.Exp(Mathf.Lerp(Mathf.Log(22000), Mathf.Log(1800), altitude * .6f + speed * .4f));
            cutoff = Mathf.Lerp(cutoff, desired, 1 - Mathf.Exp(-6 * Time.deltaTime));
            for (int i = 0; i < filters.Length; i++) filters[i].cutoffFrequency = cutoff;
        }
        public void PlayWebImpact(Vector3 position)
        {
            if (WebImpact == null) return;
            AudioSource source = impacts[cursor]; cursor = (cursor + 1) % impacts.Length;
            source.transform.position = position; source.clip = WebImpact; source.Play();
        }
        public void PlayMusic(AudioClip clip) { musicSource.clip = clip; musicSource.Play(); }
        // Stan Lee narration routes to the separate, intelligible 2D dialogue bus.
        public void PlayDialogue(AudioClip clip) { dialogueSource.clip = clip; dialogueSource.Play(); }
    }
}
