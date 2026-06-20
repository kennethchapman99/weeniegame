namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for the Long-dog geometry / Physical bridge beat (doctrine #5):
    /// the two dogs stretch a span between them — a blanket to catch falling food, a long-dog bridge
    /// over a gap — defined by BOTH their positions. The co-op lock is a two-body spacing band that
    /// neither dog controls alone:
    ///
    ///   - too close together and the span goes <see cref="Slack"/> (sags, useless);
    ///   - too far apart and it <see cref="Overstretched"/> and rips (<see cref="Rips"/>);
    ///   - only in the <see cref="Taut"/> band is it usable, and its <see cref="MidpointX"/> (the
    ///     average of the two dogs) must be under the target to catch/bridge it.
    ///
    /// So both dogs must coordinate spacing AND position together. Pure logic: a mission feeds the live
    /// separation + midpoint via <see cref="UpdateSpan"/> from the two dog transforms and calls
    /// <see cref="TryCatch"/> on falling items / at a gap; tests drive both deterministically.
    /// </summary>
    public sealed class CoopStretchSpanPuzzle
    {
        private float _minSeparation = 1.5f;
        private float _maxSeparation = 5f;
        private float _catchTolerance = 1.5f;
        private int _catchesNeeded = 4;
        private int _maxRips = 3;

        public float Separation { get; private set; }
        public float MidpointX { get; private set; }

        public int Caught { get; private set; }
        public int Missed { get; private set; }
        public int Rips { get; private set; }

        public bool Taut => Separation >= _minSeparation && Separation <= _maxSeparation;
        public bool Slack => Separation < _minSeparation;
        public bool Overstretched => Separation > _maxSeparation;

        public bool Solved => Caught >= _catchesNeeded;
        public bool TooManyRips => Rips >= _maxRips;
        public int CatchesNeeded => _catchesNeeded;

        private bool _wasOverstretched;

        public void Configure(float minSeparation, float maxSeparation, float catchTolerance,
            int catchesNeeded, int maxRips)
        {
            _minSeparation = minSeparation < 0f ? 0f : minSeparation;
            _maxSeparation = maxSeparation <= _minSeparation ? _minSeparation + 1f : maxSeparation;
            _catchTolerance = catchTolerance <= 0f ? 1f : catchTolerance;
            _catchesNeeded = catchesNeeded < 1 ? 1 : catchesNeeded;
            _maxRips = maxRips < 1 ? 1 : maxRips;
            Reset();
        }

        /// <summary>Update the live span from the two dogs' separation and midpoint. Over-stretching rips once.</summary>
        public void UpdateSpan(float separation, float midpointX)
        {
            Separation = separation < 0f ? 0f : separation;
            MidpointX = midpointX;

            bool over = Overstretched;
            if (over && !_wasOverstretched) Rips++; // edge: rips once per over-stretch event
            _wasOverstretched = over;
        }

        /// <summary>Attempt to catch/bridge an item at <paramref name="itemX"/> with the current span.</summary>
        public void TryCatch(float itemX)
        {
            if (Solved) return;

            if (Taut && System.Math.Abs(MidpointX - itemX) <= _catchTolerance)
                Caught++;
            else
                Missed++; // slack, ripped, or not centered under the item
        }

        public void Reset()
        {
            Separation = 0f;
            MidpointX = 0f;
            Caught = 0;
            Missed = 0;
            Rips = 0;
            _wasOverstretched = false;
        }
    }
}
