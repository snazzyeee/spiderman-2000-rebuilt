using UnityEngine;
namespace SpiderManRebuild
{
    public sealed class LabGoal : MonoBehaviour
    {
        [SerializeField] GameSession session;
        public void Configure(GameSession game) => session = game;
        void OnTriggerEnter(Collider other)
        { if (other.GetComponent<CharacterMotor>() != null) session.ReachGoal(); }
    }
}
