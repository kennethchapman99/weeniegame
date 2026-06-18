using System;

namespace CheddarAndCocoa.Game
{
    public enum MinigamePhase
    {
        Intro,
        Countdown,
        Playing,
        SuddenDeath,
        Results,
        Replay
    }

    public enum MinigameKind
    {
        TugOfWarSupreme,
        SunbeamKingQueen,
        CouchClaim,
        TreatDropDuel,
        ZoomiesTag,
        ToyHoarder,
        BestBarkBattle,
        PatheticBegging,
        BlanketBurrowRace,
        SquirrelDash
    }

    [Serializable]
    public readonly struct MinigameResult
    {
        public readonly MinigameKind Kind;
        public readonly string WinnerLabel;
        public readonly int CheddarScore;
        public readonly int CocoaScore;
        public readonly float DurationSeconds;
        public readonly string SummaryLabel;

        public MinigameResult(
            MinigameKind kind,
            string winnerLabel,
            int cheddarScore,
            int cocoaScore,
            float durationSeconds,
            string summaryLabel)
        {
            Kind = kind;
            WinnerLabel = winnerLabel;
            CheddarScore = cheddarScore;
            CocoaScore = cocoaScore;
            DurationSeconds = durationSeconds;
            SummaryLabel = summaryLabel;
        }
    }

    [Serializable]
    public readonly struct MinigameSpec
    {
        public readonly MinigameKind Kind;
        public readonly string Title;
        public readonly ProductionMechanicModule PrimaryModule;
        public readonly string Objective;
        public readonly float TargetDurationSeconds;

        public MinigameSpec(
            MinigameKind kind,
            string title,
            ProductionMechanicModule primaryModule,
            string objective,
            float targetDurationSeconds)
        {
            Kind = kind;
            Title = title;
            PrimaryModule = primaryModule;
            Objective = objective;
            TargetDurationSeconds = targetDurationSeconds;
        }
    }

    public static class MinigameProductionCatalog
    {
        public static readonly MinigameSpec TugOfWarSupreme = new(
            MinigameKind.TugOfWarSupreme,
            "Tug-of-War Supreme",
            ProductionMechanicModule.SharedObject,
            "Win the rope through rhythm, stamina, and fake-outs.",
            90f);

        public static readonly MinigameSpec SunbeamKingQueen = new(
            MinigameKind.SunbeamKingQueen,
            "Sunbeam King / Queen",
            ProductionMechanicModule.TerritoryControl,
            "Hold the moving sunbeam longer than the other dog.",
            75f);

        public static readonly MinigameSpec BestBarkBattle = new(
            MinigameKind.BestBarkBattle,
            "Best Bark Battle",
            ProductionMechanicModule.RhythmPanic,
            "Match bark patterns to defeat the threat with style.",
            90f);
    }
}
