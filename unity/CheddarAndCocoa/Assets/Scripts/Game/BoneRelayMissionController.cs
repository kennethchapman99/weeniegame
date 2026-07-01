using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class BoneRelayMissionController : IMissionController
    {
        private const float ScentRange = 3.5f;
        private const float DigRange = 3f;
        private const int FindsNeeded = 3;
        private const int MaxWasted = 5;
        private static readonly Vector2 ScentZonePos = new(0f, 9f);
        private static readonly Vector2[] MoundSpots = { new(-12f, -6f), new(12f, -6f), new(-12f, 6f), new(12f, 6f) };
        private static readonly Color MoundCallColor = new(0.5f, 0.9f, 0.55f);
        private static readonly Color MoundIdleColor = new(0.42f, 0.3f, 0.16f);

        private readonly CoopScentRelayPuzzle _puzzle = new();
        private MissionContext _context;
        private int _seed;
        private GameObject _scentPost;
        private TextMesh _scentPostLabel;
        private GameObject[] _mounds;
        private TextMesh[] _moundLabels;
        private int _diggerInside = -1;
        private int _findsSeen;
        private int _blindSeen;
        private int _wrongSeen;
        private bool _failed;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.BoneRelay;
        public bool IsComplete => _puzzle.Solved;
        public bool IsFailed => _failed;
        public string FailReason => _failed ? "The dogs dug up half the yard guessing instead of waiting for Cocoa's call." : null;
        public CoopScentRelayPuzzle Puzzle => _puzzle;
        public int MoundCount => MoundSpots.Length;
        public Vector2 ScentZone => ScentZonePos;
        public Vector2 MoundSpot(int index) => index >= 0 && index < MoundSpots.Length ? MoundSpots[index] : Vector2.zero;
        public Vector2 EntryTarget => ScentZonePos;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildBoneRelaySummary(_puzzle);

        public string ObjectiveLabel
        {
            get
            {
                int wasted = _puzzle.BlindActs + _puzzle.WrongDigs;
                if (!_puzzle.Known)
                    return $"Cocoa: sniff the scent post to call the real mound - Cheddar, wait for it! (bones {_puzzle.Finds}/{FindsNeeded}, wasted {wasted}/{MaxWasted})";
                return $"Cheddar: dig the glowing mound Cocoa called! (bones {_puzzle.Finds}/{FindsNeeded}, wasted {wasted}/{MaxWasted})";
            }
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _seed = context.Random().Next();
            BuildScene();
            Cleanup();
        }

        public void StartMission()
        {
            _seed = _context.Random().Next();
            _puzzle.Configure(MoundSpots.Length, FindsNeeded, _seed);
            _diggerInside = -1;
            _findsSeen = 0;
            _blindSeen = 0;
            _wrongSeen = 0;
            _failed = false;
            SetSceneActive(true);
            UpdateMoundVisuals();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Solved || _failed || _mounds == null) return;

            int reader = _context.IndexOfDog(DogId.Cocoa);
            int digger = _context.IndexOfDog(DogId.Cheddar);
            if (reader < 0 || digger < 0) return;

            if (Vector2.Distance(_context.Dogs[reader].transform.position, ScentZonePos) <= ScentRange)
                _puzzle.Reveal();

            int inside = -1;
            for (int i = 0; i < _mounds.Length; i++)
            {
                if (_mounds[i] == null || !_mounds[i].activeSelf) continue;
                if (Vector2.Distance(_context.Dogs[digger].transform.position, _mounds[i].transform.position) <= DigRange)
                { inside = i; break; }
            }
            if (inside >= 0 && inside != _diggerInside) _puzzle.ActOn(inside);
            _diggerInside = inside;

            HandleProgress();
            if (_failed) return;
            UpdateMoundVisuals();
        }

        public bool HandleBark(int dogIndex) => false;

        public void Cleanup() => SetSceneActive(false);

        public void StageDogsForEntry()
        {
            int reader = _context.IndexOfDog(DogId.Cocoa);
            int digger = _context.IndexOfDog(DogId.Cheddar);
            // 5 units away — outside ScentRange (3.5f) so the arrow shows and auto-reveal is suppressed
            if (reader >= 0) _context.Dogs[reader].transform.position = ClampInsideBounds(ScentZonePos + Vector2.left * 5f);
            if (digger >= 0) _context.Dogs[digger].transform.position = ClampInsideBounds(ScentZonePos + Vector2.right * 5f);
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            if (_context.IndexOfDog(DogId.Cocoa) == dogIndex)
            {
                target = _scentPost != null ? _scentPost.transform : null;
                copy = "SNIFF SCENT";
                hideDistance = ScentRange;
            }
            else
            {
                int call = _puzzle.RevealedTarget;
                if (call >= 0 && _mounds != null && call < _mounds.Length && _mounds[call] != null)
                    target = _mounds[call].transform;
                else
                    target = FindNearestActiveMound(dogIndex);
                copy = call >= 0 ? "DIG THE CALL" : "WAIT FOR CALL";
                hideDistance = DigRange;
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("bone_relay", score, timeRemaining, _puzzle.Finds, FindsNeeded, _puzzle.BlindActs + _puzzle.WrongDigs,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceBoneReveal()
        {
            _puzzle.Reveal();
            UpdateMoundVisuals();
        }

        public void ForceBoneDig(int target)
        {
            _puzzle.ActOn(target);
            HandleProgress();
            if (!_failed) UpdateMoundVisuals();
        }

        private void HandleProgress()
        {
            int digger = _context.IndexOfDog(DogId.Cheddar);
            Vector2 digPos = digger >= 0 ? (Vector2)_context.Dogs[digger].transform.position : ScentZonePos;

            if (_puzzle.Finds > _findsSeen)
            {
                _findsSeen = _puzzle.Finds;
                _context.AddScore(ScoreEventCatalog.BoneFound.Points, ScoreEventCatalog.BoneFound.Label);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue($"Cocoa called it, Cheddar dug it up! ({_puzzle.Finds}/{FindsNeeded})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "BONE!");
                _context.SpawnWorldPop(digPos, "BONE!", new Color(0.5f, 0.9f, 0.55f));
                _context.LogEvent("BoneFound", $"{_puzzle.Finds}/{FindsNeeded}");
            }

            bool wasted = false;
            if (_puzzle.BlindActs > _blindSeen)
            {
                _blindSeen = _puzzle.BlindActs;
                wasted = true;
                _context.SetCue("Cheddar dug blind - wait for Cocoa's call!");
            }
            if (_puzzle.WrongDigs > _wrongSeen)
            {
                _wrongSeen = _puzzle.WrongDigs;
                wasted = true;
                _context.SetCue("Wrong mound - that one's a decoy.");
            }
            if (wasted)
            {
                _context.AddScore(ScoreEventCatalog.ColdDig.Points, ScoreEventCatalog.ColdDig.Label);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "NOPE!");
                _context.SpawnWorldPop(digPos, "NOPE!", new Color(0.85f, 0.5f, 0.3f));
                int totalWasted = _puzzle.BlindActs + _puzzle.WrongDigs;
                _context.LogEvent("BoneWaste", $"{totalWasted}/{MaxWasted}");
                if (totalWasted >= MaxWasted) _failed = true;
            }
        }

        private void UpdateMoundVisuals()
        {
            if (_mounds == null) return;
            int call = _puzzle.RevealedTarget;
            for (int i = 0; i < _mounds.Length; i++)
            {
                if (_mounds[i] == null) continue;
                bool isCall = i == call;
                if (_mounds[i].TryGetComponent<SpriteRenderer>(out var sr))
                    sr.color = isCall ? MoundCallColor : MoundIdleColor;
                if (_moundLabels != null && _moundLabels[i] != null)
                    _moundLabels[i].text = isCall ? "DIG HERE!" : "DIG?";
            }
            if (_scentPostLabel != null)
                _scentPostLabel.text = _puzzle.Known ? "SCENT POST - SHE'S CALLING IT!" : "SCENT POST - COCOA SNIFF HERE";
            if (_scentPost != null && _scentPost.TryGetComponent<SpriteRenderer>(out var psr))
                psr.color = _puzzle.Known ? MoundCallColor : new Color(0.7f, 0.6f, 0.95f);
        }

        private void BuildScene()
        {
            _mounds = new GameObject[MoundSpots.Length];
            _moundLabels = new TextMesh[MoundSpots.Length];
            for (int i = 0; i < MoundSpots.Length; i++)
            {
                var go = new GameObject($"BoneMound_{i}");
                go.transform.position = MoundSpots[i];
                go.transform.localScale = new Vector3(1.8f, 1.1f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                if (_context.ActorSprite != null) sr.sprite = _context.ActorSprite;
                sr.color = MoundIdleColor;
                _moundLabels[i] = _context.AddWorldLabel(go, "DIG?", Vector3.up * 1.2f, 13, Color.white);
                MissionPropArt.AttachObject(go, FinalGameplayArt.MissionBoneMound, 0.012f, 18, true);
                go.SetActive(false);
                _mounds[i] = go;
            }

            _scentPost = new GameObject("ScentPost");
            _scentPost.transform.position = ScentZonePos;
            _scentPost.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            var psr = _scentPost.AddComponent<SpriteRenderer>();
            if (_context.ActorSprite != null) psr.sprite = _context.ActorSprite;
            psr.color = new Color(0.7f, 0.6f, 0.95f);
            _scentPostLabel = _context.AddWorldLabel(_scentPost, "SCENT POST - COCOA SNIFF HERE", Vector3.up * 1.5f, 11, Color.white);
            MissionPropArt.AttachObject(_scentPost, FinalGameplayArt.MissionScentPost, 0.012f, 18, true);
            _scentPost.SetActive(false);
        }

        private void SetSceneActive(bool active)
        {
            if (_scentPost != null) _scentPost.SetActive(active);
            if (_mounds == null) return;
            foreach (var m in _mounds)
                if (m != null) m.SetActive(active);
        }

        private Transform FindNearestActiveMound(int dogIndex)
        {
            if (_mounds == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length || _context.Dogs[dogIndex] == null)
                return null;
            Vector2 pos = _context.Dogs[dogIndex].transform.position;
            Transform best = null;
            float bestDist = float.PositiveInfinity;
            foreach (var m in _mounds)
            {
                if (m == null || !m.activeSelf) continue;
                float d = Vector2.Distance(pos, m.transform.position);
                if (d < bestDist) { bestDist = d; best = m.transform; }
            }
            return best;
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
