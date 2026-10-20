using NUnit.Framework;
using UnityEngine;

namespace SpiderManRebuild.Tests
{
    public sealed class TraversalStateTests
    {
        GameObject root, floor, surface;
        MovementTuning movement;
        TraversalSettings settings;
        PlayerStateMachine player;
        [SetUp] public void Setup()
        {
            floor = new GameObject("Physics test floor"); floor.transform.position = Vector3.down * .5f;
            floor.AddComponent<BoxCollider>().size = new Vector3(30, 1, 30);
            root = new GameObject("Traversal state test"); root.SetActive(false);
            movement = ScriptableObject.CreateInstance<MovementTuning>(); settings = ScriptableObject.CreateInstance<TraversalSettings>();
            player = root.AddComponent<PlayerStateMachine>(); player.Settings = settings; player.Automation = true;
            root.GetComponent<SpiderManCharacterController>().Tuning = movement;
            var selector = root.AddComponent<WebAnchorSelector>(); selector.Settings = settings;
            player.Anchors = selector; root.SetActive(true);
            Physics.SyncTransforms();
        }
        [Test] public void SwingReleaseRetainsWorldVelocity()
        {
            root.transform.position = Vector3.up * 3;
            surface = new GameObject("Physical web anchor"); surface.transform.position = new Vector3(0, 12, 5);
            surface.AddComponent<BoxCollider>().size = Vector3.one; Physics.SyncTransforms();
            Vector3 direction = surface.transform.position - root.transform.position;
            Assert.That(Physics.Raycast(root.transform.position, direction.normalized, out var hit, 20, 1), Is.True);
            player.Motor.SetVelocity(new Vector3(5, 1, 2));
            Assert.That(player.BeginSwing(new WebAnchor(hit)), Is.True);
            for (int i = 0; i < 12; i++) player.Tick(new MissionInputCommand { Swing = true }, 1f / 120, i / 120f);
            Vector3 release = player.Motor.Velocity;
            Assert.That(release.magnitude, Is.GreaterThan(1));
            player.Release();
            Assert.That(player.State, Is.EqualTo(PlayerState.Airborne));
            Assert.That(Vector3.Distance(release, player.Motor.Velocity), Is.LessThan(.001f));
        }
        [Test] public void SwingStaysKinematicAndTautThenReleasesADisabledAnchor()
        {
            root.transform.position = Vector3.up * 5;
            surface = new GameObject("Raycast pendulum anchor"); surface.transform.position = new Vector3(0, 15, 5);
            var collider = surface.AddComponent<BoxCollider>(); collider.size = Vector3.one;
            Physics.SyncTransforms();
            Vector3 chest = root.transform.position + Vector3.up * 1.1f;
            Assert.That(Physics.Raycast(chest, (surface.transform.position - chest).normalized, out var hit, 30, 1), Is.True);
            player.Motor.SetVelocity(Vector3.right * 8);
            Assert.That(player.BeginSwing(new WebAnchor(hit)), Is.True);
            for (int frame = 0; frame < 120; frame++)
            {
                player.Tick(new MissionInputCommand { Swing = true }, 1f / 120, frame / 120f);
                Assert.That(player.State, Is.EqualTo(PlayerState.Swinging));
                Vector3 radial = root.transform.position + Vector3.up * 1.1f - player.SwingAnchorPoint;
                Assert.That(radial.magnitude, Is.EqualTo(player.SwingRopeLength).Within(.003f));
                Assert.That(Mathf.Abs(Vector3.Dot(radial.normalized, player.Motor.Velocity)), Is.LessThan(.003f));
            }
            Assert.That(root.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(root.GetComponent<Rigidbody>().useGravity, Is.False);
            collider.enabled = false;
            player.Tick(new MissionInputCommand { Swing = true }, 1f / 120, 2);
            Assert.That(player.State, Is.EqualTo(PlayerState.Airborne));
        }
        [Test] public void CollisionReleasesSwingWithoutPullingThroughAWall()
        {
            root.transform.position = Vector3.up * 5;
            surface = new GameObject("Collision test anchor"); surface.transform.position = new Vector3(0, 15, 5);
            surface.AddComponent<BoxCollider>(); Physics.SyncTransforms();
            Vector3 chest = root.transform.position + Vector3.up * 1.1f;
            Assert.That(Physics.Raycast(chest, (surface.transform.position - chest).normalized, out var hit, 30, 1), Is.True);
            player.Motor.SetVelocity(Vector3.right * 8);
            Assert.That(player.BeginSwing(new WebAnchor(hit)), Is.True);
            var barrier = new GameObject("Blocking collision wall");
            try
            {
                barrier.transform.position = new Vector3(.9f, 10, 0);
                barrier.AddComponent<BoxCollider>().size = new Vector3(.15f, 20, 20); Physics.SyncTransforms();
                for (int frame = 0; frame < 60 && player.State == PlayerState.Swinging; frame++)
                    player.Tick(new MissionInputCommand { Swing = true }, 1f / 120, frame / 120f);
                Assert.That(player.State, Is.EqualTo(PlayerState.Airborne));
                Assert.That(root.transform.position.x, Is.LessThan(.7f));
            }
            finally { Object.DestroyImmediate(barrier); }
        }
        [Test] public void SwingRejectsDefaultAndLowAnchors()
        {
            Assert.That(player.BeginSwing(default), Is.False);
            surface = new GameObject("Low geometry"); surface.transform.position = new Vector3(0, .5f, 6);
            surface.AddComponent<BoxCollider>(); Physics.SyncTransforms();
            Vector3 chest = root.transform.position + Vector3.up * 1.1f;
            Assert.That(Physics.Raycast(chest, (surface.transform.position - chest).normalized, out var hit, 10, 1), Is.True);
            Assert.That(player.BeginSwing(new WebAnchor(hit)), Is.False);
        }
        [Test] public void WallCrawlSlidesUpAndJumpDetachesOutward()
        {
            root.transform.position = Vector3.up * .04f;
            surface = new GameObject("Physical test wall"); surface.transform.position = new Vector3(0, 4, .85f);
            surface.AddComponent<BoxCollider>().size = new Vector3(12, 8, .2f); Physics.SyncTransforms();
            for (int i = 0; i < 30; i++)
                player.Tick(new MissionInputCommand { Crawl = true, Move = Vector2.up }, 1f / 60, i / 60f);
            Assert.That(player.State, Is.EqualTo(PlayerState.Crawling));
            Assert.That(root.transform.position.y, Is.GreaterThan(1));
            player.Tick(new MissionInputCommand { Crawl = true, Jump = true }, 1f / 60, .6f);
            Assert.That(player.State, Is.EqualTo(PlayerState.Airborne));
            Assert.That(player.Motor.Velocity.z, Is.LessThan(-1));
            Assert.That(player.Motor.Velocity.y, Is.GreaterThan(0));
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(root); Object.DestroyImmediate(floor);
            if (surface != null) Object.DestroyImmediate(surface);
            Object.DestroyImmediate(movement); Object.DestroyImmediate(settings);
        }
        [Test] public void WallCrawlMantlesRoofLipWithoutTeleportingThroughTheWall()
        {
            root.transform.position = Vector3.up * .04f;
            surface = new GameObject("Test building"); surface.transform.position = new Vector3(0, 1, 1.9f);
            surface.AddComponent<BoxCollider>().size = new Vector3(8, 2, 3); Physics.SyncTransforms();
            for (int i = 0; i < 65; i++)
            {
                player.Tick(new MissionInputCommand { Crawl = true, Move = Vector2.up }, 1f / 60, i / 60f);
                if (player.State == PlayerState.Grounded && root.transform.position.y > 1.9f) break;
            }
            Assert.That(root.transform.position.y, Is.GreaterThan(1.9f));
            Assert.That(root.transform.position.z, Is.GreaterThan(.5f));
            Assert.That(player.State, Is.EqualTo(PlayerState.Grounded));
        }
    }
}
