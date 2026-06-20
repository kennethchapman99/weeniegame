using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class CarRidePlayModeTests
    {
        private GameManager _game;

        [UnityTest]
        public IEnumerator CarRide_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(19, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.CarRide) { found = true; break; }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Car Ride Balance should be reachable from mission select.");
            Assert.AreEqual("Car Ride Balance", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator CarRide_ClearPath_SteadyEveryLurch()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;

            Assert.AreEqual("car_ride", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Lean to keep the car level"));
            int required = game.RuntimeSnapshot.ObjectiveGoal;
            Assert.Greater(required, 0);

            // Alternating lurches oscillate the balance without ever tipping over.
            int guard = 0;
            while (game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 30)
            {
                game.ForceCarLurch();
                yield return null;
            }

            Assert.AreEqual(required, game.CarRideState.LurchesSurvived);
            Assert.AreEqual(0, game.CarRideState.Spills);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Smooth Riders"));
        }

        [UnityTest]
        public IEnumerator CarRide_FailPath_TooManySpills()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;

            for (int i = 0; i < 4; i++)
            {
                game.ForceCarSpill();
                yield return null;
            }

            Assert.AreEqual(4, game.CarRideState.Spills);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Car Sick"));
        }

        [UnityTest]
        public IEnumerator CarRide_Replay_ResetsBalanceState()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            game.ForceCarLurch();
            game.ForceCarSpill();
            yield return null;
            Assert.Greater(game.CarRideState.LurchesSurvived + game.CarRideState.Spills, 0);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.CarRide, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.CarRideState.LurchesSurvived);
            Assert.AreEqual(0, game.CarRideState.Spills);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator CarRide_LurchAndSpill_HaveReadablePackReactions()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;

            _game.ForceCarLurch();
            yield return null;
            Assert.IsTrue(HasWorldPop("STEADIED"));
            foreach (var feedback in _game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, feedback.CurrentPose);
            Assert.AreEqual(ArenaFeedbackCatalog.TugRescueSuccess, _game.LastAudioCueRequested);

            _game.ForceCarSpill();
            yield return null;
            Assert.IsTrue(HasWorldPop("CAR SPILL"));
            foreach (var feedback in _game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, feedback.CurrentPose);
            Assert.AreEqual(ArenaFeedbackCatalog.ThreatWarning, _game.LastAudioCueRequested);
        }

        private static bool HasWorldPop(string text)
        {
            foreach (var pop in Object.FindObjectsByType<MissionWorldPop>(FindObjectsSortMode.None))
                if (pop.Label.Contains(text)) return true;
            return false;
        }

        private IEnumerator LoadArena()
        {
            _game = null;
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
        }
    }
}
