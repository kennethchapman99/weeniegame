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
    /// Couch feedback pass: the HUGE backyard pool (floaties/swim/shake ported from the frozen TS
    /// build), the bullet-step HOW TO PLAY copy with on-screen-label highlighting, and the eagle
    /// depth pose. Geometry/copy checks are pure; dog mode transitions run in the real ArenaScene.
    /// </summary>
    public sealed class BackyardPoolPlayModeTests
    {
        // ---- Pure pool geometry (poolGeometry.ts port) ----

        [Test]
        public void PoolGeometry_WaterRect_IsHugeAndInsideTheYard()
        {
            Assert.GreaterOrEqual(BackyardPoolZone.WaterAreaFractionOfYard, 0.09f,
                "Couch feedback: the pool must read HUGE - at least ~10% of the whole yard is open water.");
            Assert.GreaterOrEqual(BackyardPoolZone.WaterRect.xMin, -ArenaWorldScale.BackyardWidth * 0.5f);
            Assert.LessOrEqual(BackyardPoolZone.WaterRect.yMax, ArenaWorldScale.BackyardHeight * 0.5f);
        }

        [Test]
        public void PoolGeometry_FloaterEllipseAndWaterTests_MatchReferenceBehavior()
        {
            var floaters = new[]
            {
                new BackyardPoolZone.FloaterState
                {
                    Center = BackyardPoolZone.WaterRect.center,
                    Radii = new Vector2(4f, 2.5f)
                }
            };

            Vector2 onFloater = BackyardPoolZone.WaterRect.center;
            Vector2 openWater = BackyardPoolZone.WaterRect.center + new Vector2(9f, 0f);
            Vector2 dryLand = new Vector2(BackyardPoolZone.WaterRect.xMax + 5f, BackyardPoolZone.WaterRect.center.y);

            Assert.IsTrue(BackyardPoolZone.InPoolRect(onFloater));
            Assert.IsTrue(BackyardPoolZone.OnFloater(onFloater, floaters[0].Center, floaters[0].Radii));
            Assert.IsFalse(BackyardPoolZone.InWater(onFloater, floaters), "Standing on a floater is not swimming.");
            Assert.IsTrue(BackyardPoolZone.InWater(openWater, floaters), "Off the floater inside the rect is open water.");
            Assert.IsFalse(BackyardPoolZone.InWater(dryLand, floaters), "Outside the rect is deck, not water.");

            // The prototype squeezes the vertical radius by 0.9: a point just past ry*0.9 is wet.
            Vector2 justBelowRim = floaters[0].Center + new Vector2(0f, floaters[0].Radii.y * 0.95f);
            Assert.IsTrue(BackyardPoolZone.InWater(justBelowRim, floaters));
        }

        [Test]
        public void PoolGeometry_NearestDeckPoint_IsAlwaysJustOutsideTheWater()
        {
            foreach (var probe in new[]
                     {
                         BackyardPoolZone.WaterRect.center,
                         BackyardPoolZone.WaterRect.center + new Vector2(-12f, 8f),
                         BackyardPoolZone.WaterRect.center + new Vector2(14f, -9f)
                     })
            {
                Vector2 deck = BackyardPoolZone.NearestDeckPoint(probe);
                Assert.IsFalse(BackyardPoolZone.InPoolRect(deck),
                    $"Deck exit for {probe} must land outside the water rect.");
            }
        }

        [Test]
        public void ScentSearch_DigSpots_AllStayOnDryLand()
        {
            var bounds = new Rect(-ArenaWorldScale.BackyardWidth * 0.5f, -ArenaWorldScale.BackyardHeight * 0.5f,
                ArenaWorldScale.BackyardWidth, ArenaWorldScale.BackyardHeight);
            foreach (Vector2 spot in ScentSearchMissionController.ComputeDigSpots(bounds))
            {
                Assert.IsFalse(BackyardPoolZone.InPoolRect(spot),
                    $"Dig mound at {spot} would be underwater - digging must happen on dry land.");
            }
        }

        // ---- HOW TO PLAY bullet steps + on-screen label highlighting ----

        [Test]
        public void HowToPlaySteps_EveryMission_HasReadableBulletSteps()
        {
            foreach (GameManager.MissionVariant variant in System.Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                string[] steps = MissionInstructionCatalog.HowToPlayStepsFor(variant);
                Assert.GreaterOrEqual(steps.Length, 2, $"{variant} should explain itself in short steps, not a wall of text.");
                foreach (string step in steps)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(step));
                    Assert.LessOrEqual(step.Length, 170, $"{variant} step is drifting back toward a paragraph: {step}");
                }
            }
        }

        [Test]
        public void HowToPlaySteps_BackyardRescue_ShowsOnScreenLabelsVerbatim()
        {
            string[] steps = MissionInstructionCatalog.HowToPlayStepsFor(GameManager.MissionVariant.BackyardRescue);
            Assert.GreaterOrEqual(steps.Length, 5, "The mission with the most beats must be split into steps.");
            Assert.That(string.Join("\n", steps), Does.Contain("SQUIRREL STEALING - BARK!"),
                "The squirrel warning must be quoted exactly as it appears on-screen so players recognize it.");
            Assert.That(string.Join("\n", steps), Does.Contain("ESCAPE GAP"));
            Assert.That(string.Join("\n", steps).ToLowerInvariant(), Does.Contain("pool"),
                "The yard pool is part of the level now and the briefing should mention it.");
        }

        [Test]
        public void HighlightOnScreenLabels_WrapsCapsRunsAndLeavesProseAlone()
        {
            string highlighted = MissionInstructionCatalog.HighlightOnScreenLabels(
                "SQUIRREL STEALING - BARK! means bark near the squirrel.");
            Assert.That(highlighted, Does.Contain("<b><color=#ffd75e>SQUIRREL STEALING - BARK!</color></b>"),
                "The full on-screen label, including the dash join, gets one highlight span.");
            Assert.That(highlighted, Does.Contain("means bark near the squirrel."));

            string plain = MissionInstructionCatalog.HighlightOnScreenLabels(
                "Cheddar and Cocoa share one leash, so stay close.");
            Assert.That(plain, Does.Not.Contain("<color"), "Plain prose must not be highlighted.");
        }

        // ---- Eagle depth pose (pure) ----

        [Test]
        public void EagleDepthPose_BanksIntoTravelAndBreathesScale()
        {
            Assert.AreEqual(18f, ThreatReadabilityAnimator.EagleBankDegrees(Vector2.up, mirrored: false), 0.01f,
                "Climbing banks the body up to the clamp.");
            Assert.AreEqual(-18f, ThreatReadabilityAnimator.EagleBankDegrees(Vector2.up, mirrored: true), 0.01f,
                "A mirrored (left-facing) eagle banks the opposite way.");
            Assert.AreEqual(0f, ThreatReadabilityAnimator.EagleBankDegrees(Vector2.right, mirrored: false), 0.01f,
                "Level flight carries no bank.");
            Assert.Less(ThreatReadabilityAnimator.EagleDepthScale(0.07f), 1f, "High in the bob = farther = smaller.");
            Assert.Greater(ThreatReadabilityAnimator.EagleDepthScale(-0.07f), 1f, "Low in the bob = closer = bigger.");
        }

        // ---- Scene: swim / floatie / shake loop ----

        [UnityTest]
        public IEnumerator PoolZone_DogFallsIn_SwimsShakesAndComesOutWet()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            var pool = Object.FindFirstObjectByType<BackyardPoolZone>();
            Assert.IsNotNull(pool, "ArenaScene should install the backyard pool zone.");
            pool.BuildNow();

            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
            yield return null;
            Assert.IsTrue(pool.ActiveForCurrentMission, "Backyard Rescue is a yard mission - the pool is open.");

            var dog = Object.FindObjectsByType<DogController>(FindObjectsSortMode.None)[0];
            dog.SetMode(MovementMode.Free);

            Vector2 openWater = FindOpenWaterPoint(pool);
            dog.transform.position = openWater;
            yield return null;
            yield return null;
            Assert.AreEqual(MovementMode.Swimming, dog.Mode,
                "Falling into open water must flip the dog into the swimming mode.");

            // Reaching the deck edge roots the dog in a shake...
            dog.transform.position = new Vector2(BackyardPoolZone.WaterRect.xMax + 1.5f, openWater.y);
            yield return null;
            yield return null;
            Assert.AreEqual(MovementMode.Shaking, dog.Mode,
                "Climbing out at the deck edge must start the rooted shake.");

            // ...and once POOL.shake seconds elapse they are free again, but wet for a while.
            // (Headless deltaTime is tiny, so the timer is fast-forwarded via the Force* hook.)
            pool.ForceShakeElapsed(dog, BackyardPoolZone.ShakeSeconds + 0.1f);
            yield return null;
            yield return null;
            Assert.AreEqual(MovementMode.Free, dog.Mode, "The shake must end on its own.");
            Assert.IsTrue(dog.IsWet, "A freshly dried dog carries the wet timer from the prototype.");
        }

        [UnityTest]
        public IEnumerator PoolZone_DogOnFloater_StaysDryAndPoolClosesIndoors()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            var pool = Object.FindFirstObjectByType<BackyardPoolZone>();
            Assert.IsNotNull(pool);
            pool.BuildNow();

            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
            Assert.GreaterOrEqual(pool.FloaterCount, 3, "The huge pool needs multiple runnable floaties.");

            var dog = Object.FindObjectsByType<DogController>(FindObjectsSortMode.None)[0];
            dog.SetMode(MovementMode.Free);
            dog.transform.position = pool.FloaterAt(0).Center;
            yield return null;
            dog.transform.position = pool.FloaterAt(0).Center; // stay centered while it drifts
            yield return null;
            Assert.AreEqual(MovementMode.Free, dog.Mode, "Standing on a floatie is running, not swimming.");
            Assert.IsTrue(dog.OnFloater, "The floater overlay should engage for the speed bonus.");

            // Indoors the pool closes and any leftover water state resolves to Free.
            dog.SetMode(MovementMode.Swimming);
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;
            yield return null;
            Assert.IsFalse(pool.ActiveForCurrentMission, "Interior missions must not have an open pool.");
            Assert.AreEqual(MovementMode.Free, dog.Mode,
                "Scene-state reset: leaving the yard dries the dogs off immediately.");
        }

        private static Vector2 FindOpenWaterPoint(BackyardPoolZone pool)
        {
            // Scan the water rect for a spot clear of every drifting floatie.
            for (float x = BackyardPoolZone.WaterRect.xMin + 2f; x < BackyardPoolZone.WaterRect.xMax - 2f; x += 2f)
            {
                for (float y = BackyardPoolZone.WaterRect.yMin + 2f; y < BackyardPoolZone.WaterRect.yMax - 2f; y += 2f)
                {
                    var p = new Vector2(x, y);
                    if (!pool.AnyFloaterUnder(p)) return p;
                }
            }

            Assert.Fail("No open-water point found - floaties should never tile the whole pool.");
            return Vector2.zero;
        }
    }
}
