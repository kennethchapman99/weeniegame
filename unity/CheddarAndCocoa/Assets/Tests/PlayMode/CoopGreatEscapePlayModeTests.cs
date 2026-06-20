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
    /// The Great Escape mission wires the Sequence-Chain co-op puzzle into the real mission flow: the
    /// dogs run an ordered contraption chain with alternating owners (Cocoa, Cheddar, Cocoa, Cheddar),
    /// so neither can rush it alone. Wrong dog / wrong order is a harmless fumble; dawdling eases the
    /// chain back a step; botching it too many times fails the breakout.
    /// </summary>
    public sealed class CoopGreatEscapePlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator Escape_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(20, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.GreatEscape) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "The Great Escape should be reachable from mission select.");
            Assert.AreEqual("The Great Escape", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator Escape_ClearPath_RunTheChainInOrder()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            Assert.AreEqual("great_escape", _game.RuntimeSnapshot.MissionId);
            int steps = _game.GreatEscapePuzzle.StepCount;

            for (int i = 0; i < steps; i++)
                _game.ForceEscapeStep(_game.GreatEscapePuzzle.NextOwner); // the owning dog does its step

            Assert.IsTrue(_game.GreatEscapePuzzle.Solved);
            Assert.AreEqual(0, _game.GreatEscapePuzzle.Fumbles + _game.GreatEscapePuzzle.Settles);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Jailbreak"));
        }

        [UnityTest]
        public IEnumerator Escape_WrongDog_IsAHarmlessFumble()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            ChainActor owner = _game.GreatEscapePuzzle.NextOwner;
            ChainActor wrong = owner == ChainActor.Cocoa ? ChainActor.Cheddar : ChainActor.Cocoa;
            _game.ForceEscapeStep(wrong);

            Assert.AreEqual(0, _game.GreatEscapePuzzle.Step);
            Assert.AreEqual(1, _game.GreatEscapePuzzle.Fumbles);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator Escape_Dawdling_EasesTheChainBack()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            _game.ForceEscapeStep(_game.GreatEscapePuzzle.NextOwner); // advance one step first
            Assert.AreEqual(1, _game.GreatEscapePuzzle.Step);

            _game.ForceEscapeIdle(8f); // dawdle past the settle window

            Assert.AreEqual(0, _game.GreatEscapePuzzle.Step);
            Assert.AreEqual(1, _game.GreatEscapePuzzle.Settles);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator Escape_FailPath_TooManyBotches()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            ChainActor owner = _game.GreatEscapePuzzle.NextOwner;
            ChainActor wrong = owner == ChainActor.Cocoa ? ChainActor.Cheddar : ChainActor.Cocoa;
            for (int i = 0; i < 6; i++)
                _game.ForceEscapeStep(wrong); // six fumbles -> the breakout falls apart

            Assert.AreEqual(6, _game.GreatEscapePuzzle.Fumbles);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Botched Contraption"));
        }

        [UnityTest]
        public IEnumerator Escape_Replay_ResetsThePuzzle()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;
            _game.ForceEscapeStep(_game.GreatEscapePuzzle.NextOwner);
            Assert.Greater(_game.GreatEscapePuzzle.Step, 0);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.GreatEscape, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.GreatEscapePuzzle.Step);
            Assert.AreEqual(0, _game.GreatEscapePuzzle.Fumbles);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator Escape_PositionDriven_OwnerAtStationAdvancesTheChain()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            var cheddarBody = _cheddar.GetComponent<Rigidbody2D>();
            var cocoaBody = _cocoa.GetComponent<Rigidbody2D>();

            ChainActor owner = _game.GreatEscapePuzzle.NextOwner;
            DogController ownerDog = owner == ChainActor.Cheddar ? _cheddar : _cocoa;
            DogController otherDog = owner == ChainActor.Cheddar ? _cocoa : _cheddar;
            Vector2 station = _game.EscapeStationSpot(0);

            for (int i = 0; i < 12; i++)
            {
                ownerDog.transform.position = station;
                otherDog.transform.position = new Vector3(station.x - 40f, station.y, 0f);
                if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
                if (cocoaBody != null) cocoaBody.linearVelocity = Vector2.zero;
                yield return null;
            }

            Assert.GreaterOrEqual(_game.GreatEscapePuzzle.Step, 1, "The owning dog at the active station should advance the chain.");
        }

        private IEnumerator LoadArena()
        {
            _game = null; _cheddar = null; _cocoa = null;
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == DogId.Cheddar) _cheddar = id.GetComponent<DogController>();
                if (id.Id == DogId.Cocoa) _cocoa = id.GetComponent<DogController>();
            }
            Assert.IsNotNull(_game);
            Assert.IsNotNull(_cheddar);
            Assert.IsNotNull(_cocoa);
        }
    }
}
