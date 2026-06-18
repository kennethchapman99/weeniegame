using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

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
        }
    }
}
