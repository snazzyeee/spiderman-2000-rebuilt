using UnityEngine;

namespace SpiderManRebuild
{
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public sealed class SurfaceSteeringMotor : MonoBehaviour
    {
        public LayerMask World = 1;
        public Bounds WalkArea;
        public bool LimitToArea;
        readonly RaycastHit[] hits = new RaycastHit[24];
        BoxCollider body;
        void Awake()
        {
            body = GetComponent<BoxCollider>(); body.size = new Vector3(.6f, 1.8f, .5f); body.center = Vector3.up * .9f;
            var rigid = GetComponent<Rigidbody>(); rigid.isKinematic = true; rigid.useGravity = false;
        }
        public bool MoveToward(Vector3 destination, float speed, float dt)
        {
            Vector3 desired = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);
            if (desired.magnitude < .1f) return false;
            Vector3 best = Vector3.zero; float score = float.NegativeInfinity;
            for (int i = 0; i < 9; i++)
            {
                float angle = i == 0 ? 0 : ((i + 1) / 2) * 25 * (i % 2 == 0 ? -1 : 1);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * desired.normalized;
                Vector3 next = transform.position + direction * Mathf.Min(speed * dt, desired.magnitude);
                if (LimitToArea && (next.x < WalkArea.min.x || next.x > WalkArea.max.x || next.z < WalkArea.min.z || next.z > WalkArea.max.z)) continue;
                if (!Physics.Raycast(next + Vector3.up * .5f, Vector3.down, out var ground, .95f, World, QueryTriggerInteraction.Ignore)
                    || ground.normal.y < .65f || Mathf.Abs(ground.point.y - transform.position.y) > .32f) continue;
                int count = Physics.BoxCastNonAlloc(transform.position + body.center, body.size * .5f - Vector3.one * .025f,
                    direction, hits, Quaternion.identity, speed * dt + .08f, World | (1 << 8) | (1 << 2), QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int j = 0; j < count; j++) if (hits[j].collider != body) { blocked = true; break; }
                if (blocked) continue;
                float candidate = Vector3.Dot(direction, desired.normalized);
                if (candidate > score) { score = candidate; best = new Vector3(next.x, ground.point.y + .02f, next.z); }
            }
            if (float.IsNegativeInfinity(score)) return false;
            Vector3 delta = best - transform.position;
            transform.position = best;
            if (delta.sqrMagnitude > .00001f) transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(new Vector3(delta.x, 0, delta.z)), 1 - Mathf.Exp(-12 * dt));
            Physics.SyncTransforms(); return true;
        }
    }
}
