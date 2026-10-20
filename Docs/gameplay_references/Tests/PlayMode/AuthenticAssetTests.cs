using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpiderManRebuild.Tests
{
    public sealed class AuthenticAssetTests
    {
        [UnityTest]
        public IEnumerator ImportedRigAndOrbitCollisionAreValid()
        {
            const string path = "Assets/_LocalReference/Phase1/SpiderManPhase1.unity";
            if (!File.Exists(path)) Assert.Ignore("Private PC assets are not present. Run prepare_phase1_assets.py and assemble Phase 1.");
            yield return SceneManager.LoadSceneAsync(path);
            var motor = Object.FindAnyObjectByType<SpiderManCharacterController>();
            motor.Automation = true; motor.GetComponent<TraversalInput>().Automation = true;
            motor.Session.SetPaused(false);
            var skin = motor.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.That(skin.bones.Length, Is.EqualTo(18));
            foreach (var material in skin.sharedMaterials)
            {
                Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                Assert.That(material.mainTexture, Is.Not.Null);
            }
            Assert.That(Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None), Is.Empty);
            yield return null; yield return null;
            var follow = motor.Orbit.Camera.GetComponent<CinemachineThirdPersonFollow>();
            Vector3 target = motor.Orbit.transform.position;
            float clear = Vector3.Distance(UnityEngine.Camera.main.transform.position, target);
            var wall = new GameObject("Temporary camera test collider");
            wall.transform.position = target + new Vector3(0, .3f, -1.5f);
            wall.AddComponent<BoxCollider>().size = new Vector3(8, 6, .2f);
            Physics.SyncTransforms();
            yield return null; yield return null; yield return null;
            Assert.That(follow.CurrentObstacle, Is.Not.Null);
            Assert.That(Vector3.Distance(UnityEngine.Camera.main.transform.position, target), Is.LessThan(clear - .5f));
            Object.Destroy(wall);
            var empty = SceneManager.CreateScene("AfterAuthenticAssetTest");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(path));
        }
    }
}
