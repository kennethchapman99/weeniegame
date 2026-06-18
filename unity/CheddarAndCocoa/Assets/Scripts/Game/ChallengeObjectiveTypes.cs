using System;

namespace CheddarAndCocoa.Game
{
    public enum ChallengeObjectiveKind
    {
        ScoreAtLeast,
        NoFakeOuts,
        NoDogGrabbed,
        NoPoolFalls,
        UnitedBarksAtLeast,
        ClearUnderSeconds,
        PerfectCutoffs,
        CollectAllHidden
    }

    [Serializable]
    public readonly struct ChallengeObjectiveSpec
    {
        public readonly string Id;
        public readonly string Label;
        public readonly ChallengeObjectiveKind Kind;
        public readonly int TargetValue;

        public ChallengeObjectiveSpec(string id, string label, ChallengeObjectiveKind kind, int targetValue)
        {
            Id = id;
            Label = label;
            Kind = kind;
            TargetValue = targetValue;
        }
    }

    public static class ChallengeObjectiveCatalog
    {
        public static readonly ChallengeObjectiveSpec SquirrelNoFakeOuts = new(
            "squirrel_no_fakeouts",
            "No squirrel fake-outs",
            ChallengeObjectiveKind.NoFakeOuts,
            0);

        public static readonly ChallengeObjectiveSpec SquirrelScore1500 = new(
            "squirrel_score_1500",
            "Score 1500+",
            ChallengeObjectiveKind.ScoreAtLeast,
            1500);

        public static readonly ChallengeObjectiveSpec EagleNoGrab = new(
            "eagle_no_grab",
            "Nobody gets grabbed",
            ChallengeObjectiveKind.NoDogGrabbed,
            0);

        public static readonly ChallengeObjectiveSpec CoyotePerfectFence = new(
            "coyote_perfect_fence",
            "No fence breaches",
            ChallengeObjectiveKind.PerfectCutoffs,
            0);
    }
}
