using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Tests
{
    public sealed class SessionResetPlayModeTests
    {
        [UnityTest]
        public IEnumerator ResetSession_ClearsAccumulatedSessionStats()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            // Play and finish a couple of missions to accumulate session stats.
            game.StartMission(GameManager.MissionVariant.MarkTheYard);
            yield return null;
            game.ForceGameOver();
            yield return null;
            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;
            game.ForceGameOver();
            yield return null;

            Assert.AreEqual(2, game.SessionMissionsPlayed);
            Assert.Greater(game.FailuresForMission(GameManager.MissionVariant.MarkTheYard), 0);

            game.ResetSession();
            yield return null;

            Assert.AreEqual(0, game.SessionMissionsPlayed);
            Assert.AreEqual(0, game.SessionTotalScore);
            Assert.AreEqual(0, game.SessionStarsEarned);
            Assert.AreEqual(0, game.SessionFlawlessClears);
            Assert.AreEqual(0, game.SessionUniqueMissionsCompleted);
            Assert.AreEqual(0, game.FailuresForMission(GameManager.MissionVariant.MarkTheYard));
            Assert.AreEqual(0, game.BestScoreForMission(GameManager.MissionVariant.MarkTheYard));
            Assert.AreEqual("NEW", game.MissionSelectStatusFor(GameManager.MissionVariant.MarkTheYard));
            Assert.IsFalse(game.SessionSummaryReady);
            Assert.That(game.SessionSummaryLabel, Does.Contain("no missions played yet"));
            Assert.That(game.SessionRanksEarnedLabel, Does.Contain("none yet"));
        }

        [UnityTest]
        public IEnumerator DirectMissionSwitch_ClearsSpeedEmotionAndMissionStateImmediately()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddarObject = GameObject.Find("Cheddar");
            var cheddar = cheddarObject.GetComponent<DogController>();
            var feedback = cheddarObject.GetComponent<DogReadabilityFeedback>();

            game.StartMission(GameManager.MissionVariant.SquirrelConspiracy);
            yield return null;
            cheddarObject.transform.position = new Vector2(55f, -25f);
            yield return null;
            yield return null;
            Assert.IsTrue(cheddar.TravelAssist);

            game.ForceGameOver();
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, feedback.CurrentPose);

            game.StartMission(GameManager.MissionVariant.SockPanic);
            Assert.IsFalse(cheddar.TravelAssist, "New mission must not inherit distant-objective speed.");
            Assert.AreEqual(DogReadabilityFeedback.Pose.Idle, feedback.CurrentPose,
                "New mission intro must not inherit the prior failure pose.");
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.IsFalse(game.SquirrelObject.activeSelf);
            Assert.IsFalse(game.PredatorObject.activeSelf);
            Assert.IsFalse(game.RopeObject.activeSelf);

            game.StartMission(GameManager.MissionVariant.LeashWalk);
            game.ForceLeashSnap();
            Assert.AreEqual(1, game.LeashWalkState.Snaps);
            game.StartMission(GameManager.MissionVariant.CarRide);
            Assert.AreEqual(0, game.LeashWalkState.Snaps);
            Assert.AreEqual(0, game.CarRideState.Spills);
        }
    }
}
