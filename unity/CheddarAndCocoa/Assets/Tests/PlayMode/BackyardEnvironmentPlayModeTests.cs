using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.CameraRig;
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
            float height = ArenaHud.MissionSelectPanelHeight(22);
            Assert.GreaterOrEqual(height, 110f + 11f * 42f + 142f);

            Rect panel = ArenaHud.FitPanel(1920f, 1080f, 900f, height);
            Assert.GreaterOrEqual(panel.height, height);
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
            AssertHasDecorativeProp(env.transform, "BackDoorStep", "The patio should have a visible step from the house.");
            AssertHasDecorativeProp(env.transform, "EagleShadowSweepLane", "Eagle Shadow needs a readable sweep band in the yard.");
            AssertHasDecorativeProp(env.transform, "CoyoteFencePressureLane", "Coyote defense needs a readable fence pressure lane.");
            AssertHasDecorativeProp(env.transform, "SnackHeistTableBackplate", "Snack Heist should have a table district before final art.");
            AssertHasDecorativeProp(env.transform, "SockPanicLaundryCorner", "Sock Panic should have a laundry district before final art.");
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
    }
}
