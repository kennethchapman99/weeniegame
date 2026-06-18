namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Deterministic progress state for the Thunderstorm Comfort mission: the dogs must ride out a
    /// set number of thunderclaps while keeping each other calm (panic comes down by huddling, via
    /// <see cref="PanicMeter"/>). This struct only tracks how many claps the storm has thrown and how
    /// many were weathered without bolting; the panic levels themselves live on the PanicMeter.
    /// </summary>
    public sealed class ThunderstormMissionState
    {
        public int RequiredClaps { get; private set; }
        public int ClapsSurvived { get; private set; }

        public bool ReadyToClear() => RequiredClaps > 0 && ClapsSurvived >= RequiredClaps;

        public void Configure(int requiredClaps)
        {
            RequiredClaps = requiredClaps < 1 ? 1 : requiredClaps;
            ClapsSurvived = 0;
        }

        public void SurviveClap() => ClapsSurvived++;

        public void Reset()
        {
            ClapsSurvived = 0;
        }
    }
}
