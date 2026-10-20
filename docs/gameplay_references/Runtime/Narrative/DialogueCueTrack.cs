using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SpiderManRebuild
{
    [TrackClipType(typeof(DialogueCueClip)), TrackBindingType(typeof(DialoguePresenter))]
    public sealed class DialogueCueTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
            => ScriptPlayable<DialogueCueMixer>.Create(graph, inputCount);
    }
    public sealed class DialogueCueMixer : PlayableBehaviour
    {
        DialoguePresenter presenter;
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!Application.isPlaying) return;
            presenter = playerData as DialoguePresenter;
            if (presenter == null || !presenter.Claimed) return;
            int selected = -1; float best = 0;
            for (int i = 0; i < playable.GetInputCount(); i++)
                if (playable.GetInputWeight(i) > best) { selected = i; best = playable.GetInputWeight(i); }
            if (selected < 0) { presenter.ClearCue(); return; }
            var source = (ScriptPlayable<DialogueCuePlayable>)playable.GetInput(selected);
            var cue = source.GetBehaviour();
            presenter.Evaluate(cue.Cue, cue.Camera, source.GetTime(), info.effectivePlayState == PlayState.Playing);
        }
    }
}
