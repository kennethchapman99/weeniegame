using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Fast, scene-free boundary guards for the shared scoring/rank and deterministic-seed helpers
    /// every mission relies on. New file (no overlap with the active art/gameplay pass).
    /// </summary>
    public sealed class CoreScoringSeedUnitTests
    {
        [Test]
        public void Rank_ClearThresholds_AwardPawfectHeroSurvivor()
        {
            var pawfect = MissionRankCalculator.Calculate(1500, true, 1500, 1050, 350);
            Assert.AreEqual(MissionRankCalculator.PawfectRank, pawfect.Rank);
            Assert.AreEqual(3, pawfect.Stars);

            var hero = MissionRankCalculator.Calculate(1050, true, 1500, 1050, 350);
            Assert.AreEqual(MissionRankCalculator.HeroRank, hero.Rank);
            Assert.AreEqual(2, hero.Stars);

            var survivor = MissionRankCalculator.Calculate(350, true, 1500, 1050, 350);
            Assert.AreEqual(MissionRankCalculator.SurvivorRank, survivor.Rank);
            Assert.AreEqual(1, survivor.Stars);

            var low = MissionRankCalculator.Calculate(349, true, 1500, 1050, 350);
            Assert.AreEqual(MissionRankCalculator.LowRank, low.Rank);
            Assert.AreEqual(1, low.Stars, "A clear below survivor score still earns the 1 clear star.");
        }

        [Test]
        public void Rank_WithoutClear_NeverPawfectOrHero_AndZeroStars()
        {
            // A huge score without clearing the mission cannot earn the clear-only ranks.
            var notCleared = MissionRankCalculator.Calculate(99999, false, 1500, 1050, 350);
            Assert.AreEqual(MissionRankCalculator.SurvivorRank, notCleared.Rank);
            Assert.AreEqual(0, notCleared.Stars, "A failed mission earns no stars even at the survivor score.");

            var lowFail = MissionRankCalculator.Calculate(10, false, 1500, 1050, 350);
            Assert.AreEqual(MissionRankCalculator.LowRank, lowFail.Rank);
            Assert.AreEqual(0, lowFail.Stars);
        }

        [Test]
        public void Rank_ExactBoundaryScores_AreInclusive()
        {
            Assert.AreEqual(MissionRankCalculator.PawfectRank, MissionRankCalculator.Calculate(1500, true, 1500, 1050, 350).Rank);
            Assert.AreEqual(MissionRankCalculator.HeroRank, MissionRankCalculator.Calculate(1499, true, 1500, 1050, 350).Rank);
            Assert.AreEqual(MissionRankCalculator.HeroRank, MissionRankCalculator.Calculate(1050, true, 1500, 1050, 350).Rank);
            Assert.AreEqual(MissionRankCalculator.SurvivorRank, MissionRankCalculator.Calculate(1049, true, 1500, 1050, 350).Rank);
        }

        [Test]
        public void Seed_IsDeterministicNonNegativeAndInputSensitive()
        {
            int a = MissionSeedGenerator.StableSeed("eagle_shadow_panic", 0);
            int b = MissionSeedGenerator.StableSeed("eagle_shadow_panic", 0);
            Assert.AreEqual(a, b, "Same inputs must give the same seed.");
            Assert.GreaterOrEqual(a, 0, "Seed must be non-negative for System.Random.");

            Assert.AreNotEqual(a, MissionSeedGenerator.StableSeed("coyotes_fence", 0), "Different mission id changes the seed.");
            Assert.AreNotEqual(a, MissionSeedGenerator.StableSeed("eagle_shadow_panic", 1), "Different session index changes the seed.");
            Assert.AreNotEqual(a, MissionSeedGenerator.StableSeed("eagle_shadow_panic", 0, 1), "Different variant index changes the seed.");

            Assert.GreaterOrEqual(MissionSeedGenerator.StableSeed(null, 0), 0, "A null mission id must not throw and stays non-negative.");
        }

        [Test]
        public void VariantIndex_StaysInRange_AndCollapsesForSingleVariant()
        {
            Assert.AreEqual(0, MissionSeedGenerator.VariantIndex(12345, 1));
            Assert.AreEqual(0, MissionSeedGenerator.VariantIndex(12345, 0));
            for (int seed = 0; seed < 50; seed++)
            {
                int idx = MissionSeedGenerator.VariantIndex(seed, 3);
                Assert.That(idx, Is.InRange(0, 2), "Variant index must stay within the variant count.");
            }
        }
    }
}
