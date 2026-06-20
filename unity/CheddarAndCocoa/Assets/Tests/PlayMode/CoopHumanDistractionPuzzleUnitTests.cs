using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the dual-method human-distraction beat: Cheddar's burp is a
    /// cooldown-gated burst, Cocoa's belly flop is a stamina-limited sustain, both feed the same
    /// attention meter, and the sneaker only progresses while the human is distracted.
    /// </summary>
    public sealed class CoopHumanDistractionPuzzleUnitTests
    {
        private static CoopHumanDistractionPuzzle Make(
            float sneakNeeded = 3f, float threshold = 0.5f, float decay = 0.5f,
            float burpSpike = 0.7f, float burpCooldown = 2f, float flopRise = 1.2f, float flopStamina = 2.5f)
        {
            var p = new CoopHumanDistractionPuzzle();
            p.Configure(sneakNeeded, threshold, decay, burpSpike, burpCooldown, flopRise, flopStamina);
            return p;
        }

        [Test]
        public void Burp_SpikesAttentionAndGoesOnCooldown()
        {
            var p = Make(burpSpike: 0.7f, burpCooldown: 2f);
            Assert.IsTrue(p.BurpReady);
            p.Burp();
            Assert.AreEqual(0.7f, p.Attention, 0.0001f);
            Assert.IsTrue(p.HumanDistracted);
            Assert.IsFalse(p.BurpReady);

            p.Burp(); // still on cooldown -> wasted, no extra attention
            Assert.AreEqual(1, p.WastedBurps);
            Assert.AreEqual(0.7f, p.Attention, 0.0001f);

            p.Advance(2f, sneaking: false); // cooldown elapses (and attention decays away)
            Assert.IsTrue(p.BurpReady);
            Assert.AreEqual(1, p.WastedBurps, "Cooldown re-burps are wasted, but a ready burp is not.");
        }

        [Test]
        public void BellyFlop_SustainsAttentionAndLetsTheSneakerSolve()
        {
            var p = Make(sneakNeeded: 2f, flopStamina: 6f);
            p.SetBellyFlop(true);

            int guard = 0;
            while (!p.Solved && guard++ < 40)
                p.Advance(0.5f, sneaking: true);

            Assert.IsTrue(p.Solved, "A sustained belly-rub hold lets the partner sneak the objective.");
            Assert.Less(p.FlopStamina, 6f, "Holding the flop spends stamina.");
            Assert.AreEqual(0, p.WastedBurps);
        }

        [Test]
        public void BellyFlopStaminaRunsOut_AndExhaustedFlopCannotRestart()
        {
            var p = Make(flopStamina: 1f);
            p.SetBellyFlop(true);
            p.Advance(0.6f, false);
            Assert.IsTrue(p.BellyFlopped);
            p.Advance(0.6f, false); // total 1.2 > stamina -> Cocoa gets up
            Assert.IsFalse(p.BellyFlopped);
            Assert.AreEqual(0f, p.FlopStamina, 0.0001f);

            p.SetBellyFlop(true); // no stamina left
            Assert.IsFalse(p.BellyFlopped, "Can't flop again once stamina is spent.");
        }

        [Test]
        public void SneakingWhileUndistracted_MakesNoProgressAndTalliesExposures()
        {
            var p = Make();
            p.Advance(0.5f, sneaking: true); // attention 0 -> exposed (transition 1)
            p.Advance(0.5f, sneaking: true); // still exposed (no new transition)
            Assert.AreEqual(0f, p.SneakProgress);
            Assert.AreEqual(1, p.Exposures);

            p.Advance(0.5f, sneaking: false); // not sneaking -> not exposed
            p.Advance(0.5f, sneaking: true);  // exposed again (transition 2)
            Assert.AreEqual(2, p.Exposures);
        }

        [Test]
        public void SneakerProgressesOnlyDuringTheDistractionWindow()
        {
            var p = Make(sneakNeeded: 10f, threshold: 0.5f, decay: 0.5f, burpSpike: 0.9f);
            p.Burp(); // attention 0.9
            p.Advance(0.3f, sneaking: true); // still > threshold -> progress
            float progressInWindow = p.SneakProgress;
            Assert.Greater(progressInWindow, 0f);

            p.Advance(1.0f, sneaking: true); // attention decays below threshold -> no further progress
            Assert.AreEqual(progressInWindow, p.SneakProgress, 0.0001f,
                "Once the human looks away, the sneaker stops advancing.");
        }

        [Test]
        public void Reset_RestoresFullStaminaAndClearsProgress()
        {
            var p = Make(flopStamina: 2.5f);
            p.Burp();
            p.SetBellyFlop(true);
            p.Advance(0.5f, true);
            p.Reset();
            Assert.AreEqual(0f, p.Attention);
            Assert.AreEqual(0f, p.SneakProgress);
            Assert.AreEqual(2.5f, p.FlopStamina, 0.0001f);
            Assert.IsFalse(p.BellyFlopped);
            Assert.IsFalse(p.Solved);
            Assert.AreEqual(0, p.WastedBurps);
            Assert.AreEqual(0, p.Exposures);
        }
    }
}
