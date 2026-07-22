using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for the Skunk Blast Mayhem heist beat: a skunk guards a prize
    /// and only turns away from it while a loud dog holds its attention (the lure), leaving a safe
    /// window for the quiet partner to snatch the prize (the snatch). The skunk periodically
    /// telegraphs a tail-lift before it sprays - a SHARED danger clock both dogs must read and bail
    /// from, not a per-dog stat:
    ///
    ///   - <see cref="TailUp"/> rises on its own clock (<see cref="Advance"/>) regardless of who is
    ///     luring, because the skunk's patience for anyone nearby runs out, not just the lure's;
    ///   - whichever dog(s) are still within blast range when the window expires get sprayed
    ///     (<see cref="CheddarStinky"/>/<see cref="CocoaStinky"/>) - both at once is a valid, funny
    ///     outcome, not a bug;
    ///   - the grab (<see cref="TryGrab"/>) only lands while the skunk is turned away (lure held) AND
    ///     not winding up, AND the grabber is not stinky (a stinky dog reeks and the skunk can smell
    ///     it coming, so it can no longer sneak up unnoticed).
    ///
    /// The de-skunk detour is fail-forward, not a mission failure: a sprayed dog rubs a shared
    /// laundry pile clean (<see cref="Rub"/>), consuming fresh laundry the clean partner restocks
    /// from a basket (<see cref="HaulLaundry"/>). Run the pile dry and the stinky dog still clears
    /// eventually by slowly airing out on their own (<see cref="Advance"/>'s air-dry trickle) - slow,
    /// but never a dead end.
    ///
    /// Pure logic: a mission drives lure/grab/rub/haul and time; tests drive all of it deterministically.
    /// </summary>
    public sealed class CoopSkunkHeistPuzzle
    {
        private float _tailLiftInterval = 4.5f;
        private float _telegraphSeconds = 1.3f;
        private int _rubsToClean = 3;
        private int _pileCapacity = 3;
        private int _basketCapacity = 5;
        private float _airDryRubsPerSecond = 1f / 20f;

        private float _cheddarAirDryProgress;
        private float _cocoaAirDryProgress;

        public bool TailUp { get; private set; }
        public float TimeToNextTailLift { get; private set; }
        public float TailWindowRemaining { get; private set; }
        public bool PrizeSecured { get; private set; }

        public bool CheddarStinky { get; private set; }
        public bool CocoaStinky { get; private set; }
        public int CheddarRubProgress { get; private set; }
        public int CocoaRubProgress { get; private set; }

        public int BasketSupply { get; private set; }
        public int PileFresh { get; private set; }
        public int PileFunky { get; private set; }

        /// <summary>Total times either dog has been sprayed this run - the mission's Mistakes tally.</summary>
        public int SkunkEvents { get; private set; }

        public bool AnyDogStinky => CheddarStinky || CocoaStinky;
        public bool BothDogsStinky => CheddarStinky && CocoaStinky;
        public int RubsToClean => _rubsToClean;
        public int PileCapacity => _pileCapacity;
        public int BasketCapacity => _basketCapacity;
        public float TailLiftInterval => _tailLiftInterval;
        public float TelegraphSeconds => _telegraphSeconds;

        public void Configure(float tailLiftInterval, float telegraphSeconds, int rubsToClean,
            int pileCapacity, int basketCapacity, float airDryRubsPerSecond)
        {
            _tailLiftInterval = tailLiftInterval <= 0f ? 1f : tailLiftInterval;
            _telegraphSeconds = telegraphSeconds <= 0f ? 1f : telegraphSeconds;
            _rubsToClean = rubsToClean < 1 ? 1 : rubsToClean;
            _pileCapacity = pileCapacity < 1 ? 1 : pileCapacity;
            _basketCapacity = basketCapacity < 0 ? 0 : basketCapacity;
            _airDryRubsPerSecond = airDryRubsPerSecond <= 0f ? 1f / 30f : airDryRubsPerSecond;
            Reset();
        }

        /// <summary>Runs the shared danger clock and the passive air-dry trickle for stinky dogs.</summary>
        public void Advance(float dt, bool cheddarInBlastRange, bool cocoaInBlastRange)
        {
            if (PrizeSecured || dt <= 0f) return;

            if (!TailUp)
            {
                TimeToNextTailLift -= dt;
                if (TimeToNextTailLift <= 0f)
                {
                    TailUp = true;
                    TailWindowRemaining = _telegraphSeconds;
                }
            }
            else
            {
                TailWindowRemaining -= dt;
                if (TailWindowRemaining <= 0f) ResolveSpray(cheddarInBlastRange, cocoaInBlastRange);
            }

            if (CheddarStinky && PileFresh <= 0)
            {
                _cheddarAirDryProgress += dt * _airDryRubsPerSecond;
                if (_cheddarAirDryProgress >= 1f) { _cheddarAirDryProgress = 0f; RegisterCheddarRub(); }
            }
            if (CocoaStinky && PileFresh <= 0)
            {
                _cocoaAirDryProgress += dt * _airDryRubsPerSecond;
                if (_cocoaAirDryProgress >= 1f) { _cocoaAirDryProgress = 0f; RegisterCocoaRub(); }
            }
        }

        private void ResolveSpray(bool cheddarInBlastRange, bool cocoaInBlastRange)
        {
            TailUp = false;
            TailWindowRemaining = 0f;
            TimeToNextTailLift = _tailLiftInterval;

            if (cheddarInBlastRange)
            {
                CheddarStinky = true;
                CheddarRubProgress = 0;
                _cheddarAirDryProgress = 0f;
                SkunkEvents++;
            }
            if (cocoaInBlastRange)
            {
                CocoaStinky = true;
                CocoaRubProgress = 0;
                _cocoaAirDryProgress = 0f;
                SkunkEvents++;
            }
        }

        /// <summary>Cocoa's clean snatch of the guarded prize. Only valid mid-lure, tail down, clean.</summary>
        public bool TryGrab(bool grabberIsCocoa, bool lureHolding)
        {
            if (PrizeSecured || TailUp || !grabberIsCocoa || CocoaStinky || !lureHolding) return false;
            PrizeSecured = true;
            return true;
        }

        /// <summary>One rub of the shared pile. Consumes a fresh piece and advances that dog's clean-up.</summary>
        public bool Rub(DogId dog)
        {
            bool stinky = dog == DogId.Cheddar ? CheddarStinky : CocoaStinky;
            if (!stinky || PileFresh <= 0) return false;
            PileFresh--;
            PileFunky++;
            if (dog == DogId.Cheddar) { _cheddarAirDryProgress = 0f; RegisterCheddarRub(); }
            else { _cocoaAirDryProgress = 0f; RegisterCocoaRub(); }
            return true;
        }

        /// <summary>The clean dog drags one fresh piece from the basket to the rub pile.</summary>
        public bool HaulLaundry()
        {
            if (BasketSupply <= 0 || PileFresh >= _pileCapacity) return false;
            BasketSupply--;
            PileFresh++;
            return true;
        }

        private void RegisterCheddarRub()
        {
            CheddarRubProgress++;
            if (CheddarRubProgress >= _rubsToClean) { CheddarStinky = false; CheddarRubProgress = 0; }
        }

        private void RegisterCocoaRub()
        {
            CocoaRubProgress++;
            if (CocoaRubProgress >= _rubsToClean) { CocoaStinky = false; CocoaRubProgress = 0; }
        }

        /// <summary>Test hook: skip the calm timer straight to the tail-up telegraph.</summary>
        public void ForceTailLift()
        {
            if (PrizeSecured || TailUp) return;
            TailUp = true;
            TailWindowRemaining = _telegraphSeconds;
        }

        public void Reset()
        {
            TailUp = false;
            TimeToNextTailLift = _tailLiftInterval;
            TailWindowRemaining = 0f;
            PrizeSecured = false;
            SkunkEvents = 0;
            CheddarStinky = false;
            CocoaStinky = false;
            CheddarRubProgress = 0;
            CocoaRubProgress = 0;
            _cheddarAirDryProgress = 0f;
            _cocoaAirDryProgress = 0f;
            BasketSupply = _basketCapacity;
            PileFresh = _pileCapacity;
            PileFunky = 0;
        }
    }
}
