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
    /// The Rube Goldberg mission wires the Chaos-Machine co-op puzzle into the real mission flow: the
    /// dogs pre-position at their junctions and pull the lever, then the cascade runs itself - but each
    /// junction has a brief window where its owner dog must be in position or the machine misfires and
    /// stalls there. A re-pull resumes from the stall; too many misfires fail the mission.
    /// </summary>
    public sealed class CoopChaosMachinePlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator Chaos_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(22, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.ChaosMachine) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "The Rube Goldberg should be reachable from mission select.");
            Assert.AreEqual("The Rube Goldberg", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator Chaos_RunsThroughDedicatedController()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ChaosMachine);
            yield return null;

            Assert.IsInstanceOf<ChaosMachineMissionController>(
                _game.ActiveMissionController,
                "The Rube Goldberg must run entirely through its own IMissionController.");
            Assert.AreEqual(GameManager.MissionVariant.ChaosMachine, _game.ActiveMissionController.Variant);
            Assert.AreSame(_game.ChaosMachineController.Puzzle, _game.ChaosMachinePuzzle);
        }

        [UnityTest]
        public IEnumerator Chaos_ClearPath_LeverThenAssistEveryJunction()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ChaosMachine);
            yield return null;

            Assert.AreEqual("chaos_machine", _game.RuntimeSnapshot.MissionId);
            int stages = _game.ChaosMachinePuzzle.StageCount;

            _game.ForceChaosTrigger();
            Assert.IsTrue(_game.ChaosMachinePuzzle.Running);
            for (int i = 0; i < stages; i++)
                _game.ForceChaosAdvance(0.5f, assisting: true); // helper in position at each junction

            Assert.IsTrue(_game.ChaosMachinePuzzle.Solved);
            Assert.AreEqual(0, _game.ChaosMachinePuzzle.Stalls);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Cascade Complete"));
        }

        [UnityTest]
        public IEnumerator Chaos_MissedWindow_MisfiresAndStalls()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ChaosMachine);
            yield return null;

            _game.ForceChaosTrigger();
            _game.ForceChaosAdvance(4f, assisting: false); // nobody at the junction -> window expires

            Assert.AreEqual(0, _game.ChaosMachinePuzzle.Stage);
            Assert.AreEqual(1, _game.ChaosMachinePuzzle.Stalls);
            Assert.AreEqual(0, _game.ChaosMachinePuzzle.StalledStage);
            Assert.IsFalse(_game.ChaosMachinePuzzle.Running);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator Chaos_RePull_ResumesFromTheStall()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ChaosMachine);
            yield return null;

            _game.ForceChaosTrigger();
            _game.ForceChaosAdvance(4f, assisting: false); // stall at stage 0
            Assert.AreEqual(1, _game.ChaosMachinePuzzle.Stalls);

            _game.ForceChaosTrigger();                       // re-pull resumes
            Assert.IsTrue(_game.ChaosMachinePuzzle.Running);
            _game.ForceChaosAdvance(0.5f, assisting: true);
            Assert.AreEqual(1, _game.ChaosMachinePuzzle.Stage);
        }

        [UnityTest]
        public IEnumerator Chaos_FailPath_TooManyMisfires()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ChaosMachine);
            yield return null;

            for (int i = 0; i < 4; i++)
            {
                _game.ForceChaosTrigger();
                _game.ForceChaosAdvance(4f, assisting: false);
            }

            Assert.AreEqual(4, _game.ChaosMachinePuzzle.Stalls);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Misfired"));
        }

        [UnityTest]
        public IEnumerator Chaos_Replay_ResetsThePuzzle()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ChaosMachine);
            yield return null;
            _game.ForceChaosTrigger();
            _game.ForceChaosAdvance(4f, assisting: false); // a misfire
            Assert.Greater(_game.ChaosMachinePuzzle.Stalls, 0);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.ChaosMachine, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.ChaosMachinePuzzle.Stage);
            Assert.AreEqual(0, _game.ChaosMachinePuzzle.Stalls);
            Assert.IsFalse(_game.ChaosMachinePuzzle.Running);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator Chaos_PositionDriven_LeverPullThenJunctionAssist()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ChaosMachine);
            yield return null;

            var cheddarBody = _cheddar.GetComponent<Rigidbody2D>();
            var cocoaBody = _cocoa.GetComponent<Rigidbody2D>();

            // A dog at the lever pulls it and the cascade goes live.
            for (int i = 0; i < 6; i++)
            {
                _cheddar.transform.position = _game.ChaosLeverZone;
                _cocoa.transform.position = new Vector3(_game.ChaosLeverZone.x, _game.ChaosLeverZone.y - 40f, 0f);
                if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
                if (cocoaBody != null) cocoaBody.linearVelocity = Vector2.zero;
                yield return null;
            }
            Assert.IsTrue(_game.ChaosMachinePuzzle.Running, "A dog at the lever should start the cascade.");

            // The first junction's owner covers it; the cascade rolls through.
            ChainActor owner = _game.ChaosJunctionOwner(0);
            DogController ownerDog = owner == ChainActor.Cheddar ? _cheddar : _cocoa;
            DogController otherDog = owner == ChainActor.Cheddar ? _cocoa : _cheddar;
            for (int i = 0; i < 10; i++)
            {
                ownerDog.transform.position = _game.ChaosJunctionSpot(0);
                otherDog.transform.position = new Vector3(_game.ChaosJunctionSpot(0).x - 40f, _game.ChaosJunctionSpot(0).y, 0f);
                if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
                if (cocoaBody != null) cocoaBody.linearVelocity = Vector2.zero;
                yield return null;
            }
            Assert.GreaterOrEqual(_game.ChaosMachinePuzzle.Stage, 1, "The owner covering the live junction should roll the cascade on.");
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
