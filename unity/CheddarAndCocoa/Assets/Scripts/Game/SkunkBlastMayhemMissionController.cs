using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned Skunk Blast Mayhem: a skunk guards a prized dead bird in the backyard.
    /// Cheddar's loud bark holds the skunk's attention (the lure) while Cocoa sneaks in from behind
    /// for the clean grab (the snatch) - but the skunk's tail-lift telegraph is a shared danger clock
    /// both dogs must read and bail from, or whoever is still in range gets hilariously skunked.
    /// A sprayed dog goes STINKY (can no longer lure/snatch until clean) and the pair must detour into
    /// the house to rub a laundry pile clean, with the other dog hauling fresh laundry from the basket
    /// to keep the pile from running dry. Fail-forward: getting skunked costs the de-skunk detour, not
    /// the run - the shared round timer is this mission's only hard fail condition.
    /// </summary>
    public sealed class SkunkBlastMayhemMissionController : IMissionController, IMissionInteractionController,
        IMissionPressureHud, IMissionSuccessPresentationController
    {
        private const float TailLiftInterval = 4.5f;
        private const float TelegraphSeconds = 1.3f;
        private const int RubsToClean = 3;
        private const int PileCapacity = 3;
        private const int BasketCapacity = 5;
        private const float AirDryRubsPerSecond = 1f / 20f;
        private const float LureHoldSeconds = 2.2f;
        private const float LureRange = 5f;
        private const float GrabRange = 2.2f;
        private const float PileRubRange = 2f;
        private const float BasketHaulRange = 2f;
        private const float CheddarBlastRadius = 3.4f; // he over-commits and reads the telegraph late
        private const float CocoaBlastRadius = 2f;      // she reads the tail tells earliest and bails fast
        private const float SuccessHoldSeconds = 1.15f;

        private static readonly Color CalmColor = new(0.45f, 0.3f, 0.5f);
        private static readonly Color TailUpColor = new(0.9f, 0.25f, 0.15f);
        private static readonly Color LuredColor = new(0.8f, 0.55f, 0.9f);
        private static readonly Color StinkColor = new(0.55f, 0.75f, 0.2f);
        private static readonly Color FreshLaundryColor = new(0.75f, 0.85f, 1f);
        private static readonly Color FunkyLaundryColor = new(0.6f, 0.55f, 0.35f);

        private readonly CoopSkunkHeistPuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _skunkObj;
        private GameObject _prizeObj;
        private TextMesh _prizeLabel;
        private GameObject _basketObj;
        private GameObject _pileObj;
        private float _lureHeldUntil;
        private float _successHoldRemaining;

        public CoopSkunkHeistPuzzle Puzzle => _puzzle;
        public GameObject PrizeObject => _prizeObj;
        public GameObject BasketObject => _basketObj;
        public GameObject PileObject => _pileObj;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.SkunkBlastMayhem;
        public bool IsComplete => _puzzle.PrizeSecured && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _puzzle.PrizeSecured && _successHoldRemaining > 0f;

        // Fail-forward by design (spec: "getting skunked costs the de-skunk detour, not the run").
        // No controller-owned hard fail condition; only the shared round timeout can end this run.
        public bool IsFailed => false;
        public string FailReason => null;

        public Vector2 EntryTarget => SkunkPos;
        public string OutcomeSummary => _puzzle.PrizeSecured
            ? (_puzzle.SkunkEvents > 0 ? "Reeked But Victorious" : "Clean Heist")
            : _puzzle.SkunkEvents > 0 ? "Reeking In The Yard" : "Still Casing The Bird";

        public bool LureActive => _context != null && _context.Now() < _lureHeldUntil;

        public bool PressureVisible => !_puzzle.PrizeSecured;
        public string PressureLabel => _puzzle.TailUp ? "TAIL UP - BAIL!" : "SKUNK CALM";
        public float PressureNormalized => _puzzle.TailUp
            ? 1f
            : Mathf.Clamp01(1f - _puzzle.TimeToNextTailLift / TailLiftInterval);
        public Color PressureColor => Color.Lerp(new Color(0.4f, 0.85f, 0.5f), TailUpColor, PressureNormalized);

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome)
                    return "Bird secured! Cheddar held the skunk's attention while Cocoa made the clean snatch.";
                if (_puzzle.BothDogsStinky)
                    return "BOTH DOGS SKUNKED! Get to the laundry pile - whoever finishes rubbing first hauls for the other.";
                if (_puzzle.CheddarStinky)
                    return "Cheddar is STINKY - Cocoa haul fresh laundry from the basket while he rubs the pile clean!";
                if (_puzzle.CocoaStinky)
                    return "Cocoa is STINKY - Cheddar haul fresh laundry from the basket while she rubs the pile clean!";
                if (_puzzle.TailUp)
                    return "THE TAIL IS UP - both dogs bail from the skunk now!";
                if (LureActive)
                    return "Skunk's watching Cheddar - Cocoa, sneak in and snatch the bird!";
                return "Cheddar: bark near the skunk to hold its attention. Cocoa: wait for the opening.";
            }
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _skunkObj = context.PredatorObject;
            BuildProps();
            Cleanup();
        }

        public void StartMission()
        {
            _puzzle.Configure(TailLiftInterval, TelegraphSeconds, RubsToClean, PileCapacity, BasketCapacity, AirDryRubsPerSecond);
            _lureHeldUntil = 0f;
            _successHoldRemaining = 0f;

            if (_prizeObj != null)
            {
                _prizeObj.transform.position = PrizePos;
                _prizeObj.SetActive(true);
            }
            if (_basketObj != null)
            {
                _basketObj.transform.position = BasketPos;
                _basketObj.SetActive(true);
                _context.SetActorState(_basketObj, "FRESH LAUNDRY BASKET", FreshLaundryColor, 0.05f);
            }
            if (_pileObj != null)
            {
                _pileObj.transform.position = PilePos;
                _pileObj.SetActive(true);
                _context.SetActorState(_pileObj, "RUB SPOT - CLEAN FOR NOW", new Color(0.6f, 0.6f, 0.6f), 0.05f);
            }
            ActivateSkunk();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.PrizeSecured)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }
            bool tailUpPre = _puzzle.TailUp;
            bool cheddarStinkyPre = _puzzle.CheddarStinky;
            bool cocoaStinkyPre = _puzzle.CocoaStinky;

            bool cheddarInBlast = DistanceOf(DogId.Cheddar) <= CheddarBlastRadius;
            bool cocoaInBlast = DistanceOf(DogId.Cocoa) <= CocoaBlastRadius;
            _puzzle.Advance(deltaTime, cheddarInBlast, cocoaInBlast);

            if (!tailUpPre && _puzzle.TailUp) AnnounceTailLift();
            else if (tailUpPre && !_puzzle.TailUp) ResolveSprayFeedback(cheddarStinkyPre, cocoaStinkyPre);

            if (cheddarStinkyPre && !_puzzle.CheddarStinky) AnnounceAirDried(DogId.Cheddar);
            if (cocoaStinkyPre && !_puzzle.CocoaStinky) AnnounceAirDried(DogId.Cocoa);

            UpdateSkunkPresentation();
            UpdateLaundryPresentation();
        }

        public bool HandleBark(int dogIndex)
        {
            if (_puzzle.PrizeSecured || dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length) return false;
            if (!_context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)) return false;

            if (identity.Id != DogId.Cheddar)
            {
                _context.SetCue("Cocoa stays quiet and sneaks - let Cheddar make the noise!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CHEDDAR LURES!");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position, "STAY QUIET", new Color(0.55f, 0.82f, 1f));
                _context.MarkFailedInteraction(DogId.Cocoa, "Cheddar is the one who lures the skunk's attention");
                return true;
            }

            if (_puzzle.CheddarStinky)
            {
                _context.SetCue("Cheddar reeks - the skunk can smell him coming! Get him clean first.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "TOO STINKY TO LURE!");
                _context.MarkFailedInteraction(DogId.Cheddar, "Cheddar is too stinky to lure until he's clean");
                return true;
            }

            if (_skunkObj == null || Vector2.Distance(_context.Dogs[dogIndex].transform.position, _skunkObj.transform.position) > LureRange)
            {
                _context.MarkFailedInteraction(DogId.Cheddar, "get closer to the skunk to hold its attention");
                return true;
            }

            RefreshLure();
            return true;
        }

        public bool HandleInteract(int dogIndex)
        {
            if (_puzzle.PrizeSecured || dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length) return false;
            if (!_context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)) return false;

            DogId dog = identity.Id;
            bool stinky = dog == DogId.Cheddar ? _puzzle.CheddarStinky : _puzzle.CocoaStinky;
            Vector2 pos = _context.Dogs[dogIndex].transform.position;

            if (stinky)
            {
                if (_pileObj == null || Vector2.Distance(pos, _pileObj.transform.position) > PileRubRange)
                {
                    _context.MarkFailedInteraction(dog, "get to the laundry pile to rub off the stink");
                    return true;
                }
                if (!_puzzle.Rub(dog))
                {
                    _context.SetCue("No fresh laundry left in the pile - stuck airing out slowly!");
                    _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "OUT OF LAUNDRY!");
                    _context.MarkFailedInteraction(dog, "the pile is out of fresh laundry - haul more or wait it out");
                    return true;
                }
                HandleRubSuccess(dog);
                return true;
            }

            if (_puzzle.AnyDogStinky)
            {
                if (_basketObj == null || Vector2.Distance(pos, _basketObj.transform.position) > BasketHaulRange)
                {
                    _context.MarkFailedInteraction(dog, "get to the laundry basket to haul fresh laundry");
                    return true;
                }
                if (!_puzzle.HaulLaundry())
                {
                    _context.SetCue("The laundry basket is empty - no more fresh supply!");
                    _context.MarkFailedInteraction(dog, "the basket is out of fresh laundry");
                    return true;
                }
                HandleHaulSuccess(dog);
                return true;
            }

            if (dog != DogId.Cocoa)
            {
                _context.SetCue("Cheddar's too loud for the sneak - let Cocoa make the grab!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "COCOA SNATCHES!");
                _context.MarkFailedInteraction(DogId.Cheddar, "Cocoa is the one who sneaks in for the grab");
                return true;
            }

            if (_prizeObj == null || Vector2.Distance(pos, _prizeObj.transform.position) > GrabRange)
            {
                _context.MarkFailedInteraction(dog, "get closer to the bird to grab it");
                return true;
            }

            if (!_puzzle.TryGrab(true, LureActive))
            {
                if (_puzzle.TailUp)
                {
                    _context.SetCue("The tail is UP - do not reach for the bird now!");
                    _context.MarkFailedInteraction(dog, "wait for the tail-lift danger to pass");
                }
                else
                {
                    _context.SetCue("The skunk is still watching the bird - get Cheddar barking to pull its attention!");
                    _context.MarkFailedInteraction(dog, "Cheddar needs to hold the skunk's attention first");
                }
                return true;
            }

            HandleGrabSuccess();
            return true;
        }

        public void Cleanup()
        {
            if (_prizeObj != null) _prizeObj.SetActive(false);
            if (_basketObj != null) _basketObj.SetActive(false);
            if (_pileObj != null) _pileObj.SetActive(false);
            _lureHeldUntil = 0f;
            _successHoldRemaining = 0f;
        }

        public void StageDogsForEntry()
        {
            if (_context.SquirrelObject != null) _context.SquirrelObject.SetActive(false);
            ActivateSkunk();

            int cheddarIdx = _context.IndexOfDog(DogId.Cheddar);
            int cocoaIdx = _context.IndexOfDog(DogId.Cocoa);
            // Stage just outside LureRange so Cheddar's objective arrow reads immediately at cold
            // start instead of hiding because he's already "on target" (ObjectiveArrowFeedback hides
            // once distance <= hideDistance, and his hideDistance for this beat is LureRange).
            if (cheddarIdx >= 0) PlaceDog(cheddarIdx, SkunkPos + new Vector2(-7f, -2f));
            if (cocoaIdx >= 0) PlaceDog(cocoaIdx, SkunkPos + new Vector2(3f, -2.5f));
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null;
            copy = string.Empty;
            hideDistance = 1.4f;
            if (_puzzle.PrizeSecured || _context.Dogs == null) return false;

            bool isCheddar = _context.IndexOfDog(DogId.Cheddar) == dogIndex;
            if (isCheddar)
            {
                if (_puzzle.CheddarStinky)
                {
                    target = _pileObj?.transform;
                    copy = "RUB CLEAN";
                    hideDistance = PileRubRange;
                }
                else if (_puzzle.CocoaStinky)
                {
                    target = _basketObj?.transform;
                    copy = "HAUL LAUNDRY";
                    hideDistance = BasketHaulRange;
                }
                else
                {
                    target = _skunkObj?.transform;
                    copy = "BARK TO LURE";
                    hideDistance = LureRange;
                }
            }
            else
            {
                if (_puzzle.CocoaStinky)
                {
                    target = _pileObj?.transform;
                    copy = "RUB CLEAN";
                    hideDistance = PileRubRange;
                }
                else if (_puzzle.CheddarStinky)
                {
                    target = _basketObj?.transform;
                    copy = "HAUL LAUNDRY";
                    hideDistance = BasketHaulRange;
                }
                else
                {
                    target = _prizeObj?.transform;
                    copy = LureActive ? "SNATCH THE BIRD" : "WAIT FOR THE LURE";
                    hideDistance = GrabRange;
                }
            }

            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("skunk_blast_mayhem", score, timeRemaining, _puzzle.PrizeSecured ? 1 : 0, 1, _puzzle.SkunkEvents,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Deterministic seam for advancing past the live bird-secured payoff.</summary>
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        /// <summary>Test seam: resolve GameManager's dog-index space for a given dog identity.</summary>
        public int DogIndexOf(DogId dogId) => _context.IndexOfDog(dogId);

        private void RefreshLure()
        {
            bool wasActive = LureActive;
            _lureHeldUntil = _context.Now() + LureHoldSeconds;
            UpdateSkunkPresentation();
            if (wasActive) return;

            _context.SetCue("Cheddar's got the skunk's full attention - Cocoa, sneak in for the bird!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "SKUNK'S LOOKING AT CHEDDAR!");
            if (_skunkObj != null) _context.SpawnWorldPop(_skunkObj.transform.position, "LURED!", LuredColor);
            _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
            _context.LogEvent("SkunkLure", "held");
            _context.LogObjectiveChanged();
        }

        private void AnnounceTailLift()
        {
            if (_skunkObj != null)
            {
                _context.SetActorState(_skunkObj, "TAIL UP - SPRAY INCOMING!", TailUpColor, 0.4f);
                _context.SpawnWorldPop(_skunkObj.transform.position, "TAIL UP!", TailUpColor);
            }
            _context.SetFeedback(GameManager.FeedbackKind.PredatorAttack);
            _context.SetCue("THE TAIL IS UP! Both dogs bail from the skunk before it sprays!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "TAIL UP - BAIL!");
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.LogEvent("SkunkTailLift", "telegraph");
            _context.LogObjectiveChanged();
        }

        private void ResolveSprayFeedback(bool cheddarStinkyPre, bool cocoaStinkyPre)
        {
            bool cheddarNewlyStinky = !cheddarStinkyPre && _puzzle.CheddarStinky;
            bool cocoaNewlyStinky = !cocoaStinkyPre && _puzzle.CocoaStinky;

            if (!cheddarNewlyStinky && !cocoaNewlyStinky)
            {
                _context.AddScore(ScoreEventCatalog.SkunkBailed.Points, ScoreEventCatalog.SkunkBailed.Label);
                _context.SetCue("Both dogs bailed clean before the spray - nice reflexes!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "CLEAN BAIL!");
                _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                _context.LogEvent("SkunkBail", "clean");
                _context.LogObjectiveChanged();
                return;
            }

            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "SKUNKED!");
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestShake(0.2f);

            if (cheddarNewlyStinky && cocoaNewlyStinky)
            {
                _context.AddScore(ScoreEventCatalog.SkunkSprayed.Points * 2, ScoreEventCatalog.SkunkSprayed.Label);
                _context.SetCue("BOTH DOGS SKUNKED! The whole yard reeks - into the house for laundry duty!");
                if (_skunkObj != null) _context.SpawnWorldPop(_skunkObj.transform.position, "DOUBLE SKUNKED!", StinkColor);
                foreach (var feedback in _context.DogFeedback) feedback?.ShowPanic();
            }
            else if (cheddarNewlyStinky)
            {
                _context.AddScore(ScoreEventCatalog.SkunkSprayed.Points, ScoreEventCatalog.SkunkSprayed.Label);
                _context.SetCue("Cheddar got SKUNKED! He can't lure until he's scrubbed clean at the laundry pile.");
                PopAtDog(DogId.Cheddar, "SKUNKED!", StinkColor);
                ShowSadFor(DogId.Cheddar);
            }
            else
            {
                _context.AddScore(ScoreEventCatalog.SkunkSprayed.Points, ScoreEventCatalog.SkunkSprayed.Label);
                _context.SetCue("Cocoa got SKUNKED! Cheddar has to haul fresh laundry while she scrubs clean.");
                PopAtDog(DogId.Cocoa, "SKUNKED!", StinkColor);
                ShowSadFor(DogId.Cocoa);
            }
            _context.LogEvent("SkunkSprayed", $"cheddar={cheddarNewlyStinky} cocoa={cocoaNewlyStinky}");
            _context.LogObjectiveChanged();
        }

        private void AnnounceAirDried(DogId dog)
        {
            _context.SetCue($"{Name(dog)} finally aired out on their own - slow, but clean!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "AIRED OUT!");
            _context.LogEvent("SkunkAirDried", Name(dog));
            _context.LogObjectiveChanged();
        }

        private void HandleRubSuccess(DogId dog)
        {
            bool stillStinky = dog == DogId.Cheddar ? _puzzle.CheddarStinky : _puzzle.CocoaStinky;
            _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "SCRUB!");
            if (_pileObj != null) _context.SpawnWorldPop(_pileObj.transform.position, "SCRUB!", FreshLaundryColor);
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);

            if (!stillStinky)
            {
                int idx = _context.IndexOfDog(dog);
                if (idx >= 0) _context.CreditDog(idx);
                _context.AddScore(ScoreEventCatalog.SkunkDeSkunked.Points, ScoreEventCatalog.SkunkDeSkunked.Label);
                _context.SetCue($"{Name(dog)} is finally clean - back to the heist!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "FRESH AGAIN!");
                if (_pileObj != null) _context.SpawnWorldPop(_pileObj.transform.position, "FRESH AGAIN!", new Color(0.55f, 0.95f, 0.6f));
                _context.RequestRumble("skunk_clean", 0.18f, 0.36f, 0.14f);
            }
            else
            {
                _context.SetCue($"{Name(dog)} scrubs again - still stinky.");
            }
            _context.LogEvent("SkunkRub", $"{Name(dog)} progress");
            _context.LogObjectiveChanged();
        }

        private void HandleHaulSuccess(DogId dog)
        {
            int idx = _context.IndexOfDog(dog);
            if (idx >= 0) _context.CreditDog(idx);
            _context.AddScore(ScoreEventCatalog.LaundryHauled.Points, ScoreEventCatalog.LaundryHauled.Label);
            _context.SetCue($"{Name(dog)} dragged fresh laundry from the basket to the pile!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.ScoreDelta, $"+{ScoreEventCatalog.LaundryHauled.Points} LAUNDRY HAULED");
            if (_basketObj != null) _context.SpawnWorldPop(_basketObj.transform.position, "HAULED!", FreshLaundryColor);
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            _context.LogEvent("SkunkLaundryHaul", Name(dog));
            _context.LogObjectiveChanged();
        }

        private void HandleGrabSuccess()
        {
            _successHoldRemaining = SuccessHoldSeconds;
            _context.AddScore(ScoreEventCatalog.BirdSecured.Points, ScoreEventCatalog.BirdSecured.Label);
            int cocoaIdx = _context.IndexOfDog(DogId.Cocoa);
            if (cocoaIdx >= 0) _context.CreditDog(cocoaIdx);
            _context.SetFeedback(GameManager.FeedbackKind.LevelClear);
            _context.SetCue("Cocoa snatched the prized dead bird clean while Cheddar held the skunk's attention!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "BIRD SECURED!");
            if (_prizeObj != null) _context.SpawnWorldPop(_prizeObj.transform.position, "BIRD SECURED!", new Color(1f, 0.86f, 0.28f));
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowProudBrief();
            _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
            _context.RequestRumble("skunk_heist_clear", 0.3f, 0.55f, 0.2f);
            if (_prizeObj != null) _prizeObj.SetActive(false);
            if (_skunkObj != null) _context.SetActorState(_skunkObj, "SKUNK GIVES UP - PRIZE'S GONE!", Color.gray, 0.1f);
            _context.LogEvent("SkunkHeistPayoff", "Cheddar lured; Cocoa snatched the bird clean");
        }

        private void ActivateSkunk()
        {
            if (_skunkObj == null) return;
            _skunkObj.SetActive(true);
            _skunkObj.transform.position = SkunkPos;
            _context.SetActorState(_skunkObj, "SKUNK GUARDING THE BIRD - STAY BACK!", CalmColor, 0.06f);
        }

        private void UpdateSkunkPresentation()
        {
            if (_skunkObj == null || _puzzle.PrizeSecured || _puzzle.TailUp) return;
            _context.SetActorState(_skunkObj,
                LureActive ? "SKUNK FACING CHEDDAR - COCOA, GO!" : "SKUNK GUARDING THE BIRD - STAY BACK!",
                LureActive ? LuredColor : CalmColor, 0.05f);
        }

        private void UpdateLaundryPresentation()
        {
            if (_pileObj != null)
            {
                _context.SetActorState(_pileObj,
                    $"RUB PILE - FRESH {_puzzle.PileFresh}/{_puzzle.PileCapacity}",
                    _puzzle.PileFresh > 0 ? FreshLaundryColor : FunkyLaundryColor, 0f);
            }
            if (_basketObj != null)
            {
                _context.SetActorState(_basketObj, $"LAUNDRY BASKET - SUPPLY {_puzzle.BasketSupply}",
                    _puzzle.BasketSupply > 0 ? FreshLaundryColor : FunkyLaundryColor, 0f);
            }
        }

        private void BuildProps()
        {
            _prizeObj = new GameObject("SkunkBlastPrizeBird");
            _prizeObj.transform.localScale = new Vector3(0.9f, 0.55f, 1f);
            var prizeRenderer = _prizeObj.AddComponent<SpriteRenderer>();
            if (_context.ActorSprite != null) prizeRenderer.sprite = _context.ActorSprite;
            prizeRenderer.color = new Color(0.32f, 0.28f, 0.24f);
            prizeRenderer.sortingOrder = 5;
            _prizeLabel = _context.AddWorldLabel(_prizeObj, "DEAD BIRD", Vector3.up * 1f, 11, Color.white);
            _prizeObj.SetActive(false);

            // Reuse Sock Panic's authored laundry-basket art rather than inventing new art surfaces
            // for this mission's basket/pile - both are literally laundry baskets in-fiction too.
            _basketObj = _context.CreateActor(ArenaArtCatalog.ActorKind.LaundryBasket);
            _basketObj.name = "SkunkBlastLaundryBasket";
            MissionPropArt.AttachObject(_basketObj, FinalGameplayArt.SockPanicBasketClosed, 0.013f, 18, true);
            _basketObj.SetActive(false);

            _pileObj = _context.CreateActor(ArenaArtCatalog.ActorKind.LaundryBasket);
            _pileObj.name = "SkunkBlastLaundryPile";
            MissionPropArt.AttachObject(_pileObj, FinalGameplayArt.SockPanicBasketOpen, 0.013f, 18, true);
            _pileObj.SetActive(false);
        }

        private float DistanceOf(DogId dog)
        {
            int idx = _context.IndexOfDog(dog);
            if (idx < 0 || _skunkObj == null) return float.PositiveInfinity;
            return Vector2.Distance(_context.Dogs[idx].transform.position, _skunkObj.transform.position);
        }

        private void PopAtDog(DogId dog, string text, Color color)
        {
            int idx = _context.IndexOfDog(dog);
            if (idx >= 0) _context.SpawnWorldPop(_context.Dogs[idx].transform.position, text, color);
        }

        private void ShowSadFor(DogId dog)
        {
            int idx = _context.IndexOfDog(dog);
            if (idx >= 0 && idx < _context.DogFeedback.Length) _context.DogFeedback[idx]?.ShowPanic();
        }

        private void PlaceDog(int index, Vector2 point)
        {
            const float margin = 1.5f;
            _context.Dogs[index].transform.position = new Vector2(
                Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
                Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
            if (_context.Dogs[index].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        private static string Name(DogId dog) => dog == DogId.Cheddar ? "Cheddar" : "Cocoa";

        private Vector2 SkunkPos => _context.Bounds.center + new Vector2(0f, _context.Bounds.height * 0.28f);
        private Vector2 PrizePos => SkunkPos + new Vector2(0f, 1.8f);
        private Vector2 BasketPos => _context.Bounds.center + new Vector2(-_context.Bounds.width * 0.32f, -_context.Bounds.height * 0.28f);
        private Vector2 PilePos => BasketPos + new Vector2(4f, 0f);
    }
}
