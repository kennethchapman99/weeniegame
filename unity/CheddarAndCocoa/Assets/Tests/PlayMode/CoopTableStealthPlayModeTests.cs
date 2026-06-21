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
    /// The Table Stealth mission wires the Human-Distraction co-op puzzle into the real mission flow:
    /// Cocoa flops belly-up to hold the human's gaze while Cheddar sneaks the dropped steak, and
    /// sneaking while the human is watching gets the pair spotted (a recoverable exposure).
    /// </summary>
    public sealed class CoopTableStealthPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator TableStealth_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(21, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.TableStealth) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "Table Stealth should be reachable from mission select.");
            Assert.AreEqual("Table Stealth", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator TableStealth_ClearPath_DistractThenSneakTheSteak()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;

            Assert.AreEqual("table_stealth", _game.RuntimeSnapshot.MissionId);
            Assert.That(_game.ObjectiveLabel.ToLowerInvariant(), Does.Contain("human"));

            _game.ForceTableFlop(true);   // Cocoa commits to the belly-flop distraction
            _game.ForceTableSneak(2.0f);  // Cheddar sneaks the steak while the human is held (needs 1.5s)

            Assert.IsTrue(_game.TableStealthPuzzle.Solved);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Steak Sneaked"));
        }

        [UnityTest]
        public IEnumerator TableStealth_FailPath_SpottedTooManyTimes()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;

            for (int i = 0; i < 4; i++)
            {
                // Sneaking while the human is NOT distracted (no flop) gets the pair spotted.
                _game.ForceTableSneak(0.3f);
            }

            Assert.AreEqual(4, _game.TableStealthPuzzle.Exposures);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Caught At The Table"));
        }

        [UnityTest]
        public IEnumerator TableStealth_Replay_ResetsThePuzzle()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;
            _game.ForceTableSneak(0.3f); // one exposure
            Assert.Greater(_game.TableStealthPuzzle.Exposures, 0);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.TableStealth, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.TableStealthPuzzle.Exposures);
            Assert.AreEqual(0f, _game.TableStealthPuzzle.SneakProgress);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator TableStealth_PositionDriven_FloppingLetsTheSneakerProgress_LeavingExposes()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;

            // Cocoa flops by the human, Cheddar stands in the steak lane: the sneak progresses.
            // Driven against a real-time deadline so enough deltaTime accumulates for the human's
            // attention to build (per-frame dt in headless batchmode is tiny).
            float deadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < deadline)
            {
                _cocoa.transform.position = _game.TableHumanZone;
                _cheddar.transform.position = _game.TableStealZone;
                yield return null;
                if (_game.TableStealthPuzzle.SneakProgress > 0f || _game.TableStealthPuzzle.Solved) break;
            }
            Assert.IsTrue(_game.TableStealthPuzzle.SneakProgress > 0f || _game.TableStealthPuzzle.Solved,
                "With Cocoa flopping and Cheddar in the steak lane, the sneak advances.");

            if (!_game.TableStealthPuzzle.Solved)
            {
                // Cocoa gets up and wanders off; Cheddar keeps sneaking in the open -> spotted.
                int before = _game.TableStealthPuzzle.Exposures;
                deadline = Time.realtimeSinceStartup + 4f;
                while (Time.realtimeSinceStartup < deadline)
                {
                    _cocoa.transform.position = new Vector3(_game.TableHumanZone.x + 40f, _game.TableHumanZone.y, 0f);
                    _cheddar.transform.position = _game.TableStealZone;
                    yield return null;
                    if (_game.TableStealthPuzzle.Exposures > before || _game.TableStealthPuzzle.Solved) break;
                }
                Assert.IsTrue(_game.TableStealthPuzzle.Exposures > before || _game.TableStealthPuzzle.Solved,
                    "Sneaking after Cocoa stops distracting gets the pair spotted.");
            }
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
