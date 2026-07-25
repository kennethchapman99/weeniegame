using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class BoneRelayMissionController : IMissionController, IMissionInteractionController,
        IMissionSuccessPresentationController, IMissionRoleOwner
    {
        private const float ScentRange = 3.5f;
        private const float DigRange = 3f;
        private const int FindsNeeded = 3;
        private const int MaxWasted = 5;
        private const float SuccessHoldSeconds = 1.15f;
        private static readonly Vector2 ScentZonePos = new(0f, 9f);
        private static readonly Vector2[] MoundSpots = { new(-12f, -6f), new(12f, -6f), new(-12f, 6f), new(12f, 6f) };
        private static readonly Color MoundCallColor = new(0.5f, 0.9f, 0.55f);
        private static readonly Color MoundIdleColor = new(0.42f, 0.3f, 0.16f);

        private readonly CoopScentRelayPuzzle _puzzle = new();
        private MissionContext _context;
        private int _seed;
        private GameObject _scentPost;
        private MissionPropArtAttachment _scentPostArt;
        private TextMesh _scentPostLabel;
        private GameObject[] _mounds;
        private MissionPropArtAttachment[] _moundArt;
        private string[] _moundOverrideArt;
        private float[] _moundOverrideUntil;
        private int _lastActedMound = -1;
        private int _findsSeen;
        private int _blindSeen;
        private int _wrongSeen;
        private bool _failed;
        private float _successHoldRemaining;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.BoneRelay;
        public bool IsComplete => _puzzle.Solved && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _puzzle.Solved && _successHoldRemaining > 0f;
        // Before a call is revealed Cocoa must bark the scent; once revealed, Cheddar owns the dig -
        // mirrors the copy split in TryGetObjectiveTarget ("WAIT FOR CALL" vs "DIG THE CALL").
        public DogId? RoleOwnerDog => _puzzle.Solved ? null : _puzzle.RevealedTarget < 0 ? DogId.Cocoa : DogId.Cheddar;
        public bool IsFailed => _failed;
        public string FailReason => _failed ? "The dogs dug up half the yard guessing instead of waiting for Cocoa's call." : null;
        public CoopScentRelayPuzzle Puzzle => _puzzle;
        public int MoundCount => MoundSpots.Length;
        public Vector2 ScentZone => ScentZonePos;
        public Vector2 MoundSpot(int index) => index >= 0 && index < MoundSpots.Length ? MoundSpots[index] : Vector2.zero;
        public Vector2 EntryTarget => ScentZonePos;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildBoneRelaySummary(_puzzle);

        /// <summary>CF2.3 read-only presentation summary: mounds are individually reused across the
        /// relay's random target sequence, so their found/wrong art is deliberately timed rather than
        /// permanent (see <see cref="SetMoundOverride"/>). This tracks the fraction of the WHOLE
        /// relay done so the fixed scent post can carry a persistent baseline instead.</summary>
        public float RelayProgress => FindsNeeded > 0 ? Mathf.Clamp01((float)_puzzle.Finds / FindsNeeded) : 0f;

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome)
                    return "Bone detail complete! Cocoa called every scent and Cheddar dug up the stash.";
                int wasted = _puzzle.BlindActs + _puzzle.WrongDigs;
                if (!_puzzle.Known)
                    return $"Cocoa: reach the scent post and BARK to call the real mound - Cheddar, wait! (bones {_puzzle.Finds}/{FindsNeeded}, wasted {wasted}/{MaxWasted})";
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
            _findsSeen = 0;
            _blindSeen = 0;
            _wrongSeen = 0;
            _lastActedMound = -1;
            ClearMoundOverrides();
            _failed = false;
            _successHoldRemaining = 0f;
            SetSceneActive(true);
            MissionPropArt.SetSprite(_scentPostArt, FinalGameplayArt.BoneRelayScentPostIdle);
            UpdateMoundVisuals();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Solved)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }
            if (_failed || _mounds == null) return;
            UpdateMoundVisuals();
        }

        public bool HandleBark(int dogIndex)
        {
            if (_puzzle.Solved || _failed || _puzzle.Known) return false;
            if (_context.IndexOfDog(DogId.Cocoa) != dogIndex)
            {
                _context.SetCue("Cheddar can't call the mound - Cocoa's the nose, she has to bark the scent.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "COCOA: BARK THE SCENT!");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "COCOA'S JOB", new Color(1f, 0.72f, 0.25f));
                _context.MarkFailedInteraction(DogId.Cheddar, "Cheddar tried to bark the scent call");
                return true; // a coach beat fired - don't let the generic solo-bark juice clobber it
            }

            Vector2 cocoaPos = _context.Dogs[dogIndex].transform.position;
            if (Vector2.Distance(cocoaPos, ScentZonePos) > ScentRange)
            {
                _context.MarkFailedInteraction(DogId.Cocoa, "Reach the scent post before barking the call.");
                _context.SetCue("Cocoa needs the scent first - reach the purple post, then BARK the mound call.");
                return false;
            }

            RevealFromCocoaBark(dogIndex);
            return true;
        }

        public bool HandleInteract(int dogIndex)
        {
            if (_puzzle.Solved || _failed || _mounds == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length) return false;

            if (_context.IndexOfDog(DogId.Cheddar) != dogIndex)
            {
                _context.MarkFailedInteraction(DogId.Cocoa, "Cocoa can't dig - that's Cheddar's job");
                _context.SetCue("Cocoa can't dig - she's the nose, Cheddar's the one who digs the call.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CHEDDAR DIGS!");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "CHEDDAR'S JOB", new Color(1f, 0.72f, 0.25f));
                return true;
            }

            int target = FindNearestActiveMoundInRange(dogIndex);
            if (target < 0)
            {
                _context.MarkFailedInteraction(DogId.Cheddar, "get closer to a mound before digging");
                _context.SetCue("Cheddar needs to be standing at a mound to dig.");
                return true;
            }

            _lastActedMound = target;
            _puzzle.ActOn(target);
            HandleProgress();
            if (!_failed) UpdateMoundVisuals();
            return true;
        }

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
                copy = "BARK THE SCENT";
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
            ClearMoundOverrides();
            _puzzle.Reveal();
            UpdateMoundVisuals();
        }

        public bool ForceCocoaCall() => HandleBark(_context.IndexOfDog(DogId.Cocoa));

        public void ForceBoneDig(int target)
        {
            _lastActedMound = target;
            _puzzle.ActOn(target);
            HandleProgress();
            if (!_failed) UpdateMoundVisuals();
        }

        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        private void HandleProgress()
        {
            int digger = _context.IndexOfDog(DogId.Cheddar);
            Vector2 digPos = digger >= 0 ? (Vector2)_context.Dogs[digger].transform.position : ScentZonePos;

            if (_puzzle.Finds > _findsSeen)
            {
                _findsSeen = _puzzle.Finds;
                _context.AddScore(ScoreEventCatalog.BoneFound.Points, ScoreEventCatalog.BoneFound.Label);
                if (digger >= 0) _context.CreditDog(digger);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue($"Cocoa called it, Cheddar dug it up! ({_puzzle.Finds}/{FindsNeeded})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "BONE!");
                _context.SpawnWorldPop(digPos, "BONE!", new Color(0.5f, 0.9f, 0.55f));
                _context.LogEvent("BoneFound", $"{_puzzle.Finds}/{FindsNeeded}");
                SetMoundOverride(_lastActedMound, FinalGameplayArt.BoneRelayMoundFound);
                if (_puzzle.Solved) CompleteBoneDetail(digPos);
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
                SetMoundOverride(_lastActedMound, FinalGameplayArt.BoneRelayMoundWrong);
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
                if (_moundArt != null && _moundArt[i] != null)
                {
                    string overridePath = _moundOverrideArt != null && !string.IsNullOrEmpty(_moundOverrideArt[i])
                        && _context.Now() < _moundOverrideUntil[i]
                        ? _moundOverrideArt[i]
                        : null;
                    MissionPropArt.SetSprite(_moundArt[i], !string.IsNullOrEmpty(overridePath)
                        ? overridePath
                        : isCall
                            ? FinalGameplayArt.BoneRelayMoundCalled
                            : FinalGameplayArt.BoneRelayMoundUnknown);
                }
                // The static DIG? identity stays; the called mound reads through the gold tint,
                // called sprite, and the badge instead of a DIG HERE! text flip.
                ActorSignalBadge.SetStationSignal(_mounds[i], isCall);
            }
            // The relay's distance signal alternates: post while Cocoa owes a sniff, called mound after.
            ActorSignalBadge.SetStationSignal(_scentPost, !_puzzle.Known && !_puzzle.Solved && !_failed);
            if (_scentPost != null && _scentPost.TryGetComponent<SpriteRenderer>(out var psr))
                psr.color = _puzzle.Known ? MoundCallColor : new Color(0.7f, 0.6f, 0.95f);
            MissionPropArt.SetSprite(_scentPostArt, _puzzle.Known
                ? FinalGameplayArt.BoneRelayScentPostCalled
                : FinalGameplayArt.BoneRelayScentPostIdle);
            // CF2.3: mounds revert their found/wrong art on a timer because the same mound can be
            // re-called later (see SetMoundOverride) - so nothing in the world shows overall relay
            // progress once a call fades. The scent post is the one fixed prop that's always in
            // frame regardless of which mound is live; give it a persistent tally + warming tint
            // layered under its existing idle/called reactive state (reuses the same sprites/tint,
            // no new art), the same "baseline under the reactive state" pattern CF1.4 shipped.
            _scentPostArt?.SetTint(Color.Lerp(Color.white, new Color(1f, 0.9f, 0.55f), RelayProgress));
            if (_scentPostLabel != null)
                _scentPostLabel.text = _puzzle.Solved
                    ? "SCENT POST"
                    : $"SCENT POST ({_puzzle.Finds}/{FindsNeeded} FOUND)";
        }

        private void RevealFromCocoaBark(int readerIndex)
        {
            ClearMoundOverrides();
            _puzzle.Reveal();
            _context.CreditDog(readerIndex);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
            _context.SetCue("Cocoa caught the scent and barked the call - Cheddar, dig the glowing mound!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "SCENT CALLED!");
            _context.SpawnWorldPop(ScentZonePos + Vector2.up, "WOOF - FOUND IT!", new Color(0.65f, 0.9f, 1f));
            _context.RequestRumble("bone_scent_call", 0.1f, 0.24f, 0.09f);
            _context.LogEvent("BoneScentCalled", $"mound {_puzzle.RevealedTarget + 1}");
            _context.LogObjectiveChanged();
            UpdateMoundVisuals();
        }

        private void CompleteBoneDetail(Vector2 digPos)
        {
            _successHoldRemaining = SuccessHoldSeconds;
            _context.SetFeedback(GameManager.FeedbackKind.LevelClear);
            _context.SetCue("Bone detail complete! Cocoa's nose and Cheddar's paws uncovered the whole stash.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "BONE STASH!");
            _context.SpawnWorldPop(digPos + Vector2.up, "THREE BONES!", new Color(1f, 0.85f, 0.3f));
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowProudBrief();
            _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
            _context.RequestRumble("bone_detail_complete", 0.28f, 0.55f, 0.2f);
            _context.LogEvent("BoneDetailComplete", "Holding live-world scent-and-dig payoff");
            _context.LogObjectiveChanged();
        }

        private void BuildScene()
        {
            _mounds = new GameObject[MoundSpots.Length];
            _moundArt = new MissionPropArtAttachment[MoundSpots.Length];
            _moundOverrideArt = new string[MoundSpots.Length];
            _moundOverrideUntil = new float[MoundSpots.Length];
            for (int i = 0; i < MoundSpots.Length; i++)
            {
                var go = new GameObject($"BoneMound_{i}");
                go.transform.position = MoundSpots[i];
                go.transform.localScale = new Vector3(1.8f, 1.1f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                if (_context.ActorSprite != null) sr.sprite = _context.ActorSprite;
                sr.color = MoundIdleColor;
                _context.AddWorldLabel(go, "DIG?", Vector3.up * 1.2f, 13, Color.white);
                _moundArt[i] = MissionPropArt.AttachObject(go, FinalGameplayArt.BoneRelayMoundUnknown, 0.012f, 18, true);
                go.SetActive(false);
                _mounds[i] = go;
            }

            _scentPost = new GameObject("ScentPost");
            _scentPost.transform.position = ScentZonePos;
            _scentPost.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            var psr = _scentPost.AddComponent<SpriteRenderer>();
            if (_context.ActorSprite != null) psr.sprite = _context.ActorSprite;
            psr.color = new Color(0.7f, 0.6f, 0.95f);
            // Idle/called sprites, the badge while a sniff is owed, and the HUD objective line
            // carry the sniff-then-dig relay instruction; the label itself now also carries the
            // CF2.3 persistent found tally (see UpdateMoundVisuals).
            _scentPostLabel = _context.AddWorldLabel(_scentPost, "SCENT POST", Vector3.up * 1.5f, 11, Color.white);
            _scentPostArt = MissionPropArt.AttachObject(_scentPost, FinalGameplayArt.BoneRelayScentPostIdle, 0.012f, 18, true);
            _scentPost.SetActive(false);
        }

        private void SetSceneActive(bool active)
        {
            if (_scentPost != null) _scentPost.SetActive(active);
            if (_mounds == null) return;
            foreach (var m in _mounds)
                if (m != null) m.SetActive(active);
        }

        private void ClearMoundOverrides()
        {
            if (_moundOverrideArt == null) return;
            for (int i = 0; i < _moundOverrideArt.Length; i++)
            {
                _moundOverrideArt[i] = null;
                _moundOverrideUntil[i] = 0f;
            }
        }

        /// <summary>
        /// Timed, not permanent: the puzzle's random target sequence can call the same mound again
        /// for a later find, and a stale "FOUND!"/"WRONG!" override would then hide the current call
        /// (badge/tint would say "dig here", but the sprite would still say "already dug"). Matches
        /// the same timed-override pattern used in GreatEscape/ChaosMachine's station reactions.
        /// </summary>
        private void SetMoundOverride(int index, string resourcePath, float seconds = 1f)
        {
            if (_moundOverrideArt == null || index < 0 || index >= _moundOverrideArt.Length) return;
            _moundOverrideArt[index] = resourcePath;
            _moundOverrideUntil[index] = _context.Now() + seconds;
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

        /// <summary>Nearest active mound within dig range, or -1 - the Interact-press dig target.</summary>
        private int FindNearestActiveMoundInRange(int dogIndex)
        {
            if (_mounds == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length || _context.Dogs[dogIndex] == null)
                return -1;
            Vector2 pos = _context.Dogs[dogIndex].transform.position;
            int best = -1;
            float bestDist = DigRange;
            for (int i = 0; i < _mounds.Length; i++)
            {
                if (_mounds[i] == null || !_mounds[i].activeSelf) continue;
                float d = Vector2.Distance(pos, _mounds[i].transform.position);
                if (d <= bestDist) { bestDist = d; best = i; }
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
