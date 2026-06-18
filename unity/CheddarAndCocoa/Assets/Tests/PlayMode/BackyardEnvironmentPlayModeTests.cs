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
        [UnityTest]
        public IEnumerator BackyardEnvironment_BuildsDecorativePropsInsideLargeYard()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var env = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            Assert.IsNotNull(env, "Backyard environment root should be built at runtime.");
            Assert.Greater(env.transform.childCount, 12, "Yard should be dressed with multiple props, not empty.");

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

            // The dog-to-property ratio is an explicit production constraint, not an eyeballed
            // scene preference. Collider width is stable even while pose sprites animate.
            var cheddar = GameObject.Find("Cheddar");
            Assert.IsNotNull(cheddar);
            float dogLength = cheddar.GetComponent<Collider2D>().bounds.size.x;
            Assert.LessOrEqual(dogLength / bounds.width, ArenaWorldScale.MaximumDogToYardWidthRatio,
                "A dachshund should occupy no more than two percent of yard width.");

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
    }
}
