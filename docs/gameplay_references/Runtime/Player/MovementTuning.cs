using UnityEngine;

namespace SpiderManRebuild
{
    [CreateAssetMenu(menuName = "Rebuild/Movement tuning")]
    public sealed class MovementTuning : ScriptableObject
    {
        [Min(0)] public float WalkSpeed = 5.5f;
        [Min(0)] public float RunSpeed = 9f;
        [Min(0)] public float GroundAcceleration = 32f;
        [Min(0)] public float AirAcceleration = 12f;
        [Min(.01f)] public float Gravity = 26f;
        [Min(0)] public float JumpHeight = 2.1f;
        [Min(.01f)] public float TerminalSpeed = 45f;
        [Min(0)] public float CoyoteTime = .12f;
        [Min(0)] public float JumpBufferTime = .14f;
    }
}
