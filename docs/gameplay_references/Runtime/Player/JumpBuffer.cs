using System;

namespace SpiderManRebuild
{
    public sealed class JumpBuffer
    {
        double expiresAt = double.NegativeInfinity;
        public void Press(double now, double duration) => expiresAt = now + Math.Max(0, duration);
        public bool TryConsume(double now)
        {
            if (now > expiresAt) return false;
            Clear();
            return true;
        }
        public void Clear() => expiresAt = double.NegativeInfinity;
    }
}
