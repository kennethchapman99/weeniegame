using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class EagleShadowPanicPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator EagleShadowPanic_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(26, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.EagleShadowPanic)
                {
                    found = true;
                    break;
                }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Eagle Shadow Panic should be reachable from mission select.");
            Assert.AreEqual("Eagle Shadow Panic", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_ClearPath_HideRescueUnitedFront()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.EagleShadowPanic, game.ActiveMissionVariant);
            Assert.AreEqual("Eagle Shadow Panic", game.ActiveMissionName);
            Assert.AreEqual("eagle_shadow_panic", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Hide from the eagle shadow"));

            int scoreBeforeHides = game.Score;
            game.ForceEagleShadowSafeHide();
            game.ForceEagleShadowSafeHide();
            yield return null;

            Assert.AreEqual(2, game.EagleShadowPanicState.SafeHides);
            Assert.AreEqual(0, game.EagleShadowPanicState.Exposures);
            Assert.IsTrue(game.EagleShadowPanicState.RescueObjectiveActive);
            Assert.Greater(game.Score, scoreBeforeHides, "Safe hides should award score, not penalize.");
            Assert.That(game.ObjectiveLabel, Does.Contain("snatched Cheddar"));

            game.ForceEagleShadowRescue(DogId.Cocoa);
            yield return null;

            Assert.IsTrue(game.EagleShadowPanicState.RescueComplete);
            Assert.IsTrue(game.EagleRescuePuzzle.Freed);
            Assert.That(game.ObjectiveLabel, Does.Contain("United-front"));

            game.ForceEagleShadowUnitedFront();
            yield return null;

            Assert.IsTrue(game.EagleShadowPanicState.UnitedFrontComplete);
            var controller = (EagleShadowPanicMissionController)game.ActiveMissionController;
            Assert.IsTrue(controller.IsPresentingSuccessfulOutcome,
                "The united bark should hold a live eagle-retreat payoff before the end screen.");
            controller.ForceFinishSuccessPresentation();
            yield return null;
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.AreEqual(GameManager.FlowState.EndScreen, game.CurrentFlow);
            Assert.IsTrue(game.RuntimeSnapshot.IsComplete);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Backyard Defenders"));
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_Rescue_WiggleOpensWindow_PullInWindowFreesHim()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;
            game.ForceEagleShadowSafeHide();
            game.ForceEagleShadowSafeHide();
            yield return null;
            Assert.IsTrue(game.EagleShadowPanicState.RescueObjectiveActive);

            int needed = game.EagleRescuePuzzle.PullsNeeded;
            for (int i = 0; i < needed; i++)
            {
                game.ForceEagleShadowWiggle();              // Cheddar cracks the grip open
                Assert.IsTrue(game.EagleRescuePuzzle.WindowOpen);
                game.ForceEagleShadowPull();                // Cocoa pulls in the window
            }

            Assert.AreEqual(needed, game.EagleRescuePuzzle.Pulls);
            Assert.AreEqual(0, game.EagleRescuePuzzle.MissedPulls);
            Assert.IsTrue(game.EagleRescuePuzzle.Freed);
            Assert.IsTrue(game.EagleShadowPanicState.RescueComplete);
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_Rescue_PullWithNoWindow_IsAMistimedMiss()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;
            game.ForceEagleShadowSafeHide();
            game.ForceEagleShadowSafeHide();
            yield return null;

            // Wiggle to open the window, then let it fully re-tighten before pulling.
            game.ForceEagleShadowWiggle();
            game.ForceEagleRescueAdvance(5f);
            Assert.IsFalse(game.EagleRescuePuzzle.WindowOpen);
            game.ForceEagleShadowPull();

            Assert.AreEqual(0, game.EagleRescuePuzzle.Pulls);
            Assert.AreEqual(1, game.EagleRescuePuzzle.MissedPulls);
            Assert.IsFalse(game.EagleRescuePuzzle.Freed);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome,
                "A mistimed pull must be a recoverable coach beat, not a hard fail.");
            Assert.That(game.LastJuiceLabel, Does.Contain("MISTIMED"), "The mistimed pull must produce a visible reaction.");
            Assert.AreEqual(ArenaFeedbackCatalog.SquirrelStealMiss, game.LastAudioCueRequested,
                "The mistimed pull must produce an audible reaction.");

            // Still recoverable: correctly-timed wiggle+pull cycles still free the dog afterward.
            game.ForceEagleShadowRescue();
            Assert.IsTrue(game.EagleRescuePuzzle.Freed);
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_FailPath_ExposuresEndMission()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;

            game.ForceEagleShadowExposure();
            game.ForceEagleShadowExposure();
            game.ForceEagleShadowExposure();
            yield return null;

            Assert.AreEqual(3, game.EagleShadowPanicState.Exposures);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Shadow Trouble"));
            Assert.That(game.EndReasonLabel, Does.Contain("open"));
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_Replay_ResetsThreatSweepRuntimeState()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;
            game.ForceEagleShadowSafeHide();
            game.ForceEagleShadowExposure();
            yield return null;

            Assert.Greater(game.EagleShadowPanicState.SafeHides + game.EagleShadowPanicState.Exposures, 0);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.EagleShadowPanic, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.EagleShadowPanicState.SafeHides);
            Assert.AreEqual(0, game.EagleShadowPanicState.Exposures);
            Assert.IsFalse(game.EagleShadowPanicState.RescueObjectiveActive);
            Assert.IsFalse(game.EagleShadowPanicState.RescueComplete);
            Assert.IsFalse(game.EagleShadowPanicState.UnitedFrontComplete);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_SweepGeometry_CoverHidesAndOpenGroundGetsGrabbed()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;

            var controller = (EagleShadowPanicMissionController)game.ActiveMissionController;
            var zones = game.EagleCoverZones;
            Assert.Greater(zones.Length, 0);
            Assert.IsTrue(controller.InShadowRun);

            // Both dogs tucked into a bush with the shadow dragged straight over them: nobody grabbed.
            var cover = zones[0];
            _cheddar.transform.position = new Vector3(cover.x, cover.y, 0f);
            _cocoa.transform.position = new Vector3(cover.x, cover.y, 0f);
            game.ForceEagleShadowSweepPass();
            yield return null;

            Assert.IsFalse(controller.ShadowRun.GrabActive, "The shadow passing over a bush must not grab a hidden dog.");
            Assert.AreEqual(0, game.EagleShadowPanicState.Exposures);

            // Out on the open lawn the shadow grabs - a live rescue chance, not an instant strike.
            Vector2 open = OpenLawnSpot(controller);
            _cheddar.transform.position = open;
            _cocoa.transform.position = open + Vector2.right * 8f;
            game.ForceEagleShadowSweepPass();

            Assert.IsTrue(controller.ShadowRun.GrabActive, "A dog caught in the open should be grabbed by the talons.");
            Assert.AreEqual(DogId.Cheddar, controller.ShadowRun.GrabbedDog);
            Assert.AreEqual(0, game.EagleShadowPanicState.Exposures,
                "A grab is a rescue window; only a carry-off costs a strike.");

            // Nobody came to help: the eagle carries him off and the pair takes a strike.
            controller.ForceCarryOff();
            yield return null;

            Assert.IsFalse(controller.ShadowRun.GrabActive);
            Assert.AreEqual(1, game.EagleShadowPanicState.Exposures);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_LiveGrab_WiggleAndPartnerPullFreeTheDogWithoutAStrike()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;

            var controller = (EagleShadowPanicMissionController)game.ActiveMissionController;
            Vector2 open = OpenLawnSpot(controller);
            _cheddar.transform.position = open;
            _cocoa.transform.position = open + Vector2.right * 8f;
            game.ForceEagleShadowSweepPass();
            Assert.IsTrue(controller.ShadowRun.GrabActive);

            // Cocoa sprints in; a pull before Cheddar's wiggle is mistimed.
            _cocoa.transform.position = controller.LiveGrabPosition + Vector2.right * 1f;
            _cocoa.Interact();
            Assert.That(game.LastJuiceLabel, Does.Contain("MISTIMED"));
            Assert.IsTrue(controller.ShadowRun.GrabActive);

            int needed = controller.ShadowRun.GrabRescue.PullsNeeded;
            for (int i = 0; i < needed; i++)
            {
                _cheddar.Interact();   // the grabbed dog wiggles the grip open
                Assert.IsTrue(controller.ShadowRun.GrabRescue.WindowOpen);
                _cocoa.transform.position = controller.LiveGrabPosition + Vector2.right * 1f;
                _cocoa.Interact();     // partner pulls inside the window
                yield return null;
            }

            Assert.IsFalse(controller.ShadowRun.GrabActive, "Wiggle + in-window pulls should free the grabbed dog.");
            Assert.AreEqual(0, game.EagleShadowPanicState.Exposures, "A rescued grab costs no strike.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_DecoyBark_FromCoverStallsTheShadow_ButBarkingInTheOpenAlertsIt()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;

            var controller = (EagleShadowPanicMissionController)game.ActiveMissionController;
            var cover = game.EagleCoverZones[0];
            _cheddar.transform.position = new Vector3(cover.x, cover.y, 0f);
            _cocoa.transform.position = new Vector3(cover.x, cover.y, 0f);

            _cocoa.Bark();
            Assert.IsTrue(controller.ShadowRun.DecoyActive, "Cocoa's bark from a bush should decoy the eagle.");
            Assert.Greater(controller.ShadowRun.DecoyCooldownRemaining, 0f, "The decoy should go on cooldown.");
            Assert.AreEqual("DECOY!", game.LastJuiceLabel);

            yield return null;
            Vector2 open = OpenLawnSpot(controller);
            _cocoa.transform.position = open;
            _cocoa.Bark();
            Assert.IsTrue(controller.ShadowRun.EagleAlerted, "Barking out in the open should draw the eagle in faster.");
            Assert.That(game.LastJuiceLabel, Does.Contain("HEARD"));
        }

        private static Vector2 OpenLawnSpot(EagleShadowPanicMissionController controller)
        {
            // Lower-middle lawn: well clear of every bush and the pool.
            foreach (var candidate in new[] { new Vector2(8f, -24f), new Vector2(-8f, -24f), new Vector2(12f, 20f), new Vector2(40f, -20f) })
                if (!controller.ShadowRun.IsInCover(candidate) && !BackyardPoolZone.WaterRect.Contains(candidate))
                    return candidate;
            Assert.Fail("No open-lawn test spot found.");
            return Vector2.zero;
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_TalonGripOverlay_DoesNotBleedIntoTheNextMission()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;
            game.ForceEagleShadowSafeHide();
            game.ForceEagleShadowSafeHide();
            yield return null;

            var attachment = game.SquirrelObject.GetComponent<MissionPropArtAttachment>();
            Assert.IsNotNull(attachment,
                "The snatch/rescue beat should have promoted a talon-grip overlay onto the squirrel actor.");
            Assert.IsNotEmpty(attachment.ResourcePath);

            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            Assert.IsEmpty(attachment.ResourcePath,
                "Switching missions should clear the squirrel's leftover talon-grip overlay, not leave " +
                "Eagle Shadow Panic's art on a completely different mission's squirrel.");
            var fallback = MissionPropArt.FindFallbackRenderer(game.SquirrelObject);
            Assert.IsNotNull(fallback);
            Assert.AreEqual(1f, fallback.color.a, 0.001f,
                "The squirrel's own body should be back at full opacity once the overlay clears.");
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_Replay_ResetsCoverArtInsteadOfStartingAlreadySpotted()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;
            game.ForceEagleShadowExposure();
            yield return null;

            Assert.That(game.EagleCoverArtResourcePath(0), Does.Contain("spotted"),
                "Sanity check: an exposure should paint every cover zone with the Spotted art.");

            game.Restart();
            yield return null;

            Assert.That(game.EagleCoverArtResourcePath(0), Does.Not.Contain("spotted"),
                "A fresh attempt should not open with cover already showing last run's Spotted art.");
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
