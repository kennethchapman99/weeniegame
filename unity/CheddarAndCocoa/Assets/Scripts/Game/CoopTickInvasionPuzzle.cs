using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for the Tick Invasion survival beat: the backyard has
    /// exploded with ticks and BOTH dogs accumulate them continuously (Cheddar faster - chaos
    /// puppy energy - Cocoa slower - veteran composure), climbing toward a per-dog max that ends
    /// the run for whichever dog hits it first. The only way to fight back is mutual grooming
    /// (<see cref="Groom"/>): each dog can clear ticks off their partner, at the cost of picking up
    /// a small amount themselves - the resource-transfer "neither gets overwhelmed" pressure the
    /// design calls for. A dog whose ticks cross <see cref="ErraticThreshold"/> goes erratic (too
    /// hard to lock down for a normal groom) until their partner barks them calm
    /// (<see cref="Bark"/>, which also opens a brief hold window letting the very next groom land
    /// despite the erratic state) or their ticks drop back under the threshold on their own merit.
    ///
    /// <see cref="PoolDive"/> is the risk/reward escape hatch: an instant reset to zero ticks, but
    /// the diving dog comes out wet - slow and soggy - collecting ticks at double rate for a
    /// per-dog recovery window (Cocoa's is shorter; she's done this before, Cheddar belly-flops
    /// and is useless for longer) during which they cannot effectively groom anyone.
    ///
    /// Partway through the round a Super Tick locks onto ONE dog exclusively
    /// (<see cref="SuperTickTarget"/>): normal grooming cannot touch it (in either direction - the
    /// targeted dog is too overwhelmed to groom their partner either, so the partner truly has no
    /// groom relief during this window), and only a pool dive clears it.
    ///
    /// Pure logic: a mission drives groom/bark/pool-dive and time; tests drive all of it deterministically.
    /// </summary>
    public sealed class CoopTickInvasionPuzzle
    {
        public const float MaxTicks = 1f;

        private float _cheddarTickRate = 0.026f;
        private float _cocoaTickRate = 0.017f;
        private float _erraticThreshold = 0.62f;
        private float _cheddarGroomAmount = 0.24f;
        private float _cocoaGroomAmount = 0.16f;
        private float _selfIncreaseAmount = 0.05f;
        private float _barkCalmAmount = 0.12f;
        private float _barkHoldSeconds = 3.5f;
        private float _cheddarWetSeconds = 10f;
        private float _cocoaWetSeconds = 6f;
        private float _wetTickMultiplier = 2f;
        private float _superTickTriggerSeconds = 45f;
        private float _surviveSeconds = 82f;

        public float ElapsedSeconds { get; private set; }
        public float CheddarTicks { get; private set; }
        public float CocoaTicks { get; private set; }
        public float CheddarWetUntil { get; private set; }
        public float CocoaWetUntil { get; private set; }
        public float CheddarHeldUntil { get; private set; }
        public float CocoaHeldUntil { get; private set; }
        public DogId? SuperTickTarget { get; private set; }
        public bool SuperTickTriggered { get; private set; }
        public DogId? FailedDog { get; private set; }
        public bool Cleared { get; private set; }

        /// <summary>Times either dog crossed INTO erratic this run - the mission's Mistakes tally.</summary>
        public int Mistakes { get; private set; }

        public float ErraticThreshold => _erraticThreshold;
        public float SurviveSeconds => _surviveSeconds;
        public float SuperTickTriggerSeconds => _superTickTriggerSeconds;
        public float CheddarWetSeconds => _cheddarWetSeconds;
        public float CocoaWetSeconds => _cocoaWetSeconds;

        public bool IsCheddarWet => ElapsedSeconds < CheddarWetUntil;
        public bool IsCocoaWet => ElapsedSeconds < CocoaWetUntil;
        public bool CheddarErratic => FailedDog == null && CheddarTicks >= _erraticThreshold;
        public bool CocoaErratic => FailedDog == null && CocoaTicks >= _erraticThreshold;
        public bool CheddarHeld => ElapsedSeconds < CheddarHeldUntil;
        public bool CocoaHeld => ElapsedSeconds < CocoaHeldUntil;

        public float TicksOf(DogId dog) => dog == DogId.Cheddar ? CheddarTicks : CocoaTicks;
        public bool ErraticOf(DogId dog) => dog == DogId.Cheddar ? CheddarErratic : CocoaErratic;
        public bool WetOf(DogId dog) => dog == DogId.Cheddar ? IsCheddarWet : IsCocoaWet;

        public void Configure(
            float cheddarTickRate, float cocoaTickRate, float erraticThreshold,
            float cheddarGroomAmount, float cocoaGroomAmount, float selfIncreaseAmount,
            float barkCalmAmount, float barkHoldSeconds,
            float cheddarWetSeconds, float cocoaWetSeconds, float wetTickMultiplier,
            float superTickTriggerSeconds, float surviveSeconds)
        {
            _cheddarTickRate = cheddarTickRate <= 0f ? 0.02f : cheddarTickRate;
            _cocoaTickRate = cocoaTickRate <= 0f ? 0.01f : cocoaTickRate;
            _erraticThreshold = erraticThreshold <= 0f ? 0.6f : erraticThreshold;
            _cheddarGroomAmount = cheddarGroomAmount <= 0f ? 0.2f : cheddarGroomAmount;
            _cocoaGroomAmount = cocoaGroomAmount <= 0f ? 0.15f : cocoaGroomAmount;
            _selfIncreaseAmount = selfIncreaseAmount < 0f ? 0f : selfIncreaseAmount;
            _barkCalmAmount = barkCalmAmount < 0f ? 0f : barkCalmAmount;
            _barkHoldSeconds = barkHoldSeconds <= 0f ? 1f : barkHoldSeconds;
            _cheddarWetSeconds = cheddarWetSeconds <= 0f ? 5f : cheddarWetSeconds;
            _cocoaWetSeconds = cocoaWetSeconds <= 0f ? 5f : cocoaWetSeconds;
            _wetTickMultiplier = wetTickMultiplier <= 1f ? 1f : wetTickMultiplier;
            _superTickTriggerSeconds = superTickTriggerSeconds <= 0f ? 30f : superTickTriggerSeconds;
            _surviveSeconds = surviveSeconds <= 0f ? 60f : surviveSeconds;
            Reset();
        }

        /// <summary>Runs the continuous tick accumulation for both dogs and the Super Tick clock.</summary>
        public void Advance(float dt)
        {
            if (Cleared || FailedDog != null || dt <= 0f) return;

            ElapsedSeconds += dt;
            bool cheddarErraticPre = CheddarErratic;
            bool cocoaErraticPre = CocoaErratic;

            float cheddarRate = _cheddarTickRate * (IsCheddarWet ? _wetTickMultiplier : 1f);
            float cocoaRate = _cocoaTickRate * (IsCocoaWet ? _wetTickMultiplier : 1f);
            CheddarTicks = Clamp01(CheddarTicks + cheddarRate * dt);
            CocoaTicks = Clamp01(CocoaTicks + cocoaRate * dt);

            if (!cheddarErraticPre && CheddarErratic) Mistakes++;
            if (!cocoaErraticPre && CocoaErratic) Mistakes++;

            if (CheddarTicks >= MaxTicks) FailedDog = DogId.Cheddar;
            else if (CocoaTicks >= MaxTicks) FailedDog = DogId.Cocoa;
            if (FailedDog != null) return;

            if (!SuperTickTriggered && ElapsedSeconds >= _superTickTriggerSeconds)
                TriggerSuperTick(CheddarTicks >= CocoaTicks ? DogId.Cheddar : DogId.Cocoa);

            if (ElapsedSeconds >= _surviveSeconds) Cleared = true;
        }

        /// <summary>
        /// One dog grooms their partner clean. Fails if the groomer is wet (too soggy to groom
        /// effectively), the groomer is themselves the Super Tick target (too overwhelmed to help
        /// anyone), the target IS the Super Tick target (cannot be groomed off), or the target is
        /// erratic and not currently held by a bark.
        /// </summary>
        public bool Groom(DogId groomer)
        {
            if (Cleared || FailedDog != null) return false;
            DogId target = groomer == DogId.Cheddar ? DogId.Cocoa : DogId.Cheddar;

            if (WetOf(groomer)) return false;
            if (SuperTickTarget == groomer) return false;
            if (SuperTickTarget == target) return false;
            if (ErraticOf(target) && !HeldOf(target)) return false;

            float amount = groomer == DogId.Cheddar ? _cheddarGroomAmount : _cocoaGroomAmount;
            SetTicks(target, TicksOf(target) - amount);
            SetTicks(groomer, TicksOf(groomer) + _selfIncreaseAmount);
            return true;
        }

        /// <summary>
        /// Barker calms their erratic partner: a small immediate tick reduction (the "barked-calm"
        /// path) plus a brief hold window letting the very next groom land despite the erratic
        /// state ("Cocoa's call holds Cheddar still so the groom connects"). No-op (returns false)
        /// if the partner isn't erratic - nothing to calm.
        /// </summary>
        public bool Bark(DogId barker)
        {
            if (Cleared || FailedDog != null) return false;
            DogId partner = barker == DogId.Cheddar ? DogId.Cocoa : DogId.Cheddar;
            if (!ErraticOf(partner)) return false;

            SetTicks(partner, TicksOf(partner) - _barkCalmAmount);
            if (partner == DogId.Cheddar) CheddarHeldUntil = ElapsedSeconds + _barkHoldSeconds;
            else CocoaHeldUntil = ElapsedSeconds + _barkHoldSeconds;
            return true;
        }

        /// <summary>
        /// Risk/reward pool dive: instant reset to zero ticks (and clears the Super Tick if it was
        /// targeting this dog), but applies the wet-slow penalty - doubled tick accumulation for
        /// this dog's configured recovery window, during which they cannot effectively groom.
        /// </summary>
        public bool PoolDive(DogId dog)
        {
            if (Cleared || FailedDog != null) return false;
            if (dog == DogId.Cheddar)
            {
                CheddarTicks = 0f;
                CheddarWetUntil = ElapsedSeconds + _cheddarWetSeconds;
                if (SuperTickTarget == DogId.Cheddar) SuperTickTarget = null;
            }
            else
            {
                CocoaTicks = 0f;
                CocoaWetUntil = ElapsedSeconds + _cocoaWetSeconds;
                if (SuperTickTarget == DogId.Cocoa) SuperTickTarget = null;
            }
            return true;
        }

        /// <summary>Test hook: force the Super Tick onto a specific dog without waiting out the clock.</summary>
        public void ForceSuperTick(DogId target) => TriggerSuperTick(target);

        public void Reset()
        {
            ElapsedSeconds = 0f;
            CheddarTicks = 0f;
            CocoaTicks = 0f;
            CheddarWetUntil = 0f;
            CocoaWetUntil = 0f;
            CheddarHeldUntil = 0f;
            CocoaHeldUntil = 0f;
            SuperTickTarget = null;
            SuperTickTriggered = false;
            FailedDog = null;
            Cleared = false;
            Mistakes = 0;
        }

        private void TriggerSuperTick(DogId target)
        {
            if (Cleared || FailedDog != null) return;
            SuperTickTriggered = true;
            SuperTickTarget = target;
        }

        private bool HeldOf(DogId dog) => dog == DogId.Cheddar ? CheddarHeld : CocoaHeld;

        private void SetTicks(DogId dog, float value)
        {
            if (dog == DogId.Cheddar) CheddarTicks = Clamp01(value);
            else CocoaTicks = Clamp01(value);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
