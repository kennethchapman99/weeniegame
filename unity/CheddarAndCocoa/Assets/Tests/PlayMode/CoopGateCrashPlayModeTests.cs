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
    /// The Gate Crash mission wires the Hold-and-Release co-op puzzle into the real mission flow:
    /// Cocoa anchors the gate while Cheddar squeezes through, and letting go mid-squeeze snaps it.
    /// </summary>
    public sealed class CoopGateCrashPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator GateCrash_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(17, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.GateCrash) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "Gate Crash should be reachable from mission select.");
            Assert.AreEqual("Gate Crash", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator GateCrash_ClearPath_HoldThenSqueezeThrough()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;

            Assert.AreEqual("gate_crash", _game.RuntimeSnapshot.MissionId);
            Assert.That(_game.ObjectiveLabel, Does.Contain("gate"));

            _game.ForceGateHold(true);     // Cocoa braces the gate
            _game.ForceGateCross(1.0f);    // Cheddar squeezes through (cross needs 0.8s)

            Assert.IsTrue(_game.GateCrashPuzzle.Solved);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Squeezed Through"));
        }

        [UnityTest]
        public IEnumerator GateCrash_FailPath_GateSnapsTooManyTimes()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;

            for (int i = 0; i < 4; i++)
            {
                _game.ForceGateHold(true);
                _game.ForceGateCross(0.4f); // partial squeeze
                _game.ForceGateHold(false); // Cocoa lets go -> snap
            }

            Assert.AreEqual(4, _game.GateCrashPuzzle.Snaps);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Gate Trouble"));
        }

        [UnityTest]
        public IEnumerator GateCrash_Replay_ResetsThePuzzle()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;
            _game.ForceGateHold(true);
            _game.ForceGateCross(0.4f);
            _game.ForceGateHold(false); // a snap
            Assert.Greater(_game.GateCrashPuzzle.Snaps, 0);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.GateCrash, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.GateCrashPuzzle.Snaps);
            Assert.AreEqual(0f, _game.GateCrashPuzzle.CrossProgress);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator GateCrash_PositionDriven_HoldingLetsTheCrosserProgress_LeavingSnaps()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;

            // Cocoa stands in the hold zone, Cheddar in the cross corridor: the squeeze progresses.
            _cocoa.transform.position = _game.GateHoldZone;
            _cheddar.transform.position = _game.GateCrossZone;
            for (int i = 0; i < 8; i++)
            {
                _cocoa.transform.position = _game.GateHoldZone;
                _cheddar.transform.position = _game.GateCrossZone;
                yield return null;
                if (_game.GateCrashPuzzle.CrossProgress > 0f || _game.GateCrashPuzzle.Solved) break;
            }
            Assert.IsTrue(_game.GateCrashPuzzle.CrossProgress > 0f || _game.GateCrashPuzzle.Solved,
                "With Cocoa holding and Cheddar in the corridor, the squeeze advances.");

            if (!_game.GateCrashPuzzle.Solved)
            {
                // Cocoa wanders off mid-squeeze -> the gate snaps.
                _cocoa.transform.position = new Vector3(_game.GateHoldZone.x + 40f, _game.GateHoldZone.y, 0f);
                yield return null;
                yield return null;
                Assert.GreaterOrEqual(_game.GateCrashPuzzle.Snaps, 1, "Cocoa leaving mid-squeeze snaps the gate.");
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
