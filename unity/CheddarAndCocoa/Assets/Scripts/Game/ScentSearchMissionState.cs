namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Deterministic state for the Scent Search mission: the dogs sniff for hot/cold cues to locate
    /// buried bones and dig them up. Tracks how many bones have been found, how many sniffs were
    /// taken (co-op info-gathering), and how many wasted digs landed on a cold spot.
    /// </summary>
    public sealed class ScentSearchMissionState
    {
        public int Found { get; private set; }
        public int Sniffs { get; private set; }
        public int WastedDigs { get; private set; }

        public bool ReadyToClear(int required) => Found >= required;
        public bool TooManyWastedDigs(int max) => WastedDigs >= max;

        public void AddSniff() => Sniffs++;
        public void AddFind() => Found++;
        public void AddWastedDig() => WastedDigs++;

        public void Reset()
        {
            Found = 0;
            Sniffs = 0;
            WastedDigs = 0;
        }
    }
}
