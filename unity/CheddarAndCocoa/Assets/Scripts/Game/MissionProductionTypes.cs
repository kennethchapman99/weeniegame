using System;

namespace CheddarAndCocoa.Game
{
    public enum ProductionMechanicModule
    {
        Herding,
        ThreatSweep,
        PatrolDefense,
        SharedObject,
        TerritoryControl,
        ScentSearch,
        RhythmPanic,
        VehicleBalance,
        LeashPhysics
    }

    public enum ProductionDifficultyAxis
    {
        TimePressure,
        DecisionPressure,
        CoordinationPressure,
        ExecutionPressure
    }

    public enum ProductionMissionPack
    {
        Backyard,
        Pool,
        HouseChaos,
        RoadTrip,
        Minigame
    }

    [Serializable]
    public readonly struct ProductionScoreEvent
    {
        public readonly string Label;
        public readonly int Points;
        public readonly bool IsTeamwork;

        public ProductionScoreEvent(string label, int points, bool isTeamwork = false)
        {
            Label = label;
            Points = points;
            IsTeamwork = isTeamwork;
        }
    }

    [Serializable]
    public readonly struct ProductionMissionSpec
    {
        public readonly string Id;
        public readonly string Title;
        public readonly ProductionMissionPack Pack;
        public readonly ProductionMechanicModule PrimaryModule;
        public readonly string Objective;
        public readonly string ClearCondition;
        public readonly string FailCondition;

        public ProductionMissionSpec(
            string id,
            string title,
            ProductionMissionPack pack,
            ProductionMechanicModule primaryModule,
            string objective,
            string clearCondition,
            string failCondition)
        {
            Id = id;
            Title = title;
            Pack = pack;
            PrimaryModule = primaryModule;
            Objective = objective;
            ClearCondition = clearCondition;
            FailCondition = failCondition;
        }
    }

    public static class ProductionMissionCatalog
    {
        public static readonly ProductionMissionSpec SquirrelConspiracy = new(
            "squirrel_conspiracy",
            "The Great Backyard Squirrel Conspiracy",
            ProductionMissionPack.Backyard,
            ProductionMechanicModule.Herding,
            "Herd the squirrel, find the stash, stop the taunt branch.",
            "Find the stash after enough successful herds and cutoffs.",
            "The squirrel reaches the taunt branch too many times or the timer expires.");

        public static readonly ProductionMissionSpec EagleShadowPanic = new(
            "eagle_shadow_panic",
            "Eagle Shadow Panic",
            ProductionMissionPack.Backyard,
            ProductionMechanicModule.ThreatSweep,
            "Hide from the sweeping shadow, rescue the toy, and form a united-front bark circle.",
            "Complete the final united-front bark after the rescue objective.",
            "Too many shadow exposures or the timer expires.");

        public static readonly ProductionMissionSpec CoyotesFence = new(
            "coyotes_fence",
            "Coyotes at the Fence",
            ProductionMissionPack.Backyard,
            ProductionMechanicModule.PatrolDefense,
            "Patrol fence gaps, bark pressure, and fill weak spots together.",
            "Repair enough weak spots and block the final coyote pressure.",
            "The coyote breaches the fence or isolates a dog too many times.");
    }
}
