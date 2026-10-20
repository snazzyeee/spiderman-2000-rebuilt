using UnityEngine;

namespace SpiderManRebuild
{
    [CreateAssetMenu(menuName = "Rebuild/Mission traversal settings")]
    public sealed class TraversalSettings : ScriptableObject
    {
        public float AnchorRange = 75, AnchorMinimumHeight = 1, RopeDamping = .08f;
        public float SwingGravity = 26, TroughBoost = 1.6f, SwingPump = 18, MaximumSwingSpeed = 38;
        public float ZipSpeed = 36, ZipReleaseSpeed = 9, ZipCooldown = .35f;
        public float CrawlSpeed = 4.5f, WallProbe = 1, WallJumpOutward = 7, WallJumpUpward = 6;
    }
}
