using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class ReadabilityValidationPlayModeTests
    {
        [Test]
        public void ReadabilityValidator_ReportsMissingRequirements()
        {
            var required = ReadabilityRequirement.ObjectiveVisible |
                           ReadabilityRequirement.ScoreVisible |
                           ReadabilityRequirement.ReplayVisible;

            var present = ReadabilityRequirement.ObjectiveVisible |
                          ReadabilityRequirement.ScoreVisible;

            var result = ReadabilityValidator.Validate(present, required);

            Assert.IsFalse(result.Passed);
            Assert.IsTrue((result.Missing & ReadabilityRequirement.ReplayVisible) != 0);
        }

        [Test]
        public void ReadabilityValidator_PassesWhenRequirementsAreMet()
        {
            var required = ReadabilityRequirement.ObjectiveVisible |
                           ReadabilityRequirement.ScoreVisible |
                           ReadabilityRequirement.DogIdentityReadable;

            var result = ReadabilityValidator.Validate(required, required);

            Assert.IsTrue(result.Passed);
            Assert.AreEqual(ReadabilityRequirement.None, result.Missing);
        }
    }
}
