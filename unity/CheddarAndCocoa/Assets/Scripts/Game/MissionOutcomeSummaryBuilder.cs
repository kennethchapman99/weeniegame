namespace CheddarAndCocoa.Game
{
    public static class MissionOutcomeSummaryBuilder
    {
        public static string BuildSquirrelSummary(HerdingMissionState state)
        {
            if (state.StashFound)
                return "Conspiracy Cracked";

            if (state.Taunts > 0)
                return "Outplayed By A Rodent";

            return "Still Investigating";
        }

        public static string BuildThreatSweepSummary(ThreatSweepMissionState state)
        {
            if (state.UnitedFrontComplete)
                return "Backyard Defenders";

            if (state.Exposures > 0)
                return "Shadow Trouble";

            return "Keeping Watch";
        }

        public static string BuildCarrySummary(CarryRoundupMissionState state, int required)
        {
            if (state.ReadyToClear(required))
                return "Weenie Wranglers";

            if (state.Drops > 0)
                return "Butterpaws";

            return "Still Rounding Up";
        }

        public static string BuildPatrolSummary(PatrolDefenseMissionState state)
        {
            if (state.FinalPressureComplete)
                return "Fence Guardians";

            if (state.Breaches > 0)
                return "Needs More Patrols";

            return "Holding The Line";
        }
    }
}
