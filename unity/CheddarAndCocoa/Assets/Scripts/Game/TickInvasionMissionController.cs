using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned Tick Invasion: the backyard has exploded with ticks and both dogs
    /// accumulate them continuously - Cheddar's chaos-puppy energy attracts ticks faster but he
    /// grooms faster too (high risk, high output); Cocoa's veteran composure accumulates slower and
    /// her groom reaches a wider area (long-dog advantage). The only defense is mutual grooming:
    /// stand close and groom your partner clean, at the cost of a small pickup yourself. A dog whose
    /// ticks cross the erratic threshold runs hard to lock down until barked calm or groomed back
    /// under it. Either dog can pool-dive for an instant reset, but emerges wet - slow, and
    /// collecting ticks at double rate for a per-dog recovery window (Cocoa recovers faster).
    /// Partway through the round a Super Tick locks onto one dog exclusively, immune to normal
    /// grooming - only a pool dive clears it, and the partner has no groom relief while it's active.
    /// Hard fail: either dog's ticks maxing out ends the run (Cheddar messy, Cocoa dignified).
    /// </summary>
    public sealed class TickInvasionMissionController : IMissionController, IMissionInteractionController,
        IMissionPressureHud, IMissionSuccessPresentationController
    {
        private const float CheddarTickRate = 0.026f;
        private const float CocoaTickRate = 0.017f;
        private const float ErraticThreshold = 0.62f;
        private const float CheddarGroomAmount = 0.24f;  // fast/strong - high risk, high output
        private const float CocoaGroomAmount = 0.16f;    // slower per action, but wider reach below
        private const float SelfIncreaseAmount = 0.05f;
        private const float BarkCalmAmount = 0.12f;
        private const float BarkHoldSeconds = 3.5f;
        private const float CheddarWetSeconds = 10f;     // belly-flops, useless for longer
        private const float CocoaWetSeconds = 6f;        // done this before, out faster
        private const float WetTickMultiplier = 2f;
        private const float SuperTickTriggerSeconds = 45f;
        private const float SurviveSeconds = 82f;         // clears comfortably inside the shared 100s round

        private const float CheddarGroomRadius = 1.8f;
        private const float CocoaGroomRadius = 2.6f;      // long-dog advantage - wider area
        private const float PoolDiveRadius = 2.2f;
        private const float WorstInfestationGap = 0.22f;
        private const float SuccessHoldSeconds = 1.15f;

        private static readonly Color SafeColor = new(0.42f, 0.85f, 0.5f);
        private static readonly Color ErraticColor = new(1f, 0.35f, 0.2f);
        private static readonly Color SuperTickColor = new(0.85f, 0.15f, 0.65f);
        private static readonly Color PoolColor = new(0.3f, 0.65f, 0.95f);
        private static readonly Color WetColor = new(0.45f, 0.7f, 0.95f);

        private readonly CoopTickInvasionPuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _poolObj;
        private TextMesh _cheddarTickLabel;
        private TextMesh _cocoaTickLabel;
        private float _successHoldRemaining;
        private DogId? _worstCalloutFiredFor;

        public CoopTickInvasionPuzzle Puzzle => _puzzle;
        public GameObject PoolObject => _poolObj;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.TickInvasion;
        public bool IsComplete => _puzzle.Cleared && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _puzzle.Cleared && _successHoldRemaining > 0f;
        public bool IsFailed => _puzzle.FailedDog != null;
        public string FailReason => _puzzle.FailedDog switch
        {
            DogId.Cheddar => "Cheddar, completely covered in ticks, does frantic zoomies until he keels over.",
            DogId.Cocoa => "Cocoa sits perfectly still, absolutely furious, as the tick meter maxes out.",
            _ => null
        };

        public Vector2 EntryTarget => _context != null ? _context.Bounds.center : Vector2.zero;
        public string OutcomeSummary => _puzzle.Cleared
            ? (_puzzle.Mistakes == 0 ? "Never Once Erratic" : "Scratched But Survived")
            : _puzzle.FailedDog == DogId.Cheddar ? "Buried In Ticks" : "Furious And Overrun";

        public bool PressureVisible => !_puzzle.Cleared;
        public string PressureLabel
        {
            get
            {
                DogId worse = _puzzle.CheddarTicks >= _puzzle.CocoaTicks ? DogId.Cheddar : DogId.Cocoa;
                if (_puzzle.SuperTickTarget != null) worse = _puzzle.SuperTickTarget.Value;
                string name = Name(worse);
                int pct = Mathf.RoundToInt(_puzzle.TicksOf(worse) * 100f);
                if (_puzzle.SuperTickTarget == worse) return $"{name} SUPER TICK!";
                if (_puzzle.ErraticOf(worse)) return $"{name} TICKS {pct}% - ERRATIC!";
                return $"{name} TICKS {pct}%";
            }
        }
        public float PressureNormalized => Mathf.Clamp01(Mathf.Max(_puzzle.CheddarTicks, _puzzle.CocoaTicks));
        public Color PressureColor => _puzzle.SuperTickTarget != null
            ? SuperTickColor
            : Color.Lerp(SafeColor, ErraticColor, PressureNormalized);

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome)
                    return "Infestation cleared! Both dogs groomed each other through the whole invasion.";
                if (_puzzle.SuperTickTarget != null)
                {
                    DogId target = _puzzle.SuperTickTarget.Value;
                    return $"SUPER TICK on {Name(target)}! Immune to grooming - {Name(target)} must dive in the pool while {Name(Other(target))} holds the line.";
                }
                if (_puzzle.CheddarErratic && !_puzzle.CheddarHeld)
                    return "Cheddar is ERRATIC and running wild - Cocoa, bark to hold him still, then groom!";
                if (_puzzle.CocoaErratic && !_puzzle.CocoaHeld)
                    return "Cocoa is ERRATIC and bolting - Cheddar, bark to hold her still, then groom!";
                return "Stand close to your partner and groom the ticks off each other before anyone gets overwhelmed.";
            }
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            BuildProps();
            Cleanup();
        }

        public void StartMission()
        {
            _puzzle.Configure(
                CheddarTickRate, CocoaTickRate, ErraticThreshold,
                CheddarGroomAmount, CocoaGroomAmount, SelfIncreaseAmount,
                BarkCalmAmount, BarkHoldSeconds,
                CheddarWetSeconds, CocoaWetSeconds, WetTickMultiplier,
                SuperTickTriggerSeconds, SurviveSeconds);
            _successHoldRemaining = 0f;
            _worstCalloutFiredFor = null;

            if (_poolObj != null)
            {
                _poolObj.transform.position = PoolPos;
                _poolObj.SetActive(true);
                _context.SetActorState(_poolObj, "POOL - DIVE TO RINSE OFF!", PoolColor, 0.05f);
            }
            UpdateTickLabels();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Cleared)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }
            if (_puzzle.FailedDog != null) return;

            bool cheddarErraticPre = _puzzle.CheddarErratic;
            bool cocoaErraticPre = _puzzle.CocoaErratic;
            bool superTickPre = _puzzle.SuperTickTarget != null;

            _puzzle.Advance(deltaTime);

            if (!cheddarErraticPre && _puzzle.CheddarErratic) AnnounceErratic(DogId.Cheddar);
            if (!cocoaErraticPre && _puzzle.CocoaErratic) AnnounceErratic(DogId.Cocoa);
            if (!superTickPre && _puzzle.SuperTickTarget != null) AnnounceSuperTick(_puzzle.SuperTickTarget.Value);
            if (_puzzle.FailedDog != null) { AnnounceFailure(_puzzle.FailedDog.Value); return; }
            if (_puzzle.Cleared) { AnnounceClear(); return; }

            UpdateWorstInfestationCallout();
            UpdateTickLabels();
        }

        public bool HandleBark(int dogIndex)
        {
            if (_puzzle.Cleared || _puzzle.FailedDog != null || !TryResolveDog(dogIndex, out DogId dog)) return false;

            if (_puzzle.Bark(dog))
            {
                DogId partner = Other(dog);
                _context.SetCue($"{Name(dog)} barks {Name(partner)} calm - hold still for the groom now!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "HOLD STILL!");
                PopAtDog(partner, "CALMED!", SafeColor);
                _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
                _context.LogEvent("TickBarkCalm", $"{Name(dog)}->{Name(partner)}");
                _context.LogObjectiveChanged();
                return true;
            }
            _context.SetCue($"{Name(Other(dog))} isn't erratic right now - nothing to calm.");
            return true;
        }

        public bool HandleInteract(int dogIndex)
        {
            if (_puzzle.Cleared || _puzzle.FailedDog != null || !TryResolveDog(dogIndex, out DogId dog)) return false;
            Vector2 pos = _context.Dogs[dogIndex].transform.position;
            DogId partner = Other(dog);

            if (_poolObj != null && Vector2.Distance(pos, _poolObj.transform.position) <= PoolDiveRadius)
            {
                bool clearedSuperTick = _puzzle.SuperTickTarget == dog;
                _puzzle.PoolDive(dog);
                int idx = _context.IndexOfDog(dog);
                if (idx >= 0) _context.CreditDog(idx);
                _context.AddScore(ScoreEventCatalog.PoolDiveRinse.Points, ScoreEventCatalog.PoolDiveRinse.Label);
                _context.SetCue(clearedSuperTick
                    ? $"{Name(dog)} dives in and shakes the Super Tick right off! Wet and slow for a bit now."
                    : $"{Name(dog)} cannonballs in - instantly clean, but wet and slow to come out.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "SPLASH!");
                PopAtDog(dog, "RINSED!", PoolColor);
                if (clearedSuperTick) _context.AddScore(ScoreEventCatalog.SuperTickShaken.Points, ScoreEventCatalog.SuperTickShaken.Label);
                _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
                _context.RequestShake(0.15f);
                if (_context.DogFeedback != null && dogIndex < _context.DogFeedback.Length) _context.DogFeedback[dogIndex]?.ShowProudBrief();
                UpdateTickLabels();
                _context.LogEvent("TickPoolDive", Name(dog));
                _context.LogObjectiveChanged();
                return true;
            }

            int partnerIdx = _context.IndexOfDog(partner);
            Vector2 partnerPos = partnerIdx >= 0 ? _context.Dogs[partnerIdx].transform.position : pos;
            float groomRadius = dog == DogId.Cheddar ? CheddarGroomRadius : CocoaGroomRadius;
            if (Vector2.Distance(pos, partnerPos) > groomRadius)
            {
                _context.SetCue($"{Name(dog)} is too far to groom {Name(partner)} - get closer, or reach the pool to dive clean.");
                _context.MarkFailedInteraction(dog, "get closer to your partner to groom them, or reach the pool to dive clean");
                return true;
            }

            if (!_puzzle.Groom(dog))
            {
                string reason = _puzzle.WetOf(dog)
                    ? $"{Name(dog)} is too wet and sluggish to groom effectively right now."
                    : _puzzle.SuperTickTarget == dog
                        ? $"{Name(dog)} is overwhelmed by the Super Tick and can't groom anyone - get to the pool!"
                        : _puzzle.SuperTickTarget == partner
                            ? $"The Super Tick can't be groomed off {Name(partner)} - only a pool dive clears it."
                            : $"{Name(partner)} is too erratic to lock down - bark to hold them still first!";
                _context.SetCue(reason);
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CAN'T GROOM!");
                _context.MarkFailedInteraction(dog, reason);
                return true;
            }

            int groomerIdx = _context.IndexOfDog(dog);
            if (groomerIdx >= 0) _context.CreditDog(groomerIdx);
            _context.AddScore(ScoreEventCatalog.GroomLanded.Points, ScoreEventCatalog.GroomLanded.Label);
            _context.SetCue($"{Name(dog)} grooms {Name(partner)} clean - a few ticks jump over in the process.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.ScoreDelta, $"+{ScoreEventCatalog.GroomLanded.Points} GROOMED");
            PopAtDog(partner, "GROOMED!", SafeColor);
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            UpdateTickLabels();
            _context.LogEvent("TickGroom", $"{Name(dog)}->{Name(partner)}");
            _context.LogObjectiveChanged();
            return true;
        }

        public void Cleanup()
        {
            if (_poolObj != null) _poolObj.SetActive(false);
            _successHoldRemaining = 0f;
            _worstCalloutFiredFor = null;
        }

        public void StageDogsForEntry()
        {
            int cheddarIdx = _context.IndexOfDog(DogId.Cheddar);
            int cocoaIdx = _context.IndexOfDog(DogId.Cocoa);
            // Stage far enough apart that neither starts already inside the other's groom radius
            // (their cold-start objective arrow points at their partner) while staying comfortably
            // inside MaximumMissionEntryDistance of EntryTarget (Bounds.center).
            Vector2 center = _context.Bounds.center;
            if (cheddarIdx >= 0) PlaceDog(cheddarIdx, center + new Vector2(-4f, 0f));
            if (cocoaIdx >= 0) PlaceDog(cocoaIdx, center + new Vector2(4f, 0f));
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null;
            copy = string.Empty;
            hideDistance = 1.5f;
            if (_puzzle.Cleared || _context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length) return false;

            DogId me = DogIdAt(dogIndex);
            DogId partner = Other(me);

            if (_puzzle.SuperTickTarget == me)
            {
                target = _poolObj?.transform;
                copy = "SUPER TICK - DIVE NOW!";
                hideDistance = PoolDiveRadius;
                return target != null;
            }
            if (_puzzle.SuperTickTarget == partner) return false; // hold the line - nothing to groom right now

            int partnerIdx = _context.IndexOfDog(partner);
            target = partnerIdx >= 0 ? _context.Dogs[partnerIdx].transform : null;
            bool partnerErratic = _puzzle.ErraticOf(partner);
            bool partnerHeld = partner == DogId.Cheddar ? _puzzle.CheddarHeld : _puzzle.CocoaHeld;
            copy = partnerErratic && !partnerHeld ? "BARK TO CALM, THEN GROOM" : "GROOM PARTNER";
            hideDistance = me == DogId.Cheddar ? CheddarGroomRadius : CocoaGroomRadius;
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("tick_invasion", score, timeRemaining, _puzzle.Cleared ? 1 : 0, 1, _puzzle.Mistakes,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Deterministic seam for advancing past the live clear payoff.</summary>
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        /// <summary>Test seam: resolve GameManager's dog-index space for a given dog identity.</summary>
        public int DogIndexOf(DogId dogId) => _context.IndexOfDog(dogId);

        private bool TryResolveDog(int dogIndex, out DogId dog)
        {
            dog = DogId.Cheddar;
            if (_context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length) return false;
            if (!_context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)) return false;
            dog = identity.Id;
            return true;
        }

        private DogId DogIdAt(int dogIndex) =>
            _context.Dogs != null && dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
            _context.Dogs[dogIndex] != null && _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                ? identity.Id : DogId.Cheddar;

        private static DogId Other(DogId dog) => dog == DogId.Cheddar ? DogId.Cocoa : DogId.Cheddar;

        private void AnnounceErratic(DogId dog)
        {
            _context.AddScore(ScoreEventCatalog.TickOverload.Points, ScoreEventCatalog.TickOverload.Label);
            _context.SetCue($"{Name(dog)} is ERRATIC and running wild - {Name(Other(dog))}, bark to hold them still!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, $"{Name(dog).ToUpperInvariant()} ERRATIC!");
            PopAtDog(dog, "ERRATIC!", ErraticColor);
            ShowSadFor(dog);
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.LogEvent("TickErratic", Name(dog));
            _context.LogObjectiveChanged();
        }

        private void AnnounceClear()
        {
            _successHoldRemaining = SuccessHoldSeconds;
            _context.AddScore(ScoreEventCatalog.InfestationCleared.Points, ScoreEventCatalog.InfestationCleared.Label);
            if (_context.Dogs != null)
                for (int i = 0; i < _context.Dogs.Length; i++)
                    _context.CreditDog(i);
            _context.SetFeedback(GameManager.FeedbackKind.LevelClear);
            _context.SetCue("The backyard is finally tick-free - both dogs groomed each other through the whole invasion!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "INFESTATION CLEARED!");
            _context.SpawnWorldPop(_context.Bounds.center + Vector2.up * 2f, "INFESTATION CLEARED!", SafeColor);
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowProudBrief();
            _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
            _context.RequestRumble("tick_invasion_clear", 0.28f, 0.5f, 0.2f);
            UpdateTickLabels();
            _context.LogEvent("TickInvasionCleared", $"mistakes={_puzzle.Mistakes}");
        }

        private void AnnounceSuperTick(DogId dog)
        {
            _context.SetCue($"SUPER TICK latched onto {Name(dog)}! Grooming won't touch it - straight to the pool!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "SUPER TICK!");
            PopAtDog(dog, "SUPER TICK!", SuperTickColor);
            _context.SetFeedback(GameManager.FeedbackKind.PredatorAttack);
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestShake(0.25f);
            _context.LogEvent("TickSuperTick", Name(dog));
            _context.LogObjectiveChanged();
        }

        private void AnnounceFailure(DogId dog)
        {
            PopAtDog(dog, "MAXED OUT!", ErraticColor);
            ShowSadFor(dog);
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.LogEvent("TickInvasionFailed", Name(dog));
        }

        private void UpdateWorstInfestationCallout()
        {
            float gap = Mathf.Abs(_puzzle.CheddarTicks - _puzzle.CocoaTicks);
            if (gap < WorstInfestationGap) { _worstCalloutFiredFor = null; return; }

            DogId worse = _puzzle.CheddarTicks > _puzzle.CocoaTicks ? DogId.Cheddar : DogId.Cocoa;
            if (_worstCalloutFiredFor == worse) return;
            _worstCalloutFiredFor = worse;

            // Cocoa's veteran read spots the worst infestation first, per her queen-composure trait.
            _context.SetCue($"Cocoa spots it: {Name(worse)} is covered a lot worse - groom {Name(worse)} first!");
            PopAtDog(worse, "WORSE OFF!", ErraticColor);
        }

        private void UpdateTickLabels()
        {
            if (_cheddarTickLabel != null)
            {
                _cheddarTickLabel.text = $"TICKS {Pct(_puzzle.CheddarTicks)}%";
                _cheddarTickLabel.color = LabelColorFor(DogId.Cheddar);
            }
            if (_cocoaTickLabel != null)
            {
                _cocoaTickLabel.text = $"TICKS {Pct(_puzzle.CocoaTicks)}%";
                _cocoaTickLabel.color = LabelColorFor(DogId.Cocoa);
            }
        }

        private Color LabelColorFor(DogId dog)
        {
            if (_puzzle.SuperTickTarget == dog) return SuperTickColor;
            if (_puzzle.WetOf(dog)) return WetColor;
            if (_puzzle.ErraticOf(dog)) return ErraticColor;
            return Color.Lerp(SafeColor, ErraticColor, _puzzle.TicksOf(dog));
        }

        private void BuildProps()
        {
            _poolObj = new GameObject("TickInvasionPool");
            var renderer = _poolObj.AddComponent<SpriteRenderer>();
            renderer.sprite = _context.RangeSprite != null ? _context.RangeSprite : _context.ActorSprite;
            renderer.color = new Color(PoolColor.r, PoolColor.g, PoolColor.b, 0.5f);
            renderer.sortingOrder = 2;
            _poolObj.transform.localScale = Vector3.one * 2.2f;
            // No bespoke pool-dive art yet - reuse the existing dog bowl prop art as a stand-in
            // basin (round vessel, reads as "something to dunk into"), matching Skunk Blast
            // Mayhem's precedent of reusing an existing prop's art for new fiction.
            MissionPropArt.AttachObject(_poolObj, FinalGameplayArt.DogBowl, 0.018f, 18, true);
            _context.AddWorldLabel(_poolObj, "POOL", Vector3.up * 1.3f, 13, Color.white);
            _poolObj.SetActive(false);

            if (_context.Dogs != null)
            {
                int cheddarIdx = _context.IndexOfDog(DogId.Cheddar);
                int cocoaIdx = _context.IndexOfDog(DogId.Cocoa);
                if (cheddarIdx >= 0)
                    _cheddarTickLabel = _context.AddWorldLabel(_context.Dogs[cheddarIdx].gameObject, "TICKS 0%", new Vector3(0f, -1.05f, -0.1f), 11, Color.white);
                if (cocoaIdx >= 0)
                    _cocoaTickLabel = _context.AddWorldLabel(_context.Dogs[cocoaIdx].gameObject, "TICKS 0%", new Vector3(0f, -1.05f, -0.1f), 11, Color.white);
            }
        }

        private void PlaceDog(int index, Vector2 point)
        {
            const float margin = 1.5f;
            _context.Dogs[index].transform.position = new Vector2(
                Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
                Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
            if (_context.Dogs[index].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        private void PopAtDog(DogId dog, string text, Color color)
        {
            int idx = _context.IndexOfDog(dog);
            if (idx >= 0) _context.SpawnWorldPop(_context.Dogs[idx].transform.position, text, color);
        }

        private void ShowSadFor(DogId dog)
        {
            int idx = _context.IndexOfDog(dog);
            if (idx >= 0 && idx < _context.DogFeedback.Length) _context.DogFeedback[idx]?.ShowPanic();
        }

        private static string Name(DogId dog) => dog == DogId.Cheddar ? "Cheddar" : "Cocoa";
        private static int Pct(float value) => Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);

        private Vector2 PoolPos => _context.Bounds.center + new Vector2(_context.Bounds.width * 0.32f, _context.Bounds.height * 0.3f);
    }
}
