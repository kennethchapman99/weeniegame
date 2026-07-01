using System;
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

        [Test]
        public void AllMissionDefinitions_HaveReusableCouchTestPresentationMetadata()
        {
            foreach (GameManager.MissionVariant variant in Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                var definition = GameManager.BuildMissionDefinition(variant);
                var result = ReadabilityValidator.ValidateMissionDefinition(definition);

                Assert.IsTrue(result.Passed, $"{variant} missing readability metadata: {result.Missing}");
                Assert.IsNotEmpty(definition.RoleHint, $"{variant} needs a couch-test role hint.");
                Assert.IsNotEmpty(definition.MechanicTag, $"{variant} needs a reusable mechanic tag.");
                Assert.IsNotEmpty(definition.SceneCue, $"{variant} needs a scene cue.");
                Assert.IsNotEmpty(definition.ReusablePresentation, $"{variant} needs reusable presentation guidance.");
                StringAssert.Contains("Cheddar", definition.RoleHint + definition.ReusablePresentation);
                StringAssert.Contains("Cocoa", definition.RoleHint + definition.ReusablePresentation);
            }
        }

        [Test]
        public void PresentationLine_CombinesMechanicAndSceneForHud()
        {
            var peeBreak = GameManager.BuildMissionDefinition(GameManager.MissionVariant.OperationPeeBreak);

            StringAssert.Contains("Social manipulation", peeBreak.PresentationLine);
            StringAssert.Contains("Couch", peeBreak.PresentationLine);
            StringAssert.Contains("Cocoa", peeBreak.RoleHint);
            StringAssert.Contains("Cheddar", peeBreak.RoleHint);
        }

        [Test]
        public void MissionBadges_HaveExplicitReadableCodesAndColors()
        {
            foreach (GameManager.MissionVariant variant in Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                string code = ArenaHud.MissionBadgeCodeFor(variant);
                var color = ArenaHud.MissionBadgeColorFor(variant);

                Assert.AreEqual(3, code.Length, $"{variant} needs a compact TV-readable badge code.");
                Assert.Greater(color.a, 0.9f, $"{variant} badge should be opaque enough for couch distance.");
                Assert.IsTrue(color.r > 0.3f || color.g > 0.3f || color.b > 0.3f, $"{variant} badge color should not be near black.");
            }
        }
    }
}
