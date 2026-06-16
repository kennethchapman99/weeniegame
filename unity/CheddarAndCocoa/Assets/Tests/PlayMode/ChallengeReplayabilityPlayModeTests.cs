using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class ChallengeReplayabilityPlayModeTests
    {
        [UnityTest]
        public IEnumerator UpgradedLevels_Load_AndExposeCoopChallengeState()
        {
            yield return AssertLevel("ArenaScene", "Breakfast Heist");
            yield return AssertLevel("LivingRoomChaosScene", "Living Room Chaos");
        }

        private static IEnumerator AssertLevel(string sceneName, string expectedName)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game, $"{sceneName} should build a GameManager.");
            StringAssert.Contains(expectedName, game.LevelName);
            Assert.IsTrue(game.ModifierDisplayed, "A random round modifier should be selected and displayed.");
            Assert.Greater(game.TimeRemaining, 0f);

            DogController cheddar = null, cocoa = null;
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == DogId.Cheddar) cheddar = id.GetComponent<DogController>();
                if (id.Id == DogId.Cocoa) cocoa = id.GetComponent<DogController>();
            }
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);
            Assert.AreNotSame(cheddar, cocoa);

            float pressureBefore = game.Pressure;
            yield return new WaitForSeconds(0.25f);
            Assert.Greater(game.Pressure, pressureBefore, "Pressure/meter should change over time.");

            cheddar.transform.position = cocoa.transform.position = Vector3.zero;
            float pressureHigh = game.Pressure;
            cheddar.Bark();
            cocoa.Bark();
            Assert.Greater(game.UnitedBarks, 0, "Bark should change gameplay state via a united/team action.");
            Assert.LessOrEqual(game.Pressure, pressureHigh, "Bark/team action should recover or control pressure.");

            game.CompleteObjectiveForTest();
            Assert.IsTrue(game.IsLevelClear, "Objective should be completable.");
            Assert.Greater(game.Score, 0, "Completion should award score.");

            game.Restart();
            Assert.IsFalse(game.IsGameOver);
            Assert.IsFalse(game.IsLevelClear);
            Assert.AreEqual(0, game.Score);
            Assert.Greater(game.TimeRemaining, 0f);
        }
    }
}
