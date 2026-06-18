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
            Assert.GreaterOrEqual(bounds.width, 40f, "Arena should read as a real yard (wide).");
            Assert.GreaterOrEqual(bounds.height, 24f, "Arena should read as a real yard (tall).");
            Assert.IsTrue(cameraRig.IsClampedToBounds, "Large yard camera should clamp to the level bounds.");

            // The cover bushes should sit on the Eagle Shadow hide zones so "HIDE HERE" reads as
            // real backyard cover.
            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
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
    }
}
