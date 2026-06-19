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
            Assert.IsNull(CharacterMotionArt.Load(DogId.Cheddar, CharacterMotionArt.Clip.Run,
                CharacterMotionArt.Facing8.SE, 2));
            Assert.AreEqual("cheddar_run", CharacterMotionArt.LoadOrFallback(DogId.Cheddar,
                CharacterMotionArt.Clip.Run, CharacterMotionArt.Facing8.SE, 2).name);
            Assert.AreEqual("cocoa_idle", CharacterMotionArt.LoadOrFallback(DogId.Cocoa,
                CharacterMotionArt.Clip.Carry, CharacterMotionArt.Facing8.N, 1).name);
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
            Assert.IsNotNull(cheddar.GetComponent<Collider2D>(), "Final art must not replace dog collision.");
            Assert.IsNotNull(game.SquirrelObject.transform.Find(BackyardRescueArtEnhancer.SquirrelOverlayName));
            Assert.IsNotNull(game.PredatorObject.transform.Find(BackyardRescueArtEnhancer.PredatorOverlayName));
            Assert.IsNotNull(game.RopeObject.transform.Find(BackyardRescueArtEnhancer.RopeOverlayName));

            var treat = Object.FindFirstObjectByType<Treat>();
            Assert.IsNotNull(treat);
            Assert.IsNotNull(treat.transform.Find(DynamicTreatArtEnhancer.OverlayName));
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

            var environment = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            Assert.IsNotNull(environment.transform.Find("FinalBush_0"));
            Assert.IsNotNull(environment.transform.Find("FinalRock_1"));
            var fallbackBush = environment.transform.Find("CoverBush_0");
            var fallbackRock = environment.transform.Find("SteppingStone_0");
            Assert.IsNotNull(fallbackBush, "Generated fallback objects must remain available.");
            Assert.IsNotNull(fallbackRock, "Generated fallback objects must remain available.");
            Assert.IsFalse(fallbackBush.GetComponent<SpriteRenderer>().enabled,
                "The fallback bush renderer should not show a box behind loaded final art.");
            Assert.IsFalse(fallbackRock.GetComponent<SpriteRenderer>().enabled,
                "The fallback rock renderer should not show a box behind loaded final art.");

            cheddar.GetComponent<DogController>().Bark();
            yield return null;
            var ring = GameObject.Find(ArenaArtCatalog.BarkFeedback.RingName);
            Assert.IsNotNull(ring);
            Assert.AreEqual("bark_ring", ring.GetComponent<SpriteRenderer>().sprite.name);
        }
    }
}
