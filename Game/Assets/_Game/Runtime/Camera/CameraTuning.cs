using UnityEngine;

namespace SpiderManRebuild
{
    [CreateAssetMenu(menuName = "Rebuild/Camera tuning")]
    public sealed class CameraTuning : ScriptableObject
    {
        public float MouseDegreesPerPixel = .12f;
        public float MinPitch = -30;
        public float MaxPitch = 65;
        public float StartPitch = 15;
        public float TargetHeight = 1.45f;
    }
}
