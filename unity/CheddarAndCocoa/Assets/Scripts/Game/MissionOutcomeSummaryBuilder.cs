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

        public static string BuildScentSummary(ScentSearchMissionState state, int required)
        {
            if (state.ReadyToClear(required))
                return "Master Sniffers";

            if (state.WastedDigs > 0)
                return "Dug Up The Whole Yard";

            return "Still Sniffing";
        }

        public static string BuildThunderstormSummary(ThunderstormMissionState state)
        {
            if (state.ReadyToClear())
                return "Weathered The Storm";

            return "Spooked By Thunder";
        }

        public static string BuildTerritorySummary(TerritoryMissionState state)
        {
            if (state.AllClaimed)
                return "Yard Is Ours";

            if (state.Reclaims > 0)
                return "Squirrel Keeps Stealing It";

            return "Still Marking";
        }

        public static string BuildLeashSummary(LeashWalkMissionState state)
        {
            if (state.ReadyToClear())
                return "Best Walk Ever";

            if (state.Snaps > 0)
                return "Tangled Leash";

            return "Still Walking";
        }

        public static string BuildCarBalanceSummary(CarBalanceMissionState state)
        {
            if (state.ReadyToClear())
                return "Smooth Riders";

            if (state.Spills > 0)
                return "Car Sick";

            return "Still Riding";
        }

        public static string BuildGateCrashSummary(CoopHoldReleasePuzzle state)
        {
            if (state.Solved)
                return "Squeezed Through";

            if (state.Snaps > 0)
                return "Gate Trouble";

            return "At The Gate";
        }

        public static string BuildTableStealthSummary(CoopHumanDistractionPuzzle state)
        {
            if (state.Solved)
                return "Steak Sneaked";

            if (state.Exposures > 0)
                return "Caught At The Table";

            return "Under The Table";
        }

        public static string BuildSwitcherooSummary(CoopBaitSwitchPuzzle state)
        {
            if (state.Solved)
                return "Switcheroo Pulled";

            if (state.Backfires > 0)
                return "Squirrel Wised Up";

            return "Working The Bait";
        }

        public static string BuildWalkCampaignSummary(CoopSocialManipulationPuzzle state)
        {
            if (state.Solved)
                return "Walkies Secured";

            if (state.Misreads > 0)
                return "Mixed Signals";

            return "Working The Human";
        }

        public static string BuildBoneRelaySummary(CoopScentRelayPuzzle state)
        {
            if (state.Solved)
                return "Bones Recovered";

            if (state.BlindActs + state.WrongDigs > 0)
                return "Dug Up The Yard";

            return "On The Scent";
        }

        public static string BuildGreatEscapeSummary(CoopSequenceChainPuzzle state)
        {
            if (state.Solved)
                return "Jailbreak!";

            if (state.Fumbles + state.Settles > 0)
                return "Botched Contraption";

            return "Rigging It Up";
        }

        public static string BuildChaosMachineSummary(CoopChaosMachinePuzzle state)
        {
            if (state.Solved)
                return "Cascade Complete";

            if (state.Stalls > 0)
                return "Misfired";

            return "Setting It Up";
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
