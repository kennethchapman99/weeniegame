using System.Collections;
using CheddarAndCocoa.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CheddarAndCocoa.Tests
{
    public sealed class PeeBreakPlayModeTests
    {
        private GameManager _game;
        private PeeBreakMissionController Controller => _game.PeeBreakController;

        [UnityTest]
        public IEnumerator StartState_IsControllerOwnedReadableAndLowPressure()
        {
            yield return LoadMission();
            Assert.IsInstanceOf<PeeBreakMissionController>(_game.ActiveMissionController);
            Assert.AreEqual(PeeBreakMissionController.Beat.DoorStare, Controller.CurrentBeat);
            Assert.AreEqual(SocialStimulus.DoorStare, Controller.Required);
            Assert.Less(Controller.Bladder, 0.2f);
            Assert.AreEqual(1f, Controller.PhoneBattery, 0.001f);
            Assert.IsFalse(Controller.DoorOpen);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual("operation_pee_break", _game.RuntimeSnapshot.MissionId);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa"));
            Assert.IsNotNull(GameObject.Find("PeeBreakTeenager"));
            Assert.IsNotNull(GameObject.Find("PeeBreakDoor"));
        }

        [UnityTest]
        public IEnumerator ExactCombosAdvanceAndChangeRoleLocks()
        {
            yield return LoadMission();
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 1f);
            Assert.AreEqual(PeeBreakMissionController.Beat.LeashMessage, Controller.CurrentBeat);
            Assert.AreEqual(SocialStimulus.DoorStare | SocialStimulus.PresentLeash, Controller.Required);
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 1f);
            Assert.AreEqual(0f, Controller.Puzzle.Comprehension, 0.001f);
            Assert.Greater(Controller.Puzzle.Confusion, 0f);
            _game.ForcePeeBreakAdvance(Controller.Required, 2.1f);
            Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);
            Assert.AreEqual(SocialStimulus.UnplugCharger | SocialStimulus.BlockHallway, Controller.Required);
        }

        [UnityTest]
        public IEnumerator OffMessageMisreadIsFunnyRecoverableFailure()
        {
            yield return LoadMission();
            _game.ForcePeeBreakAdvance(SocialStimulus.BarkRhythm, 4f);
            Assert.AreEqual(1, Controller.Misreads);
            Assert.AreEqual(PeeBreakMissionController.Beat.DoorStare, Controller.CurrentBeat);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0f, Controller.Puzzle.Comprehension, 0.001f);
            Assert.IsTrue(LogContains("PeeBreakMisread"));
        }

        [UnityTest]
        public IEnumerator ChargerGambitDropsComprehensionWhenBlockDrops()
        {
            yield return LoadMission();
            AdvanceToCharger();
            _game.ForcePeeBreakAdvance(Controller.Required, 1f);
            float heldProgress = Controller.Puzzle.Comprehension;
            Assert.Greater(heldProgress, 0f);
            _game.ForcePeeBreakAdvance(SocialStimulus.UnplugCharger, 0.5f);
            Assert.Less(Controller.Puzzle.Comprehension, heldProgress);
            Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);
        }

        [UnityTest]
        public IEnumerator ClimaxOpensDoorClearsAndReplayResetsEverything()
        {
            yield return LoadMission();
            AdvanceToCharger();
            _game.ForcePeeBreakAdvance(Controller.Required, 2.6f);
            Assert.AreEqual(PeeBreakMissionController.Beat.UnitedBark, Controller.CurrentBeat);
            _game.ForcePeeBreakAdvance(Controller.Required, 2.3f);
            yield return null;
            Assert.IsTrue(Controller.DoorOpen);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.AreEqual(GameManager.FlowState.EndScreen, _game.CurrentFlow);
            Assert.IsTrue(LogContains("PeeBreakDoorOpen"));
            _game.Restart();
            yield return null;
            Assert.AreEqual(PeeBreakMissionController.Beat.DoorStare, Controller.CurrentBeat);
            Assert.AreEqual(0, Controller.Misreads);
            Assert.AreEqual(0, Controller.CompletedBeats);
            Assert.Less(Controller.Bladder, 0.2f);
            Assert.IsFalse(Controller.DoorOpen);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        private void AdvanceToCharger()
        {
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 1f);
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare | SocialStimulus.PresentLeash, 2.1f);
            Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);
        }

        private IEnumerator LoadMission()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
            _game.StartMission(GameManager.MissionVariant.OperationPeeBreak);
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
