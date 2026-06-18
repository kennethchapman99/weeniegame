namespace CheddarAndCocoa.Game
{
    public sealed class ThreatSweepMissionState
    {
        public int SweepIndex { get; private set; }
        public int SafeHides { get; private set; }
        public int Exposures { get; private set; }
        public bool RescueObjectiveActive { get; private set; }
        public bool RescueComplete { get; private set; }
        public bool UnitedFrontComplete { get; private set; }

        public bool ReadyForRescue(int requiredHides) => !RescueObjectiveActive && SafeHides >= requiredHides;
        public bool ReadyForUnitedFront => RescueComplete && !UnitedFrontComplete;
        public bool TooManyExposures(int maxExposures) => Exposures >= maxExposures;

        public void AdvanceSweep(int sweepCount)
        {
            SweepIndex = sweepCount <= 0 ? 0 : (SweepIndex + 1) % sweepCount;
        }

        public void AddSafeHide() => SafeHides++;
        public void AddExposure() => Exposures++;
        public void StartRescue() => RescueObjectiveActive = true;
        public void CompleteRescue()
        {
            RescueObjectiveActive = true;
            RescueComplete = true;
        }
        public void CompleteUnitedFront() => UnitedFrontComplete = true;

        public void Reset()
        {
            SweepIndex = 0;
            SafeHides = 0;
            Exposures = 0;
            RescueObjectiveActive = false;
            RescueComplete = false;
            UnitedFrontComplete = false;
        }
    }
}
