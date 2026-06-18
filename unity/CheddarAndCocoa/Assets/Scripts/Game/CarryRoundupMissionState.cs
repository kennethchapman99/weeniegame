namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Deterministic state for the Weenie Roundup mission: scattered weenies must be picked up and
    /// carried back to the home bowl. Tracks how many are still loose in the yard, how many have
    /// been delivered, and how many have been dropped (knocked loose again). Pure logic so PlayMode
    /// tests can drive it without real-time proximity timing.
    /// </summary>
    public sealed class CarryRoundupMissionState
    {
        public int Loose { get; private set; }
        public int Delivered { get; private set; }
        public int Drops { get; private set; }

        public bool ReadyToClear(int required) => Delivered >= required;

        public void Configure(int loose)
        {
            Loose = loose < 0 ? 0 : loose;
            Delivered = 0;
            Drops = 0;
        }

        /// <summary>A free dog grabs a loose weenie. Returns false if none are loose.</summary>
        public bool TryPickup()
        {
            if (Loose <= 0) return false;
            Loose--;
            return true;
        }

        public void Deliver() => Delivered++;

        /// <summary>A carried weenie is knocked loose again and returns to the yard.</summary>
        public void Drop()
        {
            Loose++;
            Drops++;
        }

        public void Reset()
        {
            Loose = 0;
            Delivered = 0;
            Drops = 0;
        }
    }
}
