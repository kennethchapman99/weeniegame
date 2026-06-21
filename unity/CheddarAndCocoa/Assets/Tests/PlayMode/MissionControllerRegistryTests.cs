using System.Collections.Generic;
using NUnit.Framework;

namespace CheddarAndCocoa.Game.Tests
{
    public sealed class MissionControllerRegistryTests
    {
        [Test]
        public void RegisteredMissions_HaveMatchingControllerAndDefinition()
        {
            var seen = new HashSet<GameManager.MissionVariant>();
            var tuning = ArenaMissionTuning.CreateDefault();

            foreach (var variant in MissionControllerRegistry.RegisteredVariants)
            {
                Assert.IsTrue(seen.Add(variant), $"Duplicate controller registration for {variant}.");
                Assert.IsTrue(MissionControllerRegistry.TryCreate(variant, out var controller));
                Assert.AreEqual(variant, controller.Variant);
                Assert.IsTrue(MissionControllerRegistry.TryBuildDefinition(variant, tuning, out var definition));
                Assert.AreEqual(variant, definition.Variant);
                Assert.IsNotEmpty(definition.Name);
                Assert.IsNotEmpty(definition.IntroPrompt);
            }

            CollectionAssert.AreEquivalent(
                new[]
                {
                    GameManager.MissionVariant.KitchenFoodFrenzy,
                    GameManager.MissionVariant.OperationPeeBreak,
                    GameManager.MissionVariant.MarkTheYard,
                    GameManager.MissionVariant.GateCrash,
                    GameManager.MissionVariant.TableStealth,
                    GameManager.MissionVariant.SquirrelSwitcheroo,
                    GameManager.MissionVariant.WalkCampaign,
                    GameManager.MissionVariant.GreatEscape,
                    GameManager.MissionVariant.ChaosMachine,
                    GameManager.MissionVariant.BlanketCatch,
                    GameManager.MissionVariant.BoneRelay,
                    GameManager.MissionVariant.ThunderstormComfort,
                    GameManager.MissionVariant.LeashWalk,
                    GameManager.MissionVariant.SockPanic,
                    GameManager.MissionVariant.CarRide,
                    GameManager.MissionVariant.ScentSearch,
                    GameManager.MissionVariant.WeenieRoundup,
                    GameManager.MissionVariant.SquirrelConspiracy,
                    GameManager.MissionVariant.SnackHeist,
                    GameManager.MissionVariant.BackyardRescue
                },
                seen,
                "Only explicitly migrated missions belong in the controller registry.");
        }

        [Test]
        public void UnmigratedMission_HasNoControllerRegistration()
        {
            var tuning = ArenaMissionTuning.CreateDefault();

            Assert.IsFalse(MissionControllerRegistry.TryCreate(
                GameManager.MissionVariant.EagleShadowPanic, out var controller));
            Assert.IsNull(controller);
            Assert.IsFalse(MissionControllerRegistry.TryBuildDefinition(
                GameManager.MissionVariant.EagleShadowPanic, tuning, out var definition));
            Assert.IsNull(definition);
        }
    }
}
