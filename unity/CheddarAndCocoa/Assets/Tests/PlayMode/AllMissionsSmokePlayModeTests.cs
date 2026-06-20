using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Broad guard that every mission in the rotation starts cleanly: enters Playing with a non-empty
    /// objective, a non-empty runtime snapshot id, and a finite timer. Catches a future mission whose
    /// BeginRound wiring throws or leaves the round in a bad state, without needing a bespoke test.
    /// </summary>
    public sealed class AllMissionsSmokePlayModeTests
    {
        [UnityTest]
        public IEnumerator EveryMission_StartsIntoAPlayableRound()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            int count = game.MissionSelectOptionCount;
            Assert.GreaterOrEqual(count, 13, "All missions should be in the rotation.");

            foreach (GameManager.MissionVariant variant in Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                game.StartMission(variant);
                yield return null;

                Assert.AreEqual(variant, game.ActiveMissionVariant, $"{variant} should be the active mission.");
                Assert.AreEqual(GameManager.State.Playing, game.Phase, $"{variant} should start in Playing.");
                Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome, $"{variant} should start in progress.");
                Assert.IsFalse(string.IsNullOrEmpty(game.ObjectiveLabel), $"{variant} needs a non-empty objective.");
                Assert.IsFalse(string.IsNullOrEmpty(game.ActiveMissionName), $"{variant} needs a name.");
                Assert.IsFalse(string.IsNullOrEmpty(game.RuntimeSnapshot.MissionId), $"{variant} needs a runtime id.");
                Assert.Greater(game.RuntimeSnapshot.ObjectiveGoal, 0, $"{variant} needs a positive goal.");
                Assert.That(game.TimeRemaining, Is.GreaterThan(0f), $"{variant} needs a running timer.");
            }
        }

        [UnityTest]
        public IEnumerator EveryMission_ReplaysCleanlyFromTheEndScreen()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            foreach (GameManager.MissionVariant variant in Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                game.StartMission(variant);
                yield return null;
                game.ForceGameOver();
                yield return null;
                Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome, $"{variant} forced game over should fail.");

                game.Restart();
                yield return null;
                Assert.AreEqual(variant, game.ActiveMissionVariant);
                Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome, $"{variant} should be in progress after replay.");
                Assert.AreEqual(GameManager.State.Playing, game.Phase, $"{variant} should be Playing after replay.");
            }
        }
    }
}
