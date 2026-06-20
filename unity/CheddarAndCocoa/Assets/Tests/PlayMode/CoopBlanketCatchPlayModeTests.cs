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
    /// The Blanket Catch mission wires the Stretch-Span co-op puzzle into the real mission flow: both
    /// dogs hold a blanket stretched between them to catch falling food. The span is only usable in a
    /// taut separation band (too close = slack, too far = rip) and its midpoint must be under the snack.
    /// Over-stretching rips the blanket; too many rips fail the round.
    /// </summary>
    public sealed class CoopBlanketCatchPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator Blanket_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(20, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.BlanketCatch) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "The Blanket Catch should be reachable from mission select.");
            Assert.AreEqual("The Blanket Catch", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator Blanket_ClearPath_TautAndCenteredCatchesEnough()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BlanketCatch);
            yield return null;

            Assert.AreEqual("blanket_catch", _game.RuntimeSnapshot.MissionId);
            int needed = _game.BlanketPuzzle.CatchesNeeded;

            for (int i = 0; i < needed; i++)
            {
                _game.ForceBlanketSpan(7.5f, 0f);   // taut band, centered
                Assert.IsTrue(_game.BlanketPuzzle.Taut);
                _game.ForceBlanketCatch(0f);        // snack falls dead center
            }

            Assert.IsTrue(_game.BlanketPuzzle.Solved);
            Assert.AreEqual(needed, _game.BlanketPuzzle.Caught);
            Assert.AreEqual(0, _game.BlanketPuzzle.Rips);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Dinner Saved"));
        }

        [UnityTest]
        public IEnumerator Blanket_SlackOrOffCenter_Misses()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BlanketCatch);
            yield return null;

            // Too close together: the blanket sags and the snack bounces off.
            _game.ForceBlanketSpan(2f, 0f);
            Assert.IsTrue(_game.BlanketPuzzle.Slack);
            _game.ForceBlanketCatch(0f);
            Assert.AreEqual(0, _game.BlanketPuzzle.Caught);
            Assert.AreEqual(1, _game.BlanketPuzzle.Missed);

            // Taut but not under the snack: still a miss.
            _game.ForceBlanketSpan(7.5f, 0f);
            _game.ForceBlanketCatch(20f);
            Assert.AreEqual(0, _game.BlanketPuzzle.Caught);
            Assert.AreEqual(2, _game.BlanketPuzzle.Missed);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator Blanket_FailPath_TooManyRips()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BlanketCatch);
            yield return null;

            // Each over-stretch (after easing back to taut) rips once; three rips ends it.
            for (int i = 0; i < 3; i++)
            {
                _game.ForceBlanketSpan(15f, 0f);  // too far -> rip
                _game.ForceBlanketSpan(7.5f, 0f); // ease back to taut to re-arm the rip edge
            }

            Assert.AreEqual(3, _game.BlanketPuzzle.Rips);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Tattered Blanket"));
        }

        [UnityTest]
        public IEnumerator Blanket_Replay_ResetsThePuzzle()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BlanketCatch);
            yield return null;
            _game.ForceBlanketSpan(15f, 0f); // a rip
            Assert.Greater(_game.BlanketPuzzle.Rips, 0);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.BlanketCatch, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.BlanketPuzzle.Caught);
            Assert.AreEqual(0, _game.BlanketPuzzle.Rips);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator Blanket_PositionDriven_SpacingDrivesTheSpanBand()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BlanketCatch);
            yield return null;

            var cheddarBody = _cheddar.GetComponent<Rigidbody2D>();
            var cocoaBody = _cocoa.GetComponent<Rigidbody2D>();
            float y = _game.BlanketCatchY;

            // A comfortable spread: the span pulls taut.
            for (int i = 0; i < 4; i++)
            {
                _cheddar.transform.position = new Vector3(-4f, y, 0f);
                _cocoa.transform.position = new Vector3(4f, y, 0f);
                if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
                if (cocoaBody != null) cocoaBody.linearVelocity = Vector2.zero;
                yield return null;
            }
            Assert.IsTrue(_game.BlanketPuzzle.Taut, "A mid spread should pull the blanket taut.");

            // Yank too far apart: the blanket over-stretches and rips.
            for (int i = 0; i < 4; i++)
            {
                _cheddar.transform.position = new Vector3(-9f, y, 0f);
                _cocoa.transform.position = new Vector3(9f, y, 0f);
                if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
                if (cocoaBody != null) cocoaBody.linearVelocity = Vector2.zero;
                yield return null;
            }
            Assert.IsTrue(_game.BlanketPuzzle.Overstretched, "Too far apart should over-stretch the span.");
            Assert.Greater(_game.BlanketPuzzle.Rips, 0, "Over-stretching should rip the blanket.");
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
