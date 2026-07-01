using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;
using CheddarAndCocoa.CameraRig;
using CheddarAndCocoa.Input;

namespace CheddarAndCocoa.Tests
{
    public sealed class ArenaGameLoopPlayModeTests
    {
        [Test]
        public void ArenaTuningDefaults_AreCentralized_AndKeepMissionBalanceInRange()
        {
            var tuning = ArenaMissionTuning.CreateDefault();
            Assert.AreEqual(9f, tuning.FirstSquirrelBaseDelay);
            Assert.AreEqual(7f, tuning.FirstSquirrelTroubleDelay);
            Assert.AreEqual(3.4f, tuning.SquirrelBaseDelay);
            Assert.AreEqual(2.2f, tuning.SquirrelTroubleDelay);
            Assert.AreEqual(1.9f, tuning.SquirrelMoveSpeed);
            Assert.AreEqual(25f, tuning.PredatorWarningAt);
            Assert.AreEqual(5f, tuning.PredatorWarningSeconds);
            Assert.AreEqual(0.5f, tuning.TugChargePerSecond);
            Assert.AreEqual(2f, tuning.RescueBarkRange);
            Assert.AreEqual(100, tuning.UnitedBarkScore);
            Assert.AreEqual(500, tuning.ClearScore);
            Assert.AreEqual(7.5f, tuning.CameraMinOrthoSize);
            Assert.AreEqual(34f, tuning.CameraMaxOrthoSize);
            Assert.AreEqual(5.0f, tuning.CameraHorizontalMargin);
            Assert.AreEqual(4.0f, tuning.CameraVerticalMargin);
            Assert.AreEqual(28f, tuning.TravelAssistEngageDistance);
            Assert.AreEqual(20f, tuning.TravelAssistReleaseDistance);
            Assert.AreEqual(1.55f, tuning.TravelAssistSpeedMultiplier);
            Assert.AreEqual(tuning.SingleBarkSquirrelRange, tuning.SquirrelRangeIndicatorRadius);
            Assert.AreEqual(tuning.RescueBarkRange, tuning.RescueRangeIndicatorRadius);
            Assert.AreEqual(tuning.TugTogetherDistance, tuning.TugRangeIndicatorRadius);

            AssertMissionBalance(GameManager.MissionVariant.BackyardRescue, tuning, expectSquirrel: true, expectPredator: true, expectTug: true);
            AssertMissionBalance(GameManager.MissionVariant.SnackHeist, tuning, expectSquirrel: true, expectPredator: false, expectTug: false);
            AssertMissionBalance(GameManager.MissionVariant.SockPanic, tuning, expectSquirrel: false, expectPredator: false, expectTug: false);
            AssertMissionBalance(GameManager.MissionVariant.SquirrelConspiracy, tuning, expectSquirrel: true, expectPredator: false, expectTug: false);
        }

        [Test]
        public void ArenaArtCatalog_DefinesReplaceableVisualSlots()
        {
            Assert.That(ArenaArtCatalog.Dog(DogId.Cheddar).Title, Does.Contain("CHEDDAR"));
            Assert.That(ArenaArtCatalog.Dog(DogId.Cocoa).Title, Does.Contain("COCOA"));
            Assert.That(ArenaArtCatalog.DogPartNames(DogId.Cheddar), Does.Contain("CheddarRedCollar"));
            Assert.That(ArenaArtCatalog.DogPartNames(DogId.Cheddar), Does.Contain("CheddarChaosBolt"));
            Assert.That(ArenaArtCatalog.DogPartNames(DogId.Cocoa), Does.Contain("CocoaTealCollar"));
            Assert.That(ArenaArtCatalog.DogPartNames(DogId.Cocoa), Does.Contain("CocoaQueenSpotC"));
            Assert.That(ArenaArtCatalog.ActorPartNames(ArenaArtCatalog.ActorKind.Squirrel), Does.Contain("SquirrelFlagTail"));
            Assert.That(ArenaArtCatalog.ActorPartNames(ArenaArtCatalog.ActorKind.Squirrel), Does.Contain("SquirrelLootAcorn"));
            Assert.That(ArenaArtCatalog.ActorPartNames(ArenaArtCatalog.ActorKind.Predator), Does.Contain("PredatorWarningEyeA"));
            Assert.That(ArenaArtCatalog.ActorPartNames(ArenaArtCatalog.ActorKind.Predator), Does.Contain("CoyoteFenceEars"));
            Assert.That(ArenaArtCatalog.ActorPartNames(ArenaArtCatalog.ActorKind.Rope), Does.Contain("RopeStripeA"));
            Assert.That(ArenaArtCatalog.ActorPartNames(ArenaArtCatalog.ActorKind.Rope), Does.Contain("RopeCenterKnot"));
            Assert.That(ArenaArtCatalog.CollectiblePartNames(GameManager.MissionVariant.BackyardRescue), Does.Contain("WeenieMustard"));
            Assert.That(ArenaArtCatalog.CollectiblePartNames(GameManager.MissionVariant.SnackHeist), Does.Contain("SnackCrumbA"));
            Assert.That(ArenaArtCatalog.CollectiblePartNames(GameManager.MissionVariant.SockPanic), Does.Contain("SockStripeA"));
            Assert.AreEqual("BarkBurst", ArenaArtCatalog.BarkFeedback.BurstName);
            Assert.AreEqual("ObjectiveArrowLabel", ArenaArtCatalog.ObjectiveArrowLabel.Name);
            Assert.That(ArenaDraftArt.PathFor(ArenaDraftArt.SpriteId.CheddarPortrait), Does.StartWith("ArenaDraft/Characters/Dogs"));
            Assert.That(ArenaDraftArt.PathFor(ArenaDraftArt.SpriteId.SquirrelCharacter), Does.StartWith("ArenaDraft/Characters/Squirrel"));
            Assert.IsTrue(ArenaDraftArt.HasSprite(ArenaDraftArt.SpriteId.CheddarPortrait));
            Assert.IsTrue(ArenaDraftArt.HasSprite(ArenaDraftArt.SpriteId.CocoaPortrait));
            Assert.IsTrue(ArenaDraftArt.HasSprite(ArenaDraftArt.SpriteId.SquirrelCharacter));
            Assert.IsTrue(ArenaDraftArt.HasSprite(ArenaDraftArt.SpriteId.EagleReference));
            Assert.IsTrue(ArenaDraftArt.HasSprite(ArenaDraftArt.SpriteId.CoyoteReference));
            Assert.IsTrue(ArenaDraftArt.HasSprite(ArenaDraftArt.SpriteId.BunnyReference));
            Assert.IsTrue(ArenaDraftArt.HasSprite(ArenaDraftArt.SpriteId.BackyardProps));
            Assert.IsTrue(ArenaDraftArt.HasSprite(ArenaDraftArt.SpriteId.UiKit));
            Assert.IsTrue(ArenaDraftArt.HasSprite(ArenaDraftArt.SpriteId.Vfx));
        }

        [Test]
        public void ArenaFeedbackCatalog_DefinesReplaceableAudioCueSlots()
        {
            string[] required =
            {
                ArenaFeedbackCatalog.Bark,
                ArenaFeedbackCatalog.TugRescueSuccess,
                ArenaFeedbackCatalog.SnackSockCollect,
                ArenaFeedbackCatalog.SquirrelStealMiss,
                ArenaFeedbackCatalog.ScoreGain,
                ArenaFeedbackCatalog.ScorePenalty,
                ArenaFeedbackCatalog.MissionWin,
                ArenaFeedbackCatalog.MissionFail,
                ArenaFeedbackCatalog.UiReplayNextSelect,
                ArenaFeedbackCatalog.ThreatWarning
            };

            foreach (string cue in required)
                Assert.IsTrue(ArenaFeedbackCatalog.ContainsCue(cue), $"Missing replaceable audio cue slot {cue}.");

            var names = new HashSet<string>();
            var signatures = new HashSet<string>();
            bool hasDogLifeCue = false;
            bool hasNoiseShapedCue = false;
            bool hasPitchMotion = false;
            foreach (var cue in ArenaFeedbackCatalog.RequiredAudioCues)
            {
                Assert.IsTrue(names.Add(cue.Name), $"Duplicate audio cue slot {cue.Name}.");
                Assert.IsTrue(signatures.Add(ArenaFeedbackCatalog.SignatureFor(cue)),
                    $"Audio cue {cue.Name} should have a distinct generated SFX profile.");
                Assert.Greater(cue.Seconds, 0f);
                Assert.Greater(cue.Volume, 0f);
                Assert.AreNotEqual(default(ArenaFeedbackCatalog.GeneratedSfxKind), cue.Kind);
                hasDogLifeCue |= cue.Kind == ArenaFeedbackCatalog.GeneratedSfxKind.DogBark
                    || cue.Kind == ArenaFeedbackCatalog.GeneratedSfxKind.CrunchCollect
                    || cue.Kind == ArenaFeedbackCatalog.GeneratedSfxKind.SquirrelAlarm;
                hasNoiseShapedCue |= cue.Noise > 0.2f;
                hasPitchMotion |= Mathf.Abs(cue.Sweep) > 0.3f;
            }

            Assert.IsTrue(hasDogLifeCue, "Arena feedback should use dog-life SFX profiles with authored identities.");
            Assert.IsTrue(hasNoiseShapedCue, "At least one cue should include generated texture/noise for physical action.");
            Assert.IsTrue(hasPitchMotion, "At least one cue should use pitch motion so cues are not flat tones.");
            Assert.AreEqual(ArenaFeedbackCatalog.GeneratedSfxKind.DogBark,
                ArenaFeedbackCatalog.BuildLookup()[ArenaFeedbackCatalog.Bark].Kind);
            Assert.AreEqual(ArenaFeedbackCatalog.GeneratedSfxKind.VictoryFanfare,
                ArenaFeedbackCatalog.BuildLookup()[ArenaFeedbackCatalog.MissionWin].Kind);
        }

