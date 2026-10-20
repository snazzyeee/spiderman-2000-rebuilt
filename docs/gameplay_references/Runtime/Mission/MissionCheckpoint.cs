using UnityEngine;

namespace SpiderManRebuild
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MissionCheckpoint : MonoBehaviour
    {
        public MissionManager Mission;
        public int Index;
        public Transform SafeSpawn;
        public string ObjectiveLabel;
        void Awake() => GetComponent<BoxCollider>().isTrigger = true;
        void OnTriggerEnter(Collider other)
        { if (other.GetComponentInParent<PlayerStateMachine>() == Mission.Player) Mission.TryCheckpoint(this); }
        void OnTriggerStay(Collider other)
        { if (other.GetComponentInParent<PlayerStateMachine>() == Mission.Player) Mission.TryCheckpoint(this); }
        public bool ContainsPlayer()
        { return GetComponent<BoxCollider>().bounds.Contains(Mission.Player.transform.position + Vector3.up); }
    }
}
