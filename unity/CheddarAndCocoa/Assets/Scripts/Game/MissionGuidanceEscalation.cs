using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Shared, controller-agnostic "how long since this team made progress" clock. GameManager owns
    /// one instance and ticks it only while a mission is actively playing (see the guard around the
    /// call site in <c>GameManager.Update()</c>); every freeze window (briefing/sniff lead-in, the
    /// opening explainer, pause, a held success payoff, end cards) simply stops ticking rather than
    /// resetting, so a team that stalls right up to a freeze resumes at the same stall level after.
    /// Mission start/replay explicitly call <see cref="Reset"/> instead of relying on the freeze.
    ///
    /// Any progress signal (a score event, an objective-copy change) calls <see cref="NotifyProgress"/>
    /// and drops the tier back to 0 (Discovery). Tier thresholds and the per-mission tier ceiling are
    /// data via <see cref="Configure"/>, not code branches - see
    /// <c>GameManager.MissionDefinition.GuidanceTierCap</c> and the matching timing fields.
    /// </summary>
    public sealed class MissionGuidanceEscalation
    {
        public const float DefaultTier1Seconds = 12f;
        public const float DefaultTier2Seconds = 25f;
        public const float DefaultTier3Seconds = 45f;
        public const int MaxTier = 3;

        private float _tier1Seconds = DefaultTier1Seconds;
        private float _tier2Seconds = DefaultTier2Seconds;
        private float _tier3Seconds = DefaultTier3Seconds;
        private int _tierCap = MaxTier;

        private float _stallSeconds;
        private int _tier;

        /// <summary>Current escalation tier: 0 Discovery, 1 Nudge, 2 Coach, 3 Rescue.</summary>
        public int Tier => _tier;

        /// <summary>Seconds of uninterrupted stall accumulated since the last progress signal or reset.</summary>
        public float StallSeconds => _stallSeconds;

        /// <summary>Sets per-mission tier ceiling and timing overrides. Call once per mission start.</summary>
        public void Configure(int tierCap, float tier1Seconds, float tier2Seconds, float tier3Seconds)
        {
            _tierCap = Mathf.Clamp(tierCap, 0, MaxTier);
            _tier1Seconds = tier1Seconds;
            _tier2Seconds = tier2Seconds;
            _tier3Seconds = tier3Seconds;
        }

        /// <summary>Zeroes the stall clock and drops back to Tier 0. Call on mission start and replay.</summary>
        public void Reset()
        {
            _stallSeconds = 0f;
            _tier = 0;
        }

        /// <summary>Any progress signal (score event, objective-copy change) resets the ladder.</summary>
        public void NotifyProgress() => Reset();

        /// <summary>
        /// Advances the stall clock by <paramref name="deltaTime"/>. Callers must skip this call
        /// entirely during frozen windows - a skipped Tick is what makes the freeze a freeze, there is
        /// no separate frozen flag to check here.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            _stallSeconds += deltaTime;
            _tier = Mathf.Min(_tierCap, TierForStall(_stallSeconds));
        }

        private int TierForStall(float stallSeconds)
        {
            if (stallSeconds >= _tier3Seconds) return 3;
            if (stallSeconds >= _tier2Seconds) return 2;
            if (stallSeconds >= _tier1Seconds) return 1;
            return 0;
        }
    }
}
