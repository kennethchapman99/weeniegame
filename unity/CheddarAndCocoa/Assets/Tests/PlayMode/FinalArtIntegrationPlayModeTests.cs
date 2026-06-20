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

            Sprite motion = CharacterMotionArt.Load(DogId.Cheddar, CharacterMotionArt.Clip.Run,
                CharacterMotionArt.Facing8.E, 0);
            Assert.AreEqual(512, motion.rect.width);
            Assert.AreEqual(384, motion.rect.height);
            Assert.AreEqual(24f, motion.pivot.y, 0.1f, "Motion sprites should pivot on the normalized paw baseline.");

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
            cheddar.GetComponent<DogController>().Bark();
            yield return new WaitForSeconds(0.08f);
            Assert.That(cheddarMotion.AuthoredPoseSpriteName, Does.StartWith("cheddar_bark_n_"));
            Assert.IsFalse(cheddar.transform.Find("CheddarAuthoredPose").GetComponent<SpriteRenderer>().flipX);
            cheddarInput.enabled = true;
            var ring = GameObject.Find(ArenaArtCatalog.BarkFeedback.RingName);
            Assert.IsNotNull(ring);
            Assert.AreEqual("bark_ring", ring.GetComponent<SpriteRenderer>().sprite.name);
        }
    }
}
