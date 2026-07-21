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
    /// The Walk Campaign mission wires the Social-Manipulation co-op puzzle into the real mission flow:
    /// the dogs con the human into a walk by sending ONE clear message built from BOTH of them at once -
    /// Cocoa's door-stare AND Cheddar presenting the leash. Cover only one (or neither) and the human
    /// gets confused and brings the wrong thing; confuse them too many times and the walk is off.
    /// </summary>
    public sealed class CoopWalkCampaignPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator Walk_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(23, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.WalkCampaign) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "The Walk Campaign should be reachable from mission select.");
            Assert.AreEqual("The Walk Campaign", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator Walk_RunsThroughDedicatedController()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            Assert.IsInstanceOf<WalkCampaignMissionController>(
                _game.ActiveMissionController,
                "The Walk Campaign must run entirely through its own IMissionController.");
            Assert.AreEqual(GameManager.MissionVariant.WalkCampaign, _game.ActiveMissionController.Variant);
            Assert.AreSame(_game.WalkCampaignController.Puzzle, _game.WalkCampaignPuzzle);
        }

        [UnityTest]
        public IEnumerator Walk_ClearPath_BothDogsHoldTheCombo()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            Assert.AreEqual("walk_campaign", _game.RuntimeSnapshot.MissionId);

            // Both stimuli held together -> the human reads the message and takes them for a walk.
            _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: true);

            Assert.IsTrue(_game.WalkCampaignPuzzle.Solved);
            Assert.AreEqual(0, _game.WalkCampaignPuzzle.Misreads);
            Assert.IsInstanceOf<IMissionSuccessPresentationController>(_game.WalkCampaignController);
            Assert.IsTrue(_game.WalkCampaignController.IsPresentingSuccessfulOutcome);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.That(_game.ObjectiveLabel, Does.Contain("WALKIES"));
            foreach (var feedback in _game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, feedback.CurrentPose,
                    "Both dogs should show an animated proud read during the held payoff, not frozen dogs.");
            _game.ForceWalkCampaignSuccessPresentationComplete();
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Walkies Secured"));
            Assert.AreNotEqual("MVP: awaiting dog heroics", _game.MvpLabel,
                "Selling the human on the walk should credit both dogs toward the MVP stat.");
        }

        [UnityTest]
        public IEnumerator Walk_OneDogAlone_NeverConvincesTheHuman()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            // Only Cocoa stares - the message is incomplete, so comprehension never builds.
            _game.ForceWalkCampaign(2f, doorStare: true, presentLeash: false);

            Assert.IsFalse(_game.WalkCampaignPuzzle.Solved);
            Assert.IsFalse(_game.WalkCampaignPuzzle.ExactMatch);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator Walk_FailPath_TooManyMixedSignals()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            // Hold an incomplete message long enough to misread, three times over.
            for (int i = 0; i < 3; i++)
                _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: false);

            Assert.AreEqual(3, _game.WalkCampaignPuzzle.Misreads);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Mixed Signals"));
        }

        [UnityTest]
        public IEnumerator Walk_SingleMisread_CoachesRecoverably_AndTheCorrectComboStillWorks()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            // Hold an incomplete message long enough for exactly one misread - well short of the fail threshold.
            _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: false);

            Assert.AreEqual(1, _game.WalkCampaignPuzzle.Misreads);
            Assert.That(_game.LastJuiceLabel, Does.Contain("CONFUSED"), "A single misread must produce a visible coach beat.");
            Assert.AreEqual(ArenaFeedbackCatalog.ThreatWarning, _game.LastAudioCueRequested,
                "A single misread must produce an audible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome,
                "One misread must be a recoverable coach beat, not a hard fail.");

            // Still recoverable: the correct combo, held long enough, still earns the walk.
            _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: true);
            Assert.IsTrue(_game.WalkCampaignPuzzle.Solved);
        }

        /// <summary>
        /// CF2.4 (funny-failure audit): the catalog's WalkCampaign HowToPlay step promises a misread
        /// "makes the human fetch a funny wrong item," but pre-fix the branch only swapped a label/
        /// sprite and played the same ThreatWarning cue every other mission's generic warning-miss
        /// uses - no different from a beep. Mirrors CF1.3's gag-template test shape: negative case
        /// first (no misread yet -> stays quiet), then the positive case (an actual misread starts
        /// the human's offer-lean toward the dogs, a distinct audio cue, and both dogs turning to
        /// react), read through a real frame so the tween's mid-flight state (not a teleport) is
        /// directly observable.
        /// </summary>
        [UnityTest]
        public IEnumerator Walk_SingleMisread_TriggersComedicGagButStaysQuietBeforehand()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;
            var human = GameObject.Find("WalkCampaignHuman");
            Assert.IsNotNull(human);
            // Opposite sides of the human so a genuine per-dog "face the reaction" nudge is
            // distinguishable from any coincidental default facing.
            _cheddar.transform.position = (Vector2)human.transform.position + Vector2.left * 5f;
            _cocoa.transform.position = (Vector2)human.transform.position + Vector2.right * 5f;
            var cheddarFeedback = _cheddar.GetComponent<DogReadabilityFeedback>();
            var cocoaFeedback = _cocoa.GetComponent<DogReadabilityFeedback>();

            // Negative case: holding the correct combo (no misread) must never fire the comedic gag.
            _game.ForceWalkCampaign(0.2f, doorStare: true, presentLeash: true);
            Assert.AreEqual(0, _game.WalkCampaignPuzzle.Misreads);
            Assert.AreEqual(0f, _game.WalkCampaignController.MisreadGagProgress, 0.001f,
                "The offer-lean must stay at rest until an actual misread happens.");
            Assert.That(_game.AudioCueRequests, Does.Not.Contain(ArenaFeedbackCatalog.SquirrelStunned),
                "The comedic cue must stay quiet when nothing funny has happened yet.");

            // Positive case: an incomplete combo held long enough to misread once.
            _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: false);
            Assert.AreEqual(1, _game.WalkCampaignPuzzle.Misreads);
            Assert.IsFalse(_game.WalkCampaignController.MisreadEscalated, "A single misread must not trigger the bigger flourish.");
            Assert.That(_game.AudioCueRequests, Does.Contain(ArenaFeedbackCatalog.SquirrelStunned),
                "A distinct comedic cue, not just the generic ThreatWarning, should play on a misread.");
            Assert.AreEqual(ArenaFeedbackCatalog.ThreatWarning, _game.LastAudioCueRequested,
                "The comedic cue must fire BEFORE the existing ThreatWarning coach beat, not replace it.");
            Assert.AreEqual("FacingRight", cheddarFeedback.FacingIntentLabel,
                "Cheddar (to the human's left) should turn to face the misread reaction.");
            Assert.AreEqual("FacingLeft", cocoaFeedback.FacingIntentLabel,
                "Cocoa (to the human's right) should turn to face the misread reaction from the other side.");

            // Let one real frame tick so the eased offer-lean is directly observable mid-flight
            // (proves a brief lerp toward the dogs, not a teleport) without crossing another misread
            // threshold (Confusion resets to 0 on a misread and a single small frame can't refill it).
            yield return null;
            Assert.Greater(_game.WalkCampaignController.MisreadGagProgress, 0f, "The offer-lean should have started...");
            Assert.Less(_game.WalkCampaignController.MisreadGagProgress, 1f, "...but not teleported straight to the offered position.");
            Assert.AreEqual(1, _game.WalkCampaignPuzzle.Misreads, "The settling frame must not itself cause a second misread.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        /// <summary>
        /// CF2.4 done-criterion: a direct behavioral proof that misread MECHANICS are unchanged by
        /// the comedy layer - same Misreads count, fail threshold, and outcome CoopSocialManipulation-
        /// Puzzle.Advance() has always produced - and that the escalation flourish (the third,
        /// mission-ending misread) fires without altering any of it.
        /// </summary>
        [UnityTest]
        public IEnumerator Walk_ThirdMisread_EscalatesGagWithoutChangingFailMechanics()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: false);
            Assert.AreEqual(1, _game.WalkCampaignPuzzle.Misreads);
            Assert.IsFalse(_game.WalkCampaignController.MisreadEscalated);

            _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: false);
            Assert.AreEqual(2, _game.WalkCampaignPuzzle.Misreads);
            Assert.IsFalse(_game.WalkCampaignController.MisreadEscalated, "Two misreads still must not escalate.");

            _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: false);
            Assert.AreEqual(3, _game.WalkCampaignPuzzle.Misreads);
            Assert.IsTrue(_game.WalkCampaignController.MisreadEscalated, "The third (mission-ending) misread should trip the bigger flourish.");
            Assert.AreEqual(1f, _game.WalkCampaignController.MisreadGagProgress, 0.001f,
                "The escalated misread snaps straight to the full offer - there are no more frames left to ease through.");

            // Mechanics unchanged: same fail threshold/outcome the pre-existing Walk_FailPath test pins.
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Mixed Signals"));
        }

        [UnityTest]
        public IEnumerator Walk_HumanActorStatesShowConfusionComprehensionMisreadAndOutcome()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            var human = GameObject.Find("WalkCampaignHuman");
            Assert.IsNotNull(human);
            var feedback = human.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback);
            Assert.IsTrue(feedback.HasContextualTextVisibility,
                "The walk human explanation must be contextual/debug text, not an always-on production crutch.");
            Assert.That(feedback.Label, Does.Contain("CONFUSED"));

            _game.ForceWalkCampaign(1f, doorStare: true, presentLeash: true);
            Assert.That(feedback.Label, Does.Contain("GETTING IT"));

            _game.ForceWalkCampaign(2f, doorStare: true, presentLeash: true);
            Assert.That(feedback.Label, Does.Contain("WALKIES"));

            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;
            _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: false);
            Assert.That(feedback.Label, Does.Contain("WRONG THING"));

            for (int i = 0; i < 2; i++) _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: false);
            Assert.That(feedback.Label, Does.Contain("GAVE UP"));
        }

        [UnityTest]
        public IEnumerator Walk_Replay_ResetsThePuzzle()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;
            _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: false); // a misread
            Assert.Greater(_game.WalkCampaignPuzzle.Misreads, 0);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.WalkCampaign, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.WalkCampaignPuzzle.Misreads);
            Assert.AreEqual(0f, _game.WalkCampaignPuzzle.Comprehension);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator Walk_PositionDriven_BothDogsMustInteractThenHoldTheirStations()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            var cheddarBody = _cheddar.GetComponent<Rigidbody2D>();
            var cocoaBody = _cocoa.GetComponent<Rigidbody2D>();

            _cocoa.transform.position = _game.WalkDoorZone;
            _cheddar.transform.position = _game.WalkLeashZone;
            if (cocoaBody != null) cocoaBody.linearVelocity = Vector2.zero;
            if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
            yield return null;
            Assert.IsFalse(_game.WalkCampaignPuzzle.ExactMatch,
                "Standing on both stations alone must not silently perform either social signal.");
            Assert.AreEqual(0f, _game.WalkCampaignPuzzle.Comprehension);

            _cocoa.Interact();
            _cheddar.Interact();
            yield return null;
            Assert.IsTrue(_game.WalkCampaignController.DoorStareEngaged);
            Assert.IsTrue(_game.WalkCampaignController.LeashPresented);
            // S5.2: both the door-stare and leash-present engagements were silent success beats
            // (rumble but no audio) - either dog's Interact now fires the same cue. Cheddar's
            // engagement also completes the exact-match combo, whose own pre-existing
            // SnackSockCollect cue fires right after in the same call and is the LAST cue
            // requested - check containment, not LastAudioCueRequested, to observe both correctly.
            Assert.That(_game.AudioCueRequests, Does.Contain(ArenaFeedbackCatalog.TugRescueSuccess));

            // Both deliberately engaged signals held together build comprehension.
            for (int i = 0; i < 30; i++)
            {
                _cocoa.transform.position = _game.WalkDoorZone;
                _cheddar.transform.position = _game.WalkLeashZone;
                if (cocoaBody != null) cocoaBody.linearVelocity = Vector2.zero;
                if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
                yield return null;
            }
            Assert.IsTrue(_game.WalkCampaignPuzzle.ExactMatch, "Both Interact-engaged stations should send the exact message.");
            Assert.Greater(_game.WalkCampaignPuzzle.Comprehension, 0f);

            _cocoa.transform.position = new Vector3(_game.WalkDoorZone.x - 40f, _game.WalkDoorZone.y, 0f);
            yield return null;
            Assert.IsFalse(_game.WalkCampaignController.DoorStareEngaged,
                "Leaving the door breaks Cocoa's stare and requires another Interact.");
            Assert.IsFalse(_game.WalkCampaignPuzzle.ExactMatch);
        }

        /// <summary>
        /// CF2.5 (roster carry-visual audit): the leash used to sit fixed at LeashZone regardless of
        /// whether Cheddar was actually presenting it - the same "static prop that's logically held"
        /// gap CF1.7 fixed for Pee Break's leash (this task's reference implementation). Drives the
        /// REAL position+Interact path (mirrors Walk_PositionDriven_BothDogsMustInteractThenHoldTheir
        /// Stations above), not ForceWalkCampaign, because ForceWalkCampaign is a puzzle-level-only
        /// test shortcut that never sets _leashPresented - the field the fixed ResolveLeashPosition()
        /// actually reads.
        /// </summary>
        [UnityTest]
        public IEnumerator Walk_LeashVisualFollowsCheddarWhilePresenting_AndRestsAtStationOtherwise()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            var leash = GameObject.Find("WalkCampaignLeash");
            Assert.IsNotNull(leash);
            Vector2 leashZone = _game.WalkLeashZone;
            Assert.AreEqual(leashZone, (Vector2)leash.transform.position,
                "Before Cheddar engages, the leash should rest at its fixed station.");

            // Stand Cheddar off-center within StationRange (not exactly on the zone) so a followed
            // position is unambiguously distinguishable from the old fixed-station position.
            Vector2 presentSpot = leashZone + Vector2.up * 1.5f;
            _cheddar.transform.position = presentSpot;
            var cheddarBody = _cheddar.GetComponent<Rigidbody2D>();
            if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
            yield return null;
            _cheddar.Interact();
            yield return null;

            Assert.IsTrue(_game.WalkCampaignController.LeashPresented);
            float followedDistance = Vector2.Distance(leash.transform.position, presentSpot);
            Assert.Less(followedDistance, 1f,
                "While Cheddar presents, the leash art should follow near his position, not stay pinned to the fixed station.");
            Assert.Greater(Vector2.Distance(leash.transform.position, leashZone), 0.5f,
                "The followed leash position should visibly differ from the old fixed-station spot.");

            // Cheddar walks the leash far away - Tick() should drop _leashPresented and the leash
            // should return to resting exactly at the fixed station, not linger at his last spot.
            _cheddar.transform.position = leashZone + Vector2.right * 40f;
            if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
            yield return null;
            yield return null;

            Assert.IsFalse(_game.WalkCampaignController.LeashPresented);
            Assert.AreEqual(leashZone, (Vector2)leash.transform.position,
                "Once Cheddar drops the presentation, the leash must rest back at the fixed station.");
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
