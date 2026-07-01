using System.Collections.Generic;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class SnackHeistMissionController : IMissionController, IMissionTreatCollector
    {
        private MissionContext _context;
        private Treat _squirrelTarget;
        private GameObject _guardLane;
        private MissionPropArtAttachment _guardLaneArt;
        private float _squirrelTimer;
        private float _scaredUntil;
        private float _nextScareScoreAt;
        private bool _squirrelHasStarted;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.SnackHeist;
        public bool IsComplete => Recovered >= _context.ObjectiveGoal;
        public bool IsFailed => Stolen >= _context.MaxStolenFood;
        public string FailReason => IsFailed ? "The squirrel union escaped with too many forbidden snacks." : null;
        public string OutcomeSummary => null;
        public string ObjectiveLabel => _squirrelTarget != null
            ? "Bark-guard the snack thief"
            : $"Stash snacks {Recovered}/{_context.ObjectiveGoal}";
        public Vector2 EntryTarget => NearestTreat(_context.Bounds.center)?.transform.position ?? _context.Bounds.center;
        public int Recovered { get; private set; }
        public int Stolen { get; private set; }
        public bool SpawnTreatsHidden => false;

        public void Initialize(MissionContext context) => _context = context;

        public void StartMission()
        {
            Recovered = 0;
            Stolen = 0;
            _squirrelTarget = null;
            _squirrelHasStarted = false;
            _scaredUntil = 0f;
            _nextScareScoreAt = 0f;
            _squirrelTimer = NextDelay();
            HideGuardLane();
            _context.SquirrelObject.SetActive(true);
            _context.SquirrelObject.transform.position = new Vector2(11f, 7f);
            _context.SetActorState(_context.SquirrelObject, "Squirrel: WAITING", new Color(0.55f, 0.32f, 0.12f), 0.06f);
        }

        public void Tick(float deltaTime, float now)
        {
            if (now < _scaredUntil) return;
            Treat nearby = FindTreatNear(_context.SquirrelObject.transform.position, 0.3f);
            if (nearby != null)
            {
                _squirrelTarget = nearby;
                StealTarget();
                return;
            }
            if (_squirrelTarget == null)
            {
                _squirrelTimer -= deltaTime;
                if (_squirrelTimer <= 0f)
                {
                    var treats = ActiveTreats();
                    if (treats.Count > 0) StartSteal(treats[_context.Random().Next(treats.Count)]);
                }
                return;
            }

            _context.SquirrelObject.transform.position = Vector3.MoveTowards(
                _context.SquirrelObject.transform.position,
                _squirrelTarget.transform.position,
                deltaTime * _context.SquirrelMoveSpeed);
            if (Vector2.Distance(_context.SquirrelObject.transform.position, _squirrelTarget.transform.position) < 0.25f)
                StealTarget();
        }

        public bool HandleBark(int dogIndex)
        {
            if (dogIndex < 0 || dogIndex >= _context.Dogs.Length) return false;
            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, _context.SquirrelObject.transform.position) >= _context.SingleBarkSquirrelRange)
                return false;

            _squirrelTarget = null;
            HideGuardLane();
            _scaredUntil = Mathf.Max(_scaredUntil, _context.Now() + _context.SingleBarkScareSeconds);
            _squirrelTimer = NextDelay();
            if (_context.Now() >= _nextScareScoreAt)
            {
                _context.AddScore(_context.SquirrelScareScore, "SNACK GUARD BARK");
                _context.CreditDog(dogIndex);
                _nextScareScoreAt = _context.Now() + 1f;
            }
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
            _context.SetCue($"{_context.Dogs[dogIndex].name} scared the squirrel! It dropped the snack plan!");
            _context.SetActorState(_context.SquirrelObject, "SQUIRREL DROPPED THE SNACK!", new Color(0.85f, 0.85f, 0.85f), 0.08f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "SNACK DROP POP!");
            _context.SpawnWorldPop(_context.SquirrelObject.transform.position, "DROP!", new Color(0.9f, 0.95f, 1f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.LogEvent("SquirrelScared", "Snack heist interrupted");
            _context.LogObjectiveChanged();
            return true;
        }

        public bool HandleTreatCollected(Treat treat, int dogIndex)
        {
            if (treat == null) return false;
            SetTreatProp(treat, FinalGameplayArt.SnackHeistPlateStashed);
            Recovered++;
            _context.AddScore(_context.ItemScore, "SNACK STASHED");
            if (dogIndex >= 0) _context.CreditDog(dogIndex);
            _context.SetCue($"{DogName(dogIndex)} recovered a forbidden snack!");
            if (dogIndex >= 0) _context.Pulse(_context.Dogs[dogIndex].gameObject, 1.2f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.ScoreDelta, $"+{_context.ItemScore} SNACK STASHED");
            Vector2 popAt = dogIndex >= 0 ? _context.Dogs[dogIndex].transform.position : treat.transform.position;
            _context.SpawnWorldPop(popAt, $"+{_context.ItemScore} SNACK STASHED", new Color(1f, 0.78f, 0.25f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            if (_squirrelTarget == treat) _squirrelTarget = null;
            HideGuardLane();
            _context.ReplaceCollectible(treat);
            _context.LogEvent("Collection", $"{DogName(dogIndex)} collected a forbidden snack {Recovered}/{_context.ObjectiveGoal}");
            _context.LogObjectiveChanged();
            return true;
        }

        public void Cleanup()
        {
            _squirrelTarget = null;
            HideGuardLane();
            if (_context?.SquirrelObject != null) _context.SquirrelObject.SetActive(false);
        }

        public void StageDogsForEntry()
        {
            Vector2 target = EntryTarget;
            Vector2 inward = (_context.Bounds.center - target).normalized;
            if (inward.sqrMagnitude < 0.01f) inward = Vector2.down;
            Vector2 center = target + inward * 7f;
            Vector2 side = new Vector2(-inward.y, inward.x) * 1.5f;
            for (int i = 0; i < _context.Dogs.Length; i++)
            {
                Vector2 position = center + (i % 2 == 0 ? -side : side);
                position.x = Mathf.Clamp(position.x, _context.Bounds.xMin + 1.5f, _context.Bounds.xMax - 1.5f);
                position.y = Mathf.Clamp(position.y, _context.Bounds.yMin + 1.5f, _context.Bounds.yMax - 1.5f);
                _context.Dogs[i].transform.position = position;
                if (_context.Dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
            }
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            if (_squirrelTarget != null)
            {
                target = _context.SquirrelObject.transform;
                copy = "BARK SQUIRREL";
                hideDistance = _context.SingleBarkSquirrelRange;
                return true;
            }
            Treat nearest = dogIndex >= 0 && dogIndex < _context.Dogs.Length
                ? NearestTreat(_context.Dogs[dogIndex].transform.position)
                : null;
            target = nearest != null ? nearest.transform : null;
            copy = "SNACK";
            hideDistance = 1.2f;
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("snack_heist", score, timeRemaining, Recovered, _context.ObjectiveGoal, Stolen,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceStealAttempt()
        {
            Treat target = NearestTreat(_context.Bounds.center);
            if (target == null) return;
            _squirrelTarget = target;
            _context.SquirrelObject.transform.position = target.transform.position;
            StealTarget();
        }

        public void ForceCollectTreat(int dogIndex = 0)
        {
            Treat treat = NearestTreat(_context.Bounds.center);
            if (treat != null) HandleTreatCollected(treat, dogIndex);
        }

        public void ForceSteal()
        {
            Treat target = NearestTreat(_context.SquirrelObject.transform.position);
            if (target == null) return;
            _squirrelTarget = target;
            StealTarget();
        }

        public void ForceStartStealForArt()
        {
            Treat target = NearestTreat(_context.Bounds.center);
            StartSteal(target);
        }

        private void StartSteal(Treat target)
        {
            if (target == null) return;
            _squirrelTarget = target;
            _squirrelHasStarted = true;
            SetTreatProp(target, FinalGameplayArt.SnackHeistPlateTargeted);
            ShowGuardLane(target);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStealing);
            _context.SetCue("Squirrel is reaching for the forbidden snack stash - bark guard!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "BARK-GUARD THE SNACK THIEF");
            _context.SetActorState(_context.SquirrelObject, "SQUIRREL SNACK HEIST - BARK!", new Color(0.7f, 0.35f, 0.08f), 0.32f);
            _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            _context.RequestRumble("squirrel_warning", 0.12f, 0.24f, 0.12f);
            _context.LogEvent("SquirrelPressure", "SQUIRREL SNACK HEIST - BARK!");
            _context.LogObjectiveChanged();
        }

        private void StealTarget()
        {
            if (_squirrelTarget != null)
            {
                SetTreatProp(_squirrelTarget, FinalGameplayArt.SnackHeistPlateStolen);
                _context.ReplaceCollectible(_squirrelTarget);
            }
            Stolen++;
            int penalty = _context.ActiveModifier() == GameManager.RoundModifier.PancakePanic
                ? _context.PancakeSquirrelPenalty
                : _context.SquirrelPenalty;
            _context.AddScore(-penalty, "SNACK THIEF");
            _squirrelTarget = null;
            HideGuardLane();
            _squirrelTimer = NextDelay();
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue("Squirrel got a snack and looks professionally smug!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "MISS! SQUIRREL STOLE A SNACK");
            _context.SetActorState(_context.SquirrelObject, "SQUIRREL STOLE A SNACK!", Color.gray, 0.22f);
            _context.SpawnWorldPop(_context.SquirrelObject.transform.position, "MISS! -SNACK", new Color(1f, 0.35f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            _context.RequestRumble("squirrel_penalty", 0.18f, 0.38f, 0.16f);
            _context.LogEvent("SquirrelStole", $"{Stolen}/{_context.MaxStolenFood}");
        }

        private float NextDelay()
        {
            bool trouble = _context.ActiveModifier() == GameManager.RoundModifier.SquirrelTrouble;
            if (!_squirrelHasStarted) return trouble ? _context.FirstSquirrelTroubleDelay : _context.FirstSquirrelBaseDelay;
            return trouble ? _context.SquirrelTroubleDelay : _context.SquirrelBaseDelay;
        }

        private IReadOnlyList<Treat> ActiveTreats() => _context.ActiveTreats();

        private Treat FindTreatNear(Vector2 position, float radius)
        {
            foreach (var treat in ActiveTreats())
                if (treat != null && Vector2.Distance(position, treat.transform.position) <= radius) return treat;
            return null;
        }

        private Treat NearestTreat(Vector2 position)
        {
            Treat nearest = null;
            float best = float.PositiveInfinity;
            foreach (var treat in ActiveTreats())
            {
                if (treat == null) continue;
                float distance = Vector2.Distance(position, treat.transform.position);
                if (distance < best) { best = distance; nearest = treat; }
            }
            return nearest;
        }

        private string DogName(int dogIndex) => dogIndex >= 0 && dogIndex < _context.Dogs.Length
            ? _context.Dogs[dogIndex].name
            : "Dog";

        private void ShowGuardLane(Treat target)
        {
            if (target == null) return;
            if (_guardLane == null)
            {
                _guardLane = new GameObject("SnackHeistBarkGuardLane");
                var fallback = _guardLane.AddComponent<SpriteRenderer>();
                fallback.sprite = SpriteShapeCache.WhiteSquare;
                fallback.color = new Color(0.25f, 0.65f, 1f, 0.08f);
                fallback.sortingOrder = 10;
                _guardLaneArt = MissionPropArt.AttachObject(_guardLane, FinalGameplayArt.SnackHeistGuardLane, 0.019f, 11, true);
            }

            _guardLane.transform.position = Vector3.Lerp(_context.SquirrelObject.transform.position, target.transform.position, 0.5f);
            _guardLane.SetActive(true);
            MissionPropArt.SetSprite(_guardLaneArt, FinalGameplayArt.SnackHeistGuardLane);
        }

        private void HideGuardLane()
        {
            if (_guardLane != null) _guardLane.SetActive(false);
        }

        private static void SetTreatProp(Treat treat, string resourcePath)
        {
            if (treat == null || string.IsNullOrEmpty(resourcePath)) return;

            var attachment = treat.GetComponent<MissionPropArtAttachment>();
            if (attachment != null && attachment.HasRuntimeSprite)
            {
                MissionPropArt.SetSprite(attachment, resourcePath);
                return;
            }

            MissionPropArt.AttachObject(treat.gameObject, resourcePath, 0.013f, 31, true);
        }
    }
}
