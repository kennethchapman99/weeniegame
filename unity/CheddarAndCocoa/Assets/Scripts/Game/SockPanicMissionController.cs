using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class SockPanicMissionController : IMissionController, IMissionInteractionController,
        IMissionTreatCollector, IMissionSuccessPresentationController
    {
        private const float BasketInteractRange = 2.6f;
        private const float BasketHoldRange = 3.25f;
        private const float OpeningSeconds = 6f;
        private const float SuccessHoldSeconds = 1.15f;

        private readonly SockBasketMissionState _state = new();
        private MissionContext _context;
        private GameObject _basket;
        private Treat _exposedSock;
        private float _openingUntil;
        private float _basketFumbleUntil;
        private float _successHoldRemaining;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.SockPanic;
        public bool IsComplete => _state.SuccessfulDives >= _context.ObjectiveGoal && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _state.SuccessfulDives >= _context.ObjectiveGoal &&
                                                     _successHoldRemaining > 0f;
        public bool IsFailed => false;
        public string FailReason => null;
        public string OutcomeSummary => IsComplete ? "Socks Rescued"
            : _state.Fumbles > 0 ? "Laundry Day Chaos" : "Still Diving";
        public Vector2 EntryTarget => _context != null ? _context.Bounds.center : Vector2.zero;
        public SockBasketMissionState State => _state;
        public GameObject BasketObject => _basket;
        public Treat ExposedSock => _exposedSock;
        public bool SpawnTreatsHidden => true;

        public string ObjectiveLabel => IsPresentingSuccessfulOutcome
            ? "Sock mountain rescued! Cocoa holds steady while Cheddar claims the laundry prize."
            : _state.BasketOpen
                ? $"Cocoa: HOLD the basket. Cheddar: DIVE for the sock! ({_state.SuccessfulDives}/{_context.ObjectiveGoal} returned)"
                : $"Cocoa: Interact to tip the laundry basket and hold. Cheddar: get ready to dive. Socks {_state.SuccessfulDives}/{_context.ObjectiveGoal}, fumbles {_state.Fumbles}";

        public void Initialize(MissionContext context)
        {
            _context = context;
            _basket = _context.CreateActor(ArenaArtCatalog.ActorKind.LaundryBasket);
            MissionPropArt.AttachObject(_basket, FinalGameplayArt.SockPanicBasketClosed, 0.013f, 18, true);
            _basket.SetActive(false);
        }

        public void StartMission()
        {
            HideExposedSock();
            _state.Reset();
            _openingUntil = 0f;
            _basketFumbleUntil = 0f;
            _successHoldRemaining = 0f;
            _basket.transform.position = _context.Bounds.center;
            _basket.SetActive(true);
            SetBasketClosed("LAUNDRY BASKET - ONE DOG TIP, PARTNER DIVE!");
        }

        public void Tick(float deltaTime, float now)
        {
            UpdateBasketArt();
            if (_state.SuccessfulDives >= _context.ObjectiveGoal)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }
            if (_state.BasketOpen)
            {
                int cocoa = _context.IndexOfDog(DogId.Cocoa);
                if (cocoa < 0 || Vector2.Distance(_context.Dogs[cocoa].transform.position, _basket.transform.position) > BasketHoldRange)
                {
                    _state.ExpireOpening();
                    RegisterFumble("FUMBLE! Cocoa left the basket and it flopped shut before Cheddar's dive.");
                    return;
                }
            }
            if (!_state.BasketOpen || now < _openingUntil) return;
            _state.ExpireOpening();
            RegisterFumble("FUMBLE! The basket flopped shut on the runaway sock.");
        }

        public bool HandleBark(int dogIndex) => false;

        public bool HandleInteract(int dogIndex)
        {
            return TryTipBasket(dogIndex, false);
        }

        public bool HandleTreatCollected(Treat treat, int dogIndex)
        {
            if (treat == null) return false;
            if (treat != _exposedSock)
            {
                _context.MarkFailedInteraction(DogIdAt(dogIndex), "tip the laundry basket first");
                return true;
            }

            if (DogIdAt(dogIndex) != DogId.Cheddar)
            {
                SetTreatProp(treat, FinalGameplayArt.SockPanicSockDecoy);
                _state.TryCollect(dogIndex);
                RegisterFumble("DECOY! Cocoa has to hold the basket while Cheddar dives for the sock.");
                return true;
            }

            var result = _state.TryCollect(dogIndex);
            if (result == SockBasketMissionState.CollectResult.BasketClosed)
            {
                _context.MarkFailedInteraction(DogIdAt(dogIndex), "tip the laundry basket first");
                return true;
            }
            if (result == SockBasketMissionState.CollectResult.SameDogDecoy)
            {
                SetTreatProp(treat, FinalGameplayArt.SockPanicSockDecoy);
                RegisterFumble("DECOY! The basket-tipper needs their partner to dive.");
                return true;
            }

            SetTreatProp(treat, FinalGameplayArt.SockPanicSockSaved);
            _exposedSock = null;
            SetBasketClosed("LAUNDRY BASKET - TIP AGAIN!");
            _context.AddScore(ScoreEventCatalog.SockDive.Points, ScoreEventCatalog.SockDive.Label);
            if (dogIndex >= 0) _context.CreditDog(dogIndex);
            string scoreLabel = $"+{ScoreEventCatalog.SockDive.Points} {ScoreEventCatalog.SockDive.Label}";
            _context.SetCue($"{DogName(dogIndex)} recovered a dramatic sock!");
            _context.Pulse(dogIndex >= 0 && dogIndex < _context.Dogs.Length ? _context.Dogs[dogIndex].gameObject : null, 1.2f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.ScoreDelta, scoreLabel);
            _context.SpawnWorldPop(treat.transform.position, scoreLabel, new Color(0.62f, 0.9f, 1f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            _context.RecoverCollectible(treat);
            _context.LogEvent("Collection", $"{DogName(dogIndex)} collected a sock {_state.SuccessfulDives}/{_context.ObjectiveGoal}");
            if (_state.SuccessfulDives >= _context.ObjectiveGoal)
                CompleteSockRescue(treat.transform.position);
            _context.LogObjectiveChanged();
            return true;
        }

        public void Cleanup()
        {
            HideExposedSock();
            if (_basket != null) _basket.SetActive(false);
        }

        public void StageDogsForEntry()
        {
            if (_context.Dogs == null || _context.Dogs.Length < 2) return;
            Vector2 center = _context.Bounds.center;
            Vector2 staging = center + Vector2.down * 7f;
            _context.Dogs[0].transform.position = staging + Vector2.left * 1.5f;
            _context.Dogs[1].transform.position = staging + Vector2.right * 1.5f;
            foreach (var dog in _context.Dogs)
                if (dog != null && dog.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            if (_state.BasketOpen && dogIndex != _state.OpenerDogIndex)
            {
                target = _exposedSock != null ? _exposedSock.transform : null;
                copy = DogIdAt(dogIndex) == DogId.Cheddar ? "DIVE FOR SOCK" : "HOLD BASKET";
                hideDistance = 1.2f;
            }
            else
            {
                target = _basket != null ? _basket.transform : null;
                copy = DogIdAt(dogIndex) == DogId.Cocoa
                    ? _state.BasketOpen ? "HOLD BASKET" : "INTERACT TO TIP"
                    : _state.BasketOpen ? "DIVE FOR SOCK" : "WAIT TO DIVE";
                hideDistance = BasketInteractRange;
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("sock_panic", score, timeRemaining, _state.SuccessfulDives, _context.ObjectiveGoal, _state.Fumbles,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceTip(DogId dogId) => TryTipBasket(_context.IndexOfDog(dogId), true);
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        public void ForceTimeout()
        {
            if (!_state.ExpireOpening()) return;
            RegisterFumble("FUMBLE! The basket flopped shut on the runaway sock.");
        }

        private bool TryTipBasket(int dogIndex, bool force)
        {
            if (dogIndex < 0 || _basket == null || _state.SuccessfulDives >= _context.ObjectiveGoal) return false;
            if (DogIdAt(dogIndex) != DogId.Cocoa)
            {
                _context.MarkFailedInteraction(DogIdAt(dogIndex), "Cocoa anchors this basket; Cheddar gets ready to dive");
                _context.SetCue("Cheddar cannot hold still long enough - Cocoa must Interact to anchor the basket.");
                return false;
            }
            if (force)
            {
                _context.Dogs[dogIndex].transform.position = _basket.transform.position;
                if (_context.Dogs[dogIndex].TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            }
            if (!force && Vector2.Distance(_context.Dogs[dogIndex].transform.position, _basket.transform.position) > BasketInteractRange)
            {
                _context.MarkFailedInteraction(DogIdAt(dogIndex), "too far from laundry basket");
                return false;
            }
            if (!_state.TryOpen(dogIndex))
            {
                _context.MarkFailedInteraction(DogIdAt(dogIndex), "basket already held open");
                return false;
            }

            _exposedSock = _context.AcquireHiddenTreat();
            if (_exposedSock == null)
            {
                _state.ExpireOpening();
                return false;
            }

            _exposedSock.transform.position = _basket.transform.position + Vector3.right * 2f;
            _exposedSock.gameObject.SetActive(true);
            SetTreatProp(_exposedSock, FinalGameplayArt.SockPanicSockExposed);
            _openingUntil = _context.Now() + OpeningSeconds;
            _context.AddScore(ScoreEventCatalog.BasketTipped.Points, ScoreEventCatalog.BasketTipped.Label);
            _context.CreditDog(dogIndex);
            _context.SetCue($"{DogName(dogIndex)} tipped the basket - partner dive for the sock!");
            // The open basket is a closing timed window: pulse into the urgency channel (0.26+)
            // so the partner reads the distance signal badge, not just the close-range text.
            _context.SetActorState(_basket, "BASKET HELD OPEN - PARTNER DIVE NOW!", new Color(0.96f, 0.72f, 0.32f), 0.3f);
            MissionPropArt.SetSprite(_basket.GetComponent<MissionPropArtAttachment>(), FinalGameplayArt.SockPanicBasketOpen);
            _context.SpawnWorldPop(_basket.transform.position, "TIP! PARTNER DIVE!", new Color(0.62f, 0.9f, 1f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
            _context.LogEvent("SockBasket", $"{DogName(dogIndex)} tipped the basket");
            _context.LogObjectiveChanged();
            return true;
        }

        private void CompleteSockRescue(Vector3 position)
        {
            _successHoldRemaining = SuccessHoldSeconds;
            _context.SetFeedback(GameManager.FeedbackKind.LevelClear);
            _context.SetCue("Sock mountain rescued! Cocoa held the line and Cheddar stole every last prize.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "SOCK MOUNTAIN!");
            _context.SpawnWorldPop(position + Vector3.up, "SOCK MOUNTAIN RESCUED!", new Color(0.65f, 0.9f, 1f));
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowProudBrief();
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("sock_rescue", 0.2f, 0.45f, 0.16f);
            _context.LogEvent("SockPanicPayoff", "Cocoa anchored; Cheddar recovered the sock mountain");
        }

        private void RegisterFumble(string cue)
        {
            HideExposedSock();
            _basketFumbleUntil = _context.Now() + 1f;
            _context.AddScore(ScoreEventCatalog.SockDecoy.Points, ScoreEventCatalog.SockDecoy.Label);
            _context.SetCue(cue);
            SetBasketClosed("LAUNDRY BASKET - TIP AGAIN!");
            _context.SpawnWorldPop(_basket != null ? _basket.transform.position : Vector3.zero, "DECOY FUMBLE!", new Color(1f, 0.45f, 0.25f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
            _context.LogEvent("SockFumble", cue);
            _context.LogObjectiveChanged();
        }

        private void HideExposedSock()
        {
            if (_exposedSock != null) _exposedSock.gameObject.SetActive(false);
            _exposedSock = null;
        }

        private void SetBasketClosed(string label)
        {
            _openingUntil = 0f;
            if (_basket != null)
            {
                _context.SetActorState(_basket, label, new Color(0.78f, 0.56f, 0.3f), 0.08f);
                UpdateBasketArt();
            }
        }

        private void UpdateBasketArt()
        {
            if (_basket == null || _context == null) return;
            string path = _state.BasketOpen
                ? FinalGameplayArt.SockPanicBasketOpen
                : _context.Now() < _basketFumbleUntil
                    ? FinalGameplayArt.SockPanicBasketFumble
                    : FinalGameplayArt.SockPanicBasketClosed;
            MissionPropArt.SetSprite(_basket.GetComponent<MissionPropArtAttachment>(), path);
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

        private DogId DogIdAt(int dogIndex) => _context.Dogs != null && dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
            _context.Dogs[dogIndex] != null && _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                ? identity.Id : DogId.Cheddar;

        private string DogName(int dogIndex) => dogIndex >= 0 && dogIndex < _context.Dogs.Length && _context.Dogs[dogIndex] != null
            ? _context.Dogs[dogIndex].name : "Dog";
    }
}