        [UnityTest]
        public IEnumerator ArenaMovementCameraAndRangeFeel_AreConfigured_AndStateDriven()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);

            game.StartSelectedMission();
            yield return null;

            Assert.Greater(cheddar.MaxSpeedUnitsPerSecond, cocoa.MaxSpeedUnitsPerSecond);
            Assert.Greater(cheddar.AccelerationUnitsPerSecond, 25f);
            Assert.Greater(cocoa.DecelerationUnitsPerSecond, cheddar.DecelerationUnitsPerSecond);
            Assert.Greater(cocoa.TurnResponsivenessUnitsPerSecond, cheddar.TurnResponsivenessUnitsPerSecond);
            Assert.AreEqual(0.08f, cheddar.StopSpeed);
            Assert.That(cheddar.GetComponent<DogIdentity>().Tuning.inputDeadzone, Is.InRange(0.15f, 0.35f));
            Assert.That(cocoa.GetComponent<DogIdentity>().Tuning.inputDeadzone, Is.InRange(0.15f, 0.35f));
            Assert.AreEqual(cheddar.GetComponent<DogIdentity>().Tuning.inputDeadzone,
                cheddar.GetComponent<GamepadPlayerInput>().Deadzone);

            foreach (var input in Object.FindObjectsByType<CheddarAndCocoa.Input.GamepadPlayerInput>(FindObjectsSortMode.None))
                input.enabled = false;

            Vector2 cheddarStart = cheddar.transform.position;
            Vector2 cocoaStart = cocoa.transform.position;
            for (int i = 0; i < 20; i++)
            {
                cheddar.Tick(new DogController.MoveIntent { move = Vector2.right }, Time.deltaTime);
                cocoa.Tick(new DogController.MoveIntent { move = Vector2.left }, Time.deltaTime);
                yield return new WaitForFixedUpdate();
            }

            Assert.Greater(cheddar.transform.position.x - cheddarStart.x, 0.2f);
            Assert.Less(cocoa.transform.position.x - cocoaStart.x, -0.2f);
            Assert.LessOrEqual(cheddar.CurrentVelocity.magnitude, cheddar.MaxSpeedUnitsPerSecond + 0.1f);
            Assert.LessOrEqual(cocoa.CurrentVelocity.magnitude, cocoa.MaxSpeedUnitsPerSecond + 0.1f);
            Assert.AreNotEqual(Mathf.Sign(cheddar.transform.position.x - cheddarStart.x),
                Mathf.Sign(cocoa.transform.position.x - cocoaStart.x));

            for (int i = 0; i < 20; i++)
            {
                cheddar.Tick(new DogController.MoveIntent { move = Vector2.zero }, Time.deltaTime);
                cocoa.Tick(new DogController.MoveIntent { move = Vector2.zero }, Time.deltaTime);
                yield return new WaitForFixedUpdate();
            }
            Assert.Less(cheddar.CurrentVelocity.magnitude, 0.12f);
            Assert.Less(cocoa.CurrentVelocity.magnitude, 0.12f);

            var cameraRig = Camera.main.GetComponent<SharedCameraController>();
            Assert.IsNotNull(cameraRig);
            Assert.AreEqual(game.Tuning.CameraMinOrthoSize, cameraRig.MinOrthoSize);
            Assert.AreEqual(game.Tuning.CameraMaxOrthoSize, cameraRig.MaxOrthoSize);
            Assert.AreEqual(game.Tuning.CameraHorizontalMargin, cameraRig.HorizontalMargin);
            Assert.AreEqual(game.Tuning.CameraVerticalMargin, cameraRig.VerticalMargin);
            Assert.IsTrue(cameraRig.IsClampedToBounds);

            foreach (var indicator in game.InteractionRangeIndicators)
            {
                if (indicator != null) Assert.IsFalse(indicator.IsVisible);
            }

            var target = FirstTreat();
            game.SquirrelObject.transform.position = target.transform.position + Vector3.right;
            game.ForceSquirrelStealAttempt();
            yield return null;
            var squirrelRange = game.SquirrelObject.GetComponent<InteractionRangeIndicator>();
            Assert.IsTrue(squirrelRange.IsVisible);
            Assert.AreEqual("BARK RANGE", squirrelRange.Label);
            Assert.AreEqual(game.Tuning.SquirrelRangeIndicatorRadius, squirrelRange.Radius);
            Assert.IsTrue(squirrelRange.UsesGeneratedCueArt);
            Assert.AreEqual("cue_bark_range", squirrelRange.CueSpriteName);

            for (int i = 0; i < 3; i++)
            {
                FirstTreat().CollectBy(cheddar);
                yield return null;
            }
            yield return null;
            var tugRange = game.RopeObject.GetComponent<InteractionRangeIndicator>();
            Assert.IsTrue(tugRange.IsVisible);
            Assert.AreEqual("BOTH DOGS", tugRange.Label);
            Assert.AreEqual(game.Tuning.TugRangeIndicatorRadius, tugRange.Radius);
            Assert.IsTrue(tugRange.UsesGeneratedCueArt);
            Assert.AreEqual("cue_tug_range", tugRange.CueSpriteName);

