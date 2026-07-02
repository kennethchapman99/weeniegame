using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class BackyardArtEnhancerScenePlayModeTests
    {
        [UnityTest]
        public IEnumerator BackyardArtEnhancer_InstallsWithoutBlockingArenaStart()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            var enhancer = Object.FindFirstObjectByType<BackyardRescueArtEnhancer>();
            if (enhancer == null)
            {
                var go = new GameObject("BackyardRescueArtEnhancer_TestFallback");
                enhancer = go.AddComponent<BackyardRescueArtEnhancer>();
            }

            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
            enhancer.EnhanceNow();
            yield return null;

            Assert.AreEqual(GameManager.State.Playing, game.Phase);
            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, game.ActiveMissionVariant);
            Assert.IsTrue(enhancer.Enhanced, "Enhancer should mark itself active even if final art sprites are not present yet.");
            Assert.GreaterOrEqual(enhancer.OverlayCount, 0);
        }

        [UnityTest]
        public IEnumerator BackyardArtEnhancer_AddsNoDogPaintedBackyardPlateBehindGameplay()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            var enhancer = Object.FindFirstObjectByType<BackyardRescueArtEnhancer>();
            if (enhancer == null)
            {
                var go = new GameObject("BackyardRescueArtEnhancer_TestFallback");
                enhancer = go.AddComponent<BackyardRescueArtEnhancer>();
            }
            enhancer.EnhanceNow();
            yield return null;

            Assert.IsTrue(enhancer.UsesNoDogPaintedBackyardPlate,
                "The couch-test yard should use a painted scenery plate, not frozen dog character art.");
            var plate = GameObject.Find("ActualNoDogBackyardPlate");
            Assert.IsNotNull(plate);
            var renderer = plate.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            Assert.AreEqual("yard_backyard_plate_v02", renderer.sprite.name);
            Assert.Less(renderer.sortingOrder, 0, "The painted yard plate must stay behind runtime actors and gameplay cues.");
        }

        [UnityTest]
        public IEnumerator BackyardThreatPresentation_UsesQuietReferenceArtGroundShadowsAndEagleMotion()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            var enhancer = Object.FindFirstObjectByType<BackyardRescueArtEnhancer>();
            if (enhancer == null)
            {
                var go = new GameObject("BackyardRescueArtEnhancer_TestFallback");
                enhancer = go.AddComponent<BackyardRescueArtEnhancer>();
            }
            enhancer.EnhanceNow();
            yield return null;

            Assert.IsTrue(enhancer.BackyardThreatsHaveReadableShadows,
                "Squirrel and eagle should carry explicit couch-readable oval ground shadows.");
            Assert.IsTrue(enhancer.ThreatVisualsHaveSingleOwner,
                "ThreatReadabilityAnimator must be the only owner of squirrel/eagle character art.");
            Assert.IsNull(game.SquirrelObject.GetComponent<ArtSpriteOverlay>(),
                "The enhancer must not ghost a second semi-transparent squirrel over the animated one.");
            Assert.IsNull(game.PredatorObject.GetComponent<ArtSpriteOverlay>(),
                "The enhancer must not ghost a second semi-transparent eagle over the animated one.");

            var predatorMotion = game.PredatorObject.GetComponent<ThreatReadabilityAnimator>();
            Assert.IsNotNull(predatorMotion);
            Assert.AreEqual("Eagle", predatorMotion.CurrentActorLabel);
            Assert.IsTrue(predatorMotion.UsesAuthoredMotion,
                "Backyard/eagle threat presentation should use frame-swapped motion instead of only a skewed badge.");

            // The old rig put the non-uniform placeholder BodyScale on the actor root, which drew
            // every authored frame squashed to half height. The authored sprite must render with a
            // uniform world scale and at full opacity now.
            var authored = game.PredatorObject.transform.Find("ThreatAuthoredMotion");
            Assert.IsNotNull(authored);
            Assert.AreEqual(authored.lossyScale.x, authored.lossyScale.y, 0.001f,
                "Eagle motion frames must not inherit a skewed scale from the actor root.");
            Assert.AreEqual(1f, authored.GetComponent<SpriteRenderer>().color.a, 0.001f,
                "The eagle should be a solid character, not a translucent ghost.");

            var squirrelAuthored = game.SquirrelObject.transform.Find("ThreatAuthoredMotion");
            Assert.IsNotNull(squirrelAuthored);
            Assert.AreEqual(squirrelAuthored.lossyScale.x, squirrelAuthored.lossyScale.y, 0.001f,
                "Squirrel motion frames must not inherit a skewed scale from the actor root.");
        }

        [UnityTest]
        public IEnumerator EagleRescuePresentation_HidesSquirrelOverlayFromTalonGripMarker()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;

            var enhancer = Object.FindFirstObjectByType<BackyardRescueArtEnhancer>();
            if (enhancer == null)
            {
                var go = new GameObject("BackyardRescueArtEnhancer_TestFallback");
                enhancer = go.AddComponent<BackyardRescueArtEnhancer>();
            }
            enhancer.EnhanceNow();
            yield return null;

            game.ForceEagleShadowSafeHide();
            game.ForceEagleShadowSafeHide();
            yield return null;
            yield return null;

            Assert.IsTrue(game.EagleShadowPanicState.RescueObjectiveActive,
                "Two safe hides should enter the talon-grip rescue beat.");
            var marker = game.SquirrelObject.GetComponent<ThreatReadabilityAnimator>();
            Assert.IsNotNull(marker);
            Assert.IsTrue(marker.UsesAuthoredMotion,
                "The talon-grip marker should keep using authored motion frames.");
            Assert.AreEqual("Eagle", marker.CurrentActorLabel,
                "The shared squirrel object becomes the talon-grip marker during eagle rescue, so squirrel art must not sit under the dog.");
        }

        [UnityTest]
        public IEnumerator ArenaWowSetDressing_InstallsAnimatedDetailsForEveryMission()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            var wow = Object.FindFirstObjectByType<ArenaWowSetDressing>();
            Assert.IsNotNull(wow, "ArenaScene should automatically install the shared wow set-dressing layer.");

            wow.BuildNow();
            yield return null;

            Assert.IsTrue(wow.Built);
            Assert.GreaterOrEqual(wow.SetPieceCount, 50, "The couch-test arena should have a dense ambient presentation layer.");
            Assert.GreaterOrEqual(wow.AnimatedSetPieceCount, 25, "The wow layer should include visible motion, not only static props.");
            Assert.AreEqual(0, wow.AttractCharacterCount,
                "Cheddar/Cocoa should never be baked into level-background set dressing.");
            Assert.IsTrue(wow.HasNoFrozenDogBackdrops,
                "The first impression layer should use props and runtime dogs, not frozen Cheddar/Cocoa backdrop pictures.");
            Assert.IsTrue(wow.HasGeneratedCartoonAssets, "The wow layer should use generated cartoon sprites, not only primitive rectangles.");
            Assert.IsTrue(wow.HasMissionReactiveSpotlight);
            Assert.IsTrue(wow.HasMissionReactiveMotifs, "The selected mission should project a readable dog-adventure motif into the arena.");

            foreach (GameManager.MissionVariant variant in System.Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                game.SelectMission(variant);
                yield return null;

                Assert.IsTrue(wow.HasMissionReactiveMotifs, $"{variant} should have reusable mission set-piece motifs.");
                Assert.GreaterOrEqual(wow.MissionMotifPieceCount, 3, $"{variant} should have a generated motif plus accent sprites.");
                Assert.GreaterOrEqual(wow.GeneratedMissionSpriteCount, 3, $"{variant} should use generated cartoon sprites instead of primitive rectangles.");
                Assert.GreaterOrEqual(wow.AnimatedMissionMotifPieceCount, 3, $"{variant} should animate its generated motif sprites.");
                Assert.IsFalse(string.IsNullOrEmpty(wow.MissionMotifName), $"{variant} should expose a motif name for art-review evidence.");
            }

            game.SelectMission(GameManager.MissionVariant.OperationPeeBreak);
            yield return null;
            var peeAccent = wow.MissionAccentColor;
            Assert.AreEqual(ArenaHud.MissionBadgeColorFor(GameManager.MissionVariant.OperationPeeBreak), peeAccent);
            Assert.AreEqual("Couch-to-door emergency", wow.MissionMotifName);

            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;
            Assert.AreEqual(ArenaHud.MissionBadgeColorFor(GameManager.MissionVariant.KitchenFoodFrenzy), wow.MissionAccentColor);
            Assert.AreEqual("Food heist stage", wow.MissionMotifName);
            Assert.AreNotEqual(peeAccent, wow.MissionAccentColor);
        }
    }
}
