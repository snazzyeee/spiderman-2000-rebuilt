using UnityEngine;
namespace SpiderManRebuild
{
    public sealed class KillVolume : MonoBehaviour
    {
        [SerializeField] RespawnController respawn;
        public void Configure(RespawnController controller) => respawn = controller;
        void OnTriggerEnter(Collider other)
        { if (other.GetComponent<CharacterMotor>() != null) respawn.ResetPlayer(); }
    }
}
