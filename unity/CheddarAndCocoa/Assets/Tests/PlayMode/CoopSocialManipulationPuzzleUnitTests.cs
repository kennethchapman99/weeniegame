using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the social-manipulation beat: the human only "gets it" on the EXACT
    /// required combo (which needs both dogs), incomplete/off-message combos build confusion, and a
    /// maxed confusion misreads and resets.
    /// </summary>
    public sealed class CoopSocialManipulationPuzzleUnitTests
    {
        // "Take us for a walk" = Cocoa's door-stare + Cheddar's presented leash.
        private const SocialStimulus Walk = SocialStimulus.DoorStare | SocialStimulus.PresentLeash;

        private static CoopSocialManipulationPuzzle Make(float comprehend = 2f, float confusionMax = 3f)
        {
            var p = new CoopSocialManipulationPuzzle();
            p.Configure(Walk, comprehend, confusionMax);
            return p;
        }

        [Test]
        public void ExactCombo_BuildsComprehensionToSolve()
        {
            var p = Make(comprehend: 1f);
            p.SetStimulus(SocialStimulus.DoorStare, true);
            p.SetStimulus(SocialStimulus.PresentLeash, true);
            Assert.IsTrue(p.ExactMatch);
            p.Advance(1.1f);
            Assert.IsTrue(p.Solved);
            Assert.AreEqual(0, p.Misreads);
        }

        [Test]
        public void OnlyOneDogsStimulus_IsNotEnough()
        {
            var p = Make();
            p.SetStimulus(SocialStimulus.DoorStare, true); // Cocoa alone
            Assert.IsFalse(p.ExactMatch);
            p.Advance(1f);
            Assert.AreEqual(0f, p.Comprehension, "An incomplete combo earns no comprehension.");
            Assert.Greater(p.Confusion, 0f);
        }

        [Test]
        public void OffMessageStimulus_BreaksTheComboAndBuildsConfusionFaster()
        {
            var p = Make();
            p.SetStimulus(SocialStimulus.DoorStare, true);
            p.SetStimulus(SocialStimulus.PresentLeash, true);
            p.SetStimulus(SocialStimulus.NudgeShoe, true); // off-message extra
            Assert.IsFalse(p.ExactMatch);
            Assert.IsTrue(p.HasOffMessageStimulus);
            p.Advance(1f);
            Assert.AreEqual(1.5f, p.Confusion, 0.0001f, "Off-message stimulus raises confusion 1.5x.");
        }

        [Test]
        public void ConfusionMaxing_MisreadsAndResets()
        {
            var p = Make(confusionMax: 1f);
            p.SetStimulus(SocialStimulus.BarkRhythm, true); // wrong-only stimulus
            p.Advance(1f); // confusion -> >=1 (1.5 with off-message) -> misread
            Assert.AreEqual(1, p.Misreads);
            Assert.AreEqual(0f, p.Confusion, 0.0001f);
            Assert.AreEqual(0f, p.Comprehension, 0.0001f);
        }

        [Test]
        public void FixingTheComboAfterAWrongOne_RecoversAndSolves()
        {
            var p = Make(comprehend: 1f, confusionMax: 5f);
            p.SetStimulus(SocialStimulus.DoorStare, true); // incomplete first
            p.Advance(0.5f);
            Assert.Greater(p.Confusion, 0f);

            p.SetStimulus(SocialStimulus.PresentLeash, true); // complete the combo
            Assert.IsTrue(p.ExactMatch);
            int guard = 0;
            while (!p.Solved && guard++ < 20) p.Advance(0.5f);
            Assert.IsTrue(p.Solved, "Once the exact combo holds, comprehension wins out.");
        }

        [Test]
        public void TogglingStimuliUpdatesTheActiveSet()
        {
            var p = Make();
            p.SetStimulus(SocialStimulus.DoorStare, true);
            Assert.AreEqual(SocialStimulus.DoorStare, p.Active);
            p.SetStimulus(SocialStimulus.PresentLeash, true);
            Assert.AreEqual(Walk, p.Active);
            p.SetStimulus(SocialStimulus.DoorStare, false);
            Assert.AreEqual(SocialStimulus.PresentLeash, p.Active);
        }

        [Test]
        public void ResetClearsEverything()
        {
            var p = Make(confusionMax: 1f);
            p.SetStimulus(SocialStimulus.NudgeShoe, true);
            p.Advance(1f); // misread
            p.Reset();
            Assert.AreEqual(SocialStimulus.None, p.Active);
            Assert.AreEqual(0f, p.Comprehension);
            Assert.AreEqual(0f, p.Confusion);
            Assert.AreEqual(0, p.Misreads);
            Assert.IsFalse(p.Solved);
        }
    }
}
