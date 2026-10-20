using UnityEngine;

namespace SpiderManRebuild
{
    public static class SwingSolver
    {
        public static void Integrate(ref Vector3 chest, ref Vector3 velocity, Vector3 anchor,
            float length, Vector3 steering, float dt, TraversalSettings tuning)
        {
            Vector3 radial = (chest - anchor).normalized;
            float trough = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(radial, Vector3.down)), 4);
            float boost = velocity.y < 0 ? tuning.TroughBoost * trough : 0;
            velocity += (Vector3.down * tuning.SwingGravity * (1 + boost)
                + Vector3.ProjectOnPlane(steering, radial) * tuning.SwingPump) * dt;
            velocity *= Mathf.Exp(-tuning.RopeDamping * dt);
            velocity = Vector3.ClampMagnitude(velocity, tuning.MaximumSwingSpeed);
            Vector3 predicted = chest + velocity * dt;
            Vector3 offset = predicted - anchor;
            if (offset.magnitude > length)
            {
                Vector3 normal = offset.normalized;
                predicted = anchor + normal * length;
                velocity -= normal * Mathf.Max(0, Vector3.Dot(velocity, normal));
            }
            chest = predicted;
        }
    }
}
