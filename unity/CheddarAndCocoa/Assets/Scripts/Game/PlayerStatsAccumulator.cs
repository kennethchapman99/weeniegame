namespace CheddarAndCocoa.Game
{
    public sealed class PlayerStatsAccumulator
    {
        public LifetimeDogStats Stats { get; private set; }

        public void RecordBark(bool united = false)
        {
            var stats = Stats;
            stats.Barks++;
            if (united) stats.UnitedBarks++;
            Stats = stats;
        }

        public void RecordRescue()
        {
            var stats = Stats;
            stats.Rescues++;
            Stats = stats;
        }

        public void RecordSquirrelChased()
        {
            var stats = Stats;
            stats.SquirrelsChased++;
            Stats = stats;
        }

        public void RecordToyRecovered()
        {
            var stats = Stats;
            stats.ToysRecovered++;
            Stats = stats;
        }

        public void RecordPoolFall()
        {
            var stats = Stats;
            stats.PoolFalls++;
            Stats = stats;
        }

        public void RecordFakeSnackEaten()
        {
            var stats = Stats;
            stats.FakeSnacksEaten++;
            Stats = stats;
        }

        public void Reset()
        {
            Stats = default;
        }
    }
}
