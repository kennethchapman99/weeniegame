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
            Assert.AreEqual(23, _game.MissionSelectOptionCount);

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
        public IEnumerator GateCrash_RunsThroughDedicatedController()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;

            Assert.IsInstanceOf<GateCrashMissionController>(
                _game.ActiveMissionController,
                "Gate Crash must run entirely through its own IMissionController.");
            Assert.AreEqual(GameManager.MissionVariant.GateCrash, _game.ActiveMissionController.Variant);
            Assert.AreSame(_game.GateCrashController.Puzzle, _game.GateCrashPuzzle);
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
            Assert.IsInstanceOf<IMissionSuccessPresentationController>(_game.GateCrashController);
            Assert.IsTrue(_game.GateCrashController.IsPresentingSuccessfulOutcome,
                "The claimed toy should remain visible before the result card appears.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Toy rescued"));
            foreach (var feedback in _game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, feedback.CurrentPose,
                    "Both dogs should show an animated proud read during the held payoff, not frozen dogs.");
            _game.ForceGateSuccessPresentationComplete();
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Squeezed Through"));
            Assert.AreNotEqual("MVP: awaiting dog heroics", _game.MvpLabel,
                "Holding the gate and squeezing through should credit both dogs toward the MVP stat.");
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
        public IEnumerator GateCrash_ClearAndFail_BothKickTheSharedCameraShake()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;

            Assert.AreEqual(0, _game.ShakeRequestCount, "No shake should fire mid-mission.");

            _game.ForceGateHold(true);
            _game.ForceGateCross(1.0f);
            _game.ForceGateSuccessPresentationComplete();
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.AreEqual(1, _game.ShakeRequestCount, "Mission clear should kick a cosmetic camera shake.");
            float clearShake = _game.LastShakeMagnitude;
            Assert.Greater(clearShake, 0f);

            _game.Restart();
            yield return null;
            for (int i = 0; i < 4; i++)
            {
                _game.ForceGateHold(true);
                _game.ForceGateCross(0.4f);
                _game.ForceGateHold(false);
            }
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            // 1 (clear) + 4 (one snap-jolt per snap in the loop above) + 1 (the fail-end shake).
            Assert.AreEqual(6, _game.ShakeRequestCount,
                "Mission fail should kick its own end-of-round shake on top of each snap's jolt.");
            Assert.Greater(_game.LastShakeMagnitude, clearShake,
                "A failed run should jolt the camera harder than a clean clear.");
        }

        [UnityTest]
        public IEnumerator GateCrash_FlawlessClear_ShakesHarderThanAScrappyClear()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;

            // Scrappy clear: one snap first, then complete.
            _game.ForceGateHold(true);
            _game.ForceGateCross(0.4f);
            _game.ForceGateHold(false); // snap
            _game.ForceGateHold(true);
            _game.ForceGateCross(1.0f);
            _game.ForceGateSuccessPresentationComplete();
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsFalse(_game.LastRoundFlawless, "A snap before completing should not count as flawless.");
            float scrappyShake = _game.LastShakeMagnitude;

            _game.Restart();
            yield return null;

            // Flawless clear: no snaps at all.
            _game.ForceGateHold(true);
            _game.ForceGateCross(1.0f);
            _game.ForceGateSuccessPresentationComplete();
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.LastRoundFlawless);
            Assert.Greater(_game.LastShakeMagnitude, scrappyShake,
                "A flawless clear should shake the camera harder than a clear with a snap.");
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
        public IEnumerator GateCrash_CocoaMustDeliberatelyAnchor_ThenHoldingLetsCheddarProgress_AndLeavingSnaps()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;

            _cheddar.transform.position = _game.GateHoldZone;
            _cheddar.Interact();
            Assert.IsFalse(_game.GateCrashController.AnchorEngaged,
                "Cheddar owns the squeeze route and cannot replace Cocoa's anchor role.");
            Assert.That(_game.LastCue, Does.Contain("Cocoa"));
            Assert.That(_game.LastJuiceLabel, Does.Contain("ANCHOR"), "The wrong-dog anchor attempt must produce a visible coach beat.");
            Assert.AreEqual(ArenaFeedbackCatalog.UiButtonDisabled, _game.LastAudioCueRequested,
                "The wrong-dog anchor attempt must produce an audible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            _cocoa.transform.position = _game.ArenaBounds.center;
            _cocoa.Interact();
            Assert.IsFalse(_game.GateCrashController.AnchorEngaged,
                "Cocoa must physically reach the gate before engaging the anchor.");

            // Proximity alone is not the co-op action: Cocoa must deliberately engage the anchor.
            _cocoa.transform.position = _game.GateHoldZone;
            _cheddar.transform.position = _game.GateCrossZone;
            yield return null;
            yield return null;
            Assert.IsFalse(_game.GateCrashController.AnchorEngaged);
            Assert.IsFalse(_game.GateCrashPuzzle.Held);
            Assert.AreEqual(0f, _game.GateCrashPuzzle.CrossProgress,
                "Standing on the gate marker must not silently perform Cocoa's anchor action.");

            _cocoa.Interact();
            Assert.IsTrue(_game.GateCrashController.AnchorEngaged);
            Assert.IsTrue(_game.GateCrashPuzzle.Held);
            Assert.IsTrue(HasWorldPop("COCOA ANCHORED"));
            Assert.AreEqual(ArenaFeedbackCatalog.TugRescueSuccess, _game.LastAudioCueRequested,
                "S5.2: Cocoa's anchor was a silent success beat (rumble but no audio) - must fire a cue now.");

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
                Assert.IsFalse(_game.GateCrashController.AnchorEngaged,
                    "After a snap, Cocoa must deliberately re-engage instead of auto-opening on return.");
            }
        }

        [UnityTest]
        public IEnumerator GateCrash_Snap_KicksAnImmediateShakeBeforeTheRoundEnds()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;

            Assert.AreEqual(0, _game.ShakeRequestCount, "No shake should fire before anything happens.");

            _game.ForceGateHold(true);
            _game.ForceGateCross(0.4f);
            _game.ForceGateHold(false); // one snap, mission still in progress (max is 4)

            Assert.AreEqual(1, _game.GateCrashPuzzle.Snaps);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome,
                "One snap should not end the round.");
            Assert.AreEqual(1, _game.ShakeRequestCount,
                "The snap itself should kick a small shake, independent of the end-of-round shake.");
            Assert.Less(_game.LastShakeMagnitude, 0.18f,
                "The in-mission snap jolt should read smaller than the end-of-round shakes.");
        }

        [UnityTest]
        public IEnumerator GateCrash_DoorOutrageGag_FiresWhenCheddarIdlesAtTheClosedGateAndStaysQuietWhileHeld()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;

            if (_cheddar.TryGetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>(out var input)) input.enabled = false;
            _cheddar.transform.position = _game.GateHoldZone;
            if (_cheddar.TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            yield return null;

            _game.GateCrashController.TrySpawnDoorOutrage();
            Assert.AreEqual(1, _game.GateCrashController.DoorOutrageCount,
                "Cheddar idling at the closed gate should fire the door-outrage gag.");

            // While Cocoa is bracing the gate open, the gate is no longer a personal insult.
            _game.ForceGateHold(true);
            _game.GateCrashController.TrySpawnDoorOutrage();
            Assert.AreEqual(1, _game.GateCrashController.DoorOutrageCount,
                "The gag should stay quiet while the gate is actually held open.");
        }

        /// <summary>
        /// CF2.6 (roster audit of CF1.6's finding #6, applied to Gate Crash's own tall gate art -
        /// scale (1.4, 4, 1) in BuildScene(), the same shape as Pee Break's pre-CF1.6 door).
        /// Unlike Pee Break's door (StationRange 2.25 was smaller than the ~2.48-unit gap, making
        /// the true floor spot mechanically unreachable), Gate Crash's HoldRange (4f) was always
        /// generous enough to tolerate a floor-level stand, so this is primarily a guidance/
        /// consistency fix rather than a "mechanically impossible" one - the couch-test-shaped bug
        /// here is Cocoa's arrow/breadcrumb (TryGetObjectiveTarget) pointing straight at the gate's
        /// own tall-center transform (_gate, == GateHoldZone), pulling her toward the halfway-up-
        /// the-gate spot even though the hold check itself would have tolerated the floor. This
        /// proves the guidance now targets the floor anchor instead (a distinct, meaningfully lower
        /// point that did NOT equal the old target before this fix), and that holding the anchor
        /// from that floor spot - not the tall center - is what the checks actually measure.
        /// </summary>
        [UnityTest]
        public IEnumerator GateCrash_AnchorGuidanceAndHoldAnchorToTheFloorNotTheTallGateCenter()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;

            var controller = _game.GateCrashController;
            float verticalGap = _game.GateHoldZone.y - controller.GateFloorAnchor.y;
            Assert.Greater(verticalGap, 2f,
                "CF2.6: the gate's floor anchor must sit meaningfully below the gate art's own " +
                "tall center (scale (1.4, 4, 1)), matching CF1.6's doormat ratio, or grounding it " +
                "would be a cosmetic no-op.");

            // Find Cocoa's objective-target index by her distinctive "ANCHOR" copy, rather than
            // assuming which slot FindObjectsByType handed back for _cocoa.
            int cocoaIndex = -1;
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(controller.TryGetObjectiveTarget(i, out _, out var copy, out _));
                if (copy.Contains("ANCHOR")) { cocoaIndex = i; break; }
            }
            Assert.AreNotEqual(-1, cocoaIndex, "Should find Cocoa's anchor-objective index.");

            Assert.IsTrue(controller.TryGetObjectiveTarget(cocoaIndex, out var target, out var anchorCopy, out _));
            Assert.That(anchorCopy, Does.Contain("ANCHOR"));
            Assert.AreEqual((Vector2)controller.GateFloorAnchor, (Vector2)target.position,
                "Cocoa's objective arrow/breadcrumb must guide her to the gate's floor anchor, not " +
                "the gate's own tall-center transform.");
            Assert.AreNotEqual((Vector2)_game.GateHoldZone, (Vector2)target.position,
                "The guidance target must have actually moved off the gate's rendered (tall) position " +
                "- this is the assertion that fails against the pre-CF2.6 code.");

            // The hold check itself: standing exactly at the floor anchor (not the tall art
            // center) must be enough to engage and sustain the anchor through real Interact()
            // dispatch (trap #2), proving the checks measure against GateFloorAnchor.
            _cocoa.transform.position = controller.GateFloorAnchor;
            _cocoa.Interact();
            Assert.IsTrue(controller.AnchorEngaged,
                "Standing at the grounded floor anchor should be enough for Cocoa to engage the gate.");
            controller.Tick(0.05f, Time.time);
            Assert.IsTrue(controller.Puzzle.Held,
                "The hold should sustain while Cocoa stays at the floor anchor.");
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

        private static bool HasWorldPop(string text)
        {
            foreach (var pop in Object.FindObjectsByType<MissionWorldPop>(FindObjectsSortMode.None))
                if (pop.Label.Contains(text)) return true;
            return false;
        }
    }
}
