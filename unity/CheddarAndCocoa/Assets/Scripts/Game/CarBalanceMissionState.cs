namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Deterministic progress state for the Car Ride mission (VehicleBalance module): the dogs ride
    /// in the back of a car that lurches side to side, and must lean to opposite sides to keep it
    /// level. Tracks how many lurches have been ridden out and how many times the car tipped over
    /// (a spill). The live balance value lives on the GameManager.
    /// </summary>
    public sealed class CarBalanceMissionState
    {
        public int RequiredLurches { get; private set; }
        public int LurchesSurvived { get; private set; }
        public int Spills { get; private set; }

        public bool ReadyToClear() => RequiredLurches > 0 && LurchesSurvived >= RequiredLurches;
        public bool TooManySpills(int max) => Spills >= max;

        public void Configure(int requiredLurches)
        {
            RequiredLurches = requiredLurches < 1 ? 1 : requiredLurches;
            LurchesSurvived = 0;
            Spills = 0;
        }

        public void SurviveLurch() => LurchesSurvived++;
        public void Spill() => Spills++;

        public void Reset()
        {
            LurchesSurvived = 0;
            Spills = 0;
        }
    }
}
