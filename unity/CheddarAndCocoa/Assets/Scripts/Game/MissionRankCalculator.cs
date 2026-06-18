namespace CheddarAndCocoa.Game
{
    public readonly struct MissionRankResult
    {
        public readonly string Rank;
        public readonly int Stars;

        public MissionRankResult(string rank, int stars)
        {
            Rank = rank;
            Stars = stars;
        }
    }

    public static class MissionRankCalculator
    {
        public const string PawfectRank = "Pawfect Yard";
        public const string HeroRank = "Backyard Heroes";
        public const string SurvivorRank = "Snack Survivors";
        public const string LowRank = "Needs More Bark";

        public static MissionRankResult Calculate(int score, bool clear, int pawfectScore, int heroScore, int survivorScore)
        {
            if (clear && score >= pawfectScore) return new MissionRankResult(PawfectRank, 3);
            if (clear && score >= heroScore) return new MissionRankResult(HeroRank, 2);
            if (score >= survivorScore) return new MissionRankResult(SurvivorRank, clear ? 1 : 0);
            return new MissionRankResult(LowRank, clear ? 1 : 0);
        }
    }
}
