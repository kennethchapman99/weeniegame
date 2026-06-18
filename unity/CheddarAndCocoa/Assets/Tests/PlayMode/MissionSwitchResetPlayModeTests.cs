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
