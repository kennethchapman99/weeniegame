using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Guards that switching directly from one mission to another (without passing through mission
    /// select) clears the prior mission's runtime state, so no progress bleeds across missions.
    /// </summary>
    public sealed class MissionSwitchResetPlayModeTests
    {
        private GameManager _game;

        [UnityTest]
        public IEnumerator SwitchingMissions_ClearsPriorMissionRuntimeState()
        {
            yield return LoadArena();
            var game = _game;

            // Build up progress in several missions, then switch away and confirm each reset.
            game.StartMission(GameManager.MissionVariant.MarkTheYard);
            yield return null;
            game.ForceClaimZone(DogId.Cheddar);
            yield return null;
            Assert.Greater(game.MarkTheYardState.Claimed, 0);

            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;
            Assert.AreEqual(0, game.MarkTheYardState.Claimed, "Territory state should reset when leaving Mark the Yard.");

            game.ForceScentSniff(DogId.Cheddar);
            game.ForceScentDigWrong(DogId.Cheddar);
            yield return null;
            Assert.Greater(game.ScentSearchState.Sniffs + game.ScentSearchState.WastedDigs, 0);

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;
            Assert.AreEqual(0, game.ScentSearchState.Sniffs, "Scent state should reset when leaving Scent Search.");
            Assert.AreEqual(0, game.ScentSearchState.WastedDigs);

            game.ForceThunderclap();
            yield return null;
            Assert.Greater(game.ThunderstormState.ClapsSurvived, 0);

            game.StartMission(GameManager.MissionVariant.WeenieRoundup);
            yield return null;
            Assert.AreEqual(0, game.ThunderstormState.ClapsSurvived, "Storm state should reset when leaving Thunderstorm Comfort.");
            Assert.AreEqual(0f, game.Panic.CheddarPanic);
            Assert.Greater(game.WeenieRoundupState.Loose, 0, "Weenie Roundup should be freshly configured.");
        }

        [UnityTest]
        public IEnumerator SwitchingControllerMissions_DeactivatesOwnedActorsWithoutDuplicates()
        {
            yield return LoadArena();

            _game.StartMission(GameManager.MissionVariant.SockPanic);
            yield return null;
            AssertActive(ArenaArtCatalog.LaundryBasketObjectName, true);

            _game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            AssertActive(ArenaArtCatalog.LaundryBasketObjectName, false);
            AssertActive("Car Ride Balance Vehicle", true);

            _game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;
            AssertActive("Car Ride Balance Vehicle", false);
            AssertPrefixActive("DigSpot_", expectedCount: 6, active: true);

            _game.StartMission(GameManager.MissionVariant.WeenieRoundup);
            yield return null;
            AssertPrefixActive("DigSpot_", expectedCount: 6, active: false);
            AssertPrefixActive("LooseWeenie_", expectedCount: 5, active: true);
            AssertActive("HomeBowl", true);

            _game.StartMission(GameManager.MissionVariant.LeashWalk);
            yield return null;
            AssertPrefixActive("LooseWeenie_", expectedCount: 5, active: false);
            AssertActive("HomeBowl", false);
            AssertPrefixActive("LeashCheckpoint_", expectedCount: 4, active: true);

            _game.Restart();
            yield return null;
            AssertPrefixActive("LeashCheckpoint_", expectedCount: 4, active: true);

            _game.StartMission(GameManager.MissionVariant.SquirrelConspiracy);
            yield return null;
            AssertPrefixActive("LeashCheckpoint_", expectedCount: 4, active: false);
            AssertPrefixActiveCount("SquirrelCutoff_", expectedCount: 4, activeCount: 1);

            _game.Restart();
            yield return null;
            AssertPrefixActiveCount("SquirrelCutoff_", expectedCount: 4, activeCount: 1);

            _game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            AssertPrefixActive("SquirrelCutoff_", expectedCount: 4, active: false);
        }

        private static void AssertActive(string objectName, bool expected)
        {
            var matches = FindSceneObjects(objectName, exact: true);
            Assert.AreEqual(1, matches.Length, $"Expected exactly one cached {objectName} actor.");
            Assert.AreEqual(expected, matches[0].activeSelf, $"Unexpected active state for {objectName}.");
        }

        private static void AssertPrefixActive(string prefix, int expectedCount, bool active)
        {
            var matches = FindSceneObjects(prefix, exact: false);
            Assert.AreEqual(expectedCount, matches.Length, $"Unexpected cached actor count for {prefix}.");
            foreach (var match in matches)
                Assert.AreEqual(active, match.activeSelf, $"Unexpected active state for {match.name}.");
        }

        private static void AssertPrefixActiveCount(string prefix, int expectedCount, int activeCount)
        {
            var matches = FindSceneObjects(prefix, exact: false);
            Assert.AreEqual(expectedCount, matches.Length, $"Unexpected cached actor count for {prefix}.");
            int actualActive = 0;
            foreach (var match in matches) if (match.activeSelf) actualActive++;
            Assert.AreEqual(activeCount, actualActive, $"Unexpected active actor count for {prefix}.");
        }

        private static GameObject[] FindSceneObjects(string name, bool exact)
        {
            var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var matches = new System.Collections.Generic.List<GameObject>();
            foreach (var go in all)
            {
                if (!go.scene.IsValid()) continue;
                if (exact ? go.name == name : go.name.StartsWith(name)) matches.Add(go);
            }
            return matches.ToArray();
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
    }
}
