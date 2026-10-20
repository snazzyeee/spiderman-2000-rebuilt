using UnityEngine;

namespace SpiderManRebuild
{
    [CreateAssetMenu(menuName = "Rebuild/Mission 1 layout")]
    public sealed class MissionLayout : ScriptableObject
    {
        public Vector3 StartingRoof;
        public Vector3[] CheckpointRoofs;
        public Bounds BankArena;
        public Vector3 Skylight;
    }
}
