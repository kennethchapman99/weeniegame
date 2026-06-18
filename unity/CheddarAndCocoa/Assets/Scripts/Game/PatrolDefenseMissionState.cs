namespace CheddarAndCocoa.Game
{
    public sealed class PatrolDefenseMissionState
    {
        public int ActiveGapIndex { get; private set; }
        public int GapsRepaired { get; private set; }
        public int Breaches { get; private set; }
        public int BarkPressures { get; private set; }
        public bool FakeSnackActive { get; private set; }
        public bool FinalPressureComplete { get; private set; }

        public bool ReadyForFinalPressure(int requiredRepairs) => GapsRepaired >= requiredRepairs && !FinalPressureComplete;
        public bool TooManyBreaches(int maxBreaches) => Breaches >= maxBreaches;

        public void SelectGap(int gapIndex)
        {
            ActiveGapIndex = gapIndex < 0 ? 0 : gapIndex;
        }

        public void AddRepair() => GapsRepaired++;
        public void AddBreach() => Breaches++;
        public void AddBarkPressure() => BarkPressures++;
        public void StartFakeSnack() => FakeSnackActive = true;
        public void ResolveFakeSnack() => FakeSnackActive = false;
        public void CompleteFinalPressure() => FinalPressureComplete = true;

        public void Reset()
        {
            ActiveGapIndex = 0;
            GapsRepaired = 0;
            Breaches = 0;
            BarkPressures = 0;
            FakeSnackActive = false;
            FinalPressureComplete = false;
        }
    }
}
