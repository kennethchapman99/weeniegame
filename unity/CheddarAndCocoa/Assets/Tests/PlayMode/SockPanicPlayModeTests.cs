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
                _game.ForceSockBasketTip(DogId.Cocoa);
                yield return null;
                if (_game.ExposedSock != null)
                    _game.SockPanicController.HandleTreatCollected(_game.ExposedSock, 0); // Cheddar: sock dive
                yield return null;
                if (_game.SockPanicState.SuccessfulDives >= goal)
                    _game.SockPanicController.ForceFinishSuccessPresentation();
            }

            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Socks Rescued"),
                "A clean clear should read as a distinct, flavored outcome, not a generic fallback.");
            Assert.AreNotEqual("MVP: awaiting dog heroics", _game.MvpLabel,
                "A sock dive should credit the diving dog toward the MVP stat, not just whoever last barked.");
        }

        [UnityTest]
        public IEnumerator SockPanic_CocoaMustAnchorContinuouslyWhileCheddarDives()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SockPanic);
            yield return null;

            var cocoa = FindDog(DogId.Cocoa);
            Assert.IsNotNull(cocoa);

            _game.ForceSockBasketTip(DogId.Cheddar);
            Assert.IsFalse(_game.SockPanicState.BasketOpen,
                "Cheddar is the chaotic diver; he must not replace Cocoa's anchor role.");

            _game.ForceSockBasketTip(DogId.Cocoa);
            Assert.IsTrue(_game.SockPanicState.BasketOpen);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa: HOLD"));
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cheddar: DIVE"));

            cocoa.transform.position = _game.LaundryBasketObject.transform.position + Vector3.right * 8f;
            yield return null;

            Assert.IsFalse(_game.SockPanicState.BasketOpen,
                "The basket must close if Cocoa abandons the hold before Cheddar dives.");
            Assert.AreEqual(1, _game.SockPanicState.Fumbles);
            Assert.IsNull(_game.ExposedSock);
        }

        private static DogController FindDog(DogId id)
        {
            foreach (var identity in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
                if (identity.Id == id) return identity.GetComponent<DogController>();
            return null;
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
