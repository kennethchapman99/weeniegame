using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class BackyardSquirrelTrapPlayModeTests
    {
        private GameManager _game;

        [UnityTest]
        public IEnumerator BackyardTrap_RequiresGapPartnerRecovery_ThenReversesRoles()
        {
            yield return LoadBackyard();

            Assert.AreEqual(DogId.Cheddar, _game.BackyardTrapState.PressureDog);
            Assert.AreEqual(DogId.Cocoa, _game.BackyardTrapState.GapDog);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cheddar"));
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa"));

            _game.ForceBackyardTrapRedirect(DogId.Cocoa, true);
            Assert.AreEqual(1, _game.BackyardTrapState.Fumbles, "The wrong pressure dog should produce a recoverable juke.");
            Assert.IsFalse(_game.BackyardTrapState.WeenieDropped);

            _game.ForceBackyardTrapRedirect(DogId.Cheddar, false);
            Assert.AreEqual(2, _game.BackyardTrapState.Fumbles, "An open escape gap should produce a recoverable fake route.");
            Assert.IsFalse(_game.BackyardTrapState.WeenieDropped);

            _game.ForceBackyardTrapRedirect(DogId.Cheddar, true);
            yield return null;
            Assert.IsTrue(_game.BackyardTrapState.WeenieDropped);
            Assert.IsNotNull(_game.BackyardDroppedWeenie);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa: recover"));

            _game.ForceBackyardTrapRecovery(DogId.Cheddar);
            Assert.AreEqual(3, _game.BackyardTrapState.Fumbles, "The pressure dog cannot recover its own drop.");
            Assert.IsTrue(_game.BackyardTrapState.WeenieDropped, "The funny failure must remain recoverable.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            _game.ForceBackyardTrapRecovery(DogId.Cocoa);
            yield return null;
            Assert.AreEqual(1, _game.BackyardTrapState.Recoveries);
            Assert.AreEqual(DogId.Cocoa, _game.BackyardTrapState.PressureDog, "The second pass must reverse pressure roles.");
            Assert.AreEqual(DogId.Cheddar, _game.BackyardTrapState.GapDog, "The second pass must reverse gap/recovery roles.");
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa pressures"));
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cheddar holds"));

            _game.ForceBackyardTrapRedirect(DogId.Cocoa, true);
            _game.ForceBackyardTrapRecovery(DogId.Cheddar);
            yield return null;

            Assert.IsTrue(_game.BackyardTrapState.Complete);
            Assert.AreEqual(2, _game.BackyardTrapState.Redirects);
            Assert.AreEqual(2, _game.BackyardTrapState.Recoveries);
            Assert.AreEqual("backyard_rescue", _game.RuntimeSnapshot.MissionId);
            Assert.IsTrue(LogContains("SquirrelTrapRedirect"));
            Assert.IsTrue(LogContains("SquirrelTrapRecovery"));
        }

        [UnityTest]
        public IEnumerator BackyardTrap_ReplayResetsAuthoredBeat()
        {
            yield return LoadBackyard();

            _game.ForceBackyardTrapRedirect(DogId.Cheddar, true);
            _game.ForceBackyardTrapRecovery(DogId.Cocoa);
            Assert.AreEqual(1, _game.BackyardTrapState.Recoveries);

            _game.Restart();
            yield return null;

            Assert.AreEqual(0, _game.BackyardTrapState.Recoveries);
            Assert.AreEqual(0, _game.BackyardTrapState.Redirects);
            Assert.AreEqual(0, _game.BackyardTrapState.Fumbles);
            Assert.IsFalse(_game.BackyardTrapState.WeenieDropped);
            Assert.AreEqual(DogId.Cheddar, _game.BackyardTrapState.PressureDog);
            Assert.AreEqual(DogId.Cocoa, _game.BackyardTrapState.GapDog);
        }

        private IEnumerator LoadBackyard()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
            _game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
        }

        private bool LogContains(string text)
        {
            foreach (string entry in _game.PlaytestEvents)
                if (entry.Contains(text)) return true;
            return false;
        }
    }
}
