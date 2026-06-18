using System;

namespace CheddarAndCocoa.Game
{
    [Flags]
    public enum DemoReadinessRequirement
    {
        None = 0,
        MissionSelectable = 1,
        ClearPathTested = 2,
        FailPathTested = 4,
        ReplayTested = 8,
        ControllerPathTested = 16,
        ObjectiveReadable = 32,
        ScoreReadable = 64,
        SummaryReadable = 128
    }

    public readonly struct DemoReadinessResult
    {
        public readonly DemoReadinessRequirement Missing;
        public bool Ready => Missing == DemoReadinessRequirement.None;

        public DemoReadinessResult(DemoReadinessRequirement missing)
        {
            Missing = missing;
        }
    }

    public static class DemoReadinessGate
    {
        public static readonly DemoReadinessRequirement RequiredForBackyardDemo =
            DemoReadinessRequirement.MissionSelectable |
            DemoReadinessRequirement.ClearPathTested |
            DemoReadinessRequirement.FailPathTested |
            DemoReadinessRequirement.ReplayTested |
            DemoReadinessRequirement.ControllerPathTested |
            DemoReadinessRequirement.ObjectiveReadable |
            DemoReadinessRequirement.ScoreReadable |
            DemoReadinessRequirement.SummaryReadable;

        public static DemoReadinessResult Evaluate(DemoReadinessRequirement present)
        {
            return new DemoReadinessResult(RequiredForBackyardDemo & ~present);
        }
    }
}
