using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
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

            // One-background rule: the plate IS the yard background — full opacity, full coverage,
            // with no visible placeholder geometry fighting it.
            Assert.AreEqual(1f, renderer.color.a, 0.001f,
                "The painted yard plate must render at full opacity, not as a translucent collage layer.");
            Assert.AreEqual(ArenaWorldScale.BackyardWidth, renderer.bounds.size.x, 0.5f,
                "The painted yard plate should cover the full yard width.");
            Assert.AreEqual(ArenaWorldScale.BackyardHeight, renderer.bounds.size.y, 0.5f,
                "The painted yard plate should cover the full yard height.");
            Assert.IsTrue(enhancer.PaintedPlateIsSoleYardBackground,
                "No placeholder rectangle may stay visible where the painted plate provides authored art.");
        }

        [UnityTest]
        public IEnumerator CocoaSunbeamGag_FiresWhenIdleAndStaysQuietWhenMoving()
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

            var cocoa = GameObject.Find("Cocoa");
            Assert.IsNotNull(cocoa);
            // Disable input so nothing decelerates the directly-set velocity back toward zero before
            // the assertion below runs (matches the pattern in FinalArtIntegrationPlayModeTests).
            cocoa.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            var body = cocoa.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.right * 5f;
            yield return null;

            enhancer.TrySpawnSunbeamClaim();
            Assert.AreEqual(0, enhancer.SunbeamClaimCount,
                "Cocoa should not claim a sunbeam while she is running - this is a stationary personality beat.");

            body.linearVelocity = Vector2.zero;
            yield return null;

            enhancer.TrySpawnSunbeamClaim();
            Assert.AreEqual(1, enhancer.SunbeamClaimCount,
                "Cocoa has legal ownership of all sunbeams - an idle beat should trigger the gag.");
            Assert.AreEqual(DogReadabilityFeedback.Pose.Proud,
                cocoa.GetComponent<DogReadabilityFeedback>().CurrentPose,
                "Claiming a sunbeam should read as a proud personality moment.");
        }

        [UnityTest]
        public IEnumerator SquirrelMumbleGag_FiresWhenCalmAndStaysQuietDuringRealStealWarning()
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

            enhancer.TrySpawnSquirrelMumble();
            Assert.AreEqual(1, enhancer.SquirrelMumbleCount,
                "A calm, waiting squirrel should get its unintelligible villain monologue.");

            game.ForceSquirrelStealAttempt();
            yield return null;

            enhancer.TrySpawnSquirrelMumble();
            Assert.AreEqual(1, enhancer.SquirrelMumbleCount,
                "The mumble gag must not compete with the real steal-warning signal badge.");
        }

        [UnityTest]
        public IEnumerator ToyEnvyGag_FiresWhenBothDogsClaimTheRopeAndStaysQuietWithOnlyOneDogThere()
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

            var cheddar = GameObject.Find("Cheddar");
            var cocoa = GameObject.Find("Cocoa");
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);
            Vector3 ropePos = game.RopeObject.transform.position;

            // Only Cheddar near the rope: no rival, no envy.
            cheddar.transform.position = ropePos + Vector3.left * 0.6f;
            cocoa.transform.position = ropePos + Vector3.right * 20f;
            yield return null;

            enhancer.TrySpawnToyEnvy();
            Assert.AreEqual(0, enhancer.ToyEnvyCount,
                "A toy only one dog is near is not contested - envy needs a rival.");

            // Both dogs claim it at once: the toy suddenly matters.
            cocoa.transform.position = ropePos + Vector3.right * 0.6f;
            yield return null;

            enhancer.TrySpawnToyEnvy();
            Assert.AreEqual(1, enhancer.ToyEnvyCount,
                "Every toy becomes valuable only when the other dog wants it.");
        }

        [UnityTest]
        public IEnumerator TreatReverenceGag_FiresWhenIdleNearATreatAndStaysQuietWhenFar()
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

            var treat = Object.FindFirstObjectByType<Treat>();
            Assert.IsNotNull(treat);
            var cheddar = GameObject.Find("Cheddar");
            Assert.IsNotNull(cheddar);
            cheddar.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            var body = cheddar.GetComponent<Rigidbody2D>();

            // Far from every treat: the reverent pause stays quiet.
            cheddar.transform.position = treat.transform.position + Vector3.right * 20f;
            if (body != null) body.linearVelocity = Vector2.zero;
            yield return null;

            enhancer.TrySpawnTreatReverence();
            Assert.AreEqual(0, enhancer.TreatReverenceCount,
                "No dog is idle near an uncollected treat - the gag should stay quiet.");

            // Idle right next to (not overlapping - the treat's own trigger collider is only 0.6
            // units, and this must not accidentally collect it) a treat: dropped food has religious
            // significance.
            cheddar.transform.position = treat.transform.position + Vector3.right * 1.2f;
            if (body != null) body.linearVelocity = Vector2.zero;
            yield return null;

            enhancer.TrySpawnTreatReverence();
            Assert.AreEqual(1, enhancer.TreatReverenceCount,
                "An idle dog right next to dropped food should pause in reverence.");
        }

        [UnityTest]
        public IEnumerator PoolFascinationGag_FiresAtTheWatersEdgeAndStaysQuietAwayFromIt()
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

            var cheddar = GameObject.Find("Cheddar");
            Assert.IsNotNull(cheddar);
            cheddar.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            var body = cheddar.GetComponent<Rigidbody2D>();

            // Far from the pool entirely: no fascination.
            cheddar.transform.position = new Vector3(50f, 50f, 0f);
            if (body != null) body.linearVelocity = Vector2.zero;
            yield return null;

            enhancer.TrySpawnPoolFascination();
            Assert.AreEqual(0, enhancer.PoolFascinationCount,
                "Nowhere near the water - the pool gag should stay quiet.");

            // Idle right at the water's edge (just outside the rect, not actually swimming):
            // the pool is both terrifying and fascinating.
            Rect water = BackyardPoolZone.WaterRect;
            cheddar.transform.position = new Vector3(water.xMax + 1f, water.center.y, 0f);
            if (body != null) body.linearVelocity = Vector2.zero;
            yield return null;

            enhancer.TrySpawnPoolFascination();
            Assert.AreEqual(1, enhancer.PoolFascinationCount,
                "An idle dog right at the water's edge should get the terrified-but-fascinated flinch.");
        }

        [UnityTest]
        public IEnumerator ReactToFeedbackAndScore_OnlyFireDuringBackyardRescueNotOtherMissions()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
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

            // A different mission's shared GameOver feedback must not spawn the Backyard-only
            // rope/squirrel/predator set-dressing sparkle at some unrelated, possibly stale position.
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;
            enhancer.EnhanceNow();
            yield return null;

            int beforeOtherMission = enhancer.VfxSpawnCount;
            game.ForceGameOver();
            yield return null;

            Assert.AreEqual(beforeOtherMission, enhancer.VfxSpawnCount,
                "Kitchen Food Frenzy's GameOver must not trigger Backyard Rescue's rope/squirrel/" +
                "predator sparkle - that set dressing belongs only to the mission it's named after.");

            // The same feedback kind during Backyard Rescue itself should still react as designed.
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
            int beforeBackyard = enhancer.VfxSpawnCount;
            game.ForceGameOver();
            yield return null;

            Assert.Greater(enhancer.VfxSpawnCount, beforeBackyard,
                "Backyard Rescue's own GameOver feedback should still spawn its set-dressing reaction.");
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
        public IEnumerator EagleThreat_AnimatesWithWingFramesNotBodyScalePulse()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            var predator = game.PredatorObject;
            var feedback = predator.GetComponent<MissionActorFeedback>();
            var motion = predator.GetComponent<ThreatReadabilityAnimator>();
            Assert.IsNotNull(feedback);
            Assert.IsNotNull(motion);

            // The strongest pulse the mission ever requests: this used to balloon the eagle by
            // ±42% and completely drown the wing frames (couch test #2 feedback).
            feedback.SetState("SHADOW! HUDDLE + DOUBLE BARK!", new Color(1f, 0.08f, 0.08f), 0.42f);
            motion.SetLabelState("SHADOW! HUDDLE + DOUBLE BARK!");
            yield return null;

            Assert.IsTrue(motion.UsesAuthoredMotion);
            Assert.IsTrue(feedback.ScalePulseSuppressedByAuthoredMotion,
                "Frame-animated threats must not add a whole-body scale pulse on top of their frames.");

            Vector3 baseline = predator.transform.localScale;
            int firstFrame = motion.CurrentFrameIndex;
            bool frameChanged = false;
            float deadline = Time.time + 1.2f;
            while (Time.time < deadline)
            {
                yield return null;
                Assert.AreEqual(baseline.x, predator.transform.localScale.x, 0.002f,
                    "The eagle must not grow and shrink; wing frames and the glide bob carry the motion.");
                if (motion.CurrentFrameIndex != firstFrame) frameChanged = true;
            }

            Assert.IsTrue(frameChanged, "Eagle wing frames should cycle while the threat is active.");
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
            Assert.AreEqual(0, wow.PlaceholderRectCount,
                "One-background rule: the wow layer must not add placeholder rectangles over the painted yard plate.");
            // Couch feedback trimmed the wow layer: no more mid-yard prop parade or floating
            // sparkle bone. What remains is the two fence-line showcase accents + the spotlight.
            Assert.GreaterOrEqual(wow.SetPieceCount, 3, "The wow layer should keep its authored ambient accent sprites.");
            Assert.GreaterOrEqual(wow.AnimatedSetPieceCount, 3, "The wow layer should include visible motion, not only static props.");
            Assert.IsNull(GameObject.Find("WowMissionSpark"),
                "The floating pickup-sparkle bone read as a fake collectible and must stay retired.");
            Assert.IsNull(GameObject.Find("WowBackyardPropsParade"),
                "Mid-yard bouncing prop dupes read as interactable scenery and must stay retired.");
            Assert.IsNull(GameObject.Find("WowAdventurePropsEncore"),
                "Mid-yard bouncing prop dupes read as interactable scenery and must stay retired.");
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

        [UnityTest]
        public IEnumerator RopeOverlay_RendersAtALegibleSizeNotATinySpeck()
        {
            // Couch report: "the rope isn't using our nice visual asset" - the overlay was scaled to
            // ~0.06 world units (a speck) next to the ~1.47-wide generated placeholder it should
            // replace, so players only ever saw the placeholder bars. Guard against that regressing.
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

            var overlay = game.RopeObject.GetComponent<ArtSpriteOverlay>();
            Assert.IsNotNull(overlay, "The rope should carry a promoted art overlay.");
            Assert.IsTrue(overlay.HasRuntimeSprite, "The rope overlay should have the final rope_tug art loaded.");

            var overlayRenderer = game.RopeObject.transform.Find("ActualArtOverlay").GetComponent<SpriteRenderer>();
            Assert.GreaterOrEqual(overlayRenderer.bounds.size.x, 1f,
                "The rope art must render at least as wide as the two-dog tug marker it covers, not a barely-visible speck.");
        }
    }
}
