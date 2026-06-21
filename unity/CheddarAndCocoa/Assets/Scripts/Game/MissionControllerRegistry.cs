using System;
using System.Collections.Generic;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Atomic registrations for controller-owned missions. A controller factory and its shared
    /// presentation definition live in the same entry so selection cannot expose only half of a
    /// migrated mission. Unmigrated missions intentionally have no entry yet.
    /// </summary>
    public static class MissionControllerRegistry
    {
        private sealed class Registration
        {
            public Func<IMissionController> ControllerFactory { get; }
            public Func<ArenaMissionTuning, GameManager.MissionDefinition> DefinitionFactory { get; }

            public Registration(
                Func<IMissionController> controllerFactory,
                Func<ArenaMissionTuning, GameManager.MissionDefinition> definitionFactory)
            {
                ControllerFactory = controllerFactory;
                DefinitionFactory = definitionFactory;
            }
        }

        private static readonly IReadOnlyDictionary<GameManager.MissionVariant, Registration> Registrations =
            new Dictionary<GameManager.MissionVariant, Registration>
            {
                [GameManager.MissionVariant.KitchenFoodFrenzy] = new Registration(
                    () => new KitchenFoodFrenzyMissionController(),
                    MissionCatalog.BuildKitchenDefinition),
                [GameManager.MissionVariant.OperationPeeBreak] = new Registration(
                    () => new PeeBreakMissionController(),
                    MissionCatalog.BuildPeeBreakDefinition),
                [GameManager.MissionVariant.MarkTheYard] = new Registration(
                    () => new MarkTheYardMissionController(),
                    MissionCatalog.BuildMarkTheYardDefinition),
                [GameManager.MissionVariant.GateCrash] = new Registration(
                    () => new GateCrashMissionController(),
                    MissionCatalog.BuildGateCrashDefinition),
                [GameManager.MissionVariant.TableStealth] = new Registration(
                    () => new TableStealthMissionController(),
                    MissionCatalog.BuildTableStealthDefinition),
                [GameManager.MissionVariant.SquirrelSwitcheroo] = new Registration(
                    () => new SquirrelSwitcherooMissionController(),
                    MissionCatalog.BuildSquirrelSwitcherooDefinition),
                [GameManager.MissionVariant.WalkCampaign] = new Registration(
                    () => new WalkCampaignMissionController(),
                    MissionCatalog.BuildWalkCampaignDefinition),
                [GameManager.MissionVariant.GreatEscape] = new Registration(
                    () => new GreatEscapeMissionController(),
                    MissionCatalog.BuildGreatEscapeDefinition),
                [GameManager.MissionVariant.ChaosMachine] = new Registration(
                    () => new ChaosMachineMissionController(),
                    MissionCatalog.BuildChaosMachineDefinition),
                [GameManager.MissionVariant.BlanketCatch] = new Registration(
                    () => new BlanketCatchMissionController(),
                    MissionCatalog.BuildBlanketCatchDefinition),
                [GameManager.MissionVariant.BoneRelay] = new Registration(
                    () => new BoneRelayMissionController(),
                    MissionCatalog.BuildBoneRelayDefinition)
            };

        public static IEnumerable<GameManager.MissionVariant> RegisteredVariants => Registrations.Keys;

        public static bool TryCreate(GameManager.MissionVariant variant, out IMissionController controller)
        {
            if (Registrations.TryGetValue(variant, out var registration))
            {
                controller = registration.ControllerFactory();
                if (controller == null || controller.Variant != variant)
                    throw new InvalidOperationException($"Controller registration for {variant} returned an invalid controller.");
                return true;
            }

            controller = null;
            return false;
        }

        public static bool TryBuildDefinition(
            GameManager.MissionVariant variant,
            ArenaMissionTuning tuning,
            out GameManager.MissionDefinition definition)
        {
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (Registrations.TryGetValue(variant, out var registration))
            {
                definition = registration.DefinitionFactory(tuning);
                if (definition == null || definition.Variant != variant)
                    throw new InvalidOperationException($"Definition registration for {variant} returned an invalid definition.");
                return true;
            }

            definition = null;
            return false;
        }
    }
}
