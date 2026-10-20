using UnityEngine;

namespace SpiderManRebuild
{
    public sealed class SkylightInteraction : MonoBehaviour
    {
        public MissionManager Mission;
        public float Range = 2;
        public bool Available => Mission != null && Mission.Stage == MissionStage.Skylight;
        public bool InRange => Available && Vector3.Distance(Mission.Player.transform.position, transform.position) <= Range;
        public bool TryInteract()
        {
            if (!InRange || Mission.Player.Health.Defeated || Mission.Player.Motor.Session.ControlBlocked) return false;
            return Mission.CompleteAtSkylight(this);
        }
    }
}
