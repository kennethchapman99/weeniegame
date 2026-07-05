using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class SockPanicPlayModeTests
    {
        private GameManager _game;

        [UnityTest]
        public IEnumerator SockPanic_ClearPath_HasAFlavoredOutcomeSummary()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SockPanic);
            yield return null;

            int goal = _game.RuntimeSnapshot.ObjectiveGoal;
            Assert.Greater(goal, 0);

            int guard = 0;
            while (_game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 20)
            {
                _game.ForceSockBasketTip(DogId.Cheddar);
                yield return null;
                if (_game.ExposedSock != null)
                    _game.SockPanicController.HandleTreatCollected(_game.ExposedSock, 1); // Cocoa: partner dive
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Socks Rescued"),
                "A clean clear should read as a distinct, flavored outcome, not a generic fallback.");
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
