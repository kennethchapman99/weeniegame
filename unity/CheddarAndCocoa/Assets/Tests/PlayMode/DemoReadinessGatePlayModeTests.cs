using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class DemoReadinessGatePlayModeTests
    {
        [Test]
        public void DemoReadinessGate_FailsWhenRequirementsAreMissing()
        {
            var present = DemoReadinessRequirement.MissionSelectable |
                          DemoReadinessRequirement.ObjectiveReadable |
                          DemoReadinessRequirement.ScoreReadable;

            var result = DemoReadinessGate.Evaluate(present);

            Assert.IsFalse(result.Ready);
            Assert.IsTrue((result.Missing & DemoReadinessRequirement.ClearPathTested) != 0);
            Assert.IsTrue((result.Missing & DemoReadinessRequirement.ReplayTested) != 0);
        }

        [Test]
        public void DemoReadinessGate_PassesWhenAllRequirementsArePresent()
        {
            var result = DemoReadinessGate.Evaluate(DemoReadinessGate.RequiredForBackyardDemo);

            Assert.IsTrue(result.Ready);
            Assert.AreEqual(DemoReadinessRequirement.None, result.Missing);
        }
    }
}
