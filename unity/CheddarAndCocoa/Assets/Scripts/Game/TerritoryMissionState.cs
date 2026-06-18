namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Deterministic state for the Mark the Yard mission: the dogs claim territory zones by standing
    /// in them while a squirrel re-marks claimed zones, forcing the pair to split up and cover ground.
    /// Tracks how many zones exist, how many are currently claimed, and how many times the squirrel
    /// has stolen a zone back. The per-zone claimed flags live on the GameManager; this keeps the
    /// aggregate counts so PlayMode tests can assert progress without spatial timing.
    /// </summary>
    public sealed class TerritoryMissionState
    {
        public int ZoneCount { get; private set; }
        public int Claimed { get; private set; }
        public int Reclaims { get; private set; }

        public bool AllClaimed => ZoneCount > 0 && Claimed >= ZoneCount;

        public void Configure(int zones)
        {
            ZoneCount = zones < 1 ? 1 : zones;
            Claimed = 0;
            Reclaims = 0;
        }

        public void Claim()
        {
            if (Claimed < ZoneCount) Claimed++;
        }

        public void Unclaim()
        {
            if (Claimed > 0)
            {
                Claimed--;
                Reclaims++;
            }
        }

        public void Reset()
        {
            Claimed = 0;
            Reclaims = 0;
        }
    }
}
