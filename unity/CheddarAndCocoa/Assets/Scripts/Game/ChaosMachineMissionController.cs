using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned Rube-Goldberg co-op puzzle. The dogs pre-position at their junctions and pull
    /// the lever; the cascade runs itself — but each junction has a brief assist window where its owner
    /// must be in position or the machine misfires and jams. A re-pull resumes from the jam; too many
    /// misfires fail the mission.
    /// </summary>
    public sealed class ChaosMachineMissionController : IMissionController, IMissionInteractionController,
        IMissionSuccessPresentationController
    {
        private const float LeverRangeVal = 3f;
        private const float JunctionRange = 3f;
        private const float WindowPerStage = 3f;
        private const int MaxStalls = 4;
        private const float SuccessHoldSeconds = 1.15f;

        private static readonly ChainActor[] Owners = { ChainActor.Cocoa, ChainActor.Cheddar, ChainActor.Cocoa };
        private static readonly Vector2[] JunctionSpots = { new(-4f, 7f), new(6f, -7f), new(14f, 7f) };
        private static readonly string[] Actions = { "TOWEL DROP", "BASKET TIP", "TOY LAUNCH" };
        private static readonly string[] JunctionArtPaths =
        {
            FinalGameplayArt.ChaosJunctionTowelDrop,
            FinalGameplayArt.ChaosJunctionBasketTip,
            FinalGameplayArt.ChaosJunctionToyLaunch
        };
        private static readonly Vector2 LeverPos = new(-14f, -7f);

        private readonly CoopChaosMachinePuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _lever;
        private MissionPropArtAttachment _leverArt;
        private GameObject[] _junctions;
        private TextMesh[] _junctionLabels;
        private MissionPropArtAttachment[] _junctionArt;
        private int _stageSeen;
        private int _stallsSeen;
        private bool _failed;
        private bool _completionPresented;
        private float _successHoldRemaining;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.ChaosMachine;
        public bool IsComplete => _puzzle.Solved && _successHoldRemaining <= 0f;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "The machine misfired too many times - the cascade never made it to the end."
            : null;
        public CoopChaosMachinePuzzle Puzzle => _puzzle;
        public Vector2 LeverZone => LeverPos;
        public int JunctionCount => JunctionSpots.Length;
        public Vector2 JunctionSpot(int index) => index >= 0 && index < JunctionSpots.Length ? JunctionSpots[index] : Vector2.zero;
        public ChainActor JunctionOwner(int index) => index >= 0 && index < Owners.Length ? Owners[index] : ChainActor.Either;
        public Vector2 EntryTarget => _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildChaosMachineSummary(_puzzle);
        public bool IsPresentingSuccessfulOutcome => _puzzle.Solved && _successHoldRemaining > 0f;
        public float SuccessHoldRemaining => _successHoldRemaining;

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome)
                    return "CHAOS COMPLETE! Towel, basket, and toy all fired in one glorious mess!";
                if (!_puzzle.Running)
                    return $"Cheddar: Interact-pull the lever; partner pre-position at the live junction (junctions {_puzzle.Stage}/{_puzzle.StageCount}, misfires {_puzzle.Stalls}/{MaxStalls})";
                int stage = Mathf.Clamp(_puzzle.Stage, 0, Actions.Length - 1);
                string who = Owners[stage] == ChainActor.Cheddar ? "Cheddar" : "Cocoa";
                return $"{who}: Interact at the {Actions[stage].ToLowerInvariant()} junction NOW - the cascade's rolling! (junctions {_puzzle.Stage}/{_puzzle.StageCount}, misfires {_puzzle.Stalls}/{MaxStalls})";
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
            _puzzle.Configure(JunctionSpots.Length, WindowPerStage);
            _stageSeen = 0;
            _stallsSeen = 0;
            _failed = false;
            _completionPresented = false;
            _successHoldRemaining = 0f;
            SetSceneActive(true);
            UpdateVisuals();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_failed || _context.Dogs == null) return;

            if (_puzzle.Solved)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                UpdateVisuals();
                return;
            }

            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            if (cheddar < 0 || cocoa < 0) return;

            if (_puzzle.Running)
                _puzzle.Advance(deltaTime, assisting: false);

            HandleProgress();
            if (_failed) return;
            UpdateVisuals();
        }

        public bool HandleBark(int dogIndex) => false;

        public bool HandleInteract(int dogIndex)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length)
                return false;

            DogId dog = DogIdAt(dogIndex);
            if (!_puzzle.Running)
            {
                if (dog != DogId.Cheddar)
                {
                    _context.SetCue("Cocoa covers the first live junction; Cheddar is the chaos gremlin who Interact-pulls the lever.");
                    _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "CHEDDAR PULLS", new Color(1f, 0.72f, 0.3f));
                    return true;
                }
                if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, LeverPos) > LeverRangeVal)
                {
                    _context.SetCue("Cheddar must reach the lever before he can Interact-pull the machine into motion.");
                    _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "INTERACT AT LEVER", new Color(1f, 0.72f, 0.3f));
                    return true;
                }

                _puzzle.Trigger();
                _context.SetCue("Cheddar pulled it! Cocoa, hit the towel-drop junction before the cascade jams!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "MACHINE GO!");
                _context.SpawnWorldPop(LeverPos, "CLACK-WHIRR!", new Color(1f, 0.72f, 0.3f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
                _context.RequestRumble("chaos_lever", 0.12f, 0.28f, 0.1f);
                _context.LogEvent("ChaosLeverPulled", $"stage {_puzzle.Stage}");
                UpdateVisuals();
                return true;
            }

            int stage = Mathf.Clamp(_puzzle.Stage, 0, JunctionSpots.Length - 1);
            ChainActor expected = Owners[stage];
            ChainActor actor = dog == DogId.Cheddar ? ChainActor.Cheddar : ChainActor.Cocoa;
            if (actor != expected)
            {
                string who = expected == ChainActor.Cheddar ? "Cheddar" : "Cocoa";
                _context.SetCue($"Wrong paws for {Actions[stage].ToLowerInvariant()} - {who} must Interact at this junction!");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, $"{who.ToUpperInvariant()}'S TURN", new Color(1f, 0.55f, 0.25f));
                return true;
            }
            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, JunctionSpots[stage]) > JunctionRange)
            {
                _context.SetCue($"Get to the glowing {Actions[stage].ToLowerInvariant()} junction and Interact before it jams!");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "INTERACT AT JUNCTION", new Color(1f, 0.72f, 0.3f));
                return true;
            }

            _puzzle.Advance(0.0001f, assisting: true);
            HandleProgress();
            if (!_failed) UpdateVisuals();
            return true;
        }

        public void Cleanup() => SetSceneActive(false);

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
                Vector2 pos = ClampInsideBounds(center + offset, 1.5f);
                _context.Dogs[i].transform.position = pos;
                if (_context.Dogs[i].TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            }
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            if (!_puzzle.Running)
            {
                bool cheddar = _context.IndexOfDog(DogId.Cheddar) == dogIndex;
                int stage = Mathf.Clamp(_puzzle.Stage, 0, JunctionSpots.Length - 1);
                target = cheddar ? (_lever != null ? _lever.transform : null)
                    : (_junctions != null && _junctions[stage] != null ? _junctions[stage].transform : null);
                copy = cheddar ? "INTERACT: PULL LEVER" : "PRE-POSITION";
                hideDistance = cheddar ? LeverRangeVal : JunctionRange;
            }
            else
            {
                int stage = Mathf.Clamp(_puzzle.Stage, 0, JunctionSpots.Length - 1);
                ChainActor owner = Owners[stage];
                bool isOwner = (owner == ChainActor.Cheddar && _context.IndexOfDog(DogId.Cheddar) == dogIndex)
                    || (owner == ChainActor.Cocoa && _context.IndexOfDog(DogId.Cocoa) == dogIndex);
                int targetStage = isOwner ? stage : Mathf.Min(stage + 1, JunctionSpots.Length - 1);
                target = _junctions != null && _junctions[targetStage] != null ? _junctions[targetStage].transform : null;
                copy = isOwner ? "INTERACT: FIRE IT" : stage + 1 < JunctionSpots.Length ? "PRE-POSITION NEXT" : "BACK UP PARTNER";
                hideDistance = JunctionRange;
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("chaos_machine", score, timeRemaining, _puzzle.Stage, _puzzle.StageCount, _puzzle.Stalls,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Test hook: pull (or re-pull) the lever to start/resume the cascade.</summary>
        public void ForceChaosTrigger()
        {
            _puzzle.Trigger();
            UpdateVisuals();
        }

        /// <summary>Test hook: advance the live cascade, with the current junction's owner in position or not.</summary>
        public void ForceChaosAdvance(float seconds, bool assisting)
        {
            _puzzle.Advance(seconds, assisting);
            HandleProgress();
            if (!_failed) UpdateVisuals();
        }

        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        private void HandleProgress()
        {
            if (_puzzle.Stage > _stageSeen)
            {
                ChainActor completedBy = Owners[Mathf.Clamp(_stageSeen, 0, Owners.Length - 1)];
                int actorIndex = _context.IndexOfDog(completedBy == ChainActor.Cheddar ? DogId.Cheddar : DogId.Cocoa);
                if (actorIndex >= 0) _context.CreditDog(actorIndex);
                _stageSeen = _puzzle.Stage;
                _context.AddScore(ScoreEventCatalog.ContraptionStep.Points, "CASCADE ROLLED");
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue($"Whirr-clunk! The cascade rolled through a junction. ({_puzzle.Stage}/{_puzzle.StageCount})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "WHIRR!");
                int doneStage = Mathf.Clamp(_puzzle.Stage - 1, 0, JunctionSpots.Length - 1);
                _context.SpawnWorldPop(JunctionSpots[doneStage], "WHIRR!", new Color(0.6f, 0.85f, 0.95f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
                _context.RequestRumble("chaos_stage", 0.12f, 0.28f, 0.1f);
                _context.LogEvent("ChaosStage", $"{_puzzle.Stage}/{_puzzle.StageCount}");
            }

            if (_puzzle.Stalls > _stallsSeen)
            {
                _stallsSeen = _puzzle.Stalls;
                _context.AddScore(ScoreEventCatalog.ContraptionFumble.Points, "MISFIRE");
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                int jam = Mathf.Clamp(_puzzle.StalledStage, 0, JunctionSpots.Length - 1);
                _context.SetCue($"Misfire! The machine jammed at the {Actions[jam].ToLowerInvariant()} - re-pull the lever. ({_puzzle.Stalls}/{MaxStalls})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "MISFIRE!");
                _context.SpawnWorldPop(JunctionSpots[jam], "STUCK!", new Color(1f, 0.4f, 0.25f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
                _context.RequestRumble("chaos_misfire", 0.2f, 0.42f, 0.16f);
                _context.LogEvent("ChaosStall", $"{_puzzle.Stalls}/{MaxStalls}");
                if (_puzzle.Stalls >= MaxStalls) _failed = true;
            }

            if (_puzzle.Solved && !_completionPresented)
            {
                _completionPresented = true;
                _successHoldRemaining = SuccessHoldSeconds;
                _context.SetCue("CHAOS COMPLETE! Towel, basket, and toy all fired in one glorious mess!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "GLORIOUS CHAOS!");
                _context.SpawnWorldPop(JunctionSpots[JunctionSpots.Length - 1], "TOY LAUNCHED!", new Color(1f, 0.86f, 0.3f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
                _context.RequestRumble("chaos_payoff", 0.32f, 0.58f, 0.22f);
                _context.LogEvent("ChaosMachinePayoff", "Cascade complete; holding live-world success beat");
            }
        }

        private void BuildScene()
        {
            _lever = new GameObject("ChaosMachineLever");
            _lever.transform.position = LeverPos;
            _lever.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            var leverSr = _lever.AddComponent<SpriteRenderer>();
            leverSr.sprite = _context.ActorSprite;
            leverSr.color = new Color(0.85f, 0.55f, 0.3f);
            leverSr.sortingOrder = 3;
            // Identity-only close-range text: the ready/running sprite + color, the badge until the
            // pull, and the HUD objective line carry the pull-the-lever instruction. Junction labels
            // stay full ({WHO}: {ACTION}) - that split-info map IS the puzzle and lives nowhere else.
            _context.AddWorldLabel(_lever, "LEVER", Vector3.up * 1.3f, 11, Color.white);
            _leverArt = MissionPropArt.AttachObject(_lever, FinalGameplayArt.ChaosLeverReady, 0.012f, 18, true);
            _lever.SetActive(false);

            _junctions = new GameObject[JunctionSpots.Length];
            _junctionLabels = new TextMesh[JunctionSpots.Length];
            _junctionArt = new MissionPropArtAttachment[JunctionSpots.Length];
            for (int i = 0; i < JunctionSpots.Length; i++)
            {
                var go = new GameObject($"ChaosJunction_{i}");
                go.transform.position = JunctionSpots[i];
                go.transform.localScale = new Vector3(1.7f, 1.3f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _context.ActorSprite;
                sr.color = new Color(0.3f, 0.3f, 0.34f);
                sr.sortingOrder = 3;
                _junctionLabels[i] = _context.AddWorldLabel(go, $"{i + 1}.", Vector3.up * 1.3f, 12, Color.white);
                _junctionArt[i] = MissionPropArt.AttachObject(go, JunctionArtPaths[i], 0.013f, 18, true);
                go.SetActive(false);
                _junctions[i] = go;
            }
        }

        private void SetSceneActive(bool active)
        {
            if (_lever != null) _lever.SetActive(active);
            if (_junctions == null) return;
            foreach (var j in _junctions)
                if (j != null) j.SetActive(active);
        }

        private void UpdateVisuals()
        {
            if (_lever != null && _lever.TryGetComponent<SpriteRenderer>(out var leverSr))
            {
                leverSr.color = _puzzle.Running ? new Color(0.5f, 0.85f, 0.55f) : new Color(0.85f, 0.55f, 0.3f);
                MissionPropArt.SetSprite(_leverArt, _puzzle.Running ? FinalGameplayArt.ChaosLeverRunning : FinalGameplayArt.ChaosLeverReady);
            }
            ActorSignalBadge.SetStationSignal(_lever, !_puzzle.Running && !_puzzle.Solved && !_failed);

            if (_junctions == null) return;
            int active = Mathf.Clamp(_puzzle.Stage, 0, _junctions.Length - 1);
            for (int i = 0; i < _junctions.Length; i++)
            {
                if (_junctions[i] == null) continue;
                bool fired = i < _puzzle.Stage;
                bool isActive = i == active && !_puzzle.Solved;
                bool stalledHere = _puzzle.StalledStage == i;
                ChainActor owner = Owners[i];
                Color ownerTint = owner == ChainActor.Cheddar ? new Color(0.95f, 0.72f, 0.3f) : new Color(0.55f, 0.78f, 1f);
                Color shown = fired ? new Color(0.3f, 0.55f, 0.32f)
                    : stalledHere ? new Color(0.9f, 0.35f, 0.2f)
                    : isActive && _puzzle.Running ? ownerTint
                    : new Color(0.3f, 0.3f, 0.34f);
                if (_junctions[i].TryGetComponent<SpriteRenderer>(out var sr)) sr.color = shown;
                // Distance signal: command over the junction the live cascade needs covered NOW;
                // a jam flips it to the warning skin until the lever is re-pulled.
                bool jammed = stalledHere && !_puzzle.Running;
                ActorSignalBadge.SetStationSignal(_junctions[i], (isActive && _puzzle.Running) || jammed, jammed);
                if (_junctionArt != null && _junctionArt[i] != null)
                    _junctionArt[i].SetTint(JunctionTint(fired, stalledHere, isActive, owner));
                if (_junctionLabels != null && _junctionLabels[i] != null)
                {
                    string who = owner == ChainActor.Cheddar ? "CHEDDAR" : "COCOA";
                    _junctionLabels[i].text = fired ? "FIRED" : $"{who}: {Actions[i]}";
                }
            }
        }

        private static Color JunctionTint(bool fired, bool stalled, bool active, ChainActor owner)
        {
            if (fired) return new Color(0.75f, 1f, 0.78f, 1f);
            if (stalled) return new Color(1f, 0.58f, 0.45f, 1f);
            if (!active) return Color.white;
            return owner == ChainActor.Cheddar
                ? new Color(1f, 0.88f, 0.55f, 1f)
                : new Color(0.68f, 0.9f, 1f, 1f);
        }

        private Vector2 ClampInsideBounds(Vector2 point, float margin) => new(
            Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
            Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));

        private DogId DogIdAt(int dogIndex)
        {
            if (_context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length || _context.Dogs[dogIndex] == null)
                return DogId.Cheddar;
            var identity = _context.Dogs[dogIndex].GetComponent<DogIdentity>();
            return identity != null ? identity.Id : DogId.Cheddar;
        }
    }
}
