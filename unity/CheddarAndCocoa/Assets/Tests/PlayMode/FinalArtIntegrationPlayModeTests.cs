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

        [UnityTest]
        public IEnumerator BackyardRescue_UsesFinalSpritesWithoutReplacingGameplayObjects()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
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

            var environment = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            Assert.IsNotNull(environment.transform.Find("FinalBush_0"));
            Assert.IsNotNull(environment.transform.Find("FinalRock_1"));

            cheddar.GetComponent<DogController>().Bark();
            yield return null;
            var ring = GameObject.Find(ArenaArtCatalog.BarkFeedback.RingName);
            Assert.IsNotNull(ring);
            Assert.AreEqual("bark_ring", ring.GetComponent<SpriteRenderer>().sprite.name);
        }
    }
}
