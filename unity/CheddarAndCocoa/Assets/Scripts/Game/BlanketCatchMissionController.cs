using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class BlanketCatchMissionController : IMissionController
    {
        private const float MinSeparation = 4f;
        private const float MaxSeparation = 11f;
        private const float CatchTolerance = 2.5f;
        private const int CatchesNeeded = 5;
        private const int MaxRips = 3;
        private const float CatchLineY = -6f;
        private const float SpawnY = 11f;
        private const float FallSpeed = 7f;

        private readonly CoopStretchSpanPuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _blanketObj;
        private TextMesh _blanketLabel;
        private GameObject _fallingItem;
        private float _itemX;
        private float _itemY;
        private int _caughtSeen;
        private int _missedSeen;
        private int _ripsSeen;
        private bool _failed;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.BlanketCatch;
        public bool IsComplete => _puzzle.Solved;
        public bool IsFailed => _failed;
        public string FailReason => _failed ? "The blanket tore to shreds - too many over-stretches and there was nothing left to catch with." : null;
        public CoopStretchSpanPuzzle Puzzle => _puzzle;
        public float CatchY => CatchLineY;
        public Vector2 EntryTarget => new(0f, CatchLineY);
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildBlanketCatchSummary(_puzzle);

        public string ObjectiveLabel
        {
            get
            {
                string span = _puzzle.Taut ? "taut - slide the middle under the snack"
                    : _puzzle.Overstretched ? "too far apart - close up before it rips!"
                    : "too close - spread out to pull it taut";
                return $"Hold the blanket {span} (caught {_puzzle.Caught}/{CatchesNeeded}, rips {_puzzle.Rips}/{MaxRips})";
            }
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            BuildScene();
            Cleanup();
        }

        public void StartMission()
        {
            _puzzle.Configure(MinSeparation, MaxSeparation, CatchTolerance, CatchesNeeded, MaxRips);
            _caughtSeen = 0;
            _missedSeen = 0;
            _ripsSeen = 0;
            _failed = false;
            SetSceneActive(true);
            SpawnItem();
            UpdateVisuals();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Solved || _failed) return;

            int a = _context.IndexOfDog(DogId.Cheddar);
            int b = _context.IndexOfDog(DogId.Cocoa);
            if (a >= 0 && b >= 0)
            {
                Vector2 pa = _context.Dogs[a].transform.position;
                Vector2 pb = _context.Dogs[b].transform.position;
                _puzzle.UpdateSpan(Vector2.Distance(pa, pb), (pa.x + pb.x) * 0.5f);
                HandleRips();
                if (_failed) return;
            }

            _itemY -= FallSpeed * deltaTime;
            if (_itemY <= CatchLineY)
            {
                _puzzle.TryCatch(_itemX);
                HandleProgress();
                if (_failed || _puzzle.Solved) return;
                SpawnItem();
            }
            UpdateVisuals();
        }

        public bool HandleBark(int dogIndex) => false;

        public void Cleanup() => SetSceneActive(false);

        public void StageDogsForEntry()
        {
            int a = _context.IndexOfDog(DogId.Cheddar);
            int b = _context.IndexOfDog(DogId.Cocoa);
            if (a >= 0) _context.Dogs[a].transform.position = ClampInsideBounds(new Vector2(-4f, CatchLineY));
            if (b >= 0) _context.Dogs[b].transform.position = ClampInsideBounds(new Vector2(4f, CatchLineY));
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = _fallingItem != null && _fallingItem.activeSelf ? _fallingItem.transform : null;
            copy = "GET UNDER SNACK";
            hideDistance = CatchTolerance;
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("blanket_catch", score, timeRemaining, _puzzle.Caught, CatchesNeeded, _puzzle.Rips + _puzzle.Missed,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceBlanketSpan(float separation, float midpointX)
        {
            _puzzle.UpdateSpan(separation, midpointX);
            HandleRips();
            if (!_failed) UpdateVisuals();
        }

        public void ForceBlanketCatch(float itemX)
        {
            _itemX = itemX;
            _puzzle.TryCatch(itemX);
            HandleProgress();
            if (!_failed && !_puzzle.Solved) UpdateVisuals();
        }

        private void HandleRips()
        {
            if (_puzzle.Rips <= _ripsSeen) return;
            _ripsSeen = _puzzle.Rips;
            _context.AddScore(ScoreEventCatalog.WeenieDropped.Points, "BLANKET RIP");
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue($"Over-stretched! The blanket ripped. ({_puzzle.Rips}/{MaxRips})");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "RIP!");
            _context.LogEvent("BlanketRip", $"{_puzzle.Rips}/{MaxRips}");
            if (_puzzle.TooManyRips) _failed = true;
        }

        private void HandleProgress()
        {
            if (_puzzle.Caught > _caughtSeen)
            {
                _caughtSeen = _puzzle.Caught;
                _context.AddScore(ScoreEventCatalog.WeenieDelivered.Points, "SNACK CAUGHT");
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue($"Nice catch! The blanket snagged the snack. ({_puzzle.Caught}/{CatchesNeeded})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "CAUGHT!");
                _context.SpawnWorldPop(new Vector2(_itemX, CatchLineY), "CAUGHT!", new Color(0.5f, 0.95f, 0.55f));
                _context.LogEvent("BlanketCatch", $"{_puzzle.Caught}/{CatchesNeeded}");
            }
            if (_puzzle.Missed > _missedSeen)
            {
                _missedSeen = _puzzle.Missed;
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                string cue = _puzzle.Slack ? "Too close - the blanket sagged and the snack bounced off."
                    : _puzzle.Overstretched ? "Blanket's torn - close the gap to fix the span."
                    : "Missed - get the middle of the blanket under the snack.";
                _context.SetCue(cue);
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "MISSED!");
                _context.SpawnWorldPop(new Vector2(_itemX, CatchLineY), "SPLAT!", new Color(0.85f, 0.5f, 0.3f));
                _context.LogEvent("BlanketMiss", $"{_puzzle.Missed}");
            }
        }

        private void SpawnItem()
        {
            var rng = _context.Random();
            _itemX = Mathf.Lerp(_context.Bounds.xMin + 3f, _context.Bounds.xMax - 3f, (float)rng.NextDouble());
            _itemY = SpawnY;
            if (_fallingItem != null)
            {
                _fallingItem.SetActive(true);
                _fallingItem.transform.position = new Vector3(_itemX, _itemY, 0f);
            }
        }

        private void UpdateVisuals()
        {
            if (_fallingItem != null)
                _fallingItem.transform.position = new Vector3(_itemX, _itemY, 0f);
            if (_blanketObj == null) return;
            Color tint = _puzzle.Taut ? new Color(0.45f, 0.85f, 0.55f)
                : _puzzle.Overstretched ? new Color(0.9f, 0.35f, 0.25f)
                : new Color(0.85f, 0.8f, 0.35f);
            _blanketObj.transform.position = new Vector3(_puzzle.MidpointX, CatchLineY, 0f);
            float width = Mathf.Clamp(_puzzle.Separation, 1f, MaxSeparation + 2f);
            _blanketObj.transform.localScale = new Vector3(width, 0.5f, 1f);
            if (_blanketObj.TryGetComponent<SpriteRenderer>(out var sr)) sr.color = tint;
            if (_blanketLabel != null)
            {
                _blanketLabel.text = _puzzle.Taut ? $"BLANKET TAUT - CATCH! ({_puzzle.Caught}/{CatchesNeeded})"
                    : _puzzle.Overstretched ? "TOO FAR - RIPPING!" : "TOO CLOSE - SAGGING";
                _blanketLabel.transform.localScale = new Vector3(0.08f / Mathf.Max(width, 0.01f), 0.16f, 1f);
            }
        }

        private void BuildScene()
        {
            _blanketObj = new GameObject("CatchBlanket");
            _blanketObj.transform.position = new Vector3(0f, CatchLineY, 0f);
            _blanketObj.transform.localScale = new Vector3(MinSeparation, 0.5f, 1f);
            var bsr = _blanketObj.AddComponent<SpriteRenderer>();
            if (_context.ActorSprite != null) bsr.sprite = _context.ActorSprite;
            bsr.color = new Color(0.85f, 0.8f, 0.35f);
            _blanketLabel = _context.AddWorldLabel(_blanketObj, "BLANKET", Vector3.up * 1.6f, 12, Color.white);
            MissionPropArt.AttachObject(_blanketObj, FinalGameplayArt.MissionCatchBlanket, 0.012f, 18, false);
            _blanketObj.SetActive(false);

            _fallingItem = new GameObject("FallingSnack");
            _fallingItem.transform.position = new Vector3(0f, SpawnY, 0f);
            _fallingItem.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var isr = _fallingItem.AddComponent<SpriteRenderer>();
            if (_context.ActorSprite != null) isr.sprite = _context.ActorSprite;
            isr.color = new Color(0.95f, 0.8f, 0.4f);
            _context.AddWorldLabel(_fallingItem, "SNACK", Vector3.up * 1.1f, 11, Color.white);
            MissionPropArt.AttachObject(_fallingItem, FinalGameplayArt.MissionFallingSnack, 0.012f, 18, true);
            _fallingItem.SetActive(false);
        }

        private void SetSceneActive(bool active)
        {
            if (_blanketObj != null) _blanketObj.SetActive(active);
            if (_fallingItem != null) _fallingItem.SetActive(active && !_puzzle.Solved);
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
