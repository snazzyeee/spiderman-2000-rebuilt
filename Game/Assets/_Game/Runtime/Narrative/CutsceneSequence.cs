using System;
using UnityEngine;
using UnityEngine.Playables;

namespace SpiderManRebuild
{
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class CutsceneSequence : MonoBehaviour
    {
        public TraversalSession Session;
        public TraversalInput Input;
        public DialoguePresenter Presenter;
        public bool BlocksControl = true, AllowSkip = true;
        public bool Playing { get; private set; }
        public event Action<bool> Finished;
        PlayableDirector director;
        bool aborting, pausedBySession;
        void Awake()
        {
            director = GetComponent<PlayableDirector>(); director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            director.stopped += OnStopped;
        }
        void Start() { if (Session != null) Session.PauseChanged += OnPause; }
        public bool TryPlay()
        {
            if (Playing || director.playableAsset == null || Presenter == null || !Presenter.TryAcquire(this)) return false;
            Playing = true; aborting = false; pausedBySession = false;
            if (BlocksControl && Session != null) Session.SetControlLock(this, true);
            director.time = 0; director.Play(); return true;
        }
        void Update()
        {
            if (Playing && AllowSkip && Input != null && !Input.Automation && Input.SkipPressed
                && (Session == null || !Session.Paused)) Skip();
        }
        public void Skip() { if (Playing) director.Stop(); }
        public void Abort() { if (Playing) { aborting = true; director.Stop(); } }
        void OnPause(bool paused)
        {
            if (!Playing) return;
            if (paused) { director.Pause(); pausedBySession = true; }
            else if (pausedBySession) { pausedBySession = false; director.Play(); }
        }
        void OnStopped(PlayableDirector stopped)
        {
            if (!Playing) return;
            Playing = false;
            Presenter.Release(this);
            if (BlocksControl && Session != null) Session.SetControlLock(this, false);
            Finished?.Invoke(!aborting);
        }
        void OnDisable() => Abort();
        void OnDestroy()
        {
            if (director != null) director.stopped -= OnStopped;
            if (Session != null) { Session.PauseChanged -= OnPause; Session.SetControlLock(this, false); }
        }
    }
}
