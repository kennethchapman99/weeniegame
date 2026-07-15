namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Deterministic progress state for the Car Ride mission: the dogs ride the back seat home
    /// while the driver takes turns (the cabin tilts and everything slides) and hits the brakes
    /// (unbraced dogs get flung forward). Tracks how many road events the ride has resolved and
    /// how many times a dog tumbled (bonked by sliding seat junk, squished against a door, or
    /// flung by an unbraced stop). Live slide/tilt values stay on the controller.
    /// </summary>
    public sealed class CarRideMissionState
    {
        public int RequiredEvents { get; private set; }
        public int EventsResolved { get; private set; }
        public int Tumbles { get; private set; }

        public bool ReadyToClear() => RequiredEvents > 0 && EventsResolved >= RequiredEvents;
        public bool TooManyTumbles(int max) => Tumbles >= max;

        public void Configure(int requiredEvents)
        {
            RequiredEvents = requiredEvents < 1 ? 1 : requiredEvents;
            EventsResolved = 0;
            Tumbles = 0;
        }

        public void ResolveEvent() => EventsResolved++;
        public void Tumble() => Tumbles++;

        public void Reset()
        {
            EventsResolved = 0;
            Tumbles = 0;
        }
    }
}
