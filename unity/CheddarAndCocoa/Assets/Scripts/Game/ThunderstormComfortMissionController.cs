using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class ThunderstormComfortMissionController : IMissionController, IMissionPressureHud,
        IMissionSuccessPresentationController
    {
        private const int ClapGoal = 5;
        private const float ClapInterval = 5.5f;
        private const float CheddarSpike = 0.26f;
        private const float CocoaSpike = 0.16f;
        private const float PreparedCheddarSpike = 0.1f;
        private const float PreparedCocoaSpike = 0.06f;
        private const float ReassuranceWindowSeconds = 1.6f;
        private const float SuccessHoldSeconds = 1.15f;

        private readonly ThunderstormMissionState _stormState = new();
        private MissionContext _context;
        private GameObject _stormMarker;
        private MissionPropArtAttachment _stormArt;
        private float _nextClapAt;
        private bool _cleared;
        private bool _bolted;
        private float _cocoaReassuranceUntil;
        private bool _comfortPrepared;
        private float _successHoldRemaining;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.ThunderstormComfort;
        public bool IsComplete => _cleared && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _cleared && _successHoldRemaining > 0f;
        public bool IsFailed => _context?.PanicMeter?.Maxed != null;
        public string FailReason => _context?.PanicMeter?.Maxed != null
            ? $"{_context.PanicMeter.Maxed} panicked at the thunder and bolted before the storm passed."
            : null;
        public ThunderstormMissionState StormState => _stormState;
        public bool ComfortPrepared => _comfortPrepared;
        public Vector2 EntryTarget => _context != null ? _context.Bounds.center : Vector2.zero;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildThunderstormSummary(_stormState);
        public string PressureLabel => "PANIC";
        public bool PressureVisible => true;
        public float PressureNormalized => _context?.PanicMeter != null
            ? Mathf.Clamp01(Mathf.Max(_context.PanicMeter.CheddarPanic, _context.PanicMeter.CocoaPanic))
            : 0f;
        public Color PressureColor => Color.Lerp(new Color(0.42f, 0.85f, 1f), new Color(1f, 0.2f, 0.12f), PressureNormalized);

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome)
                    return "Storm passed! Cocoa kept steady and Cheddar found his brave bark.";
                if (_comfortPrepared)
                    return $"COMFORT READY: stay huddled for the next clap! {_stormState.ClapsSurvived}/{ClapGoal} weathered";
                if (_context != null && _context.Now() <= _cocoaReassuranceUntil)
                    return $"Cocoa reassured Cheddar - Cheddar BARK back now! {_stormState.ClapsSurvived}/{ClapGoal} weathered";
                return $"Huddle: Cocoa BARKS reassurance first, Cheddar answers. Thunder claps {_stormState.ClapsSurvived}/{ClapGoal}";
            }
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _stormMarker = new GameObject("ThunderstormComfortCue");
            _stormMarker.transform.position = _context.Bounds.center + Vector2.up * 2.8f;
            _stormMarker.transform.localScale = Vector3.one * 1.4f;
            var renderer = _stormMarker.AddComponent<SpriteRenderer>();
            renderer.sprite = _context.RangeSprite != null ? _context.RangeSprite : _context.ActorSprite;
            renderer.color = new Color(0.42f, 0.5f, 0.82f, 0.18f);
            renderer.sortingOrder = 2;
            _context.AddWorldLabel(_stormMarker, "HUDDLE", Vector3.up * 0.9f, 13, Color.white);
            _stormArt = MissionPropArt.AttachPad(_stormMarker, FinalGameplayArt.ThunderstormCloudWaiting, 0.012f, 18);
            _stormMarker.SetActive(false);
        }

        public void StartMission()
        {
            _stormState.Configure(ClapGoal);
            _context.PanicMeter?.ResetMeter();
            _cleared = false;
            _bolted = false;
            _cocoaReassuranceUntil = 0f;
            _comfortPrepared = false;
            _successHoldRemaining = 0f;
            _nextClapAt = _context.Now() + ClapInterval;
            SetStormArt(FinalGameplayArt.ThunderstormCloudWaiting);
            if (_stormMarker != null)
            {
                _stormMarker.SetActive(true);
                _context.SetActorState(_stormMarker, "HUDDLE - COCOA BARKS FIRST", new Color(0.55f, 0.68f, 1f), 0.12f);
            }
        }

        public void Tick(float deltaTime, float now)
        {
            if (_cleared)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }
            if (IsFailed) return;
            var pm = _context.PanicMeter;
            if (pm == null || _context.Dogs == null || _context.Dogs.Length < 2) return;

            pm.Step(_context.Dogs[0].transform.position, _context.Dogs[1].transform.position, deltaTime);
            if (Vector2.Distance(_context.Dogs[0].transform.position, _context.Dogs[1].transform.position) <= pm.CuddleRadius)
            {
                SetStormArt(_comfortPrepared
                    ? FinalGameplayArt.ThunderstormComfortHuddle
                    : FinalGameplayArt.ThunderstormCloudWaiting);
                for (int i = 0; i < _context.DogFeedback.Length; i++)
                    if (_context.DogFeedback[i] != null) _context.DogFeedback[i].ShowComfort();
            }
            CheckBolt();
            if (pm.Maxed != null) return;

            if (now >= _nextClapAt)
            {
                _nextClapAt = now + ClapInterval;
                ApplyThunderclap();
            }
        }

        public bool HandleBark(int dogIndex)
        {
            if (_cleared || IsFailed || dogIndex < 0 || dogIndex >= _context.Dogs.Length) return false;
            DogId dogId = DogIdAt(dogIndex);
            if (!IsHuddling())
            {
                _context.MarkFailedInteraction(dogId, "get beside your partner before starting the comfort bark");
                _context.SetCue("The reassurance cannot reach across the yard - huddle first, then Cocoa barks.");
                return true;
            }
            if (_comfortPrepared)
            {
                _context.SetCue("Comfort is ready - stay huddled and hold steady for the thunder!");
                return true;
            }

            if (dogId == DogId.Cocoa)
            {
                _cocoaReassuranceUntil = _context.Now() + ReassuranceWindowSeconds;
                _context.CreditDog(dogIndex);
                if (_context.DogFeedback[dogIndex] != null) _context.DogFeedback[dogIndex].ShowComfort();
                _context.SetCue("Cocoa gives the steady reassurance bark - Cheddar, answer her now!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "I'M HERE!");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "I'M HERE!", new Color(0.62f, 0.85f, 1f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
                _context.LogEvent("ComfortReassurance", "Cocoa");
                _context.LogObjectiveChanged();
                return true;
            }

            if (_context.Now() > _cocoaReassuranceUntil)
            {
                _context.MarkFailedInteraction(dogId, "Cocoa gives the reassurance bark first");
                _context.SetCue("Cheddar barked bravely, but he needs Cocoa's steady reassurance first.");
                return true;
            }

            _comfortPrepared = true;
            _cocoaReassuranceUntil = 0f;
            _context.CreditDog(dogIndex);
            _context.AddScore(ScoreEventCatalog.StormComfort.Points, ScoreEventCatalog.StormComfort.Label);
            for (int i = 0; i < _context.DogFeedback.Length; i++)
                if (_context.DogFeedback[i] != null) _context.DogFeedback[i].ShowComfort();
            SetStormArt(FinalGameplayArt.ThunderstormComfortHuddle);
            _context.SetActorState(_stormMarker, "COMFORT READY - HOLD THE HUDDLE", new Color(0.52f, 1f, 0.78f), 0.3f);
            _context.SetCue("Cheddar answered Cocoa - comfort ready! Stay together for the clap.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "BRAVE TOGETHER!");
            _context.SpawnWorldPop(_context.Bounds.center + Vector2.up, "BRAVE TOGETHER!", new Color(0.52f, 1f, 0.78f));
            _context.RequestRumble("comfort_ready", 0.12f, 0.28f, 0.1f);
            _context.LogEvent("ComfortPrepared", $"clap {_stormState.ClapsSurvived + 1}");
            _context.LogObjectiveChanged();
            return true;
        }

        public void Cleanup()
        {
            if (_stormMarker != null) _stormMarker.SetActive(false);
        }

        public void StageDogsForEntry()
        {
            if (_context.Dogs == null || _context.Dogs.Length < 2) return;
            Vector2 center = _context.Bounds.center;
            _context.Dogs[0].transform.position = ClampInsideBounds(center + Vector2.left);
            _context.Dogs[1].transform.position = ClampInsideBounds(center + Vector2.right);
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            int partner = dogIndex == 0 ? 1 : 0;
            target = _context.Dogs != null && partner < _context.Dogs.Length && _context.Dogs[partner] != null
                ? _context.Dogs[partner].transform
                : null;
            copy = "COMFORT PARTNER";
            hideDistance = 1.8f;
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("thunderstorm_comfort", score, timeRemaining, _stormState.ClapsSurvived, ClapGoal, _stormState.ExposedClaps,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceThunderclap() => ApplyThunderclap();
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        public void ForceComfortStep(float seconds)
        {
            var pm = _context.PanicMeter;
            if (pm == null || _context.Dogs == null || _context.Dogs.Length < 2) return;
            float before = Mathf.Max(pm.CheddarPanic, pm.CocoaPanic);
            pm.Step(_context.Dogs[0].transform.position, _context.Dogs[1].transform.position, seconds);
            float after = Mathf.Max(pm.CheddarPanic, pm.CocoaPanic);
            if (after < before)
                _context.AddScore(ScoreEventCatalog.StormComfort.Points, ScoreEventCatalog.StormComfort.Label);
        }

        private void ApplyThunderclap()
        {
            if (_cleared || IsFailed) return;
            var pm = _context.PanicMeter;
            if (pm == null) return;
            bool huddling = IsHuddling();
            bool protectedClap = huddling && _comfortPrepared;
            if (!protectedClap) _stormState.RegisterExposedClap();
            pm.AddSpike(DogId.Cheddar, protectedClap ? PreparedCheddarSpike : CheddarSpike);
            pm.AddSpike(DogId.Cocoa, protectedClap ? PreparedCocoaSpike : CocoaSpike);
            _comfortPrepared = false;
            _cocoaReassuranceUntil = 0f;
            SetStormArt(FinalGameplayArt.ThunderstormThunderclap);
            for (int i = 0; i < _context.DogFeedback.Length; i++)
                if (_context.DogFeedback[i] != null) _context.DogFeedback[i].ShowPanic();
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("thunderclap", 0.3f, 0.55f, 0.2f);
            _context.SpawnWorldPop(new Vector2(0f, _context.Bounds.yMax - 2f), "BOOM!", new Color(0.8f, 0.85f, 1f));
            CheckBolt();
            if (pm.Maxed != null) return;

            if (!protectedClap)
            {
                string miss = huddling ? "TOO QUIET!" : "TOO FAR!";
                _context.SetActorState(_stormMarker, "COMFORT MISSED - COCOA BARKS FIRST", new Color(1f, 0.48f, 0.32f), 0.26f);
                _context.SetFeedback(GameManager.FeedbackKind.TugNeedsPartner);
                _context.SetCue(huddling
                    ? "They were close, but the comfort bark was not ready - Cocoa starts, Cheddar answers, then retry!"
                    : "The clap caught them apart - huddle, Cocoa reassure, Cheddar answer, then retry!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, miss);
                _context.SpawnWorldPop(_context.Bounds.center + Vector2.up, miss, new Color(1f, 0.48f, 0.32f));
                _context.LogEvent("ComfortMissed", huddling ? "unprepared" : "apart");
                _context.LogObjectiveChanged();
                return;
            }

            _stormState.SurviveClap();
            if (_context.Dogs != null)
                for (int i = 0; i < _context.Dogs.Length; i++)
                    _context.CreditDog(i);
            _context.AddScore(ScoreEventCatalog.StormWeathered.Points, ScoreEventCatalog.StormWeathered.Label);
            _context.SetFeedback(GameManager.FeedbackKind.PredatorHuddle);
            _context.SetCue($"Cocoa reassured, Cheddar answered - thunderclap weathered! ({_stormState.ClapsSurvived}/{ClapGoal})");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.StormWeathered.Label);
            _context.LogEvent("Thunderclap", $"{_stormState.ClapsSurvived}/{ClapGoal}");

            if (_stormState.ReadyToClear())
            {
                _context.AddScore(ScoreEventCatalog.StormCleared.Points, ScoreEventCatalog.StormCleared.Label);
                _cleared = true;
                _successHoldRemaining = SuccessHoldSeconds;
                SetStormArt(FinalGameplayArt.ThunderstormStormCleared);
                _context.SetActorState(_stormMarker, "STORM PASSED - BRAVE TOGETHER!", new Color(0.55f, 1f, 0.72f), 0.36f);
                _context.SetCue("The storm passed - Cocoa kept steady and Cheddar found his brave bark!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "STORM PASSED!");
                _context.SpawnWorldPop(_context.Bounds.center + Vector2.up * 2f, "STORM PASSED!", new Color(0.75f, 1f, 0.82f));
                _context.RequestRumble("storm_passed", 0.38f, 0.62f, 0.24f);
            }
            else
                _context.SetActorState(_stormMarker, "HUDDLE - COCOA BARKS FIRST", new Color(0.55f, 0.68f, 1f), 0.12f);
        }

        /// <summary>
        /// Fires the funny-failure gag exactly once, on the frame a pup's panic first maxes out.
        /// Without this the bolt was invisible: panic silently crossed 1.0 and the only sign was
        /// the generic end card a beat later.
        /// </summary>
        private void CheckBolt()
        {
            if (_bolted) return;
            var pm = _context.PanicMeter;
            var maxed = pm?.Maxed;
            if (maxed == null) return;

            _bolted = true;
            DogId dog = maxed.Value;
            string name = dog == DogId.Cheddar ? "Cheddar" : "Cocoa";
            int idx = _context.IndexOfDog(dog);
            Vector3 pos = _context.Dogs != null && idx >= 0 && idx < _context.Dogs.Length
                ? _context.Dogs[idx].transform.position
                : Vector3.zero;

            SetStormArt(FinalGameplayArt.ThunderstormThunderclap);
            for (int i = 0; i < _context.DogFeedback.Length; i++)
                if (_context.DogFeedback[i] != null) _context.DogFeedback[i].ShowPanic();
            _context.SetFeedback(GameManager.FeedbackKind.TugNeedsPartner);
            _context.SetCue($"{name} maxed out on panic and bolted for cover - too far apart, too many claps!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, $"{name.ToUpperInvariant()} BOLTED!");
            _context.SpawnWorldPop(pos, $"{name.ToUpperInvariant()} BOLTED!", new Color(1f, 0.35f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("panic_bolt", 0.2f, 0.48f, 0.2f);
            _context.LogEvent("PanicBolt", name);
        }

        private void SetStormArt(string resourcePath)
        {
            MissionPropArt.SetSprite(_stormArt, resourcePath);
        }

        private bool IsHuddling()
        {
            var pm = _context.PanicMeter;
            return pm != null && _context.Dogs != null && _context.Dogs.Length >= 2
                && Vector2.Distance(_context.Dogs[0].transform.position,
                    _context.Dogs[1].transform.position) <= pm.CuddleRadius;
        }

        private DogId DogIdAt(int dogIndex) => dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
            _context.Dogs[dogIndex] != null && _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                ? identity.Id : DogId.Cheddar;

        private Vector2 ClampInsideBounds(Vector2 point)
        {
            const float margin = 1.5f;
            return new Vector2(
                Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
                Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
        }
    }
}
