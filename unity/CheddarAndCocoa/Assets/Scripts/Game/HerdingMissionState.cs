namespace CheddarAndCocoa.Game
{
    public sealed class HerdingMissionState
    {
        public int RouteIndex { get; private set; }
        public int Herds { get; private set; }
        public int Cutoffs { get; private set; }
        public int FakeOuts { get; private set; }
        public int Taunts { get; private set; }
        public bool StashRevealed { get; private set; }
        public bool StashFound { get; private set; }

        public int ControlCount => Herds + Cutoffs;
        public bool ReadyForStash(int requiredControl) => !StashRevealed && ControlCount >= requiredControl;
        public bool TooManyTaunts(int maxTaunts) => Taunts >= maxTaunts;

        public void AdvanceRoute(int routeCount)
        {
            RouteIndex = routeCount <= 0 ? 0 : (RouteIndex + 1) % routeCount;
        }

        public void AddHerd() => Herds++;
        public void AddCutoff() => Cutoffs++;
        public void AddFakeOut() => FakeOuts++;
        public void AddTaunt() => Taunts++;
        public void RevealStash() => StashRevealed = true;
        public void FindStash()
        {
            StashRevealed = true;
            StashFound = true;
        }

        public void Reset()
        {
            RouteIndex = 0;
            Herds = 0;
            Cutoffs = 0;
            FakeOuts = 0;
            Taunts = 0;
            StashRevealed = false;
            StashFound = false;
        }
    }
}
