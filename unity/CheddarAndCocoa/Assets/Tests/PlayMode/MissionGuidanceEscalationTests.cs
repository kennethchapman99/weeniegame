using NUnit.Framework;

namespace CheddarAndCocoa.Game.Tests
{
    public sealed class MissionGuidanceEscalationTests
    {
        [Test]
        public void FreshInstance_StartsAtTierZero()
        {
            var guidance = new MissionGuidanceEscalation();
            Assert.AreEqual(0, guidance.Tier);
            Assert.AreEqual(0f, guidance.StallSeconds);
        }

        [Test]
        public void Tick_AdvancesTier_AtDefaultThresholds()
        {
            var guidance = new MissionGuidanceEscalation();
            guidance.Configure(MissionGuidanceEscalation.MaxTier,
                MissionGuidanceEscalation.DefaultTier1Seconds,
                MissionGuidanceEscalation.DefaultTier2Seconds,
                MissionGuidanceEscalation.DefaultTier3Seconds);

            guidance.Tick(11.99f);
            Assert.AreEqual(0, guidance.Tier, "Just under the Tier 1 threshold must stay Tier 0.");

            guidance.Tick(0.02f); // total 12.01s
            Assert.AreEqual(1, guidance.Tier);

            guidance.Tick(12.98f); // total 24.99s
            Assert.AreEqual(1, guidance.Tier, "Just under the Tier 2 threshold must stay Tier 1.");

            guidance.Tick(0.02f); // total 25.01s
            Assert.AreEqual(2, guidance.Tier);

            guidance.Tick(19.98f); // total 44.99s
            Assert.AreEqual(2, guidance.Tier, "Just under the Tier 3 threshold must stay Tier 2.");

            guidance.Tick(0.02f); // total 45.01s
            Assert.AreEqual(3, guidance.Tier);
        }

        [Test]
        public void Tick_WithZeroOrNegativeDeltaTime_DoesNotAccumulate()
        {
            var guidance = new MissionGuidanceEscalation();
            guidance.Configure(MissionGuidanceEscalation.MaxTier, 12f, 25f, 45f);

            guidance.Tick(0f);
            guidance.Tick(-1f);

            Assert.AreEqual(0f, guidance.StallSeconds);
            Assert.AreEqual(0, guidance.Tier);
        }

        [Test]
        public void NotifyProgress_ResetsStallAndTier()
        {
            var guidance = new MissionGuidanceEscalation();
            guidance.Configure(MissionGuidanceEscalation.MaxTier, 12f, 25f, 45f);
            guidance.Tick(30f);
            Assert.AreEqual(2, guidance.Tier);

            guidance.NotifyProgress();

            Assert.AreEqual(0, guidance.Tier);
            Assert.AreEqual(0f, guidance.StallSeconds);
        }

        [Test]
        public void Reset_ZeroesStallAndTier()
        {
            var guidance = new MissionGuidanceEscalation();
            guidance.Configure(MissionGuidanceEscalation.MaxTier, 12f, 25f, 45f);
            guidance.Tick(50f);
            Assert.AreEqual(3, guidance.Tier);

            guidance.Reset();

            Assert.AreEqual(0, guidance.Tier);
            Assert.AreEqual(0f, guidance.StallSeconds);
        }

        [Test]
        public void TierCap_PreventsReachingHigherTiers()
        {
            var guidance = new MissionGuidanceEscalation();
            guidance.Configure(2, 12f, 25f, 45f);

            guidance.Tick(90f);

            Assert.AreEqual(2, guidance.Tier, "A Tier-2 cap must never reach Tier 3, no matter how long the stall runs.");
        }

        [Test]
        public void ZeroTierCap_StaysAtDiscovery()
        {
            var guidance = new MissionGuidanceEscalation();
            guidance.Configure(0, 12f, 25f, 45f);

            guidance.Tick(90f);

            Assert.AreEqual(0, guidance.Tier);
        }

        [Test]
        public void Configure_CustomTimings_ShiftTheThresholds()
        {
            var guidance = new MissionGuidanceEscalation();
            guidance.Configure(MissionGuidanceEscalation.MaxTier, tier1Seconds: 5f, tier2Seconds: 10f, tier3Seconds: 15f);

            guidance.Tick(5f);
            Assert.AreEqual(1, guidance.Tier);

            guidance.Tick(5f); // total 10s
            Assert.AreEqual(2, guidance.Tier);

            guidance.Tick(5f); // total 15s
            Assert.AreEqual(3, guidance.Tier);
        }
    }
}
