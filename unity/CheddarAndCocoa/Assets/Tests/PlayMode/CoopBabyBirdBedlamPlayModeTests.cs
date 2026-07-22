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
    /// Baby Bird Bedlam wires the Feast-and-Fend co-op puzzle into the real mission flow: chicks
    /// drop from the nest, Cheddar grabs and shake-gulps each one while defenseless, and Cocoa must
    /// bark-repel the parent birds' dives before they peck. Three pecks fail the round; four gulped
    /// chicks clear it.
    /// </summary>
    public sealed class CoopBabyBirdBedlamPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator Bedlam_RunsThroughDedicatedController()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            Assert.IsInstanceOf<BabyBirdBedlamMissionController>(
                _game.ActiveMissionController,
                "Baby Bird Bedlam must run entirely through its own IMissionController.");
            Assert.AreEqual(GameManager.MissionVariant.BabyBirdBedlam, _game.ActiveMissionController.Variant);
            Assert.AreSame(_game.BabyBirdBedlamController.Puzzle, _game.FeastGuardPuzzle);
            var nest = GameObject.Find("BedlamNest");
            Assert.IsNotNull(nest);
            var nestArt = nest.GetComponent<MissionPropArtAttachment>();
            Assert.IsNotNull(nestArt, "The cold start needs an authored oak/nest world cue before the first chick drops.");
            Assert.AreEqual(FinalGameplayArt.EnvironmentShadeTree, nestArt.ResourcePath);
            Assert.IsTrue(nestArt.HasRuntimeSprite);
        }

        [UnityTest]
        public IEnumerator Bedlam_AuthoredCharacterSprites_ReplaceSquareAndEagleFallbacks()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            string[] characterPaths =
            {
                FinalGameplayArt.BabyBirdChickFalling,
                FinalGameplayArt.BabyBirdChickGrounded,
                FinalGameplayArt.BabyBirdChickGrabbed,
                FinalGameplayArt.BabyBirdChickShaking,
                FinalGameplayArt.BabyBirdChickGulped,
                FinalGameplayArt.BabyBirdChickPecked,
                FinalGameplayArt.BabyBirdMotherCircling,
                FinalGameplayArt.BabyBirdMotherWarning,
                FinalGameplayArt.BabyBirdMotherAttacking,
                FinalGameplayArt.BabyBirdMotherRepelled,
                FinalGameplayArt.BabyBirdMotherRescue,
                FinalGameplayArt.BabyBirdMotherDefeated
            };
            foreach (string path in characterPaths)
                Assert.IsTrue(FinalGameplayArt.Has(path), $"Missing authored Bedlam character sprite: {path}");

            var chick = FindLoadedObject("BedlamChick");
            Assert.IsNotNull(chick);
            var chickArt = chick.GetComponent<MissionPropArtAttachment>();
            var motherArt = _game.PredatorObject.GetComponent<MissionPropArtAttachment>();
            AssertCharacterArt(chick, chickArt, FinalGameplayArt.BabyBirdChickFalling);
            AssertCharacterArt(_game.PredatorObject, motherArt, FinalGameplayArt.BabyBirdMotherCircling);
            Assert.AreNotEqual(FinalGameplayArt.EagleThreat, motherArt.ResourcePath);
            Assert.AreNotEqual(FinalGameplayArt.EagleAction, motherArt.ResourcePath);
            Assert.IsFalse(MissionPropArt.FindFallbackRenderer(chick).enabled,
                "The old square chick renderer must not show behind the authored silhouette.");
            Assert.IsFalse(MissionPropArt.FindFallbackRenderer(_game.PredatorObject).enabled,
                "The generic eagle body must not show behind the angry mother sprite.");
            Assert.IsNull(chick.GetComponent<Collider2D>(),
                "Character artwork must not add gameplay collision to the controller-owned chick state.");
            Assert.IsInstanceOf<BabyBirdBedlamMissionController>(_game.ActiveMissionController);
        }

        [UnityTest]
        public IEnumerator Bedlam_VisualStates_FollowFallGrabShakeDiveRescueSuccessAndReplay()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            var controller = _game.BabyBirdBedlamController;
            var chick = FindLoadedObject("BedlamChick");
            MissionPropArtAttachment ChickArt() => chick.GetComponent<MissionPropArtAttachment>();
            MissionPropArtAttachment MotherArt() => _game.PredatorObject.GetComponent<MissionPropArtAttachment>();

            controller.ForceChickFall(0f);
            Assert.AreEqual(FinalGameplayArt.BabyBirdChickFalling, ChickArt().ResourcePath);
            _game.ForceChickLand(0f);
            Assert.AreEqual(FinalGameplayArt.BabyBirdChickGrounded, ChickArt().ResourcePath);
            _game.ForceChickGrab();
            Assert.AreEqual(FinalGameplayArt.BabyBirdChickGrabbed, ChickArt().ResourcePath);
            _game.ForceChickShake();
            Assert.AreEqual(FinalGameplayArt.BabyBirdChickShaking, ChickArt().ResourcePath);

            _game.ForceParentDive();
            Assert.AreEqual(FinalGameplayArt.BabyBirdMotherWarning, MotherArt().ResourcePath);
            controller.Tick(0.45f, Time.time);
            controller.Tick(0.45f, Time.time);
            Assert.AreEqual(FinalGameplayArt.BabyBirdMotherAttacking, MotherArt().ResourcePath);
            _game.ForceParentRepel();
            Assert.AreEqual(FinalGameplayArt.BabyBirdMotherRepelled, MotherArt().ResourcePath);

            while (_game.FeastGuardPuzzle.Chick == CoopFeastGuardPuzzle.ChickState.Held)
                _game.ForceChickShake();
            Assert.AreEqual(FinalGameplayArt.BabyBirdChickGulped, ChickArt().ResourcePath);
            Assert.IsTrue(chick.activeSelf, "The gulp key pose should hold briefly instead of vanishing instantly.");

            _game.Restart();
            yield return null;
            Assert.AreEqual(FinalGameplayArt.BabyBirdMotherCircling, MotherArt().ResourcePath);
            Assert.IsFalse(chick.activeSelf, "Replay must clean up any held chick reaction pose.");

            _game.ForceChickLand(0f);
            _game.ForceChickAirlift();
            Assert.AreEqual(FinalGameplayArt.BabyBirdMotherRescue, MotherArt().ResourcePath);

            _game.Restart();
            yield return null;
            _game.ForceChickLand(0f);
            _game.ForceChickGrab();
            _game.ForceParentDive();
            _game.ForceDiveAdvance(5f);
            Assert.AreEqual(FinalGameplayArt.BabyBirdChickPecked, ChickArt().ResourcePath);
            Assert.AreEqual(FinalGameplayArt.BabyBirdMotherAttacking, MotherArt().ResourcePath);

            _game.Restart();
            yield return null;
            for (int i = 0; i < _game.FeastGuardPuzzle.ChicksNeeded; i++)
            {
                _game.ForceChickLand(0f);
                _game.ForceChickGrab();
                for (int shake = 0; shake < _game.FeastGuardPuzzle.ShakesNeeded; shake++)
                    _game.ForceChickShake();
            }
            Assert.AreEqual(FinalGameplayArt.BabyBirdMotherDefeated, MotherArt().ResourcePath);
            Assert.IsTrue(controller.IsPresentingSuccessfulOutcome);

            _game.Restart();
            yield return null;
            Assert.AreEqual(FinalGameplayArt.BabyBirdMotherCircling, MotherArt().ResourcePath);
            Assert.IsFalse(chick.activeSelf);
        }

        [UnityTest]
        public IEnumerator Bedlam_Nest_CarriesPersistentDepletionBaselineAcrossChicks()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            var controller = _game.BabyBirdBedlamController;
            var nest = FindLoadedObject("BedlamNest");
            Assert.IsNotNull(nest);
            var nestLabel = nest.GetComponentInChildren<TextMesh>();
            Assert.IsNotNull(nestLabel, "The nest needs a label to carry the CF2.3 depletion tally.");
            var nestOverlay = nest.GetComponent<ArtSpriteOverlay>();
            Assert.IsNotNull(nestOverlay, "The nest's authored shade-tree art must expose its tint for the depletion baseline.");

            Assert.AreEqual(0f, controller.NestDepletion, "The nest should read as full before any chick is eaten.");
            Color startTint = nestOverlay.CurrentTint;
            Assert.That(nestLabel.text, Does.Contain("0/4"));

            int shakes = _game.FeastGuardPuzzle.ShakesNeeded;
            _game.ForceChickLand(0f);
            _game.ForceChickGrab();
            for (int s = 0; s < shakes; s++) _game.ForceChickShake();

            Assert.AreEqual(1, _game.FeastGuardPuzzle.ChicksEaten);
            Assert.Greater(controller.NestDepletion, 0f,
                "Gulping a chick should visibly move the nest's world-space depletion baseline, " +
                "not just the HUD's chicks n/N count.");
            Assert.That(nestLabel.text, Does.Contain("1/4"));
            Assert.AreNotEqual(startTint, nestOverlay.CurrentTint,
                "The nest's authored-art tint should reflect overall feast progress, distinct from the " +
                "parent bird's own moment-to-moment dive/repel reactions.");

            for (int chick = 1; chick < _game.FeastGuardPuzzle.ChicksNeeded; chick++)
            {
                _game.ForceChickLand(0f);
                _game.ForceChickGrab();
                for (int s = 0; s < shakes; s++) _game.ForceChickShake();
            }

            Assert.IsTrue(_game.FeastGuardPuzzle.Solved);
            Assert.AreEqual(1f, controller.NestDepletion, "A fully eaten nest should read as completely depleted.");
            Assert.That(nestLabel.text, Does.Contain("4/4"));
        }

        [UnityTest]
        public IEnumerator Bedlam_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(24, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.BabyBirdBedlam) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "Baby Bird Bedlam should be reachable from mission select.");
            Assert.AreEqual("Baby Bird Bedlam", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator Bedlam_ClearPath_GrabShakeGulpFourChicks()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            Assert.AreEqual("baby_bird_bedlam", _game.RuntimeSnapshot.MissionId);
            int needed = _game.FeastGuardPuzzle.ChicksNeeded;
            int shakes = _game.FeastGuardPuzzle.ShakesNeeded;

            for (int chick = 0; chick < needed; chick++)
            {
                _game.ForceChickLand(0f);
                Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.Grounded, _game.FeastGuardPuzzle.Chick);
                _game.ForceChickGrab();
                Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.Held, _game.FeastGuardPuzzle.Chick);
                for (int s = 0; s < shakes; s++) _game.ForceChickShake();
            }

            Assert.IsTrue(_game.FeastGuardPuzzle.Solved);
            Assert.AreEqual(needed, _game.FeastGuardPuzzle.ChicksEaten);
            Assert.AreEqual(0, _game.FeastGuardPuzzle.Mistakes);
            var controller = _game.BabyBirdBedlamController;
            Assert.IsInstanceOf<IMissionSuccessPresentationController>(controller);
            Assert.IsTrue(controller.IsPresentingSuccessfulOutcome,
                "The full-bellies payoff should remain visible before the result card replaces the yard.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa ruled the sky"));
            foreach (var feedback in _game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, feedback.CurrentPose,
                    "Both dogs should show an animated proud read during the held payoff, not frozen dogs.");

            controller.ForceFinishSuccessPresentation();
            yield return null;

            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Nest Feast"));
            Assert.AreNotEqual("MVP: awaiting dog heroics", _game.MvpLabel,
                "Gulping chicks should credit Cheddar toward the MVP stat.");
        }

        [UnityTest]
        public IEnumerator Bedlam_DefendPath_RepelledDiveNeverPecks()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            _game.ForceChickLand(0f);
            _game.ForceChickGrab();
            _game.ForceParentDive();
            Assert.IsTrue(_game.FeastGuardPuzzle.DiveActive);

            _game.ForceParentRepel();
            Assert.IsFalse(_game.FeastGuardPuzzle.DiveActive);
            Assert.AreEqual(1, _game.FeastGuardPuzzle.Repels);
            Assert.AreEqual(0, _game.FeastGuardPuzzle.Pecks);
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.Held, _game.FeastGuardPuzzle.Chick,
                "A repelled dive should leave Cheddar still working on his chick.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator Bedlam_WrongRolesAndRange_CoachBackIntoTheDefense()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            var controller = _game.BabyBirdBedlamController;
            controller.ForceChickLand(0f);
            Assert.IsTrue(controller.HandleInteract(1));
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.Grounded, _game.FeastGuardPuzzle.Chick,
                "Cocoa's snack inspection must leave the chick available for Cheddar.");
            Assert.That(_game.LastCue, Does.Contain("keeps watch"));
            Assert.That(_game.LastJuiceLabel, Does.Contain("CHEDDAR"), "Cocoa's wrong-dog grab must produce a visible coach beat.");
            Assert.AreEqual(ArenaFeedbackCatalog.UiButtonDisabled, _game.LastAudioCueRequested,
                "Cocoa's wrong-dog grab must produce an audible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            controller.ForceChickGrab();
            controller.ForceParentDive();
            Assert.IsTrue(controller.HandleBark(0));
            Assert.IsTrue(_game.FeastGuardPuzzle.DiveActive,
                "Cheddar's mouth-full bark should coach the role without canceling the dive.");
            Assert.That(_game.LastCue, Does.Contain("chick in his mouth"));
            Assert.That(_game.LastJuiceLabel, Does.Contain("COCOA"), "Cheddar's mouth-full bark must produce a visible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            _cocoa.transform.position = Vector2.down * 20f;
            Assert.IsTrue(controller.HandleBark(1));
            Assert.IsTrue(_game.FeastGuardPuzzle.DiveActive,
                "Cocoa's distant bark should preserve the same recoverable dive window.");
            Assert.That(_game.LastCue, Does.Contain("too far away"));

            _cocoa.transform.position = _game.PredatorObject.transform.position;
            Assert.IsTrue(controller.HandleBark(1));
            Assert.IsFalse(_game.FeastGuardPuzzle.DiveActive);
            Assert.AreEqual(1, _game.FeastGuardPuzzle.Repels);
        }

        [UnityTest]
        public IEnumerator Bedlam_PeckPath_ExpiredDiveDropsTheChick()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            _game.ForceChickLand(0f);
            _game.ForceChickGrab();
            _game.ForceChickShake();
            _game.ForceParentDive();
            _game.ForceDiveAdvance(5f);

            Assert.AreEqual(1, _game.FeastGuardPuzzle.Pecks);
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.None, _game.FeastGuardPuzzle.Chick,
                "The pecked-loose chick escapes back to the nest.");
            Assert.AreEqual(0, _game.FeastGuardPuzzle.ChicksEaten);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome,
                "One peck stings but does not end the mission.");
        }

        [UnityTest]
        public IEnumerator Bedlam_GulpMidDive_CancelsTheDive()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            int shakes = _game.FeastGuardPuzzle.ShakesNeeded;
            _game.ForceChickLand(0f);
            _game.ForceChickGrab();
            for (int s = 0; s < shakes - 1; s++) _game.ForceChickShake();
            _game.ForceParentDive();
            Assert.IsTrue(_game.FeastGuardPuzzle.DiveActive);

            _game.ForceChickShake(); // the gulp
            Assert.AreEqual(1, _game.FeastGuardPuzzle.ChicksEaten);
            Assert.IsFalse(_game.FeastGuardPuzzle.DiveActive,
                "Swallowing the chick mid-dive leaves the parent nothing to save.");
            _game.ForceDiveAdvance(5f);
            Assert.AreEqual(0, _game.FeastGuardPuzzle.Pecks);
        }

        [UnityTest]
        public IEnumerator Bedlam_FailPath_ThreePecksEndTheFeast()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            for (int i = 0; i < _game.FeastGuardPuzzle.MaxPecks; i++)
            {
                _game.ForceChickLand(0f);
                _game.ForceChickGrab();
                _game.ForceParentDive();
                _game.ForceDiveAdvance(5f);
            }

            Assert.AreEqual(3, _game.FeastGuardPuzzle.Pecks);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Pecked Out Of The Yard"));
            Assert.That(_game.EndReasonLabel, Does.Contain("peck"),
                "The fail overlay should explain the pecks in dog-life terms.");
        }

        [UnityTest]
        public IEnumerator Bedlam_AirliftPath_UnclaimedChickCountsAsAMistake()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            _game.ForceChickLand(0f);
            _game.ForceChickAirlift();

            Assert.AreEqual(1, _game.FeastGuardPuzzle.Airlifts);
            Assert.AreEqual(1, _game.FeastGuardPuzzle.Mistakes);
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.None, _game.FeastGuardPuzzle.Chick);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator Bedlam_Replay_ResetsThePuzzle()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            _game.ForceChickLand(0f);
            _game.ForceChickGrab();
            _game.ForceParentDive();
            _game.ForceDiveAdvance(5f); // a peck
            Assert.Greater(_game.FeastGuardPuzzle.Pecks, 0);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.BabyBirdBedlam, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.FeastGuardPuzzle.ChicksEaten);
            Assert.AreEqual(0, _game.FeastGuardPuzzle.Pecks);
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.None, _game.FeastGuardPuzzle.Chick);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator Bedlam_PositionDriven_ChickFallsAndCheddarGrabsIt()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BabyBirdBedlam);
            yield return null;

            _cheddar.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            _cocoa.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;

            // Let the first scheduled chick drop and fall all the way to the ground.
            float deadline = Time.time + 8f;
            while (Time.time < deadline
                   && _game.FeastGuardPuzzle.Chick != CoopFeastGuardPuzzle.ChickState.Grounded)
                yield return null;
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.Grounded, _game.FeastGuardPuzzle.Chick,
                "A chick should tumble out of the nest and land on its own within the opening seconds.");
            Assert.AreEqual(ArenaFeedbackCatalog.ThreatWarning, _game.LastAudioCueRequested,
                "S5.2: a chick landing (airlift countdown starts) was a silent moment (visual PLOP only) - must fire a cue now.");

            var chick = GameObject.Find("BedlamChick");
            Assert.IsNotNull(chick);
            Assert.IsTrue(chick.activeSelf, "The landed chick should be visible and labeled.");

            // Park Cheddar on the chick and grab it through the controller's interact surface.
            // The internal dog index isn't exposed, so try both: Cocoa's press is role-locked to a
            // harmless "guard the sky" fumble and only Cheddar's press can grab.
            _cheddar.transform.position = chick.transform.position;
            if (_cheddar.TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            yield return null;
            IMissionInteractionController controller = _game.BabyBirdBedlamController;
            controller.HandleInteract(0);
            controller.HandleInteract(1);
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.Held, _game.FeastGuardPuzzle.Chick,
                "Cheddar interacting on top of the grounded chick should grab it.");
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

        private static void AssertCharacterArt(GameObject owner, MissionPropArtAttachment attachment,
            string expectedPath)
        {
            Assert.IsNotNull(attachment, $"{owner.name} needs a mission-prop art attachment.");
            Assert.AreEqual(expectedPath, attachment.ResourcePath);
            Assert.IsTrue(attachment.HasRuntimeSprite);
            Assert.That(attachment.RuntimeSpriteName, Does.Not.Contain("Square"));
            var overlay = owner.GetComponent<ArtSpriteOverlay>();
            Assert.IsNotNull(overlay);
            Assert.IsFalse(overlay.HasShadow, "Character sprites must not carry floating square shadow backplates.");
            var artChild = owner.transform.Find("ActualArtOverlay");
            Assert.IsNotNull(artChild);
            Assert.IsNull(artChild.GetComponent<Collider2D>(),
                "Presentation overlays must not add or replace controller-owned collision geometry.");
        }

        private static GameObject FindLoadedObject(string objectName)
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == objectName && candidate.scene.IsValid()) return candidate;
            }
            return null;
        }
    }
}
