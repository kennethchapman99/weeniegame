using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class ThunderstormComfortMissionController : IMissionController
    {
        private const int ClapGoal = 5;
        private const float ClapInterval = 5.5f;
        private const float CheddarSpike = 0.26f;
        private const float CocoaSpike = 0.16f;

        private readonly ThunderstormMissionState _stormState = new();
        private MissionContext _context;
        private float _nextClapAt;
        private bool _cleared;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.ThunderstormComfort;
        public bool IsComplete => _cleared;
        public bool IsFailed => _context?.PanicMeter?.Maxed != null;
        public string FailReason => _context?.PanicMeter?.Maxed != null
            ? $"{_context.PanicMeter.Maxed} panicked at the thunder and bolted before the storm passed."
            : null;
        public ThunderstormMissionState StormState => _stormState;
        public Vector2 EntryTarget => _context != null ? _context.Bounds.center : Vector2.zero;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildThunderstormSummary(_stormState);

        public string ObjectiveLabel
        {
            get
            {
                int panicPct = _context?.PanicMeter != null
                    ? Mathf.RoundToInt(Mathf.Max(_context.PanicMeter.CheddarPanic, _context.PanicMeter.CocoaPanic) * 100f)
                    : 0;
                return $"Huddle to stay calm and ride out the storm: claps {_stormState.ClapsSurvived}/{ClapGoal}, panic {panicPct}%";
            }
        }

        public void Initialize(MissionContext context) => _context = context;

        public void StartMission()
        {
            _stormState.Configure(ClapGoal);
            _context.PanicMeter?.ResetMeter();
            _cleared = false;
            _nextClapAt = _context.Now() + ClapInterval;
        }

        public void Tick(float deltaTime, float now)
        {
            if (_cleared || IsFailed) return;
            var pm = _context.PanicMeter;
            if (pm == null || _context.Dogs == null || _context.Dogs.Length < 2) return;

            pm.Step(_context.Dogs[0].transform.position, _context.Dogs[1].transform.position, deltaTime);
            if (Vector2.Distance(_context.Dogs[0].transform.position, _context.Dogs[1].transform.position) <= pm.CuddleRadius)
                for (int i = 0; i < _context.DogFeedback.Length; i++)
                    if (_context.DogFeedback[i] != null) _context.DogFeedback[i].ShowComfort();
            if (pm.Maxed != null) return;

            if (now >= _nextClapAt)
            {
                _nextClapAt = now + ClapInterval;
                ApplyThunderclap();
            }
        }

        public bool HandleBark(int dogIndex) => false;

        public void Cleanup() { }

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
            new("thunderstorm_comfort", score, timeRemaining, _stormState.ClapsSurvived, ClapGoal, 0,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceThunderclap() => ApplyThunderclap();

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
            var pm = _context.PanicMeter;
            if (pm == null) return;
            pm.AddSpike(DogId.Cheddar, CheddarSpike);
            pm.AddSpike(DogId.Cocoa, CocoaSpike);
            for (int i = 0; i < _context.DogFeedback.Length; i++)
                if (_context.DogFeedback[i] != null) _context.DogFeedback[i].ShowPanic();
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("thunderclap", 0.3f, 0.55f, 0.2f);
            _context.SpawnWorldPop(new Vector2(0f, _context.Bounds.yMax - 2f), "BOOM!", new Color(0.8f, 0.85f, 1f));
            if (pm.Maxed != null) return;

            _stormState.SurviveClap();
            _context.AddScore(ScoreEventCatalog.StormWeathered.Points, ScoreEventCatalog.StormWeathered.Label);
            _context.SetFeedback(GameManager.FeedbackKind.PredatorHuddle);
            _context.SetCue($"Thunderclap weathered! ({_stormState.ClapsSurvived}/{ClapGoal}) Keep huddling.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.StormWeathered.Label);
            _context.LogEvent("Thunderclap", $"{_stormState.ClapsSurvived}/{ClapGoal}");

            if (_stormState.ReadyToClear())
            {
                _context.AddScore(ScoreEventCatalog.StormCleared.Points, ScoreEventCatalog.StormCleared.Label);
                _cleared = true;
            }
        }

        private Vector2 ClampInsideBounds(Vector2 point)
        {
            const float margin = 1.5f;
            return new Vector2(
                Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
                Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
        }
    }
}
