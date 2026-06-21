using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned territory-control mission. Cheddar and Cocoa claim every yard zone by
    /// standing in it while a controller-owned squirrel prowls toward claimed zones and re-marks
    /// them, forcing the pair to split up and hold the whole yard at once.
    /// </summary>
    public sealed class MarkTheYardMissionController : IMissionController
    {
        private const float ZoneClaimRange = 2.4f;
        private const float ReclaimInitialDelay = 4f;   // matches the legacy ZoneReclaimInterval.
        private const float ReclaimRepeatDelay = 1.5f;
        private const float SquirrelSpeed = 1.9f * 0.8f; // matches SquirrelMoveSpeed * 0.8f.

        private static readonly Color UnclaimedColor = new(0.5f, 0.5f, 0.55f, 0.4f);
        private static readonly Color ClaimedColor = new(0.3f, 0.8f, 0.4f, 0.55f);

        private readonly TerritoryMissionState _state = new();
        private MissionContext _context;
        private Vector2[] _zones;
        private GameObject[] _zoneMarkers;
        private bool[] _zoneClaimed;
        private GameObject _squirrel;
        private float _nextReclaimAt;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.MarkTheYard;
        public bool IsComplete => _state.AllClaimed;
        public TerritoryMissionState State => _state;
        public Vector2 EntryTarget => _zones != null && _zones.Length > 0 ? _zones[_zones.Length - 1] : _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildTerritorySummary(_state);

        public string ObjectiveLabel =>
            $"Claim and hold every zone at once: {_state.Claimed}/{_state.ZoneCount} marked (squirrel steals back {_state.Reclaims})";

        /// <summary>Single source of truth for zone geometry, shared with GameManager's compat accessor.</summary>
        public static Vector2[] ComputeZones(Rect bounds)
        {
            Vector2 P(float x, float y) => new(
                bounds.center.x + x * bounds.width * 0.5f,
                bounds.center.y + y * bounds.height * 0.5f);
            return new[] { P(-0.72f, 0.6f), P(0.72f, 0.6f), P(-0.72f, -0.6f), P(0.72f, -0.6f), P(0f, 0f) };
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            BuildScene();
            Cleanup();
        }

        public void StartMission()
        {
            _zones = ComputeZones(_context.Bounds);
            _state.Configure(_zones.Length);
            _nextReclaimAt = _context.Now() + ReclaimInitialDelay;
            for (int i = 0; i < _zoneMarkers.Length; i++)
            {
                if (_zoneMarkers[i] != null)
                {
                    _zoneMarkers[i].transform.position = _zones[i];
                    _zoneMarkers[i].SetActive(true);
                }
                _zoneClaimed[i] = false;
                SetZoneVisual(i, false);
            }
            if (_squirrel != null)
            {
                _squirrel.transform.position = new Vector2(0f, _context.Bounds.yMax - 2f);
                _squirrel.SetActive(true);
            }
        }

        public void Tick(float deltaTime, float now)
        {
            if (_state.AllClaimed || _context.Dogs == null || _zoneClaimed == null) return;

            for (int d = 0; d < _context.Dogs.Length; d++)
                for (int z = 0; z < _zones.Length; z++)
                    if (!_zoneClaimed[z] && Vector2.Distance(_context.Dogs[d].transform.position, _zones[z]) <= ZoneClaimRange)
                        ClaimZone(d, z);

            // The squirrel prowls toward the nearest claimed zone and re-marks it on arrival, so
            // players can see the threat coming and race to defend rather than getting teleport-sniped.
            if (_squirrel != null && _state.Claimed > 0 && !_state.AllClaimed)
            {
                int target = NearestClaimedZone(_squirrel.transform.position);
                if (target >= 0)
                {
                    _squirrel.transform.position = Vector3.MoveTowards(
                        _squirrel.transform.position, _zones[target], deltaTime * SquirrelSpeed);
                    if (Vector2.Distance(_squirrel.transform.position, _zones[target]) < 1f && now >= _nextReclaimAt)
                    {
                        _nextReclaimAt = now + ReclaimRepeatDelay;
                        ReclaimZone();
                    }
                }
            }
        }

        public bool HandleBark(int dogIndex) => false;

        public void Cleanup()
        {
            if (_zoneMarkers != null)
                foreach (var marker in _zoneMarkers)
                    if (marker != null) marker.SetActive(false);
            if (_squirrel != null) _squirrel.SetActive(false);
        }

        public void StageDogsForEntry()
        {
            Vector2 entry = EntryTarget;
            Vector2 inward = _context.Bounds.center - entry;
            inward = inward.sqrMagnitude < 0.01f ? Vector2.down : inward.normalized;
            Vector2 center = entry + inward * 7f;
            Vector2 side = new Vector2(-inward.y, inward.x) * 1.5f;

            for (int i = 0; i < _context.Dogs.Length; i++)
            {
                Vector2 offset = i % 2 == 0 ? -side : side;
                Vector2 position = ClampInsideBounds(center + offset, 1.5f);
                _context.Dogs[i].transform.position = position;
                if (_context.Dogs[i].TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            }
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = FindNearestUnclaimedZone(_context.Dogs[dogIndex].transform.position);
            copy = "MARK ZONE";
            hideDistance = 1.4f;
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("mark_the_yard", score, timeRemaining, _state.Claimed, _state.ZoneCount, _state.Reclaims,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Deterministic hook: claim the first unclaimed zone as <paramref name="dogId"/>.</summary>
        public void ForceClaimZone(DogId dogId)
        {
            if (_zoneClaimed == null) return;
            for (int z = 0; z < _zoneClaimed.Length; z++)
                if (!_zoneClaimed[z]) { ClaimZone(_context.IndexOfDog(dogId), z); return; }
        }

        /// <summary>Deterministic hook: the squirrel re-marks its nearest claimed zone.</summary>
        public void ForceSquirrelReclaim() => ReclaimZone();

        private void ClaimZone(int dogIndex, int zoneIndex)
        {
            if (zoneIndex < 0 || zoneIndex >= _zoneClaimed.Length || _zoneClaimed[zoneIndex]) return;

            _zoneClaimed[zoneIndex] = true;
            SetZoneVisual(zoneIndex, true);
            _state.Claim();
            _context.CreditDog(dogIndex);
            if (dogIndex >= 0 && dogIndex < _context.DogFeedback.Length && _context.DogFeedback[dogIndex] != null)
                _context.DogFeedback[dogIndex].ShowProudBrief();
            _context.AddScore(ScoreEventCatalog.ZoneClaimed.Points, ScoreEventCatalog.ZoneClaimed.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
            _context.SetCue($"{DogNameFor(dogIndex)} marked a zone! ({_state.Claimed}/{_state.ZoneCount})");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.ZoneClaimed.Label);
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            _context.LogEvent("ZoneClaimed", $"{_state.Claimed}/{_state.ZoneCount}");
            if (_state.AllClaimed) _context.AddScore(ScoreEventCatalog.YardMarked.Points, ScoreEventCatalog.YardMarked.Label);
            else _context.LogObjectiveChanged();
        }

        private void ReclaimZone()
        {
            int target = NearestClaimedZone(_squirrel != null ? (Vector2)_squirrel.transform.position : Vector2.zero);
            if (target < 0) return;

            _zoneClaimed[target] = false;
            SetZoneVisual(target, false);
            _state.Unclaim();
            _context.AddScore(ScoreEventCatalog.ZoneStolen.Points, ScoreEventCatalog.ZoneStolen.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStealing);
            _context.SetCue($"The squirrel re-marked a zone! ({_state.Claimed}/{_state.ZoneCount}) Go reclaim it.");
            if (_squirrel != null) _squirrel.transform.position = _zones[target];
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, ScoreEventCatalog.ZoneStolen.Label);
            _context.LogEvent("ZoneStolen", $"{_state.Claimed}/{_state.ZoneCount}");
            _context.LogObjectiveChanged();
        }

        private int NearestClaimedZone(Vector2 from)
        {
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < _zoneClaimed.Length; i++)
            {
                if (!_zoneClaimed[i]) continue;
                float distance = Vector2.Distance(from, _zones[i]);
                if (distance < bestDistance) { bestDistance = distance; best = i; }
            }
            return best;
        }

        private Transform FindNearestUnclaimedZone(Vector2 position)
        {
            Transform nearest = null;
            float nearestDistance = float.PositiveInfinity;
            if (_zoneMarkers == null || _zoneClaimed == null) return null;
            for (int i = 0; i < _zoneMarkers.Length && i < _zoneClaimed.Length; i++)
            {
                if (_zoneClaimed[i] || _zoneMarkers[i] == null || !_zoneMarkers[i].activeSelf) continue;
                float distance = Vector2.Distance(position, _zoneMarkers[i].transform.position);
                if (distance >= nearestDistance) continue;
                nearest = _zoneMarkers[i].transform;
                nearestDistance = distance;
            }
            return nearest;
        }

        private void BuildScene()
        {
            _zones = ComputeZones(_context.Bounds);
            _zoneMarkers = new GameObject[_zones.Length];
            _zoneClaimed = new bool[_zones.Length];
            for (int i = 0; i < _zones.Length; i++)
            {
                var go = new GameObject($"TerritoryZone_{i}");
                go.transform.position = _zones[i];
                go.transform.localScale = Vector3.one * (ZoneClaimRange * 1.5f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _context.ActorSprite;
                sr.color = UnclaimedColor;
                sr.sortingOrder = 1;
                _context.AddWorldLabel(go, "CLAIM", Vector3.up * 0.9f, 13, Color.white);
                go.SetActive(false);
                _zoneMarkers[i] = go;
            }

            _squirrel = new GameObject("MarkTheYardSquirrel");
            var squirrelRenderer = _squirrel.AddComponent<SpriteRenderer>();
            squirrelRenderer.sprite = _context.ActorSprite;
            squirrelRenderer.color = new Color(0.55f, 0.32f, 0.12f);
            squirrelRenderer.sortingOrder = 6;
            _squirrel.transform.localScale = Vector3.one * 0.9f;
            _context.AddWorldLabel(_squirrel, "SQUIRREL - WILL RE-MARK YOUR ZONES!", Vector3.up * 0.6f, 12, Color.white);
            _squirrel.SetActive(false);
        }

        private void SetZoneVisual(int i, bool claimed)
        {
            if (_zoneMarkers[i] == null) return;
            var sr = _zoneMarkers[i].GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = claimed ? ClaimedColor : UnclaimedColor;
        }

        private Vector2 ClampInsideBounds(Vector2 point, float margin) => new(
            Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
            Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));

        private string DogNameFor(int dogIndex)
        {
            if (dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length || _context.Dogs[dogIndex] == null)
                return "A dog";
            return _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                ? identity.Id.ToString()
                : _context.Dogs[dogIndex].name;
        }
    }
}
