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

        public static readonly ProductionMissionSpec WeenieRoundup = new(
            "weenie_roundup",
            "Weenie Roundup",
            ProductionMissionPack.Backyard,
            ProductionMechanicModule.SharedObject,
            "Carry every scattered weenie back to the home bowl together.",
            "Deliver enough weenies to the bowl before the timer expires.",
            "The timer expires before enough weenies are delivered.");

        public static readonly ProductionMissionSpec ScentSearch = new(
            "scent_search",
            "Scent Search",
            ProductionMissionPack.Backyard,
            ProductionMechanicModule.ScentSearch,
            "Sniff out the buried bones and dig them up before time runs out.",
            "Dig up enough buried bones before the timer expires.",
            "Too many cold digs, or the timer expires.");

        public static readonly ProductionMissionSpec ThunderstormComfort = new(
            "thunderstorm_comfort",
            "Thunderstorm Comfort",
            ProductionMissionPack.HouseChaos,
            ProductionMechanicModule.RhythmPanic,
            "Huddle close to keep each other calm and ride out every thunderclap.",
            "Weather the required thunderclaps without either dog maxing out panic.",
            "Either dog's panic hits the max and they bolt.");

        public static readonly ProductionMissionSpec MarkTheYard = new(
            "mark_the_yard",
            "Mark the Yard",
            ProductionMissionPack.Backyard,
            ProductionMechanicModule.TerritoryControl,
            "Claim every territory zone and hold them all at once before the squirrel re-marks them.",
            "Have every zone claimed simultaneously.",
            "The timer expires before every zone is held at once.");

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
