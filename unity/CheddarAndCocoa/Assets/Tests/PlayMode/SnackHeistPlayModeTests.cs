using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
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

            int guard = 0;
            while (_game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 40)
            {
                _game.ForceCollectTreat();
                yield return null;
            }

            Assert.AreEqual(goal, _game.BreakfastRecovered);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
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
