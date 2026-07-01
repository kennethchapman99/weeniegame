using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class LeashWalkMissionController : IMissionController
    {
        private const float MaxLeash = 7f;
        private const float CheckpointRange = 2.6f;
        private const int MaxSnaps = 4;
        private const float SnapCooldown = 1.5f;

        private readonly LeashWalkMissionState _state = new();
        private MissionContext _context;
        private Vector2[] _checkpoints;
        private GameObject[] _markers;
        private float _nextSnapAt;
        private bool _cleared;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.LeashWalk;
        public bool IsComplete => _cleared;
        public bool IsFailed => _state.TooManySnaps(MaxSnaps);
        public string FailReason => _state.TooManySnaps(MaxSnaps)
            ? "The leash snapped taut too many times - the walk fell apart."
            : null;
        public LeashWalkMissionState State => _state;
        public Vector2[] Checkpoints => _checkpoints != null ? (Vector2[])_checkpoints.Clone() : new Vector2[0];
        public Vector2 EntryTarget => _checkpoints != null && _checkpoints.Length > 0 ? _checkpoints[0] : Vector2.zero;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildLeashSummary(_state);

        public string ObjectiveLabel => _cleared
            ? "Walk complete!"
            : $"Walk the leash together to checkpoint {Mathf.Min(_state.Reached + 1, _state.RequiredCheckpoints)}/{_state.RequiredCheckpoints} - stay close (snaps {_state.Snaps}/{MaxSnaps})";

        public void Initialize(MissionContext context)
        {
            _context = context;
            _checkpoints = ComputeCheckpoints(_context.Bounds);
            BuildMarkers();
        }

        public void StartMission()
        {
            _state.Configure(_checkpoints.Length);
            _nextSnapAt = 0f;
            _cleared = false;
            for (int i = 0; i < _markers.Length; i++) SetMarkerArt(i, FinalGameplayArt.LeashWalkCheckpointWaiting);
            SetMarkersActive(true);
        }

        public void Tick(float deltaTime, float now)
        {
            if (_cleared || IsFailed || _context.Dogs == null || _context.Dogs.Length < 2) return;

            if (Vector2.Distance(_context.Dogs[0].transform.position, _context.Dogs[1].transform.position) > MaxLeash
                && now >= _nextSnapAt)
            {
                _nextSnapAt = now + SnapCooldown;
                RegisterSnap();
                if (IsFailed) return;
            }

            int idx = _state.CheckpointIndex;
            if (idx >= 0 && idx < _checkpoints.Length)
            {
                bool aOn = Vector2.Distance(_context.Dogs[0].transform.position, _checkpoints[idx]) <= CheckpointRange;
                bool bOn = Vector2.Distance(_context.Dogs[1].transform.position, _checkpoints[idx]) <= CheckpointRange;
                if (aOn && bOn) RegisterCheckpointReached();
            }
        }

        public bool HandleBark(int dogIndex) => false;

        public void Cleanup() => SetMarkersActive(false);

        public void StageDogsForEntry()
        {
            if (_context.Dogs == null || _context.Dogs.Length < 2) return;
            Vector2 entry = EntryTarget;
            Vector2 inward = _context.Bounds.center - entry;
            inward = inward.sqrMagnitude < 0.01f ? Vector2.down : inward.normalized;
            Vector2 center = entry + inward * 7f;
            Vector2 side = new Vector2(-inward.y, inward.x) * 1.5f;
            _context.Dogs[0].transform.position = center - side;
            _context.Dogs[1].transform.position = center + side;
            foreach (var dog in _context.Dogs)
                if (dog.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            int idx = _state.CheckpointIndex;
            target = _markers != null && idx >= 0 && idx < _markers.Length && _markers[idx] != null
                ? _markers[idx].transform : null;
            copy = "WALK TOGETHER";
            hideDistance = CheckpointRange;
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("leash_walk", score, timeRemaining, _state.Reached, _state.RequiredCheckpoints, _state.Snaps,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceReachCheckpoint() => RegisterCheckpointReached();
        public void ForceLeashSnap() => RegisterSnap();

        private void RegisterCheckpointReached()
        {
            if (_state.ReadyToClear()) return;
            int idx = _state.CheckpointIndex;
            _state.ReachCheckpoint();
            if (_markers != null && idx >= 0 && idx < _markers.Length && _markers[idx] != null)
            {
                SetMarkerArt(idx, FinalGameplayArt.LeashWalkCheckpointReached);
                _markers[idx].SetActive(false);
            }

            _context.AddScore(ScoreEventCatalog.CheckpointReached.Points, ScoreEventCatalog.CheckpointReached.Label);
            _context.SetFeedback(GameManager.FeedbackKind.UnitedBark);
            _context.SetCue($"Checkpoint reached together! ({_state.Reached}/{_state.RequiredCheckpoints}) Stay close.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.CheckpointReached.Label);
            if (idx >= 0 && idx < _checkpoints.Length)
                _context.SpawnWorldPop(_checkpoints[idx], "CHECKPOINT!", new Color(0.6f, 0.8f, 1f));
            foreach (var fb in _context.DogFeedback)
                if (fb != null) fb.ShowProudBrief();
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("checkpoint", 0.2f, 0.4f, 0.14f);
            _context.LogEvent("Checkpoint", $"{_state.Reached}/{_state.RequiredCheckpoints}");

            if (_state.ReadyToClear())
            {
                _context.AddScore(ScoreEventCatalog.WalkComplete.Points, ScoreEventCatalog.WalkComplete.Label);
                _cleared = true;
            }
            else
            {
                _context.LogObjectiveChanged();
            }
        }

        private void RegisterSnap()
        {
            _state.Snap();
            int idx = _state.CheckpointIndex;
            if (idx >= 0 && idx < _markers.Length) SetMarkerArt(idx, FinalGameplayArt.LeashWalkSnapWarning);
            _context.AddScore(ScoreEventCatalog.LeashSnap.Points, ScoreEventCatalog.LeashSnap.Label);
            _context.SetFeedback(GameManager.FeedbackKind.TugNeedsPartner);
            _context.SetCue($"The leash snapped taut - too far apart! ({_state.Snaps}/{MaxSnaps})");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, ScoreEventCatalog.LeashSnap.Label);
            Vector3 mid = _context.Dogs != null && _context.Dogs.Length >= 2
                ? (_context.Dogs[0].transform.position + _context.Dogs[1].transform.position) * 0.5f
                : Vector3.zero;
            _context.SpawnWorldPop(mid, "LEASH SNAP!", new Color(1f, 0.42f, 0.24f));
            foreach (var fb in _context.DogFeedback)
                if (fb != null) fb.ShowPanic();
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("leash_snap", 0.18f, 0.4f, 0.14f);
            _context.LogEvent("LeashSnap", $"{_state.Snaps}/{MaxSnaps}");
            if (!IsFailed) _context.LogObjectiveChanged();
        }

        private void BuildMarkers()
        {
            _markers = new GameObject[_checkpoints.Length];
            for (int i = 0; i < _checkpoints.Length; i++)
            {
                var go = new GameObject($"LeashCheckpoint_{i}");
                go.transform.position = _checkpoints[i];
                go.transform.localScale = Vector3.one * (CheckpointRange * 1.4f);
                var sr = go.AddComponent<SpriteRenderer>();
                if (_context.ActorSprite != null) sr.sprite = _context.ActorSprite;
                sr.color = new Color(0.45f, 0.6f, 0.85f, 0.4f);
                sr.sortingOrder = 1;
                _context.AddWorldLabel(go, "CHECKPOINT", Vector3.up * 0.9f, 13, Color.white);
                MissionPropArt.AttachPad(go, FinalGameplayArt.LeashWalkCheckpointWaiting, 0.012f, 18);
                go.SetActive(false);
                _markers[i] = go;
            }
        }

        private void SetMarkerArt(int index, string resourcePath)
        {
            if (_markers == null || index < 0 || index >= _markers.Length || _markers[index] == null) return;
            MissionPropArt.SetSprite(_markers[index].GetComponent<MissionPropArtAttachment>(), resourcePath);
        }

        private void SetMarkersActive(bool active)
        {
            if (_markers == null) return;
            for (int i = 0; i < _markers.Length; i++)
                if (_markers[i] != null)
                    _markers[i].SetActive(active && i >= _state.CheckpointIndex);
        }

        /// <summary>Single source of truth for checkpoint geometry, shared with GameManager's compat accessor.</summary>
        public static Vector2[] ComputeCheckpoints(Rect bounds)
        {
            Vector2 P(float x, float y) => new(
                bounds.center.x + x * bounds.width * 0.5f,
                bounds.center.y + y * bounds.height * 0.5f);
            return new[] { P(-0.72f, -0.62f), P(0.72f, -0.62f), P(0.72f, 0.62f), P(-0.72f, 0.62f) };
        }
    }
}
