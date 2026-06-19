using NUnit.Framework;
using UnityEngine;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class BackyardArtIntegrationPlayModeTests
    {
        [Test]
        public void RuntimeArtSpriteFactory_MissingOrPresentDraftArt_IsSafeToQuery()
        {
            Assert.DoesNotThrow(() => RuntimeArtSpriteFactory.Get(RuntimeArtSpriteFactory.RuntimeSpriteId.Squirrel));
            Assert.DoesNotThrow(() => RuntimeArtSpriteFactory.Get(RuntimeArtSpriteFactory.RuntimeSpriteId.BarkBurst));
            Assert.DoesNotThrow(() => RuntimeArtSpriteFactory.Has(RuntimeArtSpriteFactory.RuntimeSpriteId.BackyardBush));
        }

        [Test]
        public void ArtSpriteOverlay_WithNullSprite_DoesNotBreakFallbackObject()
        {
            var go = new GameObject("OverlayFallbackTest");
            try
            {
                var overlay = go.AddComponent<ArtSpriteOverlay>();
                overlay.Init(null, Vector3.zero, Vector3.one, 10, Color.white, true);

                Assert.IsFalse(overlay.HasRuntimeSprite);
                Assert.AreEqual(string.Empty, overlay.RuntimeSpriteName);
                Assert.IsNotNull(go.transform.Find("ActualArtOverlay"));
                Assert.IsNotNull(go.transform.Find("ActualArtShadow"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ArtSpriteOverlay_WithGeneratedSprite_ReportsRuntimeSprite()
        {
            var go = new GameObject("OverlayGeneratedSpriteTest");
            try
            {
                var overlay = go.AddComponent<ArtSpriteOverlay>();
                overlay.Init(SpriteShapeCache.WhiteSquare, Vector3.zero, Vector3.one, 10, Color.white, true);

                Assert.IsTrue(overlay.HasRuntimeSprite);
                Assert.That(overlay.RuntimeSpriteName, Does.Contain("RuntimeWhiteSquare"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BackyardArtVfxPulse_MissingDraftSprite_ReturnsNullSafely()
        {
            Assert.DoesNotThrow(() => BackyardArtVfxPulse.Spawn(Vector3.zero, RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert, Vector3.one, 10, Color.white, 0.1f));
        }
    }
}
