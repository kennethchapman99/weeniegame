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
    /// The Squirrel Switcheroo mission wires the Bait-and-Switch co-op puzzle into the real mission
    /// flow: Cheddar feints at a decoy to commit the squirrel, and only while it is committed can Cocoa
    /// raid the real stash. Over-baiting backfires; enough backfires fail the mission.
    /// </summary>
    public sealed class CoopSquirrelSwitcherooPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator Switcheroo_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(20, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.SquirrelSwitcheroo) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "The Ol' Switcheroo should be reachable from mission select.");
            Assert.AreEqual("The Ol' Switcheroo", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator Switcheroo_ClearPath_BaitThenRaidThreeTimes()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SquirrelSwitcheroo);
            yield return null;

            Assert.AreEqual("squirrel_switcheroo", _game.RuntimeSnapshot.MissionId);
            Assert.That(_game.ObjectiveLabel.ToLowerInvariant(), Does.Contain("stash"));

            for (int i = 0; i < 3; i++)
            {
                _game.ForceSwitcherooBait(0.7f);          // commit the squirrel to the decoy
                Assert.IsTrue(_game.SwitcherooPuzzle.Committed);
                _game.ForceSwitcherooStrike();            // Cocoa raids the stash in the window
                _game.ForceSwitcherooBait(2f, false);     // ease fully off so commitment resets before the next rep
            }

            Assert.IsTrue(_game.SwitcherooPuzzle.Solved);
            Assert.AreEqual(3, _game.SwitcherooPuzzle.Hits);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Switcheroo Pulled"));
        }

        [UnityTest]
        public IEnumerator Switcheroo_StrikeWhileGuarding_Whiffs()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SquirrelSwitcheroo);
            yield return null;

            _game.ForceSwitcherooBait(0.3f);              // under threshold: squirrel still guarding
            Assert.IsFalse(_game.SwitcherooPuzzle.Committed);
            _game.ForceSwitcherooStrike();                // swing into the guard

            Assert.AreEqual(0, _game.SwitcherooPuzzle.Hits);
            Assert.AreEqual(1, _game.SwitcherooPuzzle.Whiffs);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator Switcheroo_FailPath_OverBaitsTooManyTimes()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SquirrelSwitcheroo);
            yield return null;

            for (int i = 0; i < 4; i++)
                _game.ForceSwitcherooBait(1.0f);          // push commitment to full -> backfire each time

            Assert.AreEqual(4, _game.SwitcherooPuzzle.Backfires);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Squirrel Wised Up"));
        }

        [UnityTest]
        public IEnumerator Switcheroo_Replay_ResetsThePuzzle()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SquirrelSwitcheroo);
            yield return null;
            _game.ForceSwitcherooBait(1.0f);              // a backfire
            Assert.Greater(_game.SwitcherooPuzzle.Backfires, 0);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.SquirrelSwitcheroo, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.SwitcherooPuzzle.Hits);
            Assert.AreEqual(0, _game.SwitcherooPuzzle.Backfires);
            Assert.AreEqual(0f, _game.SwitcherooPuzzle.Commitment);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator Switcheroo_PositionDriven_BaiterRaisesCommitment_LeavingDecaysIt()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SquirrelSwitcheroo);
            yield return null;

            var cheddarBody = _cheddar.GetComponent<Rigidbody2D>();

            // Cheddar feints at the decoy: the squirrel's commitment to the decoy rises while he is there.
            for (int i = 0; i < 30; i++)
            {
                _cheddar.transform.position = _game.SwitcherooDecoyZone;
                if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
                yield return null;
            }
            float raised = _game.SwitcherooPuzzle.Commitment;
            Assert.Greater(raised, 0f, "Standing at the decoy should raise the squirrel's commitment.");

            // Cheddar backs off the decoy: commitment to the decoy decays back down.
            for (int i = 0; i < 60; i++)
            {
                _cheddar.transform.position = new Vector3(_game.SwitcherooDecoyZone.x - 40f, _game.SwitcherooDecoyZone.y, 0f);
                if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
                yield return null;
            }
            Assert.Less(_game.SwitcherooPuzzle.Commitment, raised,
                "Leaving the decoy should let the squirrel's commitment decay.");
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
