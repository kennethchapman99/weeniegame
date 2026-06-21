using System.Collections.Generic;
using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller for Backyard Rescue. Owns the squirrel/weenie loop, the two-pass squirrel
    /// trap state (pressure dog + gap dog + partner recovery), and the escape-gap marker.
    /// Predator and tug are handled by GameManager's shared systems; controller reads their
    /// resolved state via MissionContext to declare IsComplete.
    /// </summary>
    public sealed class BackyardRescueMissionController : IMissionController, IMissionTreatCollector
    {
        private const float TrapGapRadius = 3.2f;

        private readonly BackyardSquirrelTrapState _trapState = new();
        private MissionContext _context;
        private Vector2 _gapPosition;
        private GameObject _gapMarker;
        private Treat _droppedWeenie;
        private Treat _squirrelTarget;
        private float _squirrelTimer;
        private float _scaredUntil;
        private float _nextScareScoreAt;
        private bool _squirrelHasStarted;
        private int _collected;
        private int _stolen;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.BackyardRescue;

        public bool IsComplete =>
            _collected >= _context.ObjectiveGoal &&
            _trapState.Complete &&
            _context.IsPredatorResolved() &&
            _context.IsTugComplete();

        public bool IsFailed => false;
        public string FailReason => null;
        public string OutcomeSummary => null;
        public int Collected => _collected;
        public int Stolen => _stolen;
        public BackyardSquirrelTrapState TrapState => _trapState;
        public Treat DroppedWeenie => _droppedWeenie;
        public Vector2 GapPosition => _gapPosition;
        public bool IsSquirrelStealing => _squirrelTarget != null;
        public bool SpawnTreatsHidden => false;

        public string ObjectiveLabel
        {
            get
            {
                if (_trapState.Complete)
                    return $"Trap complete - save weenies {_collected}/{_context.ObjectiveGoal}";
                if (_trapState.WeenieDropped)
                    return $"Squirrel juke! {_trapState.RecoveryDog}: recover the dropped weenie (partner only) - trap {_trapState.Recoveries}/{BackyardSquirrelTrapState.RequiredRecoveries}";
                if (_squirrelTarget != null)
                    return $"Bark to scare squirrel: {_trapState.PressureDog} pressures / {_trapState.GapDog} holds escape gap - trap {_trapState.Recoveries}/{BackyardSquirrelTrapState.RequiredRecoveries}";
                if (!_context.IsTugComplete() && _collected >= Mathf.Max(2, _context.ObjectiveGoal / 2))
                    return "Both dogs tug the rope";
                return $"Save weenies {_collected}/{_context.ObjectiveGoal}: {_trapState.PressureDog} pressures, {_trapState.GapDog} holds gap ({_trapState.Recoveries}/{BackyardSquirrelTrapState.RequiredRecoveries})";
            }
        }

        public Vector2 EntryTarget => _context != null ? _context.Bounds.center : Vector2.zero;

        public void Initialize(MissionContext context)
        {
            _context = context;
            _gapPosition = new Vector2(
                context.Bounds.center.x + 0.72f * context.Bounds.width * 0.5f,
                context.Bounds.center.y + 0.08f * context.Bounds.height * 0.5f);
            BuildGapMarker();
        }

        public void StartMission()
        {
            _trapState.Reset();
            _collected = 0;
            _stolen = 0;
            _droppedWeenie = null;
            _squirrelTarget = null;
            _squirrelHasStarted = false;
            _scaredUntil = 0f;
            _nextScareScoreAt = 0f;
            _squirrelTimer = NextDelay();
            _context.SquirrelObject.SetActive(true);
            _context.SquirrelObject.transform.position = new Vector2(11f, 7f);
            _context.SetActorState(_context.SquirrelObject, "Squirrel: WAITING", new Color(0.55f, 0.32f, 0.12f), 0.06f);
            UpdateGapMarker();
        }

        public void Tick(float deltaTime, float now)
        {
            UpdateGapMarker();
            if (_trapState.WeenieDropped) return;
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
                    var treats = _context.ActiveTreats();
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

            if (!_trapState.Complete && _squirrelTarget != null &&
                Vector2.Distance(_context.Dogs[dogIndex].transform.position, _context.SquirrelObject.transform.position)
                    < _context.SingleBarkSquirrelRange)
            {
                var dogId = _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var id) ? id.Id : DogId.Cheddar;
                return ResolveRedirect(dogId);
            }

            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, _context.SquirrelObject.transform.position)
                < _context.SingleBarkSquirrelRange)
            {
                ScareSquirrel(_context.Now() + _context.SingleBarkScareSeconds);
                return true;
            }
            return false;
        }

        public bool HandleTreatCollected(Treat treat, int dogIndex)
        {
            if (treat == null) return false;

            if (treat == _droppedWeenie)
            {
                var dogId = dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
                            _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                    ? identity.Id
                    : DogId.Cheddar;
                return HandleWeenieRecovery(dogId);
            }

            _collected++;
            _context.AddScore(_context.ItemScore, "WEENIE SAVED");
            if (dogIndex >= 0) _context.CreditDog(dogIndex);
            _context.SetCue($"{DogName(dogIndex)} rescued a weenie!");
            if (dogIndex >= 0) _context.Pulse(_context.Dogs[dogIndex].gameObject, 1.2f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.ScoreDelta, $"+{_context.ItemScore} WEENIE SAVED");
            Vector2 popAt = dogIndex >= 0 ? _context.Dogs[dogIndex].transform.position : treat.transform.position;
            _context.SpawnWorldPop(popAt, $"+{_context.ItemScore} WEENIE SAVED", new Color(1f, 0.9f, 0.25f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            if (_squirrelTarget == treat) _squirrelTarget = null;
            _context.ReplaceCollectible(treat);
            _context.LogEvent("Collection", $"Weenie rescued {_collected}/{_context.ObjectiveGoal}");
            _context.LogObjectiveChanged();
            return true;
        }

        public void Cleanup()
        {
            _squirrelTarget = null;
            _droppedWeenie = null;
            if (_context?.SquirrelObject != null) _context.SquirrelObject.SetActive(false);
            if (_gapMarker != null) _gapMarker.SetActive(false);
        }

        public void StageDogsForEntry() { }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null; copy = ""; hideDistance = 1.2f;

            if (_trapState.Complete) return false;

            if (_trapState.WeenieDropped)
            {
                bool isRecoveryDog = dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
                                     _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var id) &&
                                     id.Id == _trapState.RecoveryDog;
                target = _droppedWeenie != null ? _droppedWeenie.transform : null;
                copy = isRecoveryDog ? "RECOVER DROP" : "PARTNER ONLY";
                hideDistance = 1.2f;
                return target != null;
            }

            if (_squirrelTarget != null)
            {
                bool isPressureDog = dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
                                     _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var id2) &&
                                     id2.Id == _trapState.PressureDog;
                if (isPressureDog)
                {
                    target = _context.SquirrelObject != null ? _context.SquirrelObject.transform : null;
                    copy = "BARK PRESSURE";
                    hideDistance = 2.2f;
                }
                else
                {
                    target = _gapMarker != null ? _gapMarker.transform : null;
                    copy = "HOLD ESCAPE GAP";
                    hideDistance = TrapGapRadius;
                }
                return target != null;
            }

            return false;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("backyard_rescue", score, timeRemaining,
                _collected + _trapState.Recoveries,
                _context.ObjectiveGoal + BackyardSquirrelTrapState.RequiredRecoveries,
                _context.MaxStolenFood - _collected + _trapState.Fumbles,
                outcome == GameManager.MissionOutcome.Clear,
                outcome == GameManager.MissionOutcome.Failed);

        public void ForceRedirect(DogId pressureDog, bool gapHeld)
        {
            if (_squirrelTarget == null && _context.ActiveTreats().Count > 0)
                StartSteal(_context.ActiveTreats()[0]);
            ResolveRedirect(pressureDog, gapHeld);
        }

        public void ForceWeenieRecovery(DogId dogId) => HandleWeenieRecovery(dogId);

        public void ForceStealAttempt()
        {
            Treat target = FindNearestTreat(_context.Bounds.center);
            if (target == null) return;
            StartSteal(target);
        }

        private bool ResolveRedirect(DogId dogId, bool? gapHeldOverride = null)
        {
            if (_squirrelTarget == null) return false;
            bool gapHeld = gapHeldOverride ?? IsGapHeld();
            var result = _trapState.TryRedirect(dogId, gapHeld);

            if (result == BackyardSquirrelTrapState.RedirectResult.Success)
            {
                _droppedWeenie = _squirrelTarget;
                _squirrelTarget = null;
                _droppedWeenie.transform.position = _gapPosition + Vector2.left * 3f;
                PlaceSquirrel(_gapPosition + Vector2.right * 4f);
                _squirrelTimer = NextDelay();
                _context.AddScore(_context.SquirrelScareScore, "SQUIRREL REDIRECTED");
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue($"{dogId} pressured the squirrel into the blocked route - {_trapState.RecoveryDog} recover the dropped weenie!");
                _context.SetActorState(_context.SquirrelObject, "TRAPPED! PARTNER RECOVER THE DROP", new Color(0.85f, 0.85f, 0.85f), 0.12f);
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "SQUIRREL REDIRECT! PARTNER RECOVER");
                _context.SpawnWorldPop(_droppedWeenie.transform.position, "DROP! PARTNER ONLY!", new Color(0.9f, 0.95f, 1f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                _context.LogEvent("SquirrelTrapRedirect", $"{dogId} redirected pass {_trapState.Redirects}");
                UpdateGapMarker();
                _context.LogObjectiveChanged();
                return true;
            }

            string fumbleCue = result == BackyardSquirrelTrapState.RedirectResult.WrongPressureDog
                ? $"WRONG WOOF! {_trapState.PressureDog} must pressure this pass."
                : $"FAKE ROUTE! {_trapState.GapDog} must hold the escape gap first.";
            RegisterFumble(fumbleCue);
            return result != BackyardSquirrelTrapState.RedirectResult.Complete;
        }

        private bool HandleWeenieRecovery(DogId dogId)
        {
            var result = _trapState.TryRecover(dogId);
            if (result == BackyardSquirrelTrapState.RecoveryResult.Success)
            {
                Treat recovered = _droppedWeenie;
                _droppedWeenie = null;
                _context.AddScore(_context.SquirrelScareScore, "TRAP PASS COMPLETE");
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                string banner = _trapState.Complete ? "TRAP COMPLETE!" : "SWAP ROLES!";
                _context.SetCue($"{dogId} recovered the dropped weenie - {banner}");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, $"WEENIE RECOVERED! {banner}");
                if (recovered != null)
                    _context.SpawnWorldPop(recovered.transform.position, banner, new Color(0.9f, 0.95f, 1f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                _context.LogEvent("SquirrelTrapRecovery", $"{dogId} recovered {_trapState.Recoveries}/{BackyardSquirrelTrapState.RequiredRecoveries}");
                UpdateGapMarker();
                _context.LogObjectiveChanged();
                if (recovered != null) _context.ReplaceCollectible(recovered);
                return true;
            }

            if (result == BackyardSquirrelTrapState.RecoveryResult.WrongDog)
            {
                _context.MarkFailedInteraction(dogId, $"only {_trapState.RecoveryDog} can recover the trap weenie");
                _context.SetCue($"HOT-POTATO FUMBLE! {dogId} caused the drop, so {_trapState.RecoveryDog} must recover it.");
                if (_droppedWeenie != null)
                {
                    int idx = -1;
                    for (int i = 0; i < _context.Dogs.Length; i++)
                        if (_context.Dogs[i].TryGetComponent<DogIdentity>(out var id) && id.Id == dogId) { idx = i; break; }
                    Vector2 bounce = dogId == DogId.Cheddar ? new Vector2(-2.5f, 1.5f) : new Vector2(2.5f, -1.5f);
                    _droppedWeenie.transform.position = ClampBounds((Vector2)_droppedWeenie.transform.position + bounce);
                    _context.SpawnWorldPop(_droppedWeenie.transform.position, "HOT POTATO! PARTNER ONLY!", new Color(1f, 0.45f, 0.25f));
                }
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "PARTNER RECOVERY FUMBLE");
                _context.RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
                _context.LogEvent("SquirrelTrapFumble", "Wrong dog recovery");
                _context.LogObjectiveChanged();
                return true;
            }
            return false;
        }

        private void StartSteal(Treat target)
        {
            if (target == null) return;
            _squirrelTarget = target;
            _squirrelHasStarted = true;
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStealing);
            _context.SetCue("Squirrel is tiptoeing off with a weenie - bark now!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "SQUIRREL STEALING - BARK!");
            _context.SetActorState(_context.SquirrelObject, "SQUIRREL STEALING - BARK!", new Color(0.7f, 0.35f, 0.08f), 0.32f);
            _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            _context.RequestRumble("squirrel_warning", 0.12f, 0.24f, 0.12f);
            _context.LogEvent("SquirrelPressure", "Weenie at risk");
            _context.LogObjectiveChanged();
        }

        private void StealTarget()
        {
            if (_squirrelTarget != null) _context.ReplaceCollectible(_squirrelTarget);
            _stolen++;
            int penalty = _context.ActiveModifier() == GameManager.RoundModifier.PancakePanic
                ? _context.PancakeSquirrelPenalty
                : _context.SquirrelPenalty;
            _context.AddScore(-penalty, "SQUIRREL GOT ONE");
            _squirrelTarget = null;
            _squirrelTimer = NextDelay();
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue("Squirrel got a weenie and is being rude about it!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "MISS! SQUIRREL STOLE A WEENIE");
            _context.SetActorState(_context.SquirrelObject, "SQUIRREL GOT A WEENIE!", Color.gray, 0.22f);
            _context.SpawnWorldPop(_context.SquirrelObject.transform.position, "MISS! -WEENIE", new Color(1f, 0.35f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            _context.RequestRumble("squirrel_penalty", 0.18f, 0.38f, 0.16f);
            _context.LogEvent("SquirrelStole", "Weenie stolen");
        }

        private void ScareSquirrel(float scaredUntil)
        {
            _squirrelTarget = null;
            _scaredUntil = Mathf.Max(_scaredUntil, scaredUntil);
            _squirrelTimer = NextDelay();
            if (_context.Now() >= _nextScareScoreAt)
            {
                _context.AddScore(_context.SquirrelScareScore, "SQUIRREL SCARED");
                _nextScareScoreAt = _context.Now() + 1f;
            }
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
            _context.SetCue("Squirrel dropped the weenie and ran!");
            _context.SetActorState(_context.SquirrelObject, "SQUIRREL DROPPED IT!", new Color(0.85f, 0.85f, 0.85f), 0.08f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "SQUIRREL DROP POP!");
            _context.SpawnWorldPop(_context.SquirrelObject.transform.position, "DROP!", new Color(0.9f, 0.95f, 1f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.LogObjectiveChanged();
        }

        private void RegisterFumble(string cue)
        {
            _context.SetCue(cue + " The squirrel loops back for another try.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "SQUIRREL JUKE!");
            _context.SpawnWorldPop(_context.SquirrelObject.transform.position, "NYEH-HEH! WRONG WAY!", new Color(1f, 0.45f, 0.25f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
            _context.RequestRumble("squirrel_penalty", 0.12f, 0.25f, 0.12f);
            _context.LogEvent("SquirrelTrapFumble", cue);
            _context.LogObjectiveChanged();
        }

        private bool IsGapHeld()
        {
            for (int i = 0; i < _context.Dogs.Length; i++)
            {
                if (_context.Dogs[i] == null) continue;
                if (!_context.Dogs[i].TryGetComponent<DogIdentity>(out var id)) continue;
                if (id.Id != _trapState.GapDog) continue;
                return Vector2.Distance(_context.Dogs[i].transform.position, _gapPosition) <= TrapGapRadius;
            }
            return false;
        }

        private void PlaceSquirrel(Vector2 position) =>
            _context.SquirrelObject.transform.position = position;

        private void BuildGapMarker()
        {
            _gapMarker = new GameObject("BackyardSquirrelTrapEscapeGap");
            _gapMarker.transform.position = _gapPosition;
            _gapMarker.transform.localScale = Vector3.one * (TrapGapRadius * 1.25f);
            var sr = _gapMarker.AddComponent<SpriteRenderer>();
            if (_context.ActorSprite != null) sr.sprite = _context.ActorSprite;
            sr.color = new Color(0.35f, 0.8f, 1f, 0.34f);
            sr.sortingOrder = 1;
            _context.AddWorldLabel(_gapMarker, "ESCAPE GAP - HOLD HERE", Vector3.up * 0.38f, 14, Color.white);
            _gapMarker.SetActive(false);
        }

        private void UpdateGapMarker()
        {
            if (_gapMarker == null) return;
            bool active = !_trapState.Complete;
            _gapMarker.SetActive(active);
            if (active && _gapMarker.TryGetComponent<SpriteRenderer>(out var sr))
                sr.color = IsGapHeld()
                    ? new Color(0.35f, 1f, 0.5f, 0.48f)
                    : new Color(0.35f, 0.8f, 1f, 0.34f);
        }

        private float NextDelay()
        {
            bool trouble = _context.ActiveModifier() == GameManager.RoundModifier.SquirrelTrouble;
            if (!_squirrelHasStarted) return trouble ? _context.FirstSquirrelTroubleDelay : _context.FirstSquirrelBaseDelay;
            return trouble ? _context.SquirrelTroubleDelay : _context.SquirrelBaseDelay;
        }

        private Treat FindTreatNear(Vector2 position, float radius)
        {
            foreach (var treat in _context.ActiveTreats())
                if (treat != null && treat != _droppedWeenie && Vector2.Distance(position, treat.transform.position) <= radius) return treat;
            return null;
        }

        private Treat FindNearestTreat(Vector2 position)
        {
            Treat nearest = null;
            float best = float.PositiveInfinity;
            foreach (var treat in _context.ActiveTreats())
            {
                if (treat == null || treat == _droppedWeenie) continue;
                float d = Vector2.Distance(position, treat.transform.position);
                if (d < best) { best = d; nearest = treat; }
            }
            return nearest;
        }

        private Vector2 ClampBounds(Vector2 pos)
        {
            const float margin = 1.2f;
            return new Vector2(
                Mathf.Clamp(pos.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
                Mathf.Clamp(pos.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
        }

        private string DogName(int idx) =>
            idx >= 0 && idx < _context.Dogs.Length ? _context.Dogs[idx].name : "Dog";
    }
}
