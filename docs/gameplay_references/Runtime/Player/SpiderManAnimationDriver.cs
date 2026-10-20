using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SpiderManRebuild
{
    [RequireComponent(typeof(Animator))]
    public sealed class SpiderManAnimationDriver : MonoBehaviour
    {
        public SpiderManCharacterController Motor;
        [Tooltip("Assign only after identifying the original animation slots; no guessed mapping is shipped.")]
        public AnimationClip Idle, Walk, Run, Rising, Falling;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        readonly AnimationClipPlayable[] clips = new AnimationClipPlayable[5];
        readonly bool[] present = new bool[5];
        readonly float[] weights = new float[5];
        void Start()
        {
            var animator = GetComponent<Animator>(); animator.applyRootMotion = false;
            graph = PlayableGraph.Create("Original Spider-Man animations");
            mixer = AnimationMixerPlayable.Create(graph, 5);
            var output = AnimationPlayableOutput.Create(graph, "Character", animator); output.SetSourcePlayable(mixer);
            AnimationClip[] sources = { Idle, Walk, Run, Rising, Falling };
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null) continue;
                clips[i] = AnimationClipPlayable.Create(graph, sources[i]);
                clips[i].SetApplyFootIK(false);
                graph.Connect(clips[i], 0, mixer, i); present[i] = true;
            }
            graph.Play();
        }
        void Update()
        {
            if (Motor == null || !graph.IsValid()) return;
            float planar = new Vector2(Motor.Velocity.x, Motor.Velocity.z).magnitude;
            int target = !Motor.Grounded ? (Motor.Velocity.y > 0 ? 3 : 4) : planar < .1f ? 0 :
                planar > Motor.Tuning.WalkSpeed + .5f ? 2 : 1;
            if (!present[target]) return;
            for (int i = 0; i < clips.Length; i++)
            {
                weights[i] = Mathf.MoveTowards(weights[i], i == target ? 1 : 0, Time.deltaTime * 9);
                mixer.SetInputWeight(i, weights[i]);
            }
        }
        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}
