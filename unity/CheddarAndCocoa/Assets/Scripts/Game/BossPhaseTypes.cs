using System;

namespace CheddarAndCocoa.Game
{
    public enum BossPhaseKind
    {
        Teach,
        Pressure,
        Teamwork,
        FinalChaos,
        Complete
    }

    [Serializable]
    public readonly struct BossPhaseSpec
    {
        public readonly string Id;
        public readonly BossPhaseKind Kind;
        public readonly string ObjectiveLabel;
        public readonly int RequiredProgress;

        public BossPhaseSpec(string id, BossPhaseKind kind, string objectiveLabel, int requiredProgress)
        {
            Id = id;
            Kind = kind;
            ObjectiveLabel = objectiveLabel;
            RequiredProgress = requiredProgress;
        }
    }

    public static class BossPhaseCatalog
    {
        public static readonly BossPhaseSpec SquirrelTeach = new(
            "squirrel_teach",
            BossPhaseKind.Teach,
            "Learn the squirrel route and bark at the right time.",
            1);

        public static readonly BossPhaseSpec SquirrelTeamwork = new(
            "squirrel_teamwork",
            BossPhaseKind.Teamwork,
            "One dog pressures while the other cuts off escape.",
            3);

        public static readonly BossPhaseSpec NailGrinderFinal = new(
            "nail_grinder_final",
            BossPhaseKind.FinalChaos,
            "Survive just one more nail.",
            4);
    }
}
