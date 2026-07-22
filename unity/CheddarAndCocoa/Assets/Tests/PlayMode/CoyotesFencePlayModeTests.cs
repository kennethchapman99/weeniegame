using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class CoyotesFencePlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator CoyotesFence_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(24, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.CoyotesFence)
                {
                    found = true;
                    break;
                }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Coyotes at the Fence should be reachable from mission select.");
            Assert.AreEqual("Coyotes at the Fence", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator CoyotesFence_ClearPath_BarkPinFillThenBlockFinalPush()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.CoyotesFence, game.ActiveMissionVariant);
            Assert.AreEqual("Coyotes at the Fence", game.ActiveMissionName);
            Assert.AreEqual("coyotes_fence", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Cocoa: BARK-pin"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Cheddar: fill dirt"));

            // Repair without partner bark pressure should be rejected (no progress).
            game.ForceCoyoteRepair(DogId.Cheddar);
            yield return null;
            Assert.AreEqual(0, game.CoyotesFenceState.GapsRepaired);

            // Bark pressure then fill, three times, to reach the final-push phase.
            for (int i = 0; i < 3; i++)
            {
                game.ForceCoyoteBarkPressure(DogId.Cocoa);
                game.ForceCoyoteRepair(DogId.Cheddar);
                yield return null;
            }

            Assert.AreEqual(3, game.CoyotesFenceState.GapsRepaired);
            Assert.IsTrue(game.CoyotesFenceState.ReadyForFinalPressure(3));
            Assert.That(game.ObjectiveLabel, Does.Contain("final coyote push"));

            game.ForceCoyoteFinalBlock();
            yield return null;

            Assert.IsTrue(game.CoyotesFenceState.FinalPressureComplete);
            var controller = (CoyotesFenceMissionController)game.ActiveMissionController;
            Assert.IsTrue(controller.IsPresentingSuccessfulOutcome,
                "The final block should hold a live coyote-retreat payoff before the end screen.");
            controller.ForceFinishSuccessPresentation();
            yield return null;
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.AreEqual(GameManager.FlowState.EndScreen, game.CurrentFlow);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Fence Guardians"));
        }

        [UnityTest]
        public IEnumerator CoyotesFence_CocoaPinsInRangeAndCheddarRepairsBeforeTheOpeningCloses()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;

            var controller = (CoyotesFenceMissionController)_game.ActiveMissionController;
            _cheddar.transform.position = _game.PredatorObject.transform.position;
            _cheddar.Bark();
            yield return null;
            Assert.IsFalse(controller.PressureHeld,
                "Cheddar cannot replace Cocoa's steady fence-pin role.");
            Assert.That(_game.LastJuiceLabel, Does.Contain("COCOA"), "The wrong-dog bark must produce a visible coach beat.");
            Assert.AreEqual(ArenaFeedbackCatalog.UiButtonDisabled, _game.LastAudioCueRequested,
                "The wrong-dog bark must produce an audible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            _cocoa.transform.position = _game.PredatorObject.transform.position + Vector3.right * 20f;
            _cocoa.Bark();
            yield return null;
            Assert.IsFalse(controller.PressureHeld,
                "A bark across the yard must not pin the coyote.");

            _cocoa.transform.position = _game.PredatorObject.transform.position;
            _cocoa.Bark();
            yield return null;
            Assert.IsTrue(controller.PressureHeld);

            Vector2 weakSpot = _game.FenceGaps[_game.CoyotesFenceState.ActiveGapIndex];
            _cocoa.transform.position = weakSpot;
            _cocoa.Interact();
            yield return null;
            Assert.AreEqual(0, _game.CoyotesFenceState.GapsRepaired,
                "Cocoa cannot abandon her pin and complete Cheddar's dirt-fill role herself.");

            _cheddar.transform.position = weakSpot;
            _cheddar.Interact();
            yield return null;
            Assert.AreEqual(1, _game.CoyotesFenceState.GapsRepaired);

            _game.ForceCoyoteBarkPressure(DogId.Cocoa);
            Assert.IsTrue(controller.PressureHeld);
            controller.ForcePressureTimeout();
            Assert.IsFalse(controller.PressureHeld);
            Assert.AreEqual(ArenaFeedbackCatalog.ScorePenalty, _game.LastAudioCueRequested,
                "S5.2: a pin expiring was a silent miss (visual PIN LOST only) - must fire a cue now.");
            _game.ForceCoyoteRepair(DogId.Cheddar);
            Assert.AreEqual(1, _game.CoyotesFenceState.GapsRepaired,
                "Cheddar must recover by waiting for Cocoa to repin after the opening closes.");
        }

        [UnityTest]
        public IEnumerator CoyotesFence_FakeSnackLure_FiresDeterministically()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;

            Assert.IsFalse(game.CoyotesFenceState.FakeSnackActive);
            game.ForceCoyoteFakeSnack();
            yield return null;

            Assert.IsTrue(game.CoyotesFenceState.FakeSnackActive);
            Assert.That(game.ObjectiveLabel, Does.Contain("fake snack lure"));

            // Barking the coyote resolves the lure instead of taking the bait.
            game.ForceCoyoteBarkPressure(DogId.Cocoa);
            yield return null;
            Assert.IsFalse(game.CoyotesFenceState.FakeSnackActive);
        }

        [UnityTest]
        public IEnumerator CoyotesFence_FailPath_BreachesEndMission()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;

            game.ForceCoyoteBreach();
            game.ForceCoyoteBreach();
            game.ForceCoyoteBreach();
            yield return null;

            Assert.AreEqual(3, game.CoyotesFenceState.Breaches);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Needs More Patrols"));
            Assert.That(game.EndReasonLabel, Does.Contain("breached"));
        }

        [UnityTest]
        public IEnumerator CoyotesFence_Replay_ResetsPatrolRuntimeState()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;
            game.ForceCoyoteBarkPressure(DogId.Cocoa);
            game.ForceCoyoteRepair(DogId.Cheddar);
            game.ForceCoyoteBreach();
            yield return null;

            Assert.Greater(game.CoyotesFenceState.GapsRepaired + game.CoyotesFenceState.Breaches, 0);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.CoyotesFence, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.CoyotesFenceState.GapsRepaired);
            Assert.AreEqual(0, game.CoyotesFenceState.Breaches);
            Assert.AreEqual(0, game.CoyotesFenceState.BarkPressures);
            Assert.IsFalse(game.CoyotesFenceState.FakeSnackActive);
            Assert.IsFalse(game.CoyotesFenceState.FinalPressureComplete);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator CoyotesFence_ProwlReach_BarkPressureDrivesOffElseBreaches()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;

            Assert.Greater(game.FenceGaps.Length, 0);

            // Holding bark pressure when the coyote reaches the gap drives it off (no breach).
            game.ForceCoyoteBarkPressure(DogId.Cocoa);
            game.ForceCoyoteProwlReach();
            yield return null;
            Assert.AreEqual(0, game.CoyotesFenceState.Breaches);
            Assert.AreEqual(ArenaFeedbackCatalog.TugRescueSuccess, game.LastAudioCueRequested,
                "S5.2: driving the coyote back was a silent success beat (visual DRIVEN BACK only) - must fire a cue now.");

            // Reaching an unguarded gap (pressure already spent) breaches the fence.
            game.ForceCoyoteProwlReach();
            yield return null;
            Assert.AreEqual(1, game.CoyotesFenceState.Breaches);
        }

        [UnityTest]
        public IEnumerator CoyotesFence_Replay_ResetsGapArtInsteadOfLeavingStaleBreachedOrRepairedSprites()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;

            game.ForceCoyoteBarkPressure(DogId.Cocoa);
            game.ForceCoyoteRepair(DogId.Cheddar); // gap 0 -> repaired art
            yield return null;
            game.ForceCoyoteBreach(); // gap 1 -> breached art
            yield return null;

            Assert.That(game.CoyoteGapArtResourcePath(0), Does.Contain("repaired"),
                "Sanity check: repairing a gap should promote its repaired art.");
            Assert.That(game.CoyoteGapArtResourcePath(1), Does.Contain("breached"),
                "Sanity check: breaching a gap should promote its breached art.");

            game.Restart();
            yield return null;

            Assert.That(game.CoyoteGapArtResourcePath(0), Does.Not.Contain("repaired"),
                "A fresh attempt should not start with gap 0 already showing last run's repaired art.");
            Assert.That(game.CoyoteGapArtResourcePath(1), Does.Not.Contain("breached"),
                "A fresh attempt should not start with gap 1 already showing last run's breached art.");
        }

        private IEnumerator LoadArena()
        {
            _game = null;
            _cheddar = null;
            _cocoa = null;
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
