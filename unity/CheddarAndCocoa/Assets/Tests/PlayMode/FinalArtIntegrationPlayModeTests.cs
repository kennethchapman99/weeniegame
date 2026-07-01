using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class FinalArtIntegrationPlayModeTests
    {
        private static readonly string[] RequiredPaths =
        {
            FinalGameplayArt.SquirrelIdle, FinalGameplayArt.SquirrelSteal, FinalGameplayArt.SquirrelScared,
            FinalGameplayArt.EagleThreat, FinalGameplayArt.EagleAction, FinalGameplayArt.CoyoteThreat,
            FinalGameplayArt.BunnyIdle, FinalGameplayArt.Weenie, FinalGameplayArt.RopeTug,
            FinalGameplayArt.RopeComplete, FinalGameplayArt.DogBowl, FinalGameplayArt.Bush,
            FinalGameplayArt.Fence, FinalGameplayArt.Rock, FinalGameplayArt.Grass, FinalGameplayArt.DigSpot,
            FinalGameplayArt.BarkBurst, FinalGameplayArt.BarkRing, FinalGameplayArt.PickupSparkle,
            FinalGameplayArt.SuccessPop, FinalGameplayArt.WarningAlert, FinalGameplayArt.RescueBurst,
            FinalGameplayArt.FailPuff,
        };

        [Test]
        public void ArenaFinal_RequiredResourcesLoadAndMissingResourcesStayOptional()
        {
            foreach (string path in RequiredPaths)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing ArenaFinal sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.MissionPropPackPass2)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated mission prop sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.EnvironmentPropPack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated environment prop sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.GameplayCuePack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated gameplay cue sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.DogFxPack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated dog FX sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.KitchenCuePack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated Kitchen cue sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.ChaosMachinePropPack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated Chaos Machine prop sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.PeeBreakPropPack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated Pee Break prop sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.BackyardRescueP0Pack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated Backyard Rescue P0 sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.SnackHeistP0Pack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated Snack Heist P0 sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.SockPanicP0Pack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated Sock Panic P0 sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.ThreatConspiracyP0Pack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated threat/conspiracy P0 sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.AdventureP0Pack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated Adventure P0 sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.HomeTripP0Pack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated Home Trip P0 sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.CoopTricksP0Pack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated Coop Tricks P0 sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.EscapeCatchKitchenP0Pack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated Escape/Catch/Kitchen P0 sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.BuildingPropPack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated building prop sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.LevelAreaPropPack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated level-area sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.HudSkinPack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated HUD skin sprite at Resources/{path}.");
            foreach (string path in FinalGameplayArt.WorldLabelSkinPack)
                Assert.IsNotNull(FinalGameplayArt.Load(path), $"Missing generated world-label skin sprite at Resources/{path}.");

            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            foreach (DogReadabilityFeedback.Pose pose in System.Enum.GetValues(typeof(DogReadabilityFeedback.Pose)))
                Assert.IsNotNull(FinalDogPoseArt.For(dog, pose), $"Missing final pose {dog}/{pose}.");

            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            foreach (var clip in new[] { CharacterMotionArt.Clip.Idle, CharacterMotionArt.Clip.Run, CharacterMotionArt.Clip.Bark })
            foreach (int frame in new[] { 0, 1, 2, 3 })
                Assert.IsNotNull(CharacterMotionArt.Load(dog, clip, CharacterMotionArt.Facing8.E, frame),
                    $"Missing Tier-A motion frame {dog}/{clip}/E/{frame}.");
            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            foreach (int frame in new[] { 0, 1, 2 })
                Assert.IsNotNull(CharacterMotionArt.Load(dog, CharacterMotionArt.Clip.Tug,
                    CharacterMotionArt.Facing8.E, frame), $"Missing tug frame {dog}/{frame}.");
            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            foreach (var facing in new[] { CharacterMotionArt.Facing8.SE, CharacterMotionArt.Facing8.S,
                         CharacterMotionArt.Facing8.N, CharacterMotionArt.Facing8.NE })
            foreach (int frame in new[] { 0, 1, 2, 3 })
                Assert.IsNotNull(CharacterMotionArt.Load(dog, CharacterMotionArt.Clip.Bark, facing, frame),
                    $"Missing directional bark frame {dog}/{facing}/{frame}.");
            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            foreach (var facing in new[] { CharacterMotionArt.Facing8.SE, CharacterMotionArt.Facing8.S,
                         CharacterMotionArt.Facing8.N, CharacterMotionArt.Facing8.NE })
            foreach (int frame in new[] { 0, 1, 2, 3 })
                Assert.IsNotNull(CharacterMotionArt.Load(dog, CharacterMotionArt.Clip.Idle, facing, frame),
                    $"Missing directional idle frame {dog}/{facing}/{frame}.");
            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            foreach (var clip in new[] { CharacterMotionArt.Clip.Stunned, CharacterMotionArt.Clip.Rescued,
                         CharacterMotionArt.Clip.Proud, CharacterMotionArt.Clip.Sad })
            foreach (int frame in new[] { 0, 1 })
                Assert.IsNotNull(CharacterMotionArt.Load(dog, clip, CharacterMotionArt.Facing8.E, frame),
                    $"Missing outcome frame {dog}/{clip}/{frame}.");
            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            foreach (int frame in new[] { 0, 1 })
                Assert.IsNotNull(CharacterMotionArt.Load(dog, CharacterMotionArt.Clip.Carry,
                    CharacterMotionArt.Facing8.E, frame), $"Missing carry frame {dog}/{frame}.");

            foreach (var clip in new[] { ThreatMotionArt.Clip.Idle, ThreatMotionArt.Clip.Run, ThreatMotionArt.Clip.Steal })
            foreach (int frame in new[] { 0, 1, 2, 3 })
                Assert.IsNotNull(ThreatMotionArt.Load(ThreatMotionArt.Actor.Squirrel, clip, frame),
                    $"Missing squirrel motion frame {clip}/{frame}.");
            foreach (int frame in new[] { 0, 1 })
                Assert.IsNotNull(ThreatMotionArt.Load(ThreatMotionArt.Actor.Squirrel, ThreatMotionArt.Clip.Scared, frame),
                    $"Missing squirrel scared frame {frame}.");
            foreach (var clip in new[] { ThreatMotionArt.Clip.Sweep, ThreatMotionArt.Clip.Attack })
            foreach (int frame in new[] { 0, 1, 2, 3 })
                Assert.IsNotNull(ThreatMotionArt.Load(ThreatMotionArt.Actor.Eagle, clip, frame),
                    $"Missing eagle motion frame {clip}/{frame}.");
            foreach (var clip in new[] { ThreatMotionArt.Clip.Patrol, ThreatMotionArt.Clip.Threaten, ThreatMotionArt.Clip.Retreat })
            foreach (int frame in new[] { 0, 1, 2, 3 })
                Assert.IsNotNull(ThreatMotionArt.Load(ThreatMotionArt.Actor.Coyote, clip, frame),
                    $"Missing coyote motion frame {clip}/{frame}.");

            Sprite motion = CharacterMotionArt.Load(DogId.Cheddar, CharacterMotionArt.Clip.Run,
                CharacterMotionArt.Facing8.E, 0);
            Assert.AreEqual(512, motion.rect.width);
            Assert.AreEqual(384, motion.rect.height);
            Assert.AreEqual(24f, motion.pivot.y, 0.1f, "Motion sprites should pivot on the normalized paw baseline.");

            Sprite threatMotion = ThreatMotionArt.Load(ThreatMotionArt.Actor.Eagle, ThreatMotionArt.Clip.Sweep, 0);
            Assert.AreEqual(512, threatMotion.rect.width);
            Assert.AreEqual(384, threatMotion.rect.height);

            Assert.IsNull(FinalGameplayArt.Load(FinalGameplayArt.Root + "missing_optional_sprite"));
        }

        [Test]
        public void FinalJuiceEffect_MapsGameplayFeedbackToDistinctReadableSprites()
        {
            Assert.AreEqual(FinalGameplayArt.PickupSparkle,
                FinalJuiceEffect.SelectSpritePath(GameManager.JuiceFeedbackKind.ScoreDelta, "+50", 50, false));
            Assert.AreEqual(FinalGameplayArt.FailPuff,
                FinalJuiceEffect.SelectSpritePath(GameManager.JuiceFeedbackKind.ScoreDelta, "-50", -50, false));
            Assert.AreEqual(FinalGameplayArt.SuccessPop,
                FinalJuiceEffect.SelectSpritePath(GameManager.JuiceFeedbackKind.SuccessPop, "STASH FOUND", 50, false));
            Assert.AreEqual(FinalGameplayArt.RescueBurst,
                FinalJuiceEffect.SelectSpritePath(GameManager.JuiceFeedbackKind.SuccessPop, "RESCUE POP", 50, false));
            Assert.AreEqual(FinalGameplayArt.WarningAlert,
                FinalJuiceEffect.SelectSpritePath(GameManager.JuiceFeedbackKind.WarningMiss, "SQUIRREL TAUNT", -50, false));
            Assert.AreEqual(FinalGameplayArt.FailPuff,
                FinalJuiceEffect.SelectSpritePath(GameManager.JuiceFeedbackKind.WarningMiss, "SAD FLOP", -50, true));
            Assert.IsNull(FinalJuiceEffect.SelectSpritePath(GameManager.JuiceFeedbackKind.BarkBurst, "BARK", 0, false));

            Assert.LessOrEqual(FinalJuiceEffect.EffectWorldWidth(FinalGameplayArt.WarningAlert), 2.4f);
            Assert.LessOrEqual(FinalJuiceEffect.EffectWorldWidth(FinalGameplayArt.SuccessPop), 1.3f);
        }

        [Test]
        public void ArtReviewCapture_ParsesOnlyExplicitOutputArgument()
        {
            Assert.AreEqual("/tmp/arena-review", ArenaArtReviewCapture.OutputDirectoryFromArgs(
                new[] { "player", "--arena-art-review=/tmp/arena-review" }));
            Assert.IsNull(ArenaArtReviewCapture.OutputDirectoryFromArgs(new[] { "player", "--unrelated" }));
        }

        [Test]
        public void CharacterMotionArt_BuildsStableDirectionalPathsAndFallsBackSafely()
        {
            Assert.AreEqual(
                "ArenaFinal/Characters/Dogs/Cheddar/Motion/cheddar_run_se_02",
                CharacterMotionArt.ResourcePath(DogId.Cheddar, CharacterMotionArt.Clip.Run,
                    CharacterMotionArt.Facing8.SE, 2));
            Assert.AreEqual(
                "ArenaFinal/Characters/Dogs/Cocoa/Motion/cocoa_bark_n_00",
                CharacterMotionArt.ResourcePath(DogId.Cocoa, CharacterMotionArt.Clip.Bark,
                    CharacterMotionArt.Facing8.N, -4));
            Assert.AreEqual("cheddar_run_e_02", CharacterMotionArt.Load(DogId.Cheddar,
                CharacterMotionArt.Clip.Run, CharacterMotionArt.Facing8.E, 2).name);
            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            foreach (var facing in new[] { CharacterMotionArt.Facing8.SE, CharacterMotionArt.Facing8.NE,
                         CharacterMotionArt.Facing8.S, CharacterMotionArt.Facing8.N })
            foreach (int frame in new[] { 0, 1, 2, 3 })
                Assert.IsNotNull(CharacterMotionArt.Load(dog, CharacterMotionArt.Clip.Run, facing, frame));
            Assert.AreEqual("cocoa_idle", CharacterMotionArt.LoadOrFallback(DogId.Cocoa,
                CharacterMotionArt.Clip.Carry, CharacterMotionArt.Facing8.N, 1).name);

            Assert.AreEqual(0, CharacterMotionArt.FrameAtTime(DogId.Cheddar, CharacterMotionArt.Clip.Idle, 0f));
            Assert.AreEqual(2, CharacterMotionArt.FrameAtTime(DogId.Cheddar, CharacterMotionArt.Clip.Run, 0.2f));
            Assert.AreEqual(3, CharacterMotionArt.FrameAtTime(DogId.Cocoa, CharacterMotionArt.Clip.Bark, 10f));
            Assert.AreEqual(2, CharacterMotionArt.FrameAtTime(DogId.Cheddar, CharacterMotionArt.Clip.Tug, 2f / 9f));
            Assert.AreEqual(0, CharacterMotionArt.FrameAtTime(DogId.Cheddar, CharacterMotionArt.Clip.Tug, 1f / 3f));
            Assert.AreEqual(1, CharacterMotionArt.FrameAtTime(DogId.Cheddar, CharacterMotionArt.Clip.Proud, 0.2f));
            Assert.AreEqual(0, CharacterMotionArt.FrameAtTime(DogId.Cheddar, CharacterMotionArt.Clip.Proud, 0.4f));
            Assert.AreEqual(CharacterMotionArt.Facing8.NE,
                CharacterMotionArt.FacingForDirection(new Vector2(-1f, 1f), out bool mirror));
            Assert.IsTrue(mirror);
            Assert.AreEqual(CharacterMotionArt.Facing8.SE,
                CharacterMotionArt.FacingForDirection(new Vector2(1f, -1f), out mirror));
            Assert.IsFalse(mirror);
            Assert.AreEqual(CharacterMotionArt.Facing8.N,
                CharacterMotionArt.FacingForDirection(Vector2.up, out mirror));
            Assert.IsFalse(mirror);
            Assert.AreEqual(CharacterMotionArt.Facing8.S,
                CharacterMotionArt.FacingForDirection(Vector2.down, out mirror));
        }

        [Test]
        public void ThreatMotionArt_BuildsStablePathsAndInfersReadableClips()
        {
            Assert.AreEqual(
                "ArenaFinal/Characters/Squirrel/Motion/squirrel_steal_e_02",
                ThreatMotionArt.ResourcePath(ThreatMotionArt.Actor.Squirrel, ThreatMotionArt.Clip.Steal, 2));
            Assert.AreEqual(
                "ArenaFinal/Characters/Eagle/Motion/eagle_attack_e_00",
                ThreatMotionArt.ResourcePath(ThreatMotionArt.Actor.Eagle, ThreatMotionArt.Clip.Attack, -4));
            Assert.AreEqual("squirrel_run_e_01",
                ThreatMotionArt.Load(ThreatMotionArt.Actor.Squirrel, ThreatMotionArt.Clip.Run, 1).name);
            Assert.AreEqual(2, ThreatMotionArt.FrameAtTime(ThreatMotionArt.Actor.Coyote,
                ThreatMotionArt.Clip.Threaten, 0.25f));

            Assert.IsTrue(ThreatMotionArt.TryInfer("SQUIRREL SNACK HEIST - BARK!",
                ThreatMotionArt.Actor.Squirrel, out var actor, out var clip));
            Assert.AreEqual(ThreatMotionArt.Actor.Squirrel, actor);
            Assert.AreEqual(ThreatMotionArt.Clip.Steal, clip);

            Assert.IsTrue(ThreatMotionArt.TryInfer("EAGLE SHADOW SWEEP - HIDE IN COVER!",
                ThreatMotionArt.Actor.Unknown, out actor, out clip));
            Assert.AreEqual(ThreatMotionArt.Actor.Eagle, actor);
            Assert.AreEqual(ThreatMotionArt.Clip.Sweep, clip);

            Assert.IsTrue(ThreatMotionArt.TryInfer("COYOTE DRIVEN BACK!",
                ThreatMotionArt.Actor.Unknown, out actor, out clip));
            Assert.AreEqual(ThreatMotionArt.Actor.Coyote, actor);
            Assert.AreEqual(ThreatMotionArt.Clip.Retreat, clip);

            Assert.IsFalse(ThreatMotionArt.TryInfer("WEAK SPOT - FILL DIRT",
                ThreatMotionArt.Actor.Squirrel, out _, out _));
        }

        [UnityTest]
        public IEnumerator BackyardRescue_UsesFinalSpritesWithoutReplacingGameplayObjects()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            var juice = game.GetComponent<FinalJuiceEffect>();
            Assert.IsNotNull(juice);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return new WaitForSeconds(0.3f);

            var cheddar = GameObject.Find("Cheddar");
            var cocoa = GameObject.Find("Cocoa");
            Assert.That(cheddar.GetComponent<DogReadabilityFeedback>().AuthoredPoseSpriteName, Does.Contain("cheddar_idle"));
            Assert.That(cocoa.GetComponent<DogReadabilityFeedback>().AuthoredPoseSpriteName, Does.Contain("cocoa_idle"));
            Assert.That(cheddar.GetComponent<DogReadabilityFeedback>().MotionClipLabel, Is.EqualTo("Idle"));
            Assert.That(cocoa.GetComponent<DogReadabilityFeedback>().MotionFrameIndex, Is.InRange(0, 3));
            var cheddarInput = cheddar.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>();
            cheddarInput.enabled = false;
            var cheddarBody = cheddar.GetComponent<Rigidbody2D>();
            cheddarBody.linearVelocity = Vector2.right * 4f;
            yield return new WaitForSeconds(0.14f);
            var cheddarMotion = cheddar.GetComponent<DogReadabilityFeedback>();
            Assert.AreEqual("Run", cheddarMotion.MotionClipLabel);
            Assert.That(cheddarMotion.AuthoredPoseSpriteName, Does.StartWith("cheddar_run_e_"));
            Assert.IsFalse(cheddar.transform.Find("CheddarAuthoredPose").GetComponent<SpriteRenderer>().flipX);
            cheddarBody.linearVelocity = Vector2.left * 4f;
            yield return new WaitForSeconds(0.14f);
            Assert.IsTrue(cheddar.transform.Find("CheddarAuthoredPose").GetComponent<SpriteRenderer>().flipX,
                "West travel should mirror the approved east-facing strip until west art is promoted.");
            cheddarBody.linearVelocity = Vector2.up * 4f;
            yield return new WaitForSeconds(0.14f);
            Assert.That(cheddarMotion.AuthoredPoseSpriteName, Does.StartWith("cheddar_run_n_"));
            Assert.IsFalse(cheddar.transform.Find("CheddarAuthoredPose").GetComponent<SpriteRenderer>().flipX);
            cheddarBody.linearVelocity = new Vector2(-4f, -4f);
            yield return new WaitForSeconds(0.14f);
            Assert.That(cheddarMotion.AuthoredPoseSpriteName, Does.StartWith("cheddar_run_se_"));
            Assert.IsTrue(cheddar.transform.Find("CheddarAuthoredPose").GetComponent<SpriteRenderer>().flipX,
                "Southwest travel should mirror the southeast strip.");
            cheddarBody.linearVelocity = Vector2.zero;
            cheddarInput.enabled = true;
            Assert.IsNotNull(cheddar.GetComponent<Collider2D>(), "Final art must not replace dog collision.");
            // Overlay-presence coverage lives in the origin BackyardArtEnhancer scene tests; this test
            // focuses on the directional motion pack and juice/bark paths.

            var treat = Object.FindFirstObjectByType<Treat>();
            Assert.IsNotNull(treat);
            Assert.IsNotNull(treat.GetComponent<Collider2D>(), "Final weenie overlay must not replace collection collision.");
            treat.CollectBy(cheddar.GetComponent<DogController>());
            juice.RefreshNow();
            Assert.AreEqual("pickup_sparkle", juice.LastSpawnedSpriteName);
            Assert.IsNotNull(juice.LastSpawnedObject.GetComponent<SpriteRenderer>());

            game.ForceSquirrelStealAttempt();
            juice.RefreshNow();
            Assert.AreEqual("warning_alert", juice.LastSpawnedSpriteName);
            var warning = juice.LastSpawnedObject;
            yield return new WaitForSecondsRealtime(0.9f);
            Assert.IsTrue(warning == null, "Final juice effects must self-clean between gameplay beats.");

            // Generated backyard geometry must survive the additive art pass (origin's enhancer overlays
            // art as ArtSpriteOverlay components rather than replacing these objects).
            var environment = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            Assert.IsNotNull(environment.transform.Find("CoverBush_0"), "Generated fallback objects must remain available.");
            Assert.IsNotNull(environment.transform.Find("SteppingStone_0"), "Generated fallback objects must remain available.");

            cheddarInput.enabled = false;
            cheddarBody.linearVelocity = Vector2.up * 4f;
            yield return new WaitForSeconds(0.14f);
            cheddarBody.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.14f);
            Assert.That(cheddarMotion.AuthoredPoseSpriteName, Does.StartWith("cheddar_idle_n_"));
            cheddar.GetComponent<DogController>().Bark();
            yield return new WaitForSeconds(0.08f);
            Assert.That(cheddarMotion.AuthoredPoseSpriteName, Does.StartWith("cheddar_bark_n_"));
            Assert.IsFalse(cheddar.transform.Find("CheddarAuthoredPose").GetComponent<SpriteRenderer>().flipX);
            cheddarInput.enabled = true;
            var ring = GameObject.Find(ArenaArtCatalog.BarkFeedback.RingName);
            Assert.IsNotNull(ring);
            Assert.AreEqual("bark_ring", ring.GetComponent<SpriteRenderer>().sprite.name);
        }

        [UnityTest]
        public IEnumerator MissionThreatActors_UseAuthoredMotionAndMarkerFallbacks()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return new WaitForSeconds(0.2f);
            var squirrelMotion = game.SquirrelObject.GetComponent<ThreatReadabilityAnimator>();
            Assert.IsNotNull(squirrelMotion);
            Assert.IsTrue(squirrelMotion.UsesAuthoredMotion);
            Assert.AreEqual("Squirrel", squirrelMotion.CurrentActorLabel);
            Assert.AreEqual("Idle", squirrelMotion.CurrentClipLabel);
            Assert.That(squirrelMotion.RuntimeSpriteName, Does.StartWith("squirrel_idle_e_"));

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return new WaitForSeconds(0.2f);
            var eagleMotion = game.PredatorObject.GetComponent<ThreatReadabilityAnimator>();
            Assert.IsNotNull(eagleMotion);
            Assert.IsTrue(eagleMotion.UsesAuthoredMotion);
            Assert.AreEqual("Eagle", eagleMotion.CurrentActorLabel);
            Assert.AreEqual("Sweep", eagleMotion.CurrentClipLabel);
            Assert.That(eagleMotion.RuntimeSpriteName, Does.StartWith("eagle_sweep_e_"));

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return new WaitForSeconds(0.2f);
            var coyoteMotion = game.PredatorObject.GetComponent<ThreatReadabilityAnimator>();
            Assert.IsTrue(coyoteMotion.UsesAuthoredMotion);
            Assert.AreEqual("Coyote", coyoteMotion.CurrentActorLabel);
            Assert.AreEqual("Threaten", coyoteMotion.CurrentClipLabel);
            Assert.That(coyoteMotion.RuntimeSpriteName, Does.StartWith("coyote_threaten_e_"));

            var weakSpotMotion = game.SquirrelObject.GetComponent<ThreatReadabilityAnimator>();
            Assert.IsNotNull(weakSpotMotion);
            Assert.IsFalse(weakSpotMotion.UsesAuthoredMotion,
                "Coyotes uses the shared squirrel actor as a dirt/weak-spot marker, so squirrel art should fall back there.");
        }

        [UnityTest]
        public IEnumerator GeneratedMissionPropPackPass2_CoversVisibleMissionFocusProps()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("BackyardSquirrelTrapEscapeGap", FinalGameplayArt.BackyardTrapGapOpen);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return new WaitForSeconds(0.1f);
            AssertTreatProp(FinalGameplayArt.MissionSnackPlate);
            game.SnackHeistController.ForceStartStealForArt();
            yield return null;
            AssertTreatProp(FinalGameplayArt.SnackHeistPlateTargeted);
            AssertMissionProp("SnackHeistBarkGuardLane", FinalGameplayArt.SnackHeistGuardLane);

            game.StartMission(GameManager.MissionVariant.SockPanic);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp(ArenaArtCatalog.LaundryBasketObjectName, FinalGameplayArt.SockPanicBasketClosed);
            game.ForceSockBasketTip(DogId.Cocoa);
            yield return null;
            AssertMissionProp(ArenaArtCatalog.LaundryBasketObjectName, FinalGameplayArt.SockPanicBasketOpen);
            AssertTreatProp(FinalGameplayArt.SockPanicSockExposed);
            game.ForceSockBasketTimeout();
            yield return null;
            AssertMissionProp(ArenaArtCatalog.LaundryBasketObjectName, FinalGameplayArt.SockPanicBasketFumble);

            game.StartMission(GameManager.MissionVariant.SquirrelConspiracy);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("SquirrelCutoff_0", FinalGameplayArt.SquirrelConspiracyCutoffOpen);

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("EagleCover_0", FinalGameplayArt.EagleShadowCoverSafe);
            game.ForceEagleShadowExposure();
            yield return null;
            AssertMissionProp("EagleCover_0", FinalGameplayArt.EagleShadowCoverSpotted);

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("FenceGap_0", FinalGameplayArt.CoyotesFenceGapOpen);
            game.ForceCoyoteBarkPressure(DogId.Cocoa);
            yield return null;
            AssertMissionProp("FenceGap_0", FinalGameplayArt.CoyotesFenceGapPinned);

            game.StartMission(GameManager.MissionVariant.WeenieRoundup);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("LooseWeenie_0", FinalGameplayArt.WeenieRoundupLoose);
            AssertMissionProp("HomeBowl", FinalGameplayArt.WeenieRoundupBowlEmpty);
            game.ForceWeeniePickup(DogId.Cheddar);
            yield return null;
            AssertMissionProp("CarriedWeenie_0", FinalGameplayArt.WeenieRoundupCarried);
            game.ForceWeenieDeliver(DogId.Cheddar);
            yield return null;
            AssertMissionProp("HomeBowl", FinalGameplayArt.WeenieRoundupBowlProgress);

            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("DigSpot_0", FinalGameplayArt.ScentSearchDigUnknown);
            game.ForceScentDigWrong(DogId.Cheddar);
            yield return null;
            AssertAnyActiveMissionProp(FinalGameplayArt.ScentSearchScentCold);

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("ThunderstormComfortCue", FinalGameplayArt.ThunderstormCloudWaiting);
            game.ForceThunderclap();
            yield return null;
            AssertMissionProp("ThunderstormComfortCue", FinalGameplayArt.ThunderstormThunderclap);

            game.StartMission(GameManager.MissionVariant.MarkTheYard);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("TerritoryZone_0", FinalGameplayArt.MarkYardZoneUnclaimed);
            AssertMissionProp("MarkTheYardSquirrel", FinalGameplayArt.MarkYardSquirrelWatch);
            game.ForceClaimZone(DogId.Cheddar);
            yield return null;
            AssertMissionProp("TerritoryZone_0", FinalGameplayArt.MarkYardZoneClaimed);
            game.ForceSquirrelReclaim();
            yield return null;
            AssertMissionProp("TerritoryZone_0", FinalGameplayArt.MarkYardZoneStolen);
            AssertMissionProp("MarkTheYardSquirrel", FinalGameplayArt.MarkYardSquirrelSteal);

            game.StartMission(GameManager.MissionVariant.LeashWalk);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("LeashCheckpoint_0", FinalGameplayArt.LeashWalkCheckpointWaiting);
            game.ForceLeashSnap();
            yield return null;
            AssertMissionProp("LeashCheckpoint_0", FinalGameplayArt.LeashWalkSnapWarning);

            game.StartMission(GameManager.MissionVariant.CarRide);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("Car Ride Balance Vehicle", FinalGameplayArt.CarRideLevel);
            game.ForceCarLurch();
            yield return null;
            AssertMissionProp("Car Ride Balance Vehicle", FinalGameplayArt.CarRideLurchRight);
            game.ForceCarSpill();
            yield return null;
            AssertMissionProp("Car Ride Balance Vehicle", FinalGameplayArt.CarRideSpill);

            game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("GateCrashGate", FinalGameplayArt.GateCrashGateClosed);
            AssertMissionProp("GateCrashToy", FinalGameplayArt.GateCrashToyWaiting);
            game.ForceGateHold(true);
            AssertMissionProp("GateCrashGate", FinalGameplayArt.GateCrashGateHeld);
            game.GateCrashController.ForceGateCross(1.0f);
            AssertMissionProp("GateCrashToy", FinalGameplayArt.GateCrashToyClaimed);

            game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("TableStealthHuman", FinalGameplayArt.TableStealthHumanWatching);
            AssertMissionProp("TableStealthSteak", FinalGameplayArt.TableStealthSteakAvailable);
            game.ForceTableFlop(true);
            AssertMissionProp("TableStealthHuman", FinalGameplayArt.TableStealthHumanDistracted);
            AssertMissionProp("TableStealthSteak", FinalGameplayArt.TableStealthSteakSneakProgress);
            game.TableStealthController.ForceTableSneak(2.0f);
            AssertMissionProp("TableStealthSteak", FinalGameplayArt.TableStealthSteakGone);

            game.StartMission(GameManager.MissionVariant.SquirrelSwitcheroo);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("SwitcherooDecoy", FinalGameplayArt.SwitcherooDecoyGuarded);
            AssertMissionProp("SwitcherooStash", FinalGameplayArt.SwitcherooStashGuarded);
            game.ForceSwitcherooBait(0.7f);
            AssertMissionProp("SwitcherooDecoy", FinalGameplayArt.SwitcherooDecoyChased);
            AssertMissionProp("SwitcherooStash", FinalGameplayArt.SwitcherooStashOpen);
            game.SquirrelSwitcherooController.ForceSwitcherooStrike();
            AssertMissionProp("SwitcherooStash", FinalGameplayArt.SwitcherooStashRaided);

            game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("WalkCampaignHuman", FinalGameplayArt.WalkCampaignHumanConfused);
            AssertMissionProp("WalkCampaignLeash", FinalGameplayArt.WalkCampaignLeashWaiting);
            game.ForceWalkCampaign(1f, doorStare: true, presentLeash: true);
            AssertMissionProp("WalkCampaignHuman", FinalGameplayArt.WalkCampaignHumanGettingIt);
            AssertMissionProp("WalkCampaignLeash", FinalGameplayArt.WalkCampaignLeashPresented);
            game.WalkCampaignController.ForceWalkCampaign(2f, doorStare: true, presentLeash: true);
            AssertMissionProp("WalkCampaignHuman", FinalGameplayArt.WalkCampaignHumanWalkies);
            AssertMissionProp("WalkCampaignLeash", FinalGameplayArt.WalkCampaignLeashGrabbed);

            game.StartMission(GameManager.MissionVariant.BoneRelay);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("BoneMound_0", FinalGameplayArt.BoneRelayMoundUnknown);
            AssertMissionProp("ScentPost", FinalGameplayArt.BoneRelayScentPostIdle);
            game.ForceBoneReveal();
            int calledMound = game.BoneRelayPuzzle.CorrectTarget;
            AssertMissionProp("ScentPost", FinalGameplayArt.BoneRelayScentPostCalled);
            AssertMissionProp($"BoneMound_{calledMound}", FinalGameplayArt.BoneRelayMoundCalled);
            game.BoneRelayController.ForceBoneDig(calledMound);
            AssertMissionProp($"BoneMound_{calledMound}", FinalGameplayArt.BoneRelayMoundFound);

            game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("EscapeStation_0", FinalGameplayArt.GreatEscapeStationCocoaActive);
            AssertMissionProp("EscapeStation_1", FinalGameplayArt.GreatEscapeStationWaiting);
            game.ForceEscapeStep(ChainActor.Cocoa);
            AssertMissionProp("EscapeStation_0", FinalGameplayArt.GreatEscapeStationCompleted);
            AssertMissionProp("EscapeStation_1", FinalGameplayArt.GreatEscapeStationCheddarActive);
            game.ForceEscapeStep(ChainActor.Cocoa);
            AssertMissionProp("EscapeStation_1", FinalGameplayArt.GreatEscapeStationFumble);

            game.StartMission(GameManager.MissionVariant.ChaosMachine);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("ChaosMachineLever", FinalGameplayArt.ChaosLeverReady);
            game.ForceChaosTrigger();
            AssertMissionProp("ChaosMachineLever", FinalGameplayArt.ChaosLeverRunning);
            AssertMissionProp("ChaosJunction_0", FinalGameplayArt.ChaosJunctionTowelDrop);
            AssertMissionProp("ChaosJunction_1", FinalGameplayArt.ChaosJunctionBasketTip);
            AssertMissionProp("ChaosJunction_2", FinalGameplayArt.ChaosJunctionToyLaunch);

            game.StartMission(GameManager.MissionVariant.BlanketCatch);
            yield return new WaitForSeconds(0.1f);
            game.ForceBlanketSpan(2f, 0f);
            AssertMissionProp("CatchBlanket", FinalGameplayArt.BlanketCatchSlack);
            AssertMissionProp("FallingSnack", FinalGameplayArt.BlanketSnackFalling);
            game.ForceBlanketSpan(6f, 0f);
            AssertMissionProp("CatchBlanket", FinalGameplayArt.BlanketCatchTaut);
            game.ForceBlanketCatch(0f);
            AssertMissionProp("FallingSnack", FinalGameplayArt.BlanketSnackCaught);
            game.ForceBlanketSpan(14f, 0f);
            AssertMissionProp("CatchBlanket", FinalGameplayArt.BlanketCatchRipping);

            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return new WaitForSeconds(0.1f);
            AssertMissionProp("KitchenCounterRoute", FinalGameplayArt.KitchenCounterReady);
            AssertMissionProp("KitchenSafeBowl", FinalGameplayArt.KitchenSafeBowlEmpty);
            game.ForceKitchenTelegraph(DogId.Cheddar, KitchenFoodFrenzyMissionState.FoodKind.Good);
            AssertMissionProp("KitchenCounterRoute", FinalGameplayArt.KitchenCounterBarked);
            AssertMissionProp("KitchenLandingWarning", FinalGameplayArt.KitchenCueLandingGold);
            game.ForceKitchenReleaseTelegraph();
            yield return null;
            AssertMissionProp("KitchenFallingFood", FinalGameplayArt.KitchenFoodGoodFalling);
            AssertMissionProp("KitchenCounterRoute", FinalGameplayArt.KitchenCounterReady);
            game.ForceKitchenCatch(DogId.Cocoa, true);
            AssertMissionProp("KitchenSafeBowl", FinalGameplayArt.KitchenSafeBowlCatch);
            game.ForceKitchenDrop(KitchenFoodFrenzyMissionState.FoodKind.Bad);
            yield return null;
            AssertMissionProp("KitchenFallingFood", FinalGameplayArt.KitchenFoodBadFalling);
            AssertMissionProp("KitchenLandingWarning", FinalGameplayArt.KitchenCueLandingPurple);
        }

        [UnityTest]
        public IEnumerator MissionLevelAreaArt_GivesKitchenAndCarDistinctNonblockingSpaces()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return new WaitForSeconds(0.1f);
            var kitchen = GameObject.Find(MissionLevelAreaArt.KitchenRootName);
            Assert.IsNotNull(kitchen, "Kitchen should install a mission-owned indoor level area.");
            AssertLevelAreaPlate(kitchen, "KitchenFloorPlate", "kitchen_floor_area", expectedMaxSortingOrder: -5);
            AssertLevelAreaPlate(kitchen, "KitchenCounterWallPlate", "kitchen_counter_wall", expectedMaxSortingOrder: -3);
            Assert.IsNull(GameObject.Find(MissionLevelAreaArt.CarRideRootName),
                "Starting Kitchen should not leave the car area visible.");

            game.StartMission(GameManager.MissionVariant.CarRide);
            yield return new WaitForSeconds(0.1f);
            var car = GameObject.Find(MissionLevelAreaArt.CarRideRootName);
            Assert.IsNotNull(car, "Car Ride should install a constrained car-interior level area.");
            AssertLevelAreaPlate(car, "CarInteriorCabinPlate", "car_interior_cabin", expectedMaxSortingOrder: -4);
            AssertLevelAreaPlate(car, "CarBalanceLanePlate", "car_balance_lane", expectedMaxSortingOrder: -2);
            Assert.IsNull(GameObject.Find(MissionLevelAreaArt.KitchenRootName),
                "Switching to Car Ride should clean up the Kitchen area.");
        }

        private static void AssertTreatProp(string expectedResourcePath)
        {
            foreach (var treat in Object.FindObjectsByType<Treat>(FindObjectsSortMode.None))
            {
                if (treat == null || !treat.gameObject.activeInHierarchy) continue;
                var attachment = treat.GetComponent<MissionPropArtAttachment>();
                if (attachment == null || attachment.ResourcePath != expectedResourcePath) continue;
                AssertMissionProp(treat.gameObject, expectedResourcePath, requiredActive: true);
                    return;
            }
            Assert.Fail($"No active treat uses generated prop art {expectedResourcePath}.");
        }

        private static void AssertAnyActiveMissionProp(string expectedResourcePath)
        {
            foreach (var attachment in Object.FindObjectsByType<MissionPropArtAttachment>(FindObjectsSortMode.None))
            {
                if (attachment == null || !attachment.gameObject.activeInHierarchy) continue;
                if (attachment.ResourcePath != expectedResourcePath) continue;
                AssertMissionProp(attachment.gameObject, expectedResourcePath, requiredActive: true);
                return;
            }
            Assert.Fail($"No active mission prop uses generated prop art {expectedResourcePath}.");
        }

        private static void AssertMissionProp(string objectName, string expectedResourcePath)
        {
            var go = GameObject.Find(objectName);
            Assert.IsNotNull(go, $"Missing active mission prop object {objectName}.");
            AssertMissionProp(go, expectedResourcePath, requiredActive: true);
        }

        private static bool AssertMissionProp(GameObject go, string expectedResourcePath, bool requiredActive)
        {
            if (requiredActive && !go.activeInHierarchy)
                Assert.Fail($"{go.name} should be active for art coverage.");
            var attachment = go.GetComponent<MissionPropArtAttachment>();
            if (attachment == null) return false;
            Assert.AreEqual(expectedResourcePath, attachment.ResourcePath, $"{go.name} should use the expected generated prop path.");
            Assert.IsTrue(attachment.HasRuntimeSprite, $"{go.name} should load a runtime sprite overlay.");
            Assert.That(attachment.RuntimeSpriteName, Does.Not.Contain("WhiteSquare"), $"{go.name} must not use the runtime white square as its focus art.");
            Assert.AreEqual(ExpectedSpriteName(expectedResourcePath), attachment.RuntimeSpriteName,
                $"{go.name} should keep the mission-specific generated sprite active at runtime.");
            var overlay = go.transform.Find("ActualArtOverlay");
            Assert.IsNotNull(overlay, $"{go.name} should have an actual art overlay child.");
            var renderer = overlay.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            Assert.AreNotSame(SpriteShapeCache.WhiteSquare, renderer.sprite, $"{go.name} overlay should not be the runtime white square.");
            var fallback = go.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(fallback, $"{go.name} should keep its generated fallback marker for interaction readability.");
            Assert.LessOrEqual(fallback.color.a, 0.22f, $"{go.name} fallback marker should not dominate the generated prop sprite.");
            return true;
        }

        private static string ExpectedSpriteName(string resourcePath)
        {
            int slash = resourcePath.LastIndexOf('/');
            return slash >= 0 ? resourcePath.Substring(slash + 1) : resourcePath;
        }

        private static void AssertLevelAreaPlate(GameObject root, string childName, string expectedSpriteName,
            int expectedMaxSortingOrder)
        {
            var child = root.transform.Find(childName);
            Assert.IsNotNull(child, $"Missing level-area plate {childName}.");
            var renderer = child.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer, $"{childName} should render as a decorative sprite.");
            Assert.IsNotNull(renderer.sprite, $"{childName} should load a generated level-area sprite.");
            Assert.AreEqual(expectedSpriteName, renderer.sprite.name);
            Assert.LessOrEqual(renderer.sortingOrder, expectedMaxSortingOrder,
                $"{childName} should stay behind dogs, markers, and warning art.");
            Assert.IsNull(child.GetComponent<Collider2D>(), $"{childName} must not add gameplay collision.");
        }
    }
}