            game.ForcePredatorAttack();
            yield return null;
            Assert.IsTrue(game.AnyDogGrabbed);
            bool rescueRangeVisible = false;
            foreach (var indicator in game.InteractionRangeIndicators)
            {
                if (indicator != null && indicator.IsVisible && indicator.Label == "RESCUE BARK")
                {
                    Assert.AreEqual(game.Tuning.RescueRangeIndicatorRadius, indicator.Radius);
                    Assert.IsTrue(indicator.UsesGeneratedCueArt);
                    Assert.AreEqual("cue_rescue_range", indicator.CueSpriteName);
                    rescueRangeVisible = true;
                }
            }
            Assert.IsTrue(rescueRangeVisible, "A grabbed dog should show the rescue bark range.");
        }

        [UnityTest]
        public IEnumerator BackyardMission_Objectives_Hazards_Tug_Clear_AndRestart()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            DogController cheddar = null, cocoa = null;
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                var dc = id.GetComponent<DogController>();
                if (id.Id == DogId.Cheddar) cheddar = dc;
                else if (id.Id == DogId.Cocoa) cocoa = dc;
            }
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            Assert.IsTrue(game.MissionSelectVisible);
            Assert.AreEqual(GameManager.FlowState.MissionSelect, game.CurrentFlow);
            Assert.AreEqual(22, game.MissionSelectOptionCount);
            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, game.SelectedMissionVariant);
            Assert.AreEqual("Backyard Rescue", game.SelectedMissionName);
            Assert.That(game.SelectedMissionReadinessLabel, Does.Contain("Readability gate: READY"));
            Assert.That(game.SelectedMissionReadinessLabel, Does.Contain("Rescue + bait-and-switch"));
            Assert.AreEqual(GameManager.MissionVariant.OperationPeeBreak, game.CouchTestFocusVariant);
            Assert.That(game.CouchTestFocusLabel, Does.Contain("COUCH TEST FOCUS"));
            Assert.That(game.CouchTestFocusLabel, Does.Contain("Operation Pee Break"));
            Assert.That(game.FamilyShowcaseShortcutLabel, Does.Contain("F7 Backyard"));
            Assert.That(game.FamilyShowcaseShortcutLabel, Does.Contain("F6 Kitchen"));
            Assert.That(game.FamilyShowcaseShortcutLabel, Does.Contain("F8 Weenies"));
            Assert.That(game.FamilyShowcaseShortcutLabel, Does.Contain("F9 Walkies"));
            Assert.That(game.FamilyShowcaseShortcutLabel, Does.Contain("F5 Pee"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Choose a mission"));
            Assert.AreEqual(0, game.SessionMissionsPlayed);
            Assert.AreEqual(0, game.SessionTotalScore);

            game.StartSelectedMission();
            yield return null;

            Assert.AreEqual(GameManager.FlowState.Playing, game.CurrentFlow);
            Assert.AreEqual(GameManager.State.Playing, game.Phase);
            Assert.IsNotEmpty(game.ActiveModifierLabel);
            Assert.IsNotNull(game.SquirrelObject);
            Assert.IsNotNull(game.PredatorObject);
            Assert.IsNotNull(game.RopeObject);
            Assert.IsNotEmpty(game.LastCue);
            Assert.That(game.MissionIntroPrompt, Is.EqualTo("Cheddar + Cocoa must protect the weenies together."));
            Assert.That(game.ActiveMissionReadinessLabel, Does.Contain("Readability gate: READY"));
            Assert.That(game.ActiveMissionReadinessLabel, Does.Contain("Rescue + bait-and-switch"));
            Assert.That(game.MissionBanner, Does.Contain("protect the weenies"));
            Assert.IsTrue(game.MissionBriefingVisible, "The opening goal card must remain visible long enough to orient first-time players.");
            Assert.That(game.ObjectiveLabel, Does.Contain("Save weenies"));
            Assert.AreEqual(GameManager.FeedbackKind.Intro, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.None, game.LastJuiceFeedback);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.LastScoreDelta);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.That(game.LastScoreEventLabel, Does.Contain("READY"));
            Assert.GreaterOrEqual(game.PlaytestLog.Count, 2);
            Assert.IsTrue(LogContains(game, "MissionStarted: Backyard Rescue"));
            Assert.IsTrue(LogContains(game, "ObjectiveChanged: Save weenies"));
            Assert.IsFalse(game.ReplayPromptVisible);
            Assert.IsEmpty(game.EndSummaryLabel);
            Assert.IsNotNull(game.SquirrelObject.GetComponent<MissionActorFeedback>());
            Assert.IsNotNull(game.PredatorObject.GetComponent<MissionActorFeedback>());
            Assert.IsNotNull(game.RopeObject.GetComponent<MissionActorFeedback>());
            AssertHasChildren(game.SquirrelObject.transform, ArenaArtCatalog.ActorPartNames(ArenaArtCatalog.ActorKind.Squirrel));
            AssertHasChildren(game.PredatorObject.transform, ArenaArtCatalog.ActorPartNames(ArenaArtCatalog.ActorKind.Predator));
            AssertHasChildren(game.RopeObject.transform, ArenaArtCatalog.ActorPartNames(ArenaArtCatalog.ActorKind.Rope));
            Assert.IsNotNull(game.SquirrelObject.transform.Find(ArenaDraftArt.SquirrelBadgeName));
            Assert.IsNotNull(game.PredatorObject.transform.Find(ArenaDraftArt.EagleBadgeName));
            Assert.IsNotNull(game.PredatorObject.transform.Find(ArenaDraftArt.CoyoteBadgeName));
            Assert.IsNotNull(game.RopeObject.transform.Find(ArenaDraftArt.BackyardPropsBadgeName));
            Assert.IsNotNull(GameObject.Find(ArenaDraftArt.BunnyCameoName));
            Assert.IsNotNull(game.GetComponent<AudioSource>());
            Assert.IsNotNull(Camera.main.GetComponent<AudioListener>());
            Assert.IsNotNull(GameObject.Find(ArenaArtCatalog.ArenaHudObjectName));
            Assert.IsNotNull(GameObject.Find(ArenaArtCatalog.DebugHudObjectName));

            var cheddarFeedback = cheddar.GetComponent<DogReadabilityFeedback>();
            var cocoaFeedback = cocoa.GetComponent<DogReadabilityFeedback>();
            Assert.IsNotNull(cheddarFeedback);
            Assert.IsNotNull(cocoaFeedback);
            Assert.That(cheddarFeedback.IdentityLabel, Does.Contain("CHEDDAR CHAOS PUP"));
            Assert.That(cocoaFeedback.IdentityLabel, Does.Contain("COCOA SPOT QUEEN"));
            Assert.That(cheddarFeedback.ArtDirectionSignature, Does.Contain("golden-chaos"));
            Assert.That(cocoaFeedback.ArtDirectionSignature, Does.Contain("chocolate-spot"));
            Assert.IsTrue(cheddarFeedback.UsesAuthoredPoseArt);
            Assert.IsTrue(cocoaFeedback.UsesAuthoredPoseArt);
            Assert.IsTrue(cheddarFeedback.HasShowcasePersonalityPolish);
            Assert.IsTrue(cocoaFeedback.HasShowcasePersonalityPolish);
            Assert.That(cheddarFeedback.ShowcasePersonalitySignature, Does.Contain("Cheddar"));
            Assert.That(cocoaFeedback.ShowcasePersonalitySignature, Does.Contain("Cocoa"));
            Assert.That(cheddarFeedback.AuthoredPoseSpriteName, Does.StartWith("cheddar_idle"));
            Assert.That(cocoaFeedback.AuthoredPoseSpriteName, Does.StartWith("cocoa_idle"));
            AssertHasChildren(cheddar.transform, ArenaArtCatalog.DogPartNames(DogId.Cheddar));
            AssertHasChildren(cocoa.transform, ArenaArtCatalog.DogPartNames(DogId.Cocoa));
            AssertDogShowcasePolishIsCosmetic(cheddar.transform);
            AssertDogShowcasePolishIsCosmetic(cocoa.transform);
            Assert.IsNotNull(cheddar.transform.Find(ArenaDraftArt.CheddarPortraitBadgeName));
            Assert.IsNotNull(cocoa.transform.Find(ArenaDraftArt.CocoaPortraitBadgeName));
            Assert.IsNotNull(cheddar.transform.Find(ArenaArtCatalog.DogLabel.Name));
            Assert.IsNotNull(cocoa.transform.Find(ArenaArtCatalog.DogLabel.Name));
            Assert.IsNotNull(cheddar.transform.Find(ArenaArtCatalog.ObjectiveArrowLabel.Name));
            Assert.IsNotNull(cocoa.transform.Find(ArenaArtCatalog.ObjectiveArrowLabel.Name));
            Assert.IsNotNull(cheddar.transform.Find("ObjectiveArrowCue"));
            Assert.IsNotNull(cocoa.transform.Find("ObjectiveArrowCue"));
            Assert.IsNotNull(game.ObjectiveArrows);
            Assert.AreEqual(2, game.ObjectiveArrows.Length);
            Assert.IsNotNull(game.ObjectiveArrows[0]);
            Assert.IsNotNull(game.ObjectiveArrows[1]);
            Assert.IsTrue(game.ObjectiveArrows[0].UsesGeneratedCueArt);
            Assert.IsTrue(game.ObjectiveArrows[1].UsesGeneratedCueArt);
            Assert.AreEqual("cue_objective_arrow", game.ObjectiveArrows[0].CueSpriteName);
            Assert.AreEqual("cue_objective_arrow", game.ObjectiveArrows[1].CueSpriteName);
            Assert.That(game.SquirrelObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("WAITING"));
            float introGuard = 0f;
            while (introGuard < 3f)
            {
                introGuard += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(0, game.StolenFood, "First squirrel steal should wait long enough for players to orient.");

            cheddar.Bark();
            yield return null;
            Assert.IsTrue(LogContains(game, "Bark: Cheddar"));
            Assert.AreEqual(DogReadabilityFeedback.Pose.Bark, cheddarFeedback.CurrentPose);
            Assert.That(cheddarFeedback.AuthoredPoseSpriteName, Does.StartWith("cheddar_bark"));
            Assert.That(cheddarFeedback.IdentityLabel, Does.Contain("WOOF!"));
            Assert.AreEqual(GameManager.FeedbackKind.SoloBark, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.BarkBurst, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("BARK BURST"));
            Assert.IsNotNull(GameObject.Find(ArenaArtCatalog.BarkFeedback.BurstName));
            Assert.IsNotNull(GameObject.Find(ArenaArtCatalog.BarkFeedback.RingName));
            Assert.IsNotNull(GameObject.Find(BarkEffect.FinalBurstName));

            var rb = cheddar.GetComponent<Rigidbody2D>();
            cheddar.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            rb.linearVelocity = Vector2.left;
            float intentGuard = 0f;
            while (cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Bark && intentGuard < 0.6f)
            {
                intentGuard += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(DogReadabilityFeedback.Pose.Run, cheddarFeedback.CurrentPose);
            Assert.AreEqual("FacingLeft", cheddarFeedback.FacingIntentLabel);
            rb.linearVelocity = Vector2.zero;
            game.Restart();
            yield return null;

            var treats = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None);
            Assert.Greater(treats.Length, 0);
            AssertHasChildren(treats[0].transform, ArenaArtCatalog.CollectiblePartNames(GameManager.MissionVariant.BackyardRescue));
            int scoreBefore = game.Score;
            treats[0].CollectBy(cheddar);
            Assert.AreEqual(scoreBefore + 50, game.Score);
            Assert.AreEqual(50, game.LastScoreDelta);
            Assert.AreEqual("+50 WEENIE SAVED", game.LastScoreEventLabel);
            Assert.IsTrue(LogContains(game, "ScoreDelta: +50 WEENIE SAVED"));
            Assert.AreEqual("+50 WEENIE SAVED", game.LastScorePopLabel);
            Assert.IsTrue(game.ScorePopVisible);
            Assert.IsTrue(FindWorldPopContaining("+50"));
            Assert.AreEqual(1, game.BreakfastRecovered);
            yield return null;

            cocoa.transform.position = cheddar.transform.position + Vector3.right * 6f;
            int unitedBefore = game.UnitedBarks;
            cheddar.Bark(); cocoa.Bark();
            Assert.AreEqual(unitedBefore, game.UnitedBarks, "United bark should require close dogs.");

            cocoa.transform.position = cheddar.transform.position + Vector3.right;
            cheddar.Bark(); cocoa.Bark();
            Assert.Greater(game.UnitedBarks, unitedBefore, "Close timed barks should count.");
            Assert.AreEqual(GameManager.FeedbackKind.UnitedBark, game.LastFeedback);
            Assert.AreEqual(100, game.LastScoreDelta);
            Assert.AreEqual("+100 UNITED BARK", game.LastScoreEventLabel);

            // Squirrel pressure: it can steal if ignored after being placed on food.
            game.Restart();
            yield return null;
            Assert.AreEqual(0, game.Score);
            var target = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0];
            game.SquirrelObject.transform.position = target.transform.position + Vector3.right;
            game.ForceSquirrelStealAttempt();
            Assert.That(game.ObjectiveLabel, Does.Contain("Bark to scare squirrel"));
            Assert.AreEqual(GameManager.FeedbackKind.SquirrelStealing, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.WarningMiss, game.LastJuiceFeedback);
            game.SquirrelObject.transform.position = target.transform.position;
            float guard = 0f;
            while (game.StolenFood == 0 && guard < 4f) { guard += Time.deltaTime; yield return null; }
            Assert.GreaterOrEqual(game.StolenFood, 1, "Squirrel should eventually steal breakfast.");
            Assert.Less(game.Score, 0);
            Assert.Less(game.LastScoreDelta, 0);
            Assert.That(game.LastScoreEventLabel, Does.Contain("SQUIRREL GOT ONE"));
            Assert.That(game.LastScorePopLabel, Does.Contain("SQUIRREL GOT ONE"));
            Assert.AreEqual(GameManager.FeedbackKind.SquirrelStoleFood, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.WarningMiss, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("SQUIRREL STOLE"));
            Assert.That(game.SquirrelObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("GOT A WEENIE"));
            Assert.IsTrue(FindWorldPopContaining("MISS"));

            // Backyard bark pressure only redirects when Cocoa is holding the escape gap.
            target = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0];
            game.ForceSquirrelStealAttempt();
            game.SquirrelObject.transform.position = target.transform.position;
            cheddar.transform.position = game.SquirrelObject.transform.position;
            cocoa.transform.position = game.BackyardTrapGapPosition;
            scoreBefore = game.Score;
            cheddar.Bark();
            Assert.Greater(game.Score, scoreBefore, "Barking near squirrel should affect game state.");
            Assert.AreEqual(25, game.LastScoreDelta);
            Assert.AreEqual("+25 SQUIRREL REDIRECTED", game.LastScoreEventLabel);
            Assert.That(game.LastCue, Does.Contain("squirrel").IgnoreCase);
            Assert.AreEqual(GameManager.FeedbackKind.SquirrelScared, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.SuccessPop, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("SQUIRREL REDIRECT"));
            Assert.That(game.SquirrelObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("PARTNER RECOVER"));
            Assert.IsTrue(game.BackyardTrapState.WeenieDropped);
            game.ForceBackyardTrapRecovery(DogId.Cocoa);

            // Predator warning/attack can be resolved by united bark.
            cheddar.transform.position = new Vector3(-4f, 4f, 0f);
            cocoa.transform.position = new Vector3(4f, 4f, 0f);
            game.ForcePredatorWarning();
            yield return null;
            Assert.AreEqual(GameManager.State.PredatorWarning, game.Phase);
            Assert.AreEqual(GameManager.FeedbackKind.PredatorHuddle, game.LastFeedback);
            Assert.That(game.ObjectiveLabel, Does.Contain("Huddle + bark"));
            Assert.That(game.PredatorObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("HUDDLE"));
            Assert.That(game.PredatorObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("DOUBLE BARK"));
            Assert.That(game.ObjectiveArrows[0].Label, Does.Contain("HUDDLE + BARK"));
            cocoa.transform.position = cheddar.transform.position + Vector3.right;
            scoreBefore = game.Score;
            cheddar.Bark(); cocoa.Bark();
            Assert.IsTrue(game.PredatorResolved);
            Assert.GreaterOrEqual(game.Score, scoreBefore + 400);
            Assert.AreEqual(300, game.LastScoreDelta);
            Assert.AreEqual("+300 PREDATOR YEETED", game.LastScoreEventLabel);
            Assert.That(game.LastCue, Does.Contain("predator").IgnoreCase);
            Assert.AreEqual(GameManager.FeedbackKind.UnitedBark, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.SuccessPop, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("PREDATOR YEETED"));
            Assert.That(game.PredatorObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("DOUBLE WOOF"));

            // Failed predator attack stuns/grabs, then the partner rescues by coming close and barking.
            game.Restart();
            game.ForcePredatorAttack();
            yield return null;
            Assert.AreEqual(GameManager.State.PredatorAttack, game.Phase);
            Assert.IsTrue(game.AnyDogGrabbed);
            Assert.AreEqual(GameManager.FeedbackKind.PredatorAttack, game.LastFeedback);
            Assert.That(game.ObjectiveLabel, Does.Contain("Rescue"));
            Assert.AreEqual(-150, game.LastScoreDelta);
            Assert.AreEqual("-150 PREDATOR HIT", game.LastScoreEventLabel);
            Assert.AreEqual("-150 PREDATOR HIT", game.LastScorePopLabel);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.WarningMiss, game.LastJuiceFeedback);
            Assert.IsTrue(cheddar.Mode == MovementMode.Stunned || cocoa.Mode == MovementMode.Stunned);
            Assert.That(game.PredatorObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("PARTNER BARK"));
            Assert.IsTrue(cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Stunned ||
                          cocoaFeedback.CurrentPose == DogReadabilityFeedback.Pose.Stunned);
            var stunnedFeedback = cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Stunned
                ? cheddarFeedback : cocoaFeedback;
            Assert.That(stunnedFeedback.AuthoredPoseSpriteName, Does.Contain("_stunned_e_"));
            Assert.AreEqual("Stunned", stunnedFeedback.MotionClipLabel);
            Assert.IsTrue(game.ObjectiveArrows[0].Label.Contains("BARK RESCUE") ||
                          game.ObjectiveArrows[1].Label.Contains("BARK RESCUE"));
            cheddar.transform.position = cocoa.transform.position;
            cheddar.Bark(); cocoa.Bark();
            yield return null;
            Assert.IsFalse(game.AnyDogGrabbed, "Partner bark should rescue grabbed dog.");
            Assert.AreEqual(250, game.LastScoreDelta);
            Assert.AreEqual("+250 PARTNER RESCUE", game.LastScoreEventLabel);
            Assert.AreEqual(GameManager.FeedbackKind.PartnerRescue, game.LastFeedback);
            Assert.IsTrue(LogContains(game, "Rescue:"));
            Assert.AreEqual(GameManager.JuiceFeedbackKind.SuccessPop, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("RESCUE POP"));
            Assert.IsTrue(cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Rescued ||
                          cocoaFeedback.CurrentPose == DogReadabilityFeedback.Pose.Rescued ||
                          cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Proud ||
                          cocoaFeedback.CurrentPose == DogReadabilityFeedback.Pose.Proud);
            Assert.IsTrue(cheddarFeedback.AuthoredPoseSpriteName.Contains("_rescued_e_") ||
                          cocoaFeedback.AuthoredPoseSpriteName.Contains("_rescued_e_") ||
                          cheddarFeedback.AuthoredPoseSpriteName.Contains("_proud_e_") ||
                          cocoaFeedback.AuthoredPoseSpriteName.Contains("_proud_e_"));

            // Tug completes when both dogs coordinate on the rope.
            game.Restart();
            for (int i = 0; i < 3; i++)
            {
                Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0].CollectBy(cheddar);
                yield return null;
            }
            game.RopeObject.transform.position = Vector3.zero;
            cheddar.transform.position = Vector3.left * 4f;
            cocoa.transform.position = Vector3.right * 4f;
            yield return null;
            Assert.That(game.ObjectiveArrows[0].Label, Does.Contain("BOTH TUG"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Both dogs tug"));
            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = Vector3.right * 4f;
            yield return null;
            Assert.AreEqual(GameManager.FeedbackKind.TugNeedsPartner, game.LastFeedback);
            Assert.That(game.RopeObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("NEEDS BOTH DOGS"));
            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = Vector3.right * 0.5f;
            guard = 0f;
            while (!game.TugComplete && guard < 4f) { guard += Time.deltaTime; yield return null; }
            Assert.IsTrue(game.TugComplete);
            Assert.AreEqual(200, game.LastScoreDelta);
            Assert.AreEqual("+200 TUG COMPLETE", game.LastScoreEventLabel);
            Assert.IsTrue(LogContains(game, "TugComplete: Rope objective complete"));
            Assert.AreEqual(GameManager.FeedbackKind.TugTogether, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.SuccessPop, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("TUG POP"));
            Assert.That(game.RopeObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("TEAM CHOMP"));
            Assert.IsTrue(cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Tug ||
                          cocoaFeedback.CurrentPose == DogReadabilityFeedback.Pose.Tug);
            var tugFeedback = cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Tug
                ? cheddarFeedback : cocoaFeedback;
            Assert.That(tugFeedback.AuthoredPoseSpriteName, Does.Contain("_tug_e_"));
            Assert.AreEqual("Tug", tugFeedback.MotionClipLabel);

            // Level clear requires food, the two-pass squirrel trap, tug, and predator resolution.
            game.ForcePredatorWarning();
            cheddar.Bark(); cocoa.Bark();
            game.ForceBackyardTrapRedirect(DogId.Cheddar, true);
            game.ForceBackyardTrapRecovery(DogId.Cocoa);
            game.ForceBackyardTrapRedirect(DogId.Cocoa, true);
            game.ForceBackyardTrapRecovery(DogId.Cheddar);
            Assert.IsTrue(game.BackyardTrapState.Complete);
            guard = 0f;
            while (game.BreakfastRecovered < game.BreakfastGoal && guard < 5f)
            {
                var t = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0];
                t.CollectBy(cheddar);
                guard += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(GameManager.State.LevelClear, game.Phase);
            Assert.GreaterOrEqual(game.StarRating, 1);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.IsTrue(game.EndScreenVisible);
            Assert.IsTrue(game.EndReplayAvailable);
            Assert.IsTrue(game.EndNextMissionAvailable);
            Assert.IsTrue(game.EndMissionSelectAvailable);
            Assert.AreEqual("MISSION COMPLETE", game.EndHeadlineLabel);
            Assert.That(game.EndScoreLabel, Does.Contain($"Score {game.Score}"));
            Assert.That(game.EndScoreLabel, Does.Contain($"Stars {game.StarRating}/3"));
            Assert.That(game.EndBestScoreLabel, Does.Contain("Best"));
            Assert.AreEqual("Replay", game.EndReplayActionLabel);
            Assert.AreEqual("Next Mission", game.EndNextActionLabel);
            Assert.AreEqual("Mission Select", game.EndMissionSelectActionLabel);
            Assert.AreEqual(1, game.SessionMissionsPlayed);
            Assert.AreEqual(game.Score, game.SessionTotalScore);
            Assert.AreEqual(game.StarRating, game.SessionStarsEarned);
            Assert.AreEqual(1, game.SessionUniqueMissionsCompleted);
            Assert.That(game.ReplayPromptLabel, Does.Contain("replay"));
            Assert.AreEqual("Pawfect Yard", game.EndRank);
            Assert.That(game.EndSummaryLabel, Does.Contain("Clear"));
            Assert.That(game.EndSummaryLabel, Does.Contain(game.Score.ToString()));
            Assert.That(game.EndSummaryLabel, Does.Contain(game.EndRank));
            Assert.That(game.EndReasonLabel, Does.Contain("Tiny legends"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Backyard saved"));
            Assert.That(game.LastScoreEventLabel, Does.Contain("LEVEL CLEAR"));
            Assert.AreEqual(GameManager.FeedbackKind.LevelClear, game.LastFeedback);
            Assert.IsTrue(LogContains(game, "MissionClear: Clear"));
            Assert.That(game.MissionBanner, Does.Contain("BACKYARD SAVED"));
            yield return null;
            Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, cheddarFeedback.CurrentPose);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, cocoaFeedback.CurrentPose);
            Assert.That(cheddarFeedback.AuthoredPoseSpriteName, Does.Contain("cheddar_proud_e_"));
            Assert.That(cocoaFeedback.AuthoredPoseSpriteName, Does.Contain("cocoa_proud_e_"));

            game.Restart();
            game.ForceGameOver();
            Assert.IsTrue(game.IsGameOver);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(-100, game.LastScoreDelta);
            Assert.AreEqual("-100 GAME OVER", game.LastScoreEventLabel);
            Assert.AreEqual("Needs More Bark", game.EndRank);
            Assert.That(game.EndSummaryLabel, Does.Contain("Failed"));
            Assert.That(game.EndReasonLabel, Does.Contain("Needs more bark"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Mission failed"));
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.IsTrue(game.EndReplayAvailable);
            Assert.IsTrue(game.EndNextMissionAvailable);
            Assert.IsTrue(game.EndMissionSelectAvailable);
            Assert.AreEqual("MISSION FAILED", game.EndHeadlineLabel);
            Assert.That(game.EndScoreLabel, Does.Contain($"Score {game.Score}"));
            Assert.That(game.EndScoreLabel, Does.Contain("Stars 0/3"));
            Assert.That(game.EndBestScoreLabel, Does.Contain("Best"));
            Assert.AreEqual("Replay", game.EndReplayActionLabel);
            Assert.AreEqual("Next Mission", game.EndNextActionLabel);
            Assert.AreEqual("Mission Select", game.EndMissionSelectActionLabel);
            Assert.That(game.ReplayPromptLabel, Does.Contain("replay"));
            Assert.AreEqual(GameManager.FeedbackKind.GameOver, game.LastFeedback);
            Assert.IsTrue(LogContains(game, "MissionFail: Failed"));
            Assert.That(game.MissionBanner, Does.Contain("MISSION FAILED"));
            yield return null;
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, cheddarFeedback.CurrentPose);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, cocoaFeedback.CurrentPose);
            Assert.That(cheddarFeedback.AuthoredPoseSpriteName, Does.Contain("cheddar_sad_e_"));
            Assert.That(cocoaFeedback.AuthoredPoseSpriteName, Does.Contain("cocoa_sad_e_"));
            game.Restart();
            Assert.AreEqual(GameManager.FlowState.Playing, game.CurrentFlow);
            Assert.AreEqual(GameManager.State.Playing, game.Phase);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.IsFalse(game.ReplayPromptVisible);
            Assert.IsEmpty(game.EndHeadlineLabel);
            Assert.IsEmpty(game.EndScoreLabel);
            Assert.IsEmpty(game.EndBestScoreLabel);
            Assert.IsEmpty(game.EndSummaryLabel);
            Assert.IsEmpty(game.EndReasonLabel);
            Assert.That(game.ObjectiveLabel, Does.Contain("Save weenies"));
        }

        [UnityTest]
        public IEnumerator PlaytestOverlay_Toggles_AndEventLogCapturesFlowEvents()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);
            Assert.IsFalse(game.PlaytestOverlayVisible);
            Assert.IsFalse(game.PlaytestModeEnabled);
            Assert.IsTrue(LogContains(game, "MissionSelect: Backyard Rescue"));
            Assert.That(game.LastPlaytestEvent, Does.Contain("ObjectiveChanged"));

            game.SetPlaytestOverlayVisible(true);
            Assert.IsTrue(game.PlaytestOverlayVisible);
            Assert.IsTrue(game.PlaytestModeEnabled);
            Assert.That(game.LastPlaytestEvent, Does.Contain("Overlay: shown"));

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsTrue(LogContains(game, "MissionStarted: Snack Heist"));
            Assert.IsTrue(LogContains(game, "ObjectiveChanged: Stash snacks"));
            Assert.Greater(game.ObjectiveChangeCount, 0);
            Assert.AreEqual(0, game.BarksUsed);
            Assert.AreEqual(0, game.FailedInteractions);
            Assert.That(game.FailPressureLabel, Does.Contain("squirrel 0/2"));
            Assert.That(game.DogPositionsLabel, Does.Contain("Cheddar"));

            FirstTreat().CollectBy(cheddar);
            Assert.IsTrue(LogContains(game, "ScoreDelta: +60 SNACK STASHED"));
            Assert.IsTrue(LogContains(game, "Collection: Cheddar collected"));
            Assert.AreEqual(60, game.Score);
            Assert.GreaterOrEqual(game.MissionDurationSeconds, 0f);

            yield return ClearCollectOnlyMission(cheddar);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(LogContains(game, "MissionClear: Clear"));

            game.Restart();
            yield return null;
            Assert.AreEqual(1, game.MissionReplayCount);
            Assert.IsTrue(LogContains(game, "Replay: Snack Heist"));
            Assert.AreEqual(0, game.BarksUsed);
            Assert.AreEqual(0, game.FailedInteractions);

            cheddar.Bark();
            Assert.AreEqual(1, game.BarksUsed);
            Assert.IsTrue(LogContains(game, "Bark: Cheddar"));

            cheddar.Interact();
            Assert.AreEqual(1, game.FailedInteractions);
            Assert.IsTrue(LogContains(game, "InteractionMiss: Cheddar"));

            game.ForceGameOver();
            Assert.IsTrue(LogContains(game, "MissionFail: Failed"));
            Assert.AreEqual(1, game.FailuresForMission(GameManager.MissionVariant.SnackHeist));
            Assert.That(game.MissionFailureSummaryLabel, Does.Contain("Snack Heist 1"));

            game.ChooseNextMission();
            yield return null;
            Assert.IsTrue(LogContains(game, "Next: SockPanic"));
            Assert.AreEqual(GameManager.MissionVariant.SockPanic, game.ActiveMissionVariant);

            game.SetPlaytestOverlayVisible(false);
            Assert.IsFalse(game.PlaytestOverlayVisible);
            Assert.That(game.LastPlaytestEvent, Does.Contain("Overlay: hidden"));
        }

        [UnityTest]
        public IEnumerator ArenaFeedback_AudioAndRumbleRequests_AreEventDrivenAndToggleable()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);
            Assert.IsTrue(game.AudioEnabled);
            Assert.IsTrue(game.RumbleEnabled);
            Assert.IsTrue(game.MusicLoopReady, "The arena should configure a looping music bed.");
            Assert.IsFalse(game.MusicMuted);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            game.ClearFeedbackRequests();
            cheddar.Bark();
            AssertHasAudioCue(game, ArenaFeedbackCatalog.Bark);
            AssertHasRumble(game, "bark");

            game.ClearFeedbackRequests();
            FirstTreat().CollectBy(cheddar);
            yield return null;
            AssertHasAudioCue(game, ArenaFeedbackCatalog.ScoreGain);
            AssertHasAudioCue(game, ArenaFeedbackCatalog.SnackSockCollect);

            game.ClearFeedbackRequests();
            int stolenBefore = game.StolenFood;
            ForceOneSquirrelSteal(game);
            yield return WaitForStolenFood(game, stolenBefore + 1);
            AssertHasAudioCue(game, ArenaFeedbackCatalog.ScorePenalty);
            AssertHasAudioCue(game, ArenaFeedbackCatalog.SquirrelStealMiss);
            AssertHasRumble(game, "squirrel_penalty");

            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
            game.ClearFeedbackRequests();
            game.ForcePredatorAttack();
            yield return null;
            cheddar.transform.position = cocoa.transform.position;
            cheddar.Bark();
            cocoa.Bark();
            yield return null;
            AssertHasAudioCue(game, ArenaFeedbackCatalog.TugRescueSuccess);
            AssertHasRumble(game, "rescue_success");

            game.StartMission(GameManager.MissionVariant.SockPanic);
            yield return null;
            game.ClearFeedbackRequests();
            yield return ClearCollectOnlyMission(cheddar);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            AssertHasAudioCue(game, ArenaFeedbackCatalog.MissionWin);
            AssertHasRumble(game, "mission_win");

            game.StartMission(GameManager.MissionVariant.SockPanic);
            yield return null;
            game.ClearFeedbackRequests();
            game.ForceGameOver();
            AssertHasAudioCue(game, ArenaFeedbackCatalog.MissionFail);
            AssertHasRumble(game, "mission_fail");

            game.ClearFeedbackRequests();
            game.Restart();
            AssertHasAudioCue(game, ArenaFeedbackCatalog.UiReplayNextSelect);

            game.ClearFeedbackRequests();
            game.SetAudioEnabled(false);
            Assert.IsTrue(game.MusicMuted);
            cheddar.Bark();
            Assert.AreEqual(0, game.AudioCueRequestCount);

            game.SetAudioEnabled(true);
            Assert.IsFalse(game.MusicMuted);
            game.SetRumbleEnabled(false);
            game.ClearFeedbackRequests();
            cheddar.Bark();
            AssertHasAudioCue(game, ArenaFeedbackCatalog.Bark);
            Assert.AreEqual(0, game.RumbleRequestCount);
        }

        [UnityTest]
        public IEnumerator Pause_DisablesPlay_AndAlwaysRestoresTimeScale()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            game.TogglePause();
            Assert.IsTrue(game.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            foreach (var input in Object.FindObjectsByType<GamepadPlayerInput>(FindObjectsSortMode.None))
                Assert.IsFalse(input.enabled);

            game.TogglePause();
            Assert.IsFalse(game.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
            foreach (var input in Object.FindObjectsByType<GamepadPlayerInput>(FindObjectsSortMode.None))
                Assert.IsTrue(input.enabled);

            game.TogglePause();
            game.ReturnToMissionSelect();
            Assert.IsFalse(game.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(game.MissionSelectVisible);
        }


        [UnityTest]
        public IEnumerator MissionFlow_Select_StartsEveryMission_AndEndActionsNavigate()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            Assert.IsTrue(game.MissionSelectVisible);

            // The selector is rendered as two columns (22 missions, rows = ceil(22/2) = 11: column 0 holds
            // indices 0-10, column 1 holds 11-21). Directional navigation must match that visible grid
            // instead of walking one linear list in every direction.
            game.SelectMission(GameManager.MissionVariant.BackyardRescue); // top-left (index 0)
            game.SelectMissionRight();
            Assert.AreEqual(GameManager.MissionVariant.CarRide, game.SelectedMissionVariant); // index 11
            game.SelectMissionBelow();
            Assert.AreEqual(GameManager.MissionVariant.GateCrash, game.SelectedMissionVariant); // index 12
            game.SelectMissionLeft();
            Assert.AreEqual(GameManager.MissionVariant.SnackHeist, game.SelectedMissionVariant); // index 1
            game.SelectMissionAbove();
            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, game.SelectedMissionVariant); // index 0
            game.SelectMissionAbove();
            Assert.AreEqual(GameManager.MissionVariant.LeashWalk, game.SelectedMissionVariant, // wraps to index 10
                "Vertical navigation should wrap within the visible column.");
            game.SelectCouchTestFocusMission();
            Assert.AreEqual(GameManager.MissionVariant.OperationPeeBreak, game.SelectedMissionVariant,
                "The couch-test focus shortcut should make the active deep slice one action away from cold start.");
            game.SelectKitchenShowcaseMission();
            Assert.AreEqual(GameManager.MissionVariant.KitchenFoodFrenzy, game.SelectedMissionVariant,
                "The family-showcase Kitchen shortcut should make the chaos warmup one action away.");
            game.SelectBackyardShowcaseMission();
            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, game.SelectedMissionVariant,
                "The family-showcase Backyard shortcut should make the safe warmup one action away.");
            game.SelectWeenieShowcaseMission();
            Assert.AreEqual(GameManager.MissionVariant.WeenieRoundup, game.SelectedMissionVariant,
                "The family-showcase Weenies shortcut should make the confidence reset one action away.");
            game.SelectWalkiesShowcaseMission();
            Assert.AreEqual(GameManager.MissionVariant.LeashWalk, game.SelectedMissionVariant,
                "The family-showcase Walkies shortcut should make the physical-comedy reset one action away.");
            game.SelectCouchTestFocusMission();
            Assert.That(game.SelectedMissionChallengeLabel, Does.Contain("Pawfect signal"));
            Assert.That(game.SelectedMissionReadinessLabel, Does.Contain("Social manipulation"));
            Assert.That(game.MissionSelectDetailsFor(GameManager.MissionVariant.OperationPeeBreak), Does.Contain("8m"));
            Assert.AreEqual(480f, GameManager.BuildMissionDefinition(GameManager.MissionVariant.OperationPeeBreak).RoundSeconds);
            Assert.That(GameManager.MissionChallengeLabelFor(GameManager.MissionVariant.OperationPeeBreak), Does.Contain("0 misreads"));

            game.SelectMission(GameManager.MissionVariant.BackyardRescue);
            Assert.That(game.SelectedMissionChallengeLabel, Does.Contain("all weenies"));
            game.StartSelectedMission();
            yield return null;
            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.FlowState.Playing, game.CurrentFlow);
            game.ReturnToMissionSelect();
            Assert.IsTrue(game.MissionSelectVisible);

            game.SelectMission(GameManager.MissionVariant.SnackHeist);
            Assert.AreEqual("NEW", game.MissionSelectStatusFor(GameManager.MissionVariant.SnackHeist));
            Assert.That(game.MissionSelectDetailsFor(GameManager.MissionVariant.SnackHeist), Does.Contain("80s"));
            game.StartSelectedMission();
            yield return null;
            Assert.AreEqual(GameManager.MissionVariant.SnackHeist, game.ActiveMissionVariant);
            Assert.That(game.ObjectiveLabel, Does.Contain("Stash snacks"));
            game.ForceGameOver();
            Assert.IsTrue(game.EndScreenVisible);
            Assert.That(game.MissionSelectStatusFor(GameManager.MissionVariant.SnackHeist), Does.StartWith("RETRY • BEST"));
            Assert.That(game.EndChallengeLabel, Does.Contain("Replay target"));
            Assert.AreEqual("Replay", game.EndReplayActionLabel);
            Assert.AreEqual("Next Mission", game.EndNextActionLabel);
            Assert.AreEqual("Mission Select", game.EndMissionSelectActionLabel);

            game.ChooseNextMission();
            yield return null;
            Assert.AreEqual(GameManager.FlowState.Playing, game.CurrentFlow);
            Assert.AreEqual(GameManager.MissionVariant.SockPanic, game.ActiveMissionVariant);

            game.ReturnToMissionSelect();
            game.SelectMission(GameManager.MissionVariant.SockPanic);
            game.StartSelectedMission();
            yield return null;
            Assert.AreEqual(GameManager.MissionVariant.SockPanic, game.ActiveMissionVariant);
            Assert.That(game.ObjectiveLabel, Does.Contain("Tip the laundry basket"));
        }

        [UnityTest]
        public IEnumerator DemoRegression_ColdStartFlowDogsCameraOverlay_StayReachable()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);

            Assert.AreEqual(GameManager.FlowState.MissionSelect, game.CurrentFlow);
            Assert.IsTrue(game.MissionSelectVisible);
            Assert.AreEqual(22, game.MissionSelectOptionCount);
            Assert.That(game.ObjectiveLabel, Does.Contain("Choose a mission"));
            Assert.IsTrue(LogContains(game, "MissionSelect: Backyard Rescue"));

            AssertMissionIdAvailable(game, GameManager.MissionVariant.BackyardRescue, "Backyard Rescue");
            AssertMissionIdAvailable(game, GameManager.MissionVariant.SnackHeist, "Snack Heist");
            AssertMissionIdAvailable(game, GameManager.MissionVariant.SockPanic, "Sock Panic");

            var cheddarIdentity = cheddar.GetComponent<DogIdentity>();
            var cocoaIdentity = cocoa.GetComponent<DogIdentity>();
            Assert.AreEqual(DogId.Cheddar, cheddarIdentity.Id);
            Assert.AreEqual(DogId.Cocoa, cocoaIdentity.Id);
            Assert.AreNotEqual(cheddarIdentity.Tuning.baseSpeed, cocoaIdentity.Tuning.baseSpeed);
            Assert.AreNotEqual(cheddarIdentity.Tuning.deceleration, cocoaIdentity.Tuning.deceleration);
            Assert.That(cheddar.GetComponent<DogReadabilityFeedback>().ArtDirectionSignature, Does.Contain("golden-chaos"));
            Assert.That(cocoa.GetComponent<DogReadabilityFeedback>().ArtDirectionSignature, Does.Contain("chocolate-spot"));

            var cameraRig = Camera.main.GetComponent<SharedCameraController>();
            Assert.IsNotNull(cameraRig);
            Assert.That(Camera.main.orthographicSize, Is.InRange(game.Tuning.CameraMinOrthoSize, game.Tuning.CameraMaxOrthoSize));

            Assert.IsNotNull(GameObject.Find(ArenaArtCatalog.ArenaHudObjectName));
            Assert.IsNotNull(GameObject.Find(ArenaArtCatalog.DebugHudObjectName));
            game.SetPlaytestOverlayVisible(true);
            Assert.IsTrue(game.PlaytestOverlayVisible);
            game.SelectMission(GameManager.MissionVariant.BackyardRescue);
            game.StartSelectedMission();
            yield return null;
            Assert.AreEqual(GameManager.FlowState.Playing, game.CurrentFlow);
            Assert.IsTrue(LogContains(game, "Overlay: shown"));

            game.ForceGameOver();
            Assert.AreEqual(GameManager.FlowState.EndScreen, game.CurrentFlow);
            Assert.IsTrue(game.EndReplayAvailable);
            Assert.IsTrue(game.EndNextMissionAvailable);
            Assert.IsTrue(game.EndMissionSelectAvailable);

            game.Restart();
            yield return null;
            Assert.AreEqual(GameManager.FlowState.Playing, game.CurrentFlow);
            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, game.ActiveMissionVariant);
            game.ForceGameOver();

            game.ReturnToMissionSelect();
            Assert.AreEqual(GameManager.FlowState.MissionSelect, game.CurrentFlow);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            game.ForceGameOver();
            game.ChooseNextMission();
            yield return null;
            Assert.AreEqual(GameManager.FlowState.Playing, game.CurrentFlow);
            Assert.AreEqual(GameManager.MissionVariant.SockPanic, game.ActiveMissionVariant);

            game.ForceGameOver();
            Assert.IsTrue(game.SessionSummaryReady);
            Assert.AreEqual("Next Mission", game.EndNextActionLabel);
            game.ChooseNextMission();
            Assert.AreEqual(GameManager.FlowState.SessionSummary, game.CurrentFlow);
            Assert.IsTrue(game.SessionSummaryVisible);
            game.ContinueSession();
            yield return null;
            Assert.AreEqual(GameManager.FlowState.Playing, game.CurrentFlow);
            Assert.AreEqual(GameManager.MissionVariant.SquirrelConspiracy, game.ActiveMissionVariant);
            Assert.IsTrue(LogContains(game, "ContinueSession: SquirrelConspiracy"));
        }

        [UnityTest]
        public IEnumerator MissionFlow_SessionTotals_UpdateAcrossTwoMissions()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);

            game.SelectMission(GameManager.MissionVariant.SnackHeist);
            game.StartSelectedMission();
            yield return null;
            yield return ClearCollectOnlyMission(cheddar);

            int firstScore = game.Score;
            int firstStars = game.StarRating;
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.AreEqual(1, game.SessionMissionsPlayed);
            Assert.AreEqual(firstScore, game.SessionTotalScore);
            Assert.AreEqual(firstStars, game.SessionStarsEarned);

            game.ChooseNextMission();
            yield return null;
            Assert.AreEqual(GameManager.MissionVariant.SockPanic, game.ActiveMissionVariant);
            yield return ClearCollectOnlyMission(cheddar);

            int secondScore = game.Score;
            int secondStars = game.StarRating;
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.AreEqual(2, game.SessionMissionsPlayed);
            Assert.AreEqual(firstScore + secondScore, game.SessionTotalScore);
            Assert.AreEqual(firstStars + secondStars, game.SessionStarsEarned);
            Assert.AreEqual(2, game.SessionUniqueMissionsCompleted);
            Assert.That(game.SessionSummaryLabel, Does.Contain("2 missions played"));
            Assert.That(game.SessionRanksEarnedLabel, Does.Contain("Snack Heist"));
            Assert.That(game.SessionRanksEarnedLabel, Does.Contain("Sock Panic"));

            game.ChooseNextMission();
            yield return null;
            Assert.AreEqual(GameManager.MissionVariant.SquirrelConspiracy, game.ActiveMissionVariant);
            game.ForceGameOver();
            Assert.AreEqual(3, game.SessionUniqueMissionsCompleted);
            Assert.IsTrue(game.SessionSummaryReady);
            Assert.AreEqual("Next Mission", game.EndNextActionLabel);

            game.ChooseNextMission();
            Assert.IsTrue(game.SessionSummaryVisible);
            Assert.That(game.SessionSummaryLabel, Does.Contain("3 missions played"));
            Assert.That(game.SessionRanksEarnedLabel, Does.Contain("Squirrel Conspiracy"));
            game.ContinueSession();
            yield return null;
            Assert.AreEqual(GameManager.MissionVariant.EagleShadowPanic, game.ActiveMissionVariant);
            game.ForceGameOver();
            Assert.AreEqual(4, game.SessionUniqueMissionsCompleted);
            Assert.IsFalse(game.SessionSummaryReady, "Summary should stay quiet until the six-mission milestone.");
            Assert.AreEqual("Next Mission", game.EndNextActionLabel);
            Assert.That(game.SessionRanksEarnedLabel, Does.Contain("+1 earlier"));
            Assert.That(game.SessionRanksEarnedLabel, Does.Not.Contain("Snack Heist"),
                "Long sessions should show recent ranks instead of overflowing the summary panel.");
            Assert.That(game.SessionRanksEarnedLabel, Does.Contain("Eagle Shadow Panic"));
        }

        [UnityTest]
        public IEnumerator SnackHeist_Initializes_Scores_Clears_Fails_AndReplays()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);

            Assert.IsTrue(game.MissionSelectVisible);
            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.SnackHeist, game.ActiveMissionVariant);
            Assert.IsInstanceOf<SnackHeistMissionController>(game.ActiveMissionController);
            Assert.AreEqual("Snack Heist", game.ActiveMissionName);
            Assert.That(game.MissionIntroPrompt, Does.Contain("forbidden snack stash"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Stash snacks"));
            Assert.IsTrue(game.SquirrelObject.activeSelf);
            Assert.IsFalse(game.PredatorObject.activeSelf);
            Assert.IsFalse(game.RopeObject.activeSelf);

            var firstSnack = FirstTreat();
            AssertHasChildren(firstSnack.transform, ArenaArtCatalog.CollectiblePartNames(GameManager.MissionVariant.SnackHeist));
            firstSnack.CollectBy(cheddar);
            Assert.AreEqual(60, game.LastScoreDelta);
            Assert.AreEqual("+60 SNACK STASHED", game.LastScoreEventLabel);
            Assert.That(game.ObjectiveLabel, Does.Contain("Stash snacks 1/4"));

            while (game.BreakfastRecovered < game.BreakfastGoal)
            {
                FirstTreat().CollectBy(cheddar);
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.AreEqual("MISSION COMPLETE", game.EndHeadlineLabel);
            Assert.AreEqual("Replay", game.EndReplayActionLabel);
            Assert.AreEqual("Next Mission", game.EndNextActionLabel);
            Assert.AreEqual("Mission Select", game.EndMissionSelectActionLabel);
            Assert.That(game.ReplayPromptLabel, Does.Contain("Snack Heist"));
            Assert.That(game.MissionBanner, Does.Contain("SNACK STASH SAVED"));
            Assert.That(game.LastScoreEventLabel, Does.Contain("SNACK HEIST CLEAR"));

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            int stolenBefore = game.StolenFood;
            ForceOneSquirrelSteal(game);
            yield return WaitForStolenFood(game, stolenBefore + 1);
            Assert.That(game.LastScoreEventLabel, Does.Contain("SNACK THIEF"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Stash snacks"));

            ForceOneSquirrelSteal(game);
            yield return WaitForOutcome(game, GameManager.MissionOutcome.Failed);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.AreEqual("MISSION FAILED", game.EndHeadlineLabel);
            Assert.AreEqual("Replay", game.EndReplayActionLabel);
            Assert.AreEqual("Next Mission", game.EndNextActionLabel);
            Assert.AreEqual("Mission Select", game.EndMissionSelectActionLabel);
            Assert.That(game.EndReasonLabel, Does.Contain("forbidden snacks"));
            Assert.That(game.ReplayPromptLabel, Does.Contain("Snack Heist"));
        }

        [UnityTest]
        public IEnumerator SockPanic_Initializes_Scores_Clears_Fails_AndReplays()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);

            Assert.IsTrue(game.MissionSelectVisible);
            game.StartMission(GameManager.MissionVariant.SockPanic);
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.SockPanic, game.ActiveMissionVariant);
            Assert.IsInstanceOf<SockPanicMissionController>(game.ActiveMissionController,
                "Sock Panic must run entirely through its own IMissionController.");
            Assert.AreEqual("Sock Panic", game.ActiveMissionName);
            Assert.That(game.MissionIntroPrompt, Does.Contain("laundry basket"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Tip the laundry basket"));
            Assert.IsFalse(game.SquirrelObject.activeSelf);
            Assert.IsFalse(game.PredatorObject.activeSelf);
            Assert.IsFalse(game.RopeObject.activeSelf);
            Assert.IsTrue(game.LaundryBasketObject.activeSelf);
            Assert.IsFalse(game.SockPanicState.BasketOpen);

            game.ForceSockBasketTip(DogId.Cocoa);
            var firstSock = game.ExposedSock;
            Assert.IsNotNull(firstSock);
            Assert.IsTrue(game.SockPanicState.BasketOpen);
            Assert.That(game.ObjectiveLabel, Does.Contain("Partner dive"));
            AssertHasChildren(firstSock.transform, ArenaArtCatalog.CollectiblePartNames(GameManager.MissionVariant.SockPanic));
            firstSock.CollectBy(cocoa);
            Assert.AreEqual(-15, game.LastScoreDelta);
            Assert.AreEqual(1, game.SockPanicState.Fumbles);
            Assert.IsFalse(game.SockPanicState.BasketOpen);

            game.ForceSockBasketTip(DogId.Cocoa);
            game.ExposedSock.CollectBy(cheddar);
            Assert.AreEqual(40, game.LastScoreDelta);
            Assert.AreEqual("+40 PARTNER SOCK DIVE", game.LastScoreEventLabel);
            Assert.AreEqual(1, game.SockPanicState.SuccessfulDives);
            Assert.That(game.ObjectiveLabel, Does.Contain("socks 1/5"));

            while (game.BreakfastRecovered < game.BreakfastGoal)
            {
                game.ForceSockBasketTip(DogId.Cocoa);
                game.ExposedSock.CollectBy(cheddar);
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.AreEqual("MISSION COMPLETE", game.EndHeadlineLabel);
            Assert.AreEqual("Replay", game.EndReplayActionLabel);
            Assert.AreEqual("Next Mission", game.EndNextActionLabel);
            Assert.AreEqual("Mission Select", game.EndMissionSelectActionLabel);
            Assert.That(game.ReplayPromptLabel, Does.Contain("Sock Panic"));
            Assert.That(game.MissionBanner, Does.Contain("SOCKS SORTED"));
            Assert.That(game.LastScoreEventLabel, Does.Contain("SOCK PANIC CLEAR"));

            game.StartMission(GameManager.MissionVariant.SockPanic);
            Assert.AreEqual(0, game.SockPanicState.SuccessfulDives);
            Assert.AreEqual(0, game.SockPanicState.Fumbles);
            Assert.IsFalse(game.SockPanicState.BasketOpen);
            game.SetRoundDuration(0.02f);
            yield return WaitForOutcome(game, GameManager.MissionOutcome.Failed);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.AreEqual("MISSION FAILED", game.EndHeadlineLabel);
            Assert.AreEqual("Replay", game.EndReplayActionLabel);
            Assert.AreEqual("Next Mission", game.EndNextActionLabel);
            Assert.AreEqual("Mission Select", game.EndMissionSelectActionLabel);
            Assert.That(game.EndReasonLabel, Does.Contain("Laundry order returned"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Sock Panic"));
        }

        private static DogController FindDog(DogId dogId)
        {
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == dogId) return id.GetComponent<DogController>();
            }

            return null;
        }

        [UnityTest]
        public IEnumerator ThreatActors_AreOnScreenAndReadable_NotOffscreenSpinningBlobs()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            // The dog identity label must sit clearly above the dog (authored body ~1.65 tall) instead
            // of overlapping and obscuring the character it labels.
            Assert.GreaterOrEqual(ArenaArtCatalog.DogLabel.LocalPosition.y, 1.3f,
                "Dog identity label should float above the dog, not on top of it.");

            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            // The waiting squirrel must spawn inside the close camera footprint, not parked in the far
            // 120x68 corner where players never see the threat the HUD/arrows reference.
            Vector3 squirrelStart = game.SquirrelObject.transform.position;
            Assert.That(game.SquirrelObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("WAITING"));
            Assert.Less(Mathf.Abs(squirrelStart.x), 16f, "Waiting squirrel should be on-screen horizontally.");
            Assert.Less(Mathf.Abs(squirrelStart.y), 11f, "Waiting squirrel should be on-screen vertically.");

            // Let the placeholder life-wobble run; it must stay bounded and readable rather than spinning
            // continuously (the old squirrel/rope spun 80/45 deg-per-second into unidentifiable blobs).
            float guard = 0f;
            float maxTilt = 0f;
            while (guard < 1.5f)
            {
                guard += Time.deltaTime;
                float tilt = Mathf.Abs(Mathf.DeltaAngle(0f, game.SquirrelObject.transform.localEulerAngles.z));
                maxTilt = Mathf.Max(maxTilt, tilt);
                yield return null;
            }
            Assert.LessOrEqual(maxTilt, 12f, "Actor wobble should stay bounded, not spin continuously.");

            // The eagle shadow must sweep across the dogs' play band so it is actually seen overhead,
            // instead of flying along the far top fence far above the camera.
            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;
            Assert.Less(Mathf.Abs(game.PredatorObject.transform.position.y), 12f,
                "Eagle shadow should sweep within the dogs' play band, not along the far top fence.");
        }

        private static Treat FirstTreat()
        {
            var treats = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None);
            Assert.Greater(treats.Length, 0);
            return treats[0];
        }

        private static IEnumerator ClearCollectOnlyMission(DogController dog)
        {
            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            while (game.BreakfastRecovered < game.BreakfastGoal)
            {
                if (game.ActiveMissionVariant == GameManager.MissionVariant.SockPanic)
                {
                    var collectorId = dog.GetComponent<DogIdentity>().Id;
                    var openerId = collectorId == DogId.Cheddar ? DogId.Cocoa : DogId.Cheddar;
                    game.ForceSockBasketTip(openerId);
                    Assert.IsNotNull(game.ExposedSock);
                    game.ExposedSock.CollectBy(dog);
                }
                else
                {
                    FirstTreat().CollectBy(dog);
                }
                yield return null;
            }
        }

        private static void ForceOneSquirrelSteal(GameManager game)
        {
            var target = FirstTreat();
            game.SquirrelObject.transform.position = target.transform.position;
            game.ForceSquirrelStealAttempt();
        }

        private static IEnumerator WaitForStolenFood(GameManager game, int target)
        {
            float guard = 0f;
            while (game.StolenFood < target && guard < 2f)
            {
                guard += Time.deltaTime;
                yield return null;
            }

            Assert.GreaterOrEqual(game.StolenFood, target);
        }

        private static IEnumerator WaitForOutcome(GameManager game, GameManager.MissionOutcome outcome)
        {
            float guard = 0f;
            while (game.Outcome != outcome && guard < 2f)
            {
                guard += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(outcome, game.Outcome);
        }

        private static bool FindWorldPopContaining(string text)
        {
            foreach (var pop in Object.FindObjectsByType<MissionWorldPop>(FindObjectsSortMode.None))
            {
                if (pop.Label.Contains(text)) return true;
            }

            return false;
        }

        private static void AssertHasAudioCue(GameManager game, string cueName)
        {
            foreach (string cue in game.AudioCueRequests)
            {
                if (cue == cueName) return;
            }

            Assert.Fail($"Expected audio cue request {cueName}.");
        }

        private static void AssertHasRumble(GameManager game, string requestName)
        {
            foreach (string request in game.RumbleRequests)
            {
                if (request == requestName) return;
            }

            Assert.Fail($"Expected rumble request {requestName}.");
        }

        private static void AssertHasChildren(Transform root, string[] childNames)
        {
            Assert.IsNotNull(root);
            foreach (string childName in childNames)
            {
                Assert.IsNotNull(root.Find(childName), $"{root.name} should expose visual replacement slot {childName}.");
            }
        }

        private static void AssertDogShowcasePolishIsCosmetic(Transform dog)
        {
            var root = dog.Find(DogShowcasePolish.RootName);
            Assert.IsNotNull(root, $"{dog.name} should carry dog-local family-showcase polish.");
            Assert.IsNull(root.GetComponentInChildren<Collider2D>(), $"{dog.name} showcase polish must not add gameplay collision.");
            Assert.GreaterOrEqual(root.GetComponentsInChildren<SpriteRenderer>().Length, 5,
                $"{dog.name} showcase polish should be visible at couch distance.");
            var polish = dog.GetComponent<DogShowcasePolish>();
            Assert.IsNotNull(polish);
            Assert.IsTrue(polish.UsesGeneratedDogFx, $"{dog.name} showcase polish should use generated dog FX sprites, not white-square-only geometry.");
        }

        private static void AssertMissionBalance(GameManager.MissionVariant variant, ArenaMissionTuning tuning, bool expectSquirrel, bool expectPredator, bool expectTug)
        {
            var mission = GameManager.BuildMissionDefinition(variant);
            Assert.That(mission.RoundSeconds, Is.InRange(30f, 90f), $"{mission.Name} should fit the 30-90 second playtest target.");
            Assert.AreEqual(expectSquirrel, mission.UsesSquirrel);
            Assert.AreEqual(expectPredator, mission.RequiresPredator);
            Assert.AreEqual(expectTug, mission.RequiresTug);
            Assert.Greater(mission.ItemGoal, 0);
            Assert.GreaterOrEqual(mission.SpawnedItemCount, mission.ItemGoal - 1);
            if (variant == GameManager.MissionVariant.SquirrelConspiracy)
                Assert.AreEqual(0, mission.ItemScore, "Squirrel Conspiracy scores through herding and stash events.");
            else
                Assert.Greater(mission.ItemScore, 0);
            Assert.Greater(mission.PawfectScore, mission.HeroScore);
            Assert.Greater(mission.HeroScore, mission.SurvivorScore);

            int likelyClearScore = mission.ItemScore * mission.ItemGoal + tuning.ClearScore + Mathf.CeilToInt(mission.RoundSeconds) * tuning.TimeBonusMultiplier;
            if (variant == GameManager.MissionVariant.SquirrelConspiracy)
            {
                likelyClearScore += ScoreEventCatalog.GoodHerd.Points * 4;
                likelyClearScore += ScoreEventCatalog.DoubleBarkBlock.Points;
                likelyClearScore += ScoreEventCatalog.StashFound.Points;
                likelyClearScore += ScoreEventCatalog.ConspiracyCracked.Points;
            }
            if (mission.RequiresPredator) likelyClearScore += tuning.PredatorDefendedScore;
            if (mission.RequiresTug) likelyClearScore += tuning.TugScore;
            Assert.GreaterOrEqual(likelyClearScore, mission.PawfectScore, $"{mission.Name} should have reachable top-rank scoring.");

            if (mission.UsesSquirrel)
            {
                Assert.Greater(mission.MaxStolenFood, 1);
                Assert.Greater(mission.SquirrelPenalty, 0);
                Assert.Greater(mission.SquirrelScareScore, 0);
            }
            else
            {
                Assert.AreEqual(0, mission.MaxStolenFood);
            }
        }

        private static void AssertMissionIdAvailable(GameManager game, GameManager.MissionVariant variant, string expectedName)
        {
            game.SelectMission(variant);
            Assert.AreEqual(variant, game.SelectedMissionVariant);
            Assert.AreEqual(expectedName, game.SelectedMissionName);
            Assert.AreEqual(expectedName, GameManager.BuildMissionDefinition(variant).Name);
        }

        private static bool LogContains(GameManager game, string text)
        {
            foreach (string entry in game.PlaytestEvents)
            {
                if (entry.Contains(text)) return true;
            }

            return false;
        }
    }
}
