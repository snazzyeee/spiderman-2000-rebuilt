using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpiderManRebuild.Tests
{
    public sealed class MissionIntegrationTests
    {
        MissionManager mission;
        const string ScenePath = "Assets/_LocalReference/Mission1/BankHeistRooftops.unity";
        [UnitySetUp] public IEnumerator Load()
        {
            if (!File.Exists(ScenePath)) Assert.Ignore("Private mission assets are required.");
            yield return SceneManager.LoadSceneAsync(ScenePath);
            mission = Object.FindAnyObjectByType<MissionManager>();
            mission.Player.Automation = true; mission.Player.GetComponent<TraversalInput>().Automation = true;
            mission.Player.Motor.Session.SetPaused(false); yield return null;
        }
        [UnityTest] public IEnumerator IntroPauseSkipRestoresControlAndSubtitle()
        {
            Assert.That(mission.Stage, Is.EqualTo(MissionStage.Intro));
            yield return null; yield return null;
            Assert.That(mission.Intro.Presenter.Subtitle, Is.Empty);
            mission.Player.Motor.Session.SetPaused(true);
            yield return new WaitForSecondsRealtime(.05f);
            mission.Player.Motor.Session.SetPaused(false);
            Assert.That(mission.Player.Motor.Session.ControlBlocked, Is.True);
            mission.Intro.Skip(); yield return null;
            Assert.That(mission.Lifecycle, Is.EqualTo(MissionLifecycle.MissionBriefing));
            Assert.That(mission.Player.Motor.Session.ControlBlocked, Is.True);
            Assert.That(mission.Intro.Presenter.Subtitle, Is.EqualTo(mission.BriefingText));
            mission.CompleteBriefing(); yield return null;
            Assert.That(mission.Stage, Is.EqualTo(MissionStage.RooftopRoute));
            Assert.That(mission.Player.Motor.Session.ControlBlocked, Is.False);
            Assert.That(mission.Intro.Presenter.Subtitle, Is.Empty);
        }
        IEnumerator ReachBank()
        {
            mission.Intro.Skip();
            mission.CompleteBriefing();
            mission.Player.Health.InvulnerableUntil = Time.time + 20;
            foreach (var checkpoint in mission.Checkpoints)
            {
                mission.Player.Motor.SpawnPosition = checkpoint.SafeSpawn.position;
                mission.Player.Motor.ResetPlayer();
                Assert.That(mission.TryCheckpoint(checkpoint), Is.True);
                yield return null;
            }
            Assert.That(mission.Stage, Is.EqualTo(MissionStage.BankEncounter));
        }
        [UnityTest] public IEnumerator OrderedObjectivesAndEnemyDefeatGateSkylight()
        {
            mission.Intro.Skip();
            mission.CompleteBriefing();
            Assert.That(mission.TryCheckpoint(mission.Checkpoints[mission.Checkpoints.Length - 1]), Is.False);
            yield return ReachBank();
            Assert.That(mission.Player.Motor.ArenaLocked, Is.True);
            Assert.That(mission.Skylight.TryInteract(), Is.False);
            foreach (var enemy in mission.Encounter.Enemies) enemy.ApplyWeb(3, 3);
            Assert.That(mission.Encounter.Remaining, Is.Zero);
            Assert.That(mission.Stage, Is.EqualTo(MissionStage.Skylight));
            Assert.That(mission.Player.Motor.ArenaLocked, Is.False);
            mission.Player.Motor.SpawnPosition = mission.Skylight.transform.position;
            mission.Player.Motor.ResetPlayer();
            Assert.That(mission.Skylight.TryInteract(), Is.True);
            Assert.That(mission.Stage, Is.EqualTo(MissionStage.Complete));
        }
        [UnityTest] public IEnumerator NaturalIntroCompletionStartsBriefingOnceAndKeepsInputsBlocked()
        {
            Assert.That(mission.Lifecycle, Is.EqualTo(MissionLifecycle.IntroCinematic));
            int completed = 0; mission.Intro.OnIntroFinished += () => completed++;
            Vector3 before = mission.Player.transform.position;
            mission.Player.Tick(new MissionInputCommand { Move = Vector2.one, Jump = true, Web = true }, .1f, Time.time);
            Assert.That(mission.Player.transform.position, Is.EqualTo(before));
            float energy = mission.Player.Combat.WebEnergy;
            Assert.That(mission.Player.Combat.FireWebAt(before + Vector3.forward * 5, Time.time), Is.False);
            Assert.That(mission.Player.Combat.WebDome(Time.time), Is.False);
            Assert.That(mission.Player.Combat.WebEnergy, Is.EqualTo(energy));
            Assert.That(Physics.Raycast(before + Vector3.up * 2, Vector3.down, out var roof, 4, 1), Is.True);
            Assert.That(mission.Player.BeginZip(new WebAnchor(roof)), Is.False);
            if (DialogueManager.Instance != null && DialogueManager.Instance.ActiveClipId != null)
                Assert.That(DialogueManager.Instance.Play(DialogueManager.Instance.ActiveClipId), Is.False);
            var director = mission.Intro.GetComponent<UnityEngine.Playables.PlayableDirector>();
            director.time = director.duration - .01; director.Evaluate();
            for (int frame = 0; frame < 120 && mission.Lifecycle == MissionLifecycle.IntroCinematic; frame++) yield return null;
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(mission.Lifecycle, Is.EqualTo(MissionLifecycle.MissionBriefing));
            mission.Intro.Skip(); Assert.That(completed, Is.EqualTo(1));
            mission.Player.Tick(new MissionInputCommand { Move = Vector2.one, Jump = true }, .1f, Time.time);
            Assert.That(mission.Player.transform.position, Is.EqualTo(before));
            mission.BriefingSeconds = .25f;
            yield return new WaitForSeconds(.3f);
            Assert.That(mission.Lifecycle, Is.EqualTo(MissionLifecycle.GameplayActive));
            Assert.That(mission.Intro.Presenter.Subtitle, Is.Empty);
        }
        [UnityTest] public IEnumerator AbortingIntroCannotStartBriefing()
        {
            mission.Intro.Abort(); yield return null;
            Assert.That(mission.Lifecycle, Is.EqualTo(MissionLifecycle.IntroCinematic));
            Assert.That(mission.Intro.Presenter.Subtitle, Is.Empty);
            Assert.That(mission.Player.Motor.Session.ControlBlocked, Is.True);
        }
        [UnityTest] public IEnumerator DeathDuringEncounterRestoresApproachAndEnemies()
        {
            yield return ReachBank();
            mission.Player.Health.InvulnerableUntil = 0;
            mission.Player.Health.Apply(new DamageHit(200, Team.Enemy, DamageKind.Bullet));
            Assert.That(mission.Stage, Is.EqualTo(MissionStage.Recovering));
            yield return new WaitForSeconds(1.35f);
            Assert.That(mission.Player.Health.Defeated, Is.False);
            Assert.That(mission.Stage, Is.EqualTo(MissionStage.RooftopRoute));
            Assert.That(mission.NextCheckpoint, Is.EqualTo(mission.Checkpoints.Length - 1));
            Assert.That(mission.Player.Motor.Session.ControlBlocked, Is.False);
            foreach (var enemy in mission.Encounter.Enemies) Assert.That(enemy.Health.Current, Is.EqualTo(enemy.Health.Maximum));
        }
        [UnityTest] public IEnumerator WebShooterAndDomeUseRealDamageAndStun()
        {
            yield return ReachBank();
            var enemy = mission.Encounter.Enemies[0];
            mission.Player.Combat.FireWebAt(enemy.transform.position + Vector3.up, Time.time);
            Assert.That(enemy.State, Is.EqualTo(ThugState.WebbedStun));
            Assert.That(enemy.Health.Current, Is.LessThan(enemy.Health.Maximum));
            var wrapRenderers = enemy.WebWrap.GetComponentsInChildren<Renderer>(true);
            Bounds wrapping = wrapRenderers[0].bounds;
            foreach (var renderer in wrapRenderers) wrapping.Encapsulate(renderer.bounds);
            Assert.That(Vector3.Distance(new Vector3(wrapping.center.x, wrapping.min.y, wrapping.center.z),
                enemy.transform.position), Is.LessThan(.05f));
            Assert.That(wrapping.size.y, Is.EqualTo(1.9f).Within(.05f));
            float before = mission.Player.Combat.WebEnergy;
            mission.Player.Health.InvulnerableUntil = 0;
            Assert.That(mission.Player.Combat.WebDome(Time.time), Is.True);
            Assert.That(mission.Player.Combat.WebEnergy, Is.LessThan(before));
            Assert.That(mission.Player.Health.InvulnerableUntil, Is.GreaterThan(Time.time));
        }
        [UnityTest] public IEnumerator MeleeComboDeliversThreeDistinctHits()
        {
            yield return ReachBank();
            var enemy = mission.Encounter.Enemies[0];
            foreach (var other in mission.Encounter.Enemies) if (other != enemy) other.EncounterActive = false;
            enemy.ApplyWeb(1, 10);
            mission.Player.Motor.SpawnPosition = enemy.transform.position + Vector3.back * 1.1f;
            mission.Player.Motor.ResetPlayer();
            mission.Player.Motor.Visual.rotation = Quaternion.LookRotation(enemy.transform.position - mission.Player.transform.position);
            float began = Time.time;
            for (int frame = 0; frame < 100; frame++)
            {
                float elapsed = Time.time - began;
                bool attack = frame == 0 || frame == 15 || frame == 39;
                mission.Player.Combat.Tick(new MissionInputCommand { Melee = attack }, Time.deltaTime, Time.time);
                yield return null;
            }
            Assert.That(enemy.Health.Defeated, Is.True);
        }
        [UnityTest] public IEnumerator MeleeGuardHasWindupAndDealsDamage()
        {
            yield return ReachBank();
            var enemy = mission.Encounter.Enemies[0];
            foreach (var other in mission.Encounter.Enemies) if (other != enemy) other.EncounterActive = false;
            mission.Player.Motor.SpawnPosition = enemy.transform.position + Vector3.forward * 1.2f;
            mission.Player.Motor.ResetPlayer(); mission.Player.Health.InvulnerableUntil = 0;
            yield return new WaitForSeconds(.08f);
            Assert.That(mission.Player.Health.Current, Is.EqualTo(mission.Player.Health.Maximum));
            yield return new WaitForSeconds(1.0f);
            Assert.That(mission.Player.Health.Current, Is.LessThan(mission.Player.Health.Maximum));
        }
        [UnityTest] public IEnumerator RangedGuardTelegraphsBeforeApplyingBulletDamage()
        {
            yield return ReachBank();
            var enemy = mission.Encounter.Enemies[2];
            Assert.That(enemy.Ranged, Is.True);
            foreach (var other in mission.Encounter.Enemies) if (other != enemy) other.EncounterActive = false;
            mission.Player.Motor.SpawnPosition = enemy.transform.position + Vector3.right * 4;
            mission.Player.Motor.ResetPlayer(); mission.Player.Health.InvulnerableUntil = 0;
            yield return new WaitForSeconds(.15f);
            Assert.That(enemy.State, Is.EqualTo(ThugState.RangedAttack));
            Assert.That(mission.Player.Health.Current, Is.EqualTo(mission.Player.Health.Maximum));
            yield return new WaitForSeconds(.8f);
            Assert.That(mission.Player.Health.Current, Is.EqualTo(mission.Player.Health.Maximum - 9));
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            if (mission != null) mission.Player.Motor.Session.SetPaused(false);
            var empty = SceneManager.CreateScene("AfterMissionTests"); SceneManager.SetActiveScene(empty);
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.IsValid()) yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator RecoveryAfterClearingDoesNotResurrectGuards()
        {
            yield return ReachBank();
            foreach (var enemy in mission.Encounter.Enemies) enemy.ApplyWeb(3, 3);
            Assert.That(mission.Stage, Is.EqualTo(MissionStage.Skylight));
            mission.RequestRecovery();
            yield return new WaitForSeconds(1.35f);
            Assert.That(mission.Stage, Is.EqualTo(MissionStage.Skylight));
            foreach (var enemy in mission.Encounter.Enemies) Assert.That(enemy.Health.Defeated, Is.True);
        }
    }
}
