using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.CameraRig;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class BackyardEnvironmentPlayModeTests
    {
        [TestCase(480f, 480f, 640f, 296f)]
        [TestCase(1024f, 600f, 900f, 458f)]
        [TestCase(2560f, 1080f, 680f, 292f)]
        public void HudPanels_StayInsideViewport(float screenWidth, float screenHeight, float desiredWidth, float desiredHeight)
        {
            Rect panel = ArenaHud.FitPanel(screenWidth, screenHeight, desiredWidth, desiredHeight);
            Assert.GreaterOrEqual(panel.xMin, 8f);
            Assert.GreaterOrEqual(panel.yMin, 8f);
            Assert.LessOrEqual(panel.xMax, screenWidth - 8f);
            Assert.LessOrEqual(panel.yMax, screenHeight - 8f);
        }

        [Test]
        public void MissionSelectPanel_FitsAllMissionRowsAndReadableGoal()
        {
            // The mission select screen is a UGUI/TMP paged picture-tile grid plus a detail panel.
            // The grid shape lives on GameManager so directional navigation and the rendered tiles
            // can never disagree, and the view lays out in a 1080p reference space that
            // CanvasScaler scales crisply on any resolution (unlike the old IMGUI bitmap scaling).
            Assert.AreEqual(4, GameManager.MissionSelectGridColumns);
            Assert.AreEqual(3, GameManager.MissionSelectGridRowsPerPage);
            Assert.AreEqual(12, GameManager.MissionSelectTilesPerPage);
            Assert.AreEqual(1920f, MissionSelectScreen.ReferenceWidth);
            Assert.AreEqual(1080f, MissionSelectScreen.ReferenceHeight);

            Assert.AreEqual("PEE", ArenaHud.MissionBadgeCodeFor(GameManager.MissionVariant.OperationPeeBreak));
            Assert.AreNotEqual(ArenaHud.MissionBadgeColorFor(GameManager.MissionVariant.OperationPeeBreak),
                ArenaHud.MissionBadgeColorFor(GameManager.MissionVariant.BackyardRescue),
                "Mission select should have visual mission badges, not only text rows.");
            Assert.That(ArenaHud.PlayerOwnershipLabel, Does.Contain("P1 Cheddar"));
            Assert.That(ArenaHud.PlayerOwnershipLabel, Does.Contain("P2 Cocoa"));
            Assert.That(ArenaHud.PadControlsLabel, Does.Contain("Y / West barks"));
            Assert.That(ArenaHud.PadControlsLabel, Does.Contain("X / North interacts"));
            Assert.That(ArenaHud.PadControlsLabel, Does.Contain("A / East jumps"));
            Assert.That(ArenaHud.PadControlsLabel, Does.Contain("B / South wrestles"));
            Assert.IsTrue(ArenaHud.GeneratedHudSkinAvailable,
                "Mission select/end-card HUD should have generated skin sprites, not only IMGUI boxes.");
            Assert.IsTrue(WorldLabelSkin.GeneratedWorldLabelSkinAvailable,
                "Mission world labels and score pops should have generated skins, not only raw TextMesh.");
        }

        [UnityTest]
        public IEnumerator MissionSpecificSceneryCues_OnlyShowDuringTheirMission()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);

            BackyardRescueArtEnhancer enhancer = null;
            for (int i = 0; i < 80 && (enhancer == null || !enhancer.Enhanced); i++)
            {
                enhancer = Object.FindFirstObjectByType<BackyardRescueArtEnhancer>();
                yield return null;
            }

            Assert.IsNotNull(enhancer, "ArenaScene should install the additive art enhancer.");
            Assert.IsTrue(enhancer.Enhanced);

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            var env = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            Assert.IsNotNull(env);

            Assert.IsNull(GameObject.Find("ActualBackyardPredatorLaneWarning"),
                "Couch test #2: the screen-wide predator lane-warning banner is retired nonsense scenery.");
            var openLawn = env.transform.Find("OpenLawnDistrict");
            Assert.IsNotNull(openLawn);
            Assert.IsNull(openLawn.Find("ActualEnvironmentArtOverlay"),
                "The lawn-landmark icon must not stretch across the yard as a giant X panel; the painted plate is the lawn.");

            var eagleLane = env.transform.Find("EagleShadowSweepLane").Find("ActualEnvironmentArtOverlay").GetComponent<SpriteRenderer>();
            var coyoteLane = env.transform.Find("CoyoteFencePressureLane").Find("ActualEnvironmentArtOverlay").GetComponent<SpriteRenderer>();
            var scentPatch = env.transform.Find("ScentTrailPatch_0").Find("ActualEnvironmentArtOverlay").GetComponent<SpriteRenderer>();
            var leashStone = env.transform.Find("LeashRouteStone_0").Find("ActualEnvironmentArtOverlay").GetComponent<SpriteRenderer>();

            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
            yield return null;

            Assert.IsFalse(eagleLane.enabled, "The eagle sweep lane must not litter Backyard Rescue.");
            Assert.IsFalse(coyoteLane.enabled, "The coyote pressure lane must not litter Backyard Rescue.");
            Assert.IsFalse(scentPatch.enabled, "Scent patches must not litter Backyard Rescue.");
            Assert.IsFalse(leashStone.enabled, "Leash route stones must not litter Backyard Rescue.");

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;
            yield return null;

            Assert.IsTrue(eagleLane.enabled, "Eagle Shadow keeps its readable sweep band.");
            Assert.IsFalse(coyoteLane.enabled);

            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;
            yield return null;

            Assert.IsTrue(scentPatch.enabled, "Scent Search keeps its scent patches.");
            Assert.IsFalse(eagleLane.enabled, "Mission cues turn back off when their mission ends.");
        }

        [Test]
        public void MissionBriefingCard_ContainsAllTextRowsWithoutOverlap()
        {
            var layout = ArenaHud.BuildMissionBriefingLayout(1920f, 1080f);
            Rect[] rows =
            {
                layout.Title, layout.Presentation, layout.Goal, layout.First,
                layout.Roles, layout.Ownership, layout.Controls
            };

            foreach (Rect row in rows)
            {
                Assert.GreaterOrEqual(row.xMin, layout.Card.xMin, "Briefing text must not spill off the card.");
                Assert.LessOrEqual(row.xMax, layout.Card.xMax, "Briefing text must not spill off the card.");
                Assert.GreaterOrEqual(row.yMin, layout.Card.yMin, "Briefing text must not spill off the card.");
                Assert.LessOrEqual(row.yMax, layout.Card.yMax,
                    "Briefing text must not spill past the bottom of the card (couch test #2 regression).");
            }

            for (int i = 1; i < rows.Length; i++)
            {
                Assert.GreaterOrEqual(rows[i].yMin, rows[i - 1].yMax,
                    $"Briefing row {i} must not overlap the row above it.");
            }

            Assert.GreaterOrEqual(layout.Goal.height, 80f,
                "The GOAL block needs room for a wrapped two-to-three line mission prompt.");
            Assert.GreaterOrEqual(layout.Controls.height, 250f,
                "Every level briefing needs room for visual keyboard and Switch-style controller diagrams.");
            Assert.GreaterOrEqual(ArenaHud.MissionBriefingGoalFontSize, 22,
                "The mission goal must be couch-readable.");
        }

        [Test]
        public void MissionSelect_HasPictureTileAndInstructionsForEveryMission()
        {
            foreach (GameManager.MissionVariant variant in System.Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                var tile = FinalGameplayArt.LoadMissionTile(variant);
                Assert.IsNotNull(tile,
                    $"{variant} is missing a mission select picture tile at {FinalGameplayArt.MissionTilePath(variant)}.");
                Assert.AreNotSame(SpriteShapeCache.WhiteSquare, tile,
                    $"{variant} mission select picture tile must not use the runtime white square.");
                Assert.That(tile.name, Does.Not.Contain("RuntimeWhiteSquare"),
                    $"{variant} mission select picture tile must be authored art, not a primitive square.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(MissionInstructionCatalog.DescriptionFor(variant)),
                    $"{variant} is missing level-select description text.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(MissionInstructionCatalog.HowToPlayFor(variant)),
                    $"{variant} is missing level-select how-to-play text.");
            }
        }

        [Test]
        public void MissionResultOverlay_IsLargeReadableAndNonOverlapping()
        {
            var layout = ArenaHud.BuildResultOverlayLayout(1920f, 1080f);
            Assert.AreEqual(new Rect(0f, 0f, 1920f, 1080f), layout.Backdrop,
                "Mission results should dim the full screen, not only draw a small floating panel.");
            Assert.That(layout.Card.width / 1920f, Is.InRange(0.70f, 0.85f));
            Assert.That(layout.Card.height / 1080f, Is.InRange(0.55f, 0.75f));
            Assert.GreaterOrEqual(ArenaHud.ResultHeadlineFontSize, 44);
            Assert.GreaterOrEqual(ArenaHud.ResultSubtitleFontSize, 28);
            Assert.GreaterOrEqual(ArenaHud.ResultBodyFontSize, 22);
            Assert.GreaterOrEqual(ArenaHud.ResultButtonFontSize, 24);

            Assert.Greater(layout.Headline.yMin, layout.Card.yMin + 24f);
            Assert.Greater(layout.Subtitle.yMin, layout.Headline.yMax);
            Assert.Greater(layout.Score.yMin, layout.Subtitle.yMax);
            Assert.Greater(layout.Flavor.yMin, layout.Score.yMax);
            Assert.Greater(layout.Challenge.yMin, layout.Flavor.yMax);
            Assert.AreEqual(3, layout.Buttons.Length);
            Assert.AreEqual(3, layout.ButtonHints.Length);

            foreach (Rect button in layout.Buttons)
            {
                Assert.Greater(button.yMin, layout.Challenge.yMax + 24f);
                Assert.GreaterOrEqual(button.height, 54f);
                Assert.GreaterOrEqual(button.width, 180f);
                Assert.LessOrEqual(button.xMin, layout.Card.xMax);
                Assert.LessOrEqual(button.yMax, layout.Card.yMax - 24f);
            }
        }

        [UnityTest]
        public IEnumerator ArenaHud_LoadsGeneratedHudSkinForMenusAndOverlays()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var hud = Object.FindFirstObjectByType<ArenaHud>();
            Assert.IsNotNull(hud, "ArenaScene should install ArenaHud.");
            hud.WarmGeneratedHudSkinForTests();
            Assert.IsTrue(hud.GeneratedHudSkinLoaded,
                "ArenaHud should load generated panel/tile/badge/button/overlay sprites before drawing menus.");
        }

        [UnityTest]
        public IEnumerator MissionWorldLabels_UseGeneratedSkinsForLabelsAndScorePops()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.OperationPeeBreak);
            yield return null;

            var door = GameObject.Find("PeeBreakDoor");
            Assert.IsNotNull(door, "Operation Pee Break should create a labeled door marker.");
            var doorSkin = door.GetComponentInChildren<WorldLabelSkin>(true);
            Assert.IsNotNull(doorSkin, "Shared AddWorldLabel should attach generated label skins.");
            Assert.IsTrue(doorSkin.HasGeneratedSkin, "Door label should render with a generated label bubble/command skin.");
            Assert.That(doorSkin.SkinSpriteName, Does.Contain("world_label_"));
            Assert.AreEqual(FinalGameplayArt.WorldLabelCommand,
                WorldLabelSkin.SelectSpritePath("COCOA STAND HERE", scorePop: false));
            Assert.AreEqual(FinalGameplayArt.WorldLabelWarning,
                WorldLabelSkin.SelectSpritePath("NEEDS CHEDDAR LEASH", scorePop: false));

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            var cheddar = GameObject.Find("Cheddar").GetComponent<CheddarAndCocoa.Dogs.DogController>();
            var snack = Object.FindFirstObjectByType<Treat>();
            Assert.IsNotNull(snack, "Snack Heist should spawn a collectible to score.");
            snack.CollectBy(cheddar);
            yield return null;

            bool foundScorePopSkin = false;
            foreach (var skin in Object.FindObjectsByType<WorldLabelSkin>(FindObjectsSortMode.None))
            {
                if (skin.SkinSpriteName == "world_pop_burst")
                {
                    foundScorePopSkin = true;
                    break;
                }
            }

            Assert.IsTrue(foundScorePopSkin, "Shared score pops should render with the generated burst skin.");
        }

        [UnityTest]
        public IEnumerator MissionWorldLabels_AreContextualPromptsUnlessDebugOverlayEnabled()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            var snack = Object.FindFirstObjectByType<Treat>();
            Assert.IsNotNull(snack, "Snack Heist should spawn a labeled collectible for prompt visibility coverage.");
            var prompt = snack.GetComponentInChildren<WorldLabelVisibility>();
            Assert.IsNotNull(prompt, "Shared AddWorldLabel should attach contextual prompt visibility.");

            var dogs = Object.FindObjectsByType<DogController>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(dogs.Length, 2, "Prompt visibility should use both couch co-op dogs.");
            foreach (var dog in dogs)
            {
                dog.transform.position = snack.transform.position + Vector3.right * 30f;
            }
            yield return null;

            Assert.IsFalse(prompt.IsVisible,
                "Production world labels should not behave like giant always-on objective text when dogs are far away.");

            dogs[0].transform.position = snack.transform.position + Vector3.right * (prompt.PromptRange * 0.5f);
            yield return null;

            Assert.IsTrue(prompt.IsVisible,
                "World labels should become small contextual prompts when a dog reaches interaction range.");

            dogs[0].transform.position = snack.transform.position + Vector3.right * 30f;
            yield return null;
            Assert.IsFalse(prompt.IsVisible, "Contextual prompts should hide again after the dogs leave range.");

            game.SetPlaytestOverlayVisible(true);
            yield return null;
            Assert.IsTrue(prompt.IsVisible, "The playtest/debug overlay should restore full world-label visibility for review.");
        }

        [UnityTest]
        public IEnumerator ObjectiveArrows_UseIconGuidanceInProductionAndTextOnlyInDebugOverlay()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            var cheddar = GameObject.Find("Cheddar");
            var snack = Object.FindFirstObjectByType<Treat>();
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(snack);

            cheddar.transform.position = snack.transform.position + Vector3.right * 12f;
            yield return null;
            yield return null;

            var arrow = game.ObjectiveArrows[0];
            Assert.IsTrue(arrow.IsVisible, "Production guidance should still show the generated dog-mounted arrow icon.");
            Assert.IsFalse(arrow.TextVisible,
                "Production guidance should not put explanatory objective text beside the dog by default.");

            game.SetPlaytestOverlayVisible(true);
            yield return null;

            Assert.IsTrue(arrow.IsVisible);
            Assert.IsTrue(arrow.TextVisible,
                "The playtest/debug overlay should restore dog-arrow text for developer review.");
        }

        [UnityTest]
        public IEnumerator DogIdentityPoseLabels_AreDebugTextNotProductionOverlays()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            var cheddar = GameObject.Find("Cheddar").GetComponent<CheddarAndCocoa.Dogs.DogReadabilityFeedback>();
            var cocoa = GameObject.Find("Cocoa").GetComponent<CheddarAndCocoa.Dogs.DogReadabilityFeedback>();
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);
            Assert.That(cheddar.IdentityLabel, Does.Contain("CHEDDAR"));
            Assert.That(cocoa.IdentityLabel, Does.Contain("COCOA"));
            Assert.IsFalse(cheddar.IdentityTextVisible,
                "Normal play should not draw always-on dog pose text over the character art.");
            Assert.IsFalse(cocoa.IdentityTextVisible,
                "Normal play should not draw always-on dog pose text over the character art.");

            game.SetPlaytestOverlayVisible(true);
            yield return null;

            Assert.IsTrue(cheddar.IdentityTextVisible,
                "The playtest/debug overlay should restore dog identity pose text for review.");
            Assert.IsTrue(cocoa.IdentityTextVisible,
                "The playtest/debug overlay should restore dog identity pose text for review.");
        }

        [UnityTest]
        public IEnumerator InteractionRangeRings_UseIconGuidanceInProductionAndTextOnlyInDebugOverlay()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            var target = Object.FindFirstObjectByType<Treat>();
            Assert.IsNotNull(target, "Backyard Rescue should spawn a collectible so squirrel pressure can expose the bark range.");
            Assert.IsNotNull(game.SquirrelObject);

            game.SquirrelObject.transform.position = target.transform.position + Vector3.right;
            game.ForceSquirrelStealAttempt();
            yield return null;

            var range = game.SquirrelObject.GetComponent<InteractionRangeIndicator>();
            Assert.IsNotNull(range);
            Assert.IsTrue(range.IsVisible, "The actionable bark range ring should still be visible as an icon cue.");
            Assert.AreEqual("BARK RANGE", range.Label);
            Assert.IsFalse(range.TextVisible,
                "Production range cues should not add explanatory floating text by default.");

            game.SetPlaytestOverlayVisible(true);
            yield return null;

            Assert.IsTrue(range.IsVisible);
            Assert.IsTrue(range.TextVisible,
                "The playtest/debug overlay should restore range support text for developer review.");
        }

        [UnityTest]
        public IEnumerator MissionActorStateLabels_AreContextualSupportTextUnlessDebugOverlayEnabled()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            var feedback = game.SquirrelObject.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback);
            Assert.IsTrue(feedback.HasContextualTextVisibility,
                "Mission actor state labels should participate in the shared contextual/debug visibility contract.");
            Assert.That(feedback.Label, Does.Contain("WAITING"),
                "The deterministic state string should remain available even when rendered text is hidden.");

            var dogs = Object.FindObjectsByType<DogController>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(dogs.Length, 2);
            foreach (var dog in dogs)
            {
                dog.transform.position = game.SquirrelObject.transform.position + Vector3.right * 30f;
            }
            yield return null;
            yield return null;

            Assert.IsFalse(feedback.TextVisible,
                "Production actor-state text should not stay visible across the whole level when dogs are far away.");

            dogs[0].transform.position = game.SquirrelObject.transform.position;
            yield return null;
            yield return null;

            Assert.IsTrue(feedback.TextVisible,
                "Actor-state labels may appear as close-range contextual support text.");

            dogs[0].transform.position = game.SquirrelObject.transform.position + Vector3.right * 30f;
            yield return null;
            yield return null;
            Assert.IsFalse(feedback.TextVisible);

            game.SetPlaytestOverlayVisible(true);
            yield return null;

            Assert.IsTrue(feedback.TextVisible,
                "The playtest/debug overlay should restore actor-state support text for developer review.");
        }

        [UnityTest]
        public IEnumerator MissionPropArt_ProvidesNearbyInteractableAffordanceWithoutPermanentHighlight()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            // Watch the snack farthest from the squirrel: the live steal loop destroys/replaces
            // its nearest snack, and this test must observe one stable prop across several frames.
            var snacks = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None);
            Assert.IsNotEmpty(snacks);
            Vector3 squirrelPosition = game.SquirrelObject.transform.position;
            Treat snack = snacks[0];
            foreach (var candidate in snacks)
            {
                if (Vector3.Distance(candidate.transform.position, squirrelPosition) >
                    Vector3.Distance(snack.transform.position, squirrelPosition))
                    snack = candidate;
            }
            var prop = snack.GetComponent<MissionPropArtAttachment>();
            Assert.IsNotNull(prop, "Snack Heist snacks should use shared mission prop art.");
            Assert.IsTrue(prop.HasRuntimeSprite);
            Vector3 snackPosition = snack.transform.position;
            float affordanceRange = prop.AffordanceRange;

            var dogs = Object.FindObjectsByType<DogController>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(dogs.Length, 2);
            foreach (var dog in dogs)
            {
                dog.transform.position = snackPosition + Vector3.right * 30f;
            }
            yield return null;
            yield return null;

            Assert.IsFalse(prop.IsAffordanceActive,
                "Generated mission props should not stay permanently highlighted when the dogs are far away.");

            dogs[0].transform.position = snackPosition + Vector3.right * (affordanceRange * 0.45f);
            yield return null;
            yield return null;

            Assert.IsTrue(prop.IsAffordanceActive,
                "Generated mission props should pulse/tint as a diegetic nearby interactable affordance.");

            Vector3 currentPropPosition = prop.transform.position;
            foreach (var dog in dogs)
            {
                dog.transform.position = currentPropPosition + Vector3.right * 30f;
            }
            yield return null;
            yield return null;

            Assert.IsFalse(prop.IsAffordanceActive,
                "The affordance should clear after the dogs leave interaction staging range.");
        }

        [UnityTest]
        public IEnumerator ArenaWowSetDressing_AddsShowcaseSceneryWithoutGameplayColliders()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);

            ArenaWowSetDressing wow = null;
            for (int i = 0; i < 80 && (wow == null || !wow.Built); i++)
            {
                wow = Object.FindFirstObjectByType<ArenaWowSetDressing>();
                yield return null;
            }

            Assert.IsNotNull(wow, "ArenaScene should install the cosmetic wow set dressing.");
            Assert.IsTrue(wow.Built, "Wow set dressing should finish building after scene load.");
            Assert.IsTrue(wow.HasShowcaseSceneryPolish,
                "Showcase dressing should be authored-art accents over the painted yard plate, without frozen dog backdrop art.");
            Assert.AreEqual(0, wow.PlaceholderRectCount,
                "One-background rule: the wow layer must not draw placeholder rectangles over the painted yard plate.");
            Assert.GreaterOrEqual(wow.ShowcaseScenerySetPieceCount, 2);
            Assert.GreaterOrEqual(wow.AnimatedShowcaseSceneryCount, 2);
            Assert.AreEqual(0, wow.CharacterVignetteCount);
            Assert.IsTrue(wow.HasNoFrozenDogBackdrops);

            foreach (string objectName in new[]
                     {
                         "WowPropSnapshotLeft",
                         "WowPropSnapshotRight",
                         "WowMissionSpotlight"
                     })
            {
                var go = GameObject.Find(objectName);
                Assert.IsNotNull(go, $"{objectName} should be visible showcase set dressing.");
                var renderer = go.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(renderer, $"{objectName} should render as scenery.");
                Assert.IsFalse(SpriteShapeCache.IsPlaceholder(renderer.sprite),
                    $"{objectName} should use authored art, not a runtime primitive square.");
                Assert.IsNull(go.GetComponent<Collider2D>(), $"{objectName} must stay nonblocking.");
            }
        }

        [UnityTest]
        public IEnumerator BackyardEnvironment_BuildsDecorativePropsInsideLargeYard()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var env = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            Assert.IsNotNull(env, "Backyard environment root should be built at runtime.");
            Assert.Greater(env.transform.childCount, 12, "Yard should be dressed with multiple props, not empty.");
            AssertHasDecorativeProp(env.transform, "FenceRailTop", "Backyard should read as fenced, not only bounded by invisible walls.");
            AssertHasDecorativeProp(env.transform, "FenceRailBottom", "Backyard should read as fenced, not only bounded by invisible walls.");
            AssertHasDecorativeProp(env.transform, "FenceRailLeft", "Backyard should read as fenced, not only bounded by invisible walls.");
            AssertHasDecorativeProp(env.transform, "FenceRailRight", "Backyard should read as fenced, not only bounded by invisible walls.");
            AssertHasDecorativeProp(env.transform, "BackDoorExterior", "The patio should have a visible house/back-door cue.");
            AssertHasDecorativeProp(env.transform, "HousePatioDistrict", "The house/patio district should read as a distinct yard zone.");
            AssertHasDecorativeProp(env.transform, "BackDoorWindowGlow", "The back door should read as connected to the house.");
            AssertHasDecorativeProp(env.transform, "BackDoorKnobExterior", "The back door should have a readable interaction silhouette.");
            AssertHasDecorativeProp(env.transform, "BackDoorStep", "The patio should have a visible step from the house.");
            AssertHasDecorativeProp(env.transform, "PeeBreakOutdoorPayoffPath", "Operation Pee Break should have an outdoor route/payoff cue.");
            AssertHasDecorativeProp(env.transform, "OpenLawnDistrict", "The yard should have an obvious central lawn district.");
            AssertHasDecorativeProp(env.transform, "EagleShadowSweepLane", "Eagle Shadow needs a readable sweep band in the yard.");
            AssertHasDecorativeProp(env.transform, "CoyoteFencePressureLane", "Coyote defense needs a readable fence pressure lane.");
            AssertHasDecorativeProp(env.transform, "SnackDistrictZone", "Snack Heist should read as a district, not just a prop.");
            AssertHasDecorativeProp(env.transform, "SnackHeistTableBackplate", "Snack Heist should have a table district before final art.");
            AssertHasDecorativeProp(env.transform, "SnackDistrictPlate", "Snack Heist should have food/table silhouette support.");
            AssertHasDecorativeProp(env.transform, "LaundryDistrictZone", "Sock Panic should read as a district, not just a prop.");
            AssertHasDecorativeProp(env.transform, "SockPanicLaundryCorner", "Sock Panic should have a laundry district before final art.");
            AssertHasDecorativeProp(env.transform, "LaundryLine", "Laundry district should have a readable clothesline cue.");
            AssertHasDecorativeProp(env.transform, "LaundrySockCue", "Laundry district should show a sock/laundry payoff cue.");
            AssertHasDecorativeProp(env.transform, "MissionRouteDash_HouseToLawn", "Mission routes should be staged visually through the yard.");
            AssertHasDecorativeProp(env.transform, "MissionRouteDash_LawnToFence", "Mission routes should be staged visually through the yard.");
            Assert.GreaterOrEqual(CountChildrenStartingWith(env.transform, "ScentTrailPatch_"), 6,
                "Scent Search should have visible background scent patches.");
            Assert.GreaterOrEqual(CountChildrenStartingWith(env.transform, "LeashRouteStone_"), 5,
                "Leash Walk should have a visible route path through the yard.");

            // The yard must actually be large, not a tiny demo box.
            var cameraRig = Camera.main.GetComponent<SharedCameraController>();
            Assert.IsNotNull(cameraRig);
            Rect bounds = cameraRig.LevelBounds;
            Assert.AreEqual(ArenaWorldScale.BackyardWidth, bounds.width, 0.01f);
            Assert.AreEqual(ArenaWorldScale.BackyardHeight, bounds.height, 0.01f);
            Assert.IsTrue(cameraRig.IsClampedToBounds, "Large yard camera should clamp to the level bounds.");

            // Close framing must reveal less than half the yard so exploration actually scrolls;
            // widest framing must cover the yard when co-op players deliberately split up.
            const float couchAspect = 16f / 9f;
            float closeViewWidth = cameraRig.MinOrthoSize * 2f * couchAspect;
            float strategicViewWidth = cameraRig.MaxOrthoSize * 2f * couchAspect;
            Assert.Less(closeViewWidth, bounds.width * 0.5f, "Regrouped dogs should explore with a scrolling local camera.");
            Assert.GreaterOrEqual(strategicViewWidth, bounds.width, "Split dogs should remain visible at strategic zoom.");

            // The shared-camera contract must survive couch displays and resizable desktop windows,
            // including portrait/narrow aspects where the configured 16:9 ceiling is insufficient.
            Vector2 leftDog = new Vector2(bounds.xMin + 1f, bounds.center.y);
            Vector2 rightDog = new Vector2(bounds.xMax - 1f, bounds.center.y);
            foreach (float aspect in new[] { 32f / 9f, 16f / 9f, 4f / 3f, 9f / 16f })
            {
                float required = cameraRig.RequiredOrthoSizeForTargets(leftDog, rightDog, aspect);
                Assert.LessOrEqual(required, cameraRig.MaximumOrthoSizeForAspect(aspect),
                    $"Camera must keep both dogs framed at aspect {aspect:0.00}.");
            }

            // The dog-to-property ratio is an explicit production constraint, not an eyeballed
            // scene preference. Collider width is stable even while pose sprites animate.
            var cheddar = GameObject.Find("Cheddar");
            Assert.IsNotNull(cheddar);
            float dogLength = cheddar.GetComponent<Collider2D>().bounds.size.x;
            Assert.LessOrEqual(dogLength / bounds.width, ArenaWorldScale.MaximumDogToYardWidthRatio,
                "A dachshund should occupy no more than two percent of yard width.");

            Camera.main.orthographicSize = cameraRig.MaximumOrthoSizeForAspect(9f / 16f);
            yield return null;
            Assert.Greater(cheddar.GetComponent<CheddarAndCocoa.Dogs.DogReadabilityFeedback>().StrategicLabelScale, 1f,
                "Dog identity labels should grow when strategic zoom makes dog sprites tiny.");

            // The cover bushes should sit on the Eagle Shadow hide zones so "HIDE HERE" reads as
            // real backyard cover.
            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            AssertSpatialSpread(game.EagleCoverZones, bounds.width * 0.65f, "Eagle cover");
            AssertSpatialSpread(game.FenceGaps, bounds.width * 0.9f, "Fence patrol");
            AssertSpatialSpread(game.DigSpots, bounds.width * 0.75f, "Scent search");
            AssertSpatialSpread(game.TerritoryZones, bounds.width * 0.7f, "Territory");
            AssertSpatialSpread(game.LeashCheckpoints, bounds.width * 0.7f, "Leash route");
            foreach (var zone in game.EagleCoverZones)
            {
                bool hasBushOnZone = false;
                foreach (Transform child in env.transform)
                {
                    if (!child.name.StartsWith("CoverBush")) continue;
                    if (Vector2.Distance(new Vector2(child.position.x, child.position.y), zone) < 0.5f)
                    {
                        hasBushOnZone = true;
                        break;
                    }
                }
                Assert.IsTrue(hasBushOnZone, $"A cover bush should sit on hide zone {zone}.");
            }

            // Every prop should sit inside the walls and render behind gameplay actors.
            foreach (Transform child in env.transform)
            {
                Vector3 p = child.position;
                Assert.IsTrue(bounds.Contains(new Vector2(p.x, p.y)),
                    $"Prop {child.name} at {p} should be inside the level bounds {bounds}.");
                var sr = child.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr, $"Prop {child.name} should have a SpriteRenderer.");
                Assert.Less(sr.sortingOrder, 5, $"Prop {child.name} should render behind treats and dogs.");
                Assert.IsNull(child.GetComponent<Collider2D>(),
                    $"Decorative prop {child.name} must not have a collider (treats stay reachable).");
            }
        }

        [UnityTest]
        public IEnumerator EveryMission_StagesDogsWithinAReasonableFirstObjectiveWalk()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = GameObject.Find("Cheddar");
            var cocoa = GameObject.Find("Cocoa");
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);

            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                var mission = game.MissionVariantAt(i);
                game.StartMission(mission);
                yield return null;

                float cheddarDistance = Vector2.Distance(cheddar.transform.position, game.MissionEntryTarget);
                float cocoaDistance = Vector2.Distance(cocoa.transform.position, game.MissionEntryTarget);
                Assert.LessOrEqual(cheddarDistance, game.MaximumMissionEntryDistance,
                    $"{mission} should not open with a tedious walk for Cheddar.");
                Assert.LessOrEqual(cocoaDistance, game.MaximumMissionEntryDistance,
                    $"{mission} should not open with a tedious walk for Cocoa.");

                if (mission != GameManager.MissionVariant.CarRide)
                {
                    Assert.IsTrue(game.ObjectiveArrows[0].IsVisible, $"{mission} should immediately guide Cheddar.");
                    Assert.IsTrue(game.ObjectiveArrows[1].IsVisible, $"{mission} should immediately guide Cocoa.");
                    Assert.That(game.TeamGuidanceLabel, Does.Contain("Cheddar:"));
                    Assert.That(game.TeamGuidanceLabel, Does.Contain("Cocoa:"));
                    Assert.That(game.TeamGuidanceLabel, Does.Contain("m"));
                }
            }
        }

        [UnityTest]
        public IEnumerator DistantObjective_EnablesVisibleTravelAssist_AndReleasesNearTarget()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddarObject = GameObject.Find("Cheddar");
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddarObject);
            var cheddar = cheddarObject.GetComponent<CheddarAndCocoa.Dogs.DogController>();
            var feedback = cheddarObject.GetComponent<CheddarAndCocoa.Dogs.DogReadabilityFeedback>();

            game.StartMission(GameManager.MissionVariant.SquirrelConspiracy);
            yield return null;
            float normalSpeed = cheddar.MaxSpeedUnitsPerSecond;
            cheddarObject.transform.position = new Vector2(55f, -25f);
            yield return null;
            yield return null;

            Assert.IsTrue(cheddar.TravelAssist);
            Assert.Greater(cheddar.MaxSpeedUnitsPerSecond, normalSpeed);
            Assert.That(feedback.IdentityLabel, Does.Contain("TRAIL READY"));
            Assert.That(game.TeamGuidanceLabel, Does.Contain("Cheddar:"));
            Assert.That(game.TeamGuidanceLabel, Does.Contain("[TRAIL SPRINT]"),
                "The shared HUD should tell the couch when a distant route has accelerated.");

            cheddarObject.transform.position = game.SquirrelObject.transform.position;
            yield return null;
            yield return null;
            Assert.IsFalse(cheddar.TravelAssist);
            Assert.AreEqual(1f, cheddar.TravelAssistMultiplier);
            Assert.That(game.TeamGuidanceLabel, Does.Contain("Cheddar: ON TARGET"));
            Assert.That(game.TeamGuidanceLabel, Does.Not.Contain("Cheddar: ON TARGET [TRAIL SPRINT]"));
        }

        [UnityTest]
        public IEnumerator StrategicZoom_KeepsActionWorldPopsReadable()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            Camera.main.orthographicSize = 34f;
            var go = new GameObject("StrategicWorldPopTest");
            var label = go.AddComponent<TextMesh>();
            label.text = "+200 TUG COMPLETE";
            var pop = go.AddComponent<MissionWorldPop>();
            pop.Begin(label);
            yield return null;

            Assert.Greater(pop.StrategicScale, 1f);
            Assert.Greater(go.transform.localScale.x, 1f);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator BackyardEnvironment_UsesGeneratedEnvironmentSpritesOverDistrictMarkers()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);

            BackyardRescueArtEnhancer enhancer = null;
            for (int i = 0; i < 80 && (enhancer == null || !enhancer.Enhanced); i++)
            {
                enhancer = Object.FindFirstObjectByType<BackyardRescueArtEnhancer>();
                yield return null;
            }

            Assert.IsNotNull(enhancer, "ArenaScene should install the additive art enhancer.");
            Assert.IsTrue(enhancer.Enhanced, "Backyard art enhancer should finish after scene load.");
            Assert.GreaterOrEqual(enhancer.EnvironmentArtOverlayCount, 38,
                "Broad yard districts should have generated sprite overlays, not only square markers.");
            Assert.IsTrue(enhancer.PaintedPlateIsSoleYardBackground,
                "One-background rule: the painted plate renders at full opacity and no placeholder rectangle stays visible in the yard.");

            // The photo-crop reskin collage is retired: the painted plate is the single background.
            foreach (string legacyPhotoReskin in new[]
                     {
                         "ActualPhotoReskinLawnStripes", "ActualPhotoReskinPoolPatio",
                         "ActualPhotoReskinPatioPavers", "ActualPhotoReskinHouseDeck",
                         "ActualPhotoReskinBigTree", "ActualPhotoReskinGardenBoxes",
                         "ActualPhotoReskinHedgeBottom", "ActualPhotoReskinHedgeRight",
                         "ActualPhotoReskinWoodFenceTop"
                     })
            {
                Assert.IsNull(GameObject.Find(legacyPhotoReskin),
                    $"{legacyPhotoReskin} should no longer layer photo crops over the painted yard plate.");
            }

            var env = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            Assert.IsNotNull(env);
            AssertEnvironmentOverlay(env.transform, "HousePatioDistrict", "yard_house_patio");
            AssertEnvironmentOverlay(env.transform, "BackDoorExterior", "yard_back_door");
            AssertEnvironmentOverlay(env.transform, "SnackDistrictZone", "yard_snack_table");
            AssertEnvironmentOverlay(env.transform, "LaundryDistrictZone", "yard_laundry_corner");
            AssertEnvironmentOverlay(env.transform, "ScentTrailPatch_0", "yard_scent_trail");
            AssertEnvironmentOverlay(env.transform, "LeashRouteStone_0", "yard_leash_route");
            AssertEnvironmentOverlay(env.transform, "EagleShadowSweepLane", "yard_threat_lane");
            AssertEnvironmentOverlay(env.transform, "FenceRailTop", "yard_fence_run");
            // Couch feedback: the flat vector tree/garden/pond overlays clashed with the painted
            // plate (which already paints those landmarks); BackyardPoolZone owns the water now.
            foreach (string retired in new[] { "Pond", "PondShallows", "TreeTrunk", "TreeCanopy", "TreeCanopyHi", "GardenBed" })
            {
                var target = env.transform.Find(retired);
                Assert.IsNotNull(target, $"{retired} anchor should survive as invisible staging.");
                Assert.IsNull(target.Find("ActualEnvironmentArtOverlay"),
                    $"{retired} must not re-grow a generated overlay that fights the painted plate.");
            }
            AssertEnvironmentOverlay(env.transform, "Flower_0", "yard_flower_patch");
            AssertEnvironmentOverlay(env.transform, "PicnicBlanket", "yard_picnic_blanket");
            AssertEnvironmentOverlay(env.transform, "Sandbox", "yard_sandbox");
            AssertEnvironmentOverlay(env.transform, "SteppingStone_0", "yard_stepping_stone");
            AssertEnvironmentOverlay(env.transform, "CoverBush_0", "bush");
        }

        [UnityTest]
        public IEnumerator BackyardEnvironment_UsesGeneratedBuildingSpritesForHouseCluster()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);

            BackyardRescueArtEnhancer enhancer = null;
            for (int i = 0; i < 80 && (enhancer == null || !enhancer.Enhanced); i++)
            {
                enhancer = Object.FindFirstObjectByType<BackyardRescueArtEnhancer>();
                yield return null;
            }

            Assert.IsNotNull(enhancer, "ArenaScene should install the additive art enhancer.");
            Assert.IsTrue(enhancer.Enhanced, "Backyard art enhancer should finish after scene load.");
            Assert.GreaterOrEqual(enhancer.BuildingArtOverlayCount, 3,
                "House and yard-building silhouettes should use generated building sprites, not only district rectangles.");

            var env = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            Assert.IsNotNull(env);
            AssertBuildingOverlay(env.transform, "HousePatioDistrict", "home_exterior_facade");
            AssertBuildingOverlay(env.transform, "BackDoorExterior", "back_porch_entry");
            AssertWorldBuildingArt("ActualBuildingArtYardShed", "yard_shed_storage");
        }

        private static void AssertSpatialSpread(Vector2[] points, float minimumWidth, string label)
        {
            Assert.IsNotEmpty(points);
            float min = points[0].x;
            float max = points[0].x;
            foreach (Vector2 point in points)
            {
                min = Mathf.Min(min, point.x);
                max = Mathf.Max(max, point.x);
            }
            Assert.GreaterOrEqual(max - min, minimumWidth, $"{label} should use the full yard, not cluster near spawn.");
        }

        private static void AssertHasDecorativeProp(Transform parent, string name, string message)
        {
            var child = parent.Find(name);
            Assert.IsNotNull(child, message);
            Assert.IsNotNull(child.GetComponent<SpriteRenderer>(), $"{name} should render as a background prop.");
            Assert.IsNull(child.GetComponent<Collider2D>(), $"{name} must stay non-blocking.");
        }

        private static int CountChildrenStartingWith(Transform parent, string prefix)
        {
            int count = 0;
            foreach (Transform child in parent)
                if (child.name.StartsWith(prefix)) count++;
            return count;
        }

        private static void AssertEnvironmentOverlay(Transform root, string objectName, string expectedSpriteName)
        {
            var target = root.Find(objectName);
            Assert.IsNotNull(target, $"Missing environment prop {objectName}.");
            var overlay = target.Find("ActualEnvironmentArtOverlay");
            Assert.IsNotNull(overlay, $"{objectName} should have generated environment art.");
            Assert.IsNull(overlay.GetComponent<Collider2D>(), $"{objectName} environment art must stay nonblocking.");
            var renderer = overlay.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            Assert.IsNotNull(renderer.sprite);
            Assert.AreEqual(expectedSpriteName, renderer.sprite.name);
            Assert.AreNotSame(SpriteShapeCache.WhiteSquare, renderer.sprite,
                $"{objectName} overlay should not use the runtime white square.");
            var ambient = overlay.GetComponent<SceneryAmbientMotion>();
            Assert.IsNotNull(ambient, $"{objectName} should participate in the shared ambient scenery pass.");
            Assert.IsTrue(ambient.PreservesGameplayAnchor,
                $"{objectName} ambient motion must stay on a collider-free render child.");
            var fallback = target.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(fallback);
            Assert.LessOrEqual(fallback.color.a, 0.001f,
                $"{objectName} square fallback must be fully invisible behind generated art (one-background rule).");
        }

        private static void AssertBuildingOverlay(Transform root, string objectName, string expectedSpriteName)
        {
            var target = root.Find(objectName);
            Assert.IsNotNull(target, $"Missing building anchor {objectName}.");
            var overlay = target.Find("ActualBuildingArtOverlay");
            Assert.IsNotNull(overlay, $"{objectName} should have generated building art.");
            Assert.IsNull(overlay.GetComponent<Collider2D>(), $"{objectName} building art must stay nonblocking.");
            var renderer = overlay.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            Assert.IsNotNull(renderer.sprite);
            Assert.AreEqual(expectedSpriteName, renderer.sprite.name);
            Assert.AreNotSame(SpriteShapeCache.WhiteSquare, renderer.sprite,
                $"{objectName} building overlay should not use the runtime white square.");
            Assert.IsNotNull(overlay.GetComponent<SceneryAmbientMotion>(),
                $"{objectName} building art should share the restrained scenery light pass.");
        }

        private static void AssertWorldBuildingArt(string objectName, string expectedSpriteName)
        {
            var go = GameObject.Find(objectName);
            Assert.IsNotNull(go, $"{objectName} should be visible building scenery.");
            Assert.IsNull(go.GetComponent<Collider2D>(), $"{objectName} must stay nonblocking.");
            var renderer = go.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            Assert.IsNotNull(renderer.sprite);
            Assert.AreEqual(expectedSpriteName, renderer.sprite.name);
            Assert.AreNotSame(SpriteShapeCache.WhiteSquare, renderer.sprite,
                $"{objectName} should not use the runtime white square.");
            Assert.IsNotNull(go.GetComponent<SceneryAmbientMotion>(),
                $"{objectName} world building should share the restrained scenery light pass.");
        }

    }
}
