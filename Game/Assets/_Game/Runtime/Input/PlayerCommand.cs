using UnityEngine;

namespace SpiderManRebuild
{
    public struct PlayerCommand
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool JumpPressed;
        public bool SprintHeld;
        public bool PausePressed;
        public bool ResetPressed;
    }
}
