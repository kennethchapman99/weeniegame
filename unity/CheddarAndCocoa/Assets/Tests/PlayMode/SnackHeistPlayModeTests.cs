using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class SnackHeistPlayModeTests
    {
        private GameManager _game;

        [UnityTest]
        public IEnumerator SnackHeist_RunsThroughDedicatedController()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsInstanceOf<SnackHeistMissionController>(_game.ActiveMissionController,
                "Snack Heist must run entirely through its own IMissionController.");
            Assert.AreEqual(GameManager.MissionVariant.SnackHeist, _game.ActiveMissionController.Variant);
            Assert.AreEqual("snack_heist", _game.RuntimeSnapshot.MissionId);
        }

        [UnityTest]
        public IEnumerator SnackHeist_ClearPath_CollectAllSnacks()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            int goal = _game.RuntimeSnapshot.ObjectiveGoal;
            Assert.Greater(goal, 0);

            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);

            Treat first = FirstTreat();
            first.CollectBy(cocoa);
            Assert.AreEqual(0, _game.BreakfastRecovered,
                "Cocoa should leave Cheddar's snack theft intact and get a recoverable role cue.");
            Assert.That(_game.LastCue, Does.Contain("audits"));

            first.CollectBy(cheddar);
            yield return null;
            Assert.AreEqual(1, _game.BreakfastRecovered);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa: bark-guard"));

            cheddar.transform.position = _game.SquirrelObject.transform.position;
            Assert.IsTrue(_game.SnackHeistController.HandleBark(0));
            Assert.AreEqual(0, _game.SnackHeistController.GuardBarks,
                "Cheddar's mouth-full bark must not solve Cocoa's guard role.");

            cocoa.transform.position = _game.SquirrelObject.transform.position;
            Assert.IsTrue(_game.SnackHeistController.HandleBark(1));
            Assert.AreEqual(1, _game.SnackHeistController.GuardBarks);

            int guard = 0;
            while (_game.BreakfastRecovered < goal && guard++ < 40)
            {
                _game.ForceCollectTreat();
                yield return null;
            }

            Assert.AreEqual(goal, _game.BreakfastRecovered);
            Assert.IsTrue(_game.SnackHeistController.IsPresentingSuccessfulOutcome,
                "The final stash should get a readable in-world payoff before the end card.");
            float clearWait = 0f;
            while (_game.Outcome == GameManager.MissionOutcome.InProgress && clearWait < 2f)
            {
                clearWait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsFalse(_game.SnackHeistController.IsPresentingSuccessfulOutcome);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Stash Secured"),
                "A clean clear should read as a distinct, flavored outcome, not a generic fallback.");
        }

        [UnityTest]
        public IEnumerator SnackHeist_FailPath_SquirrelStealsEnough()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            int maxStolen = _game.ActiveMissionController.IsFailed ? 0 : 4;
            int guard = 0;
            while (_game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 30)
            {
                _game.ForceStealAttempt();
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.IsTrue(_game.RuntimeSnapshot.IsFailed);
        }

        [UnityTest]
        public IEnumerator SnackHeist_Replay_ResetsState()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            _game.ForceCollectTreat();
            yield return null;

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.SnackHeist, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.BreakfastRecovered);
            Assert.AreEqual(0, _game.StolenFood);
        }

        [UnityTest]
        public IEnumerator SnackHeist_StashedSpriteLingersBeforeTreatDestroyed()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            var before = new System.Collections.Generic.List<Treat>(
                Object.FindObjectsByType<Treat>(FindObjectsSortMode.None));
            Assert.Greater(before.Count, 0);

            _game.ForceCollectTreat();
            yield return null;

            Treat collected = null;
            foreach (var treat in before)
            {
                if (treat == null) continue;
                var art = treat.GetComponent<MissionPropArtAttachment>();
                if (art != null && art.ResourcePath == FinalGameplayArt.SnackHeistPlateStashed) { collected = treat; break; }
            }

            Assert.IsNotNull(collected,
                "Right after collection the treat must still exist and show its Stashed sprite - it " +
                "must not already be destroyed in the same frame the sprite was set.");
            Assert.IsTrue(collected.gameObject.activeInHierarchy);

            yield return new WaitForSecondsRealtime(0.6f);
            yield return null;

            Assert.IsTrue(collected == null || !collected.gameObject.activeInHierarchy,
                "The collected treat should be gone once its linger window passes.");
        }

        [UnityTest]
        public IEnumerator SnackHeist_UsesReadableTargetAndGuardLaneArt()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            _game.SnackHeistController.ForceStartStealForArt();
            yield return null;

            AssertTreatArt(FinalGameplayArt.SnackHeistPlateTargeted);
            AssertMissionArt("SnackHeistBarkGuardLane", FinalGameplayArt.SnackHeistGuardLane);
        }

        private IEnumerator LoadArena()
        {
            _game = null;
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
        }

        private static DogController FindDog(DogId id)
        {
            foreach (var dog in Object.FindObjectsByType<DogController>(FindObjectsSortMode.None))
                if (dog.TryGetComponent<DogIdentity>(out var identity) && identity.Id == id) return dog;
            return null;
        }

        private static Treat FirstTreat()
        {
            foreach (var treat in Object.FindObjectsByType<Treat>(FindObjectsSortMode.None))
                if (treat != null && treat.gameObject.activeInHierarchy) return treat;
            return null;
        }

        private static void AssertTreatArt(string expectedResourcePath)
        {
            foreach (var treat in Object.FindObjectsByType<Treat>(FindObjectsSortMode.None))
            {
                if (treat == null || !treat.gameObject.activeInHierarchy) continue;
                var art = treat.GetComponent<MissionPropArtAttachment>();
                if (art == null) continue;
                if (art.ResourcePath != expectedResourcePath) continue;
                Assert.IsTrue(art.HasRuntimeSprite, $"Expected runtime sprite for {expectedResourcePath}.");
                return;
            }

            Assert.Fail($"No active treat uses generated prop art {expectedResourcePath}.");
        }

        private static void AssertMissionArt(string objectName, string expectedResourcePath)
        {
            var go = GameObject.Find(objectName);
            Assert.IsNotNull(go, $"Missing object {objectName}.");
            var art = go.GetComponent<MissionPropArtAttachment>();
            Assert.IsNotNull(art, $"Missing MissionPropArtAttachment on {go.name}.");
            Assert.AreEqual(expectedResourcePath, art.ResourcePath);
            Assert.IsTrue(art.HasRuntimeSprite, $"Expected runtime sprite for {expectedResourcePath}.");
        }
    }
}
