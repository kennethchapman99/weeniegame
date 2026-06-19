using NUnit.Framework;
using UnityEngine;
using CheddarAndCocoa.Game;
using CheddarAndCocoa.Dogs;

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
        public void FinalGameplayArt_ProvidesStableRuntimeResourcePaths()
        {
            Assert.AreEqual("ArenaFinal/Characters/Squirrel/squirrel_idle", FinalGameplayArt.PathFor(RuntimeArtSpriteFactory.RuntimeSpriteId.Squirrel));
            Assert.AreEqual("ArenaFinal/Props/Mission/weenie_collectible", FinalGameplayArt.PathFor(RuntimeArtSpriteFactory.RuntimeSpriteId.WeenieCollectible));
            Assert.AreEqual("ArenaFinal/VFX/bark_burst", FinalGameplayArt.PathFor(RuntimeArtSpriteFactory.RuntimeSpriteId.BarkBurst));
        }

        [Test]
        public void FinalDogPoseArt_ProvidesStableCheddarCocoaPosePaths()
        {
            Assert.AreEqual("ArenaFinal/Characters/Dogs/Cheddar/cheddar_idle", FinalDogPoseArt.PathFor(DogId.Cheddar, DogReadabilityFeedback.Pose.Idle));
            Assert.AreEqual("ArenaFinal/Characters/Dogs/Cheddar/cheddar_bark", FinalDogPoseArt.PathFor(DogId.Cheddar, DogReadabilityFeedback.Pose.Bark));
            Assert.AreEqual("ArenaFinal/Characters/Dogs/Cocoa/cocoa_run", FinalDogPoseArt.PathFor(DogId.Cocoa, DogReadabilityFeedback.Pose.Run));
            Assert.DoesNotThrow(() => ArenaDogPoseSprites.For(DogId.Cheddar, DogReadabilityFeedback.Pose.Idle));
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
            BackyardArtVfxPulse pulse = null;
            Assert.DoesNotThrow(() => pulse = BackyardArtVfxPulse.Spawn(Vector3.zero, RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert, Vector3.one, 10, Color.white, 0.1f));
            if (pulse != null) Object.DestroyImmediate(pulse.gameObject);
        }

        [Test]
        public void DynamicTreatArtEnhancer_NoFinalArtStillScansSafely()
        {
            var go = new GameObject("DynamicTreatArtEnhancerTest");
            var treatGo = new GameObject("TreatArtCandidate");
            try
            {
                treatGo.AddComponent<CircleCollider2D>();
                treatGo.AddComponent<Treat>();
                var enhancer = go.AddComponent<DynamicTreatArtEnhancer>();

                Assert.DoesNotThrow(() => enhancer.ScanTreats());
                Assert.GreaterOrEqual(enhancer.EnhancedTreatCount, 0);
            }
            finally
            {
                Object.DestroyImmediate(treatGo);
                Object.DestroyImmediate(go);
            }
        }
    }
}
