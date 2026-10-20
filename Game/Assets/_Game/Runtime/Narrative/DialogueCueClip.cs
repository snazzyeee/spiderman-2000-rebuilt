using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SpiderManRebuild
{
    public sealed class DialogueCueClip : PlayableAsset, ITimelineClipAsset
    {
        public string Speaker;
        [TextArea] public string Subtitle;
        public AudioClip Audio;
        public ExposedReference<CinemachineVirtualCameraBase> ShotCamera;
        public ClipCaps clipCaps => ClipCaps.None;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<DialogueCuePlayable>.Create(graph);
            playable.GetBehaviour().Cue = this;
            playable.GetBehaviour().Camera = ShotCamera.Resolve(graph.GetResolver());
            return playable;
        }
    }
    public sealed class DialogueCuePlayable : PlayableBehaviour
    {
        public DialogueCueClip Cue;
        public CinemachineVirtualCameraBase Camera;
    }
}
