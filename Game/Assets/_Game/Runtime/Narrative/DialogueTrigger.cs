using System.Collections.Generic;
using UnityEngine;

namespace SpiderManRebuild
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class DialogueTrigger : MonoBehaviour
    {
        public CutsceneSequence Sequence;
        public bool Once = true;
        readonly HashSet<Collider> occupants = new HashSet<Collider>();
        bool played;
        void Awake() => GetComponent<BoxCollider>().isTrigger = true;
        void OnTriggerEnter(Collider other)
        { if (other.GetComponentInParent<PlayerStateMachine>() != null) occupants.Add(other); }
        void OnTriggerExit(Collider other) => occupants.Remove(other);
        void Update()
        {
            if (occupants.Count == 0 || (Once && played) || Sequence == null || Sequence.Playing) return;
            if (Sequence.TryPlay()) played = true;
        }
        public void ResetTrigger() => played = false;
        void OnDisable() => occupants.Clear();
    }
}
