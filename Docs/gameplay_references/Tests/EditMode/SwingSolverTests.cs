using NUnit.Framework;
using UnityEngine;

namespace SpiderManRebuild.Tests
{
    public sealed class SwingSolverTests
    {
        [Test] public void RopeLimitsDistanceAndRemovesOutwardVelocity()
        {
            var tuning = ScriptableObject.CreateInstance<TraversalSettings>();
            Vector3 position = Vector3.down * 10, velocity = new Vector3(20, -8, 4);
            for (int i = 0; i < 120; i++)
            {
                SwingSolver.Integrate(ref position, ref velocity, Vector3.zero, 10, Vector3.right, 1f / 120, tuning);
                Assert.That(position.magnitude, Is.EqualTo(10).Within(.0001f));
                Assert.That(Mathf.Abs(Vector3.Dot(position.normalized, velocity)), Is.LessThan(.0001f));
                Assert.That(float.IsNaN(velocity.sqrMagnitude), Is.False);
            }
            Assert.That(velocity.magnitude, Is.LessThanOrEqualTo(tuning.MaximumSwingSpeed + .001f));
            Object.DestroyImmediate(tuning);
        }
        [Test] public void SwingRestoresFixedLengthAndPreservesTangentialMotion()
        {
            var tuning = ScriptableObject.CreateInstance<TraversalSettings>();
            Vector3 position = Vector3.down * 5, velocity = Vector3.right * 8;
            SwingSolver.Integrate(ref position, ref velocity, Vector3.zero, 10, Vector3.zero, 1f / 120, tuning);
            Assert.That(position.magnitude, Is.EqualTo(10).Within(.0001f));
            Assert.That(velocity.x, Is.GreaterThan(7.9f));
            Object.DestroyImmediate(tuning);
        }
    }
}
