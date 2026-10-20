using UnityEngine;

namespace SpiderManRebuild
{
    public readonly struct WebAnchor
    {
        public readonly Collider Surface;
        readonly Vector3 localPoint, localNormal;
        public WebAnchor(RaycastHit hit)
        {
            Surface = hit.collider;
            localPoint = Surface.transform.InverseTransformPoint(hit.point);
            localNormal = Surface.transform.InverseTransformDirection(hit.normal);
        }
        public bool Valid => Surface != null && Surface.enabled && Surface.gameObject.activeInHierarchy;
        public Vector3 Point => Surface.transform.TransformPoint(localPoint);
        public Vector3 Normal => Surface.transform.TransformDirection(localNormal).normalized;
    }
    public sealed class WebAnchorSelector : MonoBehaviour
    {
        public Camera AimCamera;
        public LayerMask Geometry = 1;
        public TraversalSettings Settings;
        public SpiderManCharacterController Motor;
        public bool TrySelect(bool requireAbove, out WebAnchor anchor)
        {
            anchor = default;
            if (AimCamera == null || Settings == null) return false;
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            float best = float.NegativeInfinity;
            // Exact aim first, then a deterministic upward fan against real building geometry.
            for (int i = -1; i < 30; i++)
            {
                Vector3 direction = i < 0 ? AimCamera.transform.forward :
                    Quaternion.AngleAxis((i % 5 - 2) * 14, Vector3.up) *
                    Quaternion.AngleAxis(-(12 + i / 5 * 11), AimCamera.transform.right) *
                    Vector3.ProjectOnPlane(AimCamera.transform.forward, Vector3.up).normalized;
                Vector3 rayOrigin = i < 0 ? AimCamera.transform.position : origin;
                if (!Physics.Raycast(rayOrigin, direction, out var hit, Settings.AnchorRange, Geometry, QueryTriggerInteraction.Ignore)) continue;
                if (Vector3.Distance(origin, hit.point) < 2 || (requireAbove && hit.point.y < origin.y + Settings.AnchorMinimumHeight)) continue;
                if (Motor != null && Motor.ArenaLocked && !InsideXZ(Motor.ArenaBounds, hit.point)) continue;
                Vector3 delta = hit.point - origin;
                if (Physics.Raycast(origin, delta.normalized, out var obstruction, delta.magnitude - .15f, Geometry,
                    QueryTriggerInteraction.Ignore) && Vector3.Distance(obstruction.point, hit.point) > .4f) continue;
                float value = Vector3.Dot(AimCamera.transform.forward, delta.normalized) * 4 - delta.magnitude * .012f + (i < 0 ? 10 : 0);
                if (value <= best) continue;
                best = value; anchor = new WebAnchor(hit);
            }
            return best > float.NegativeInfinity;
        }
        static bool InsideXZ(Bounds b, Vector3 p) => p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z;
    }
}
