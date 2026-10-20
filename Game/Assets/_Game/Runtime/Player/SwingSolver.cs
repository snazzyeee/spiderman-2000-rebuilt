using UnityEngine;

namespace SpiderManRebuild
{
    public static class SwingSolver
    {
        public static void Integrate(ref Vector3 chest, ref Vector3 velocity, Vector3 anchor,
            float length, Vector3 steering, float dt, TraversalSettings tuning)
        {
            if (dt <= 0) return;
            if (length <= 0 || tuning == null) throw new System.ArgumentOutOfRangeException(nameof(length));
            Vector3 offset = chest - anchor;
            Vector3 radial = offset.sqrMagnitude > .000001f ? offset.normalized : Vector3.down;
            chest = anchor + radial * length;
            velocity = Vector3.ProjectOnPlane(velocity, radial);
            float trough = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(radial, Vector3.down)), 4);
            float boost = velocity.y < 0 ? tuning.TroughBoost * trough : 0;
            velocity += (Vector3.down * tuning.SwingGravity * (1 + boost)
                + Vector3.ProjectOnPlane(steering, radial) * tuning.SwingPump) * dt;
            velocity *= Mathf.Exp(-tuning.RopeDamping * dt);
            velocity = Vector3.ClampMagnitude(velocity, tuning.MaximumSwingSpeed);
            Vector3 predicted = chest + velocity * dt;
            Vector3 normal = (predicted - anchor).normalized;
            chest = anchor + normal * length;
            velocity = Vector3.ProjectOnPlane(velocity, normal);
        }
    }
}
