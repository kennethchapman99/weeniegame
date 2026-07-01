using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class SockPanicMissionController : IMissionController, IMissionInteractionController, IMissionTreatCollector
    {
        private const float BasketInteractRange = 2.6f;
        private const float OpeningSeconds = 6f;

        private readonly SockBasketMissionState _state = new();
        private MissionContext _context;
        private GameObject _basket;
        private Treat _exposedSock;
        private float _openingUntil;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.SockPanic;
        public bool IsComplete => _state.SuccessfulDives >= _context.ObjectiveGoal;
        public bool IsFailed => false;
        public string FailReason => null;
        public string OutcomeSummary => null;
        public Vector2 EntryTarget => _context != null ? _context.Bounds.center : Vector2.zero;
        public SockBasketMissionState State => _state;
        public GameObject BasketObject => _basket;
        public Treat ExposedSock => _exposedSock;
        public bool SpawnTreatsHidden => true;

        public string ObjectiveLabel => _state.BasketOpen
            ? $"Basket open! Partner dive for the sock ({_state.SuccessfulDives}/{_context.ObjectiveGoal} returned)"
            : $"Tip the laundry basket, then partner-dive: socks {_state.SuccessfulDives}/{_context.ObjectiveGoal}, fumbles {_state.Fumbles}";

        public void Initialize(MissionContext context)
        {
            _context = context;
            _basket = _context.CreateActor(ArenaArtCatalog.ActorKind.LaundryBasket);
            MissionPropArt.AttachObject(_basket, FinalGameplayArt.MissionLaundryBasket, 0.013f, 18, true);
            _basket.SetActive(false);
        }

        public void StartMission()
        {
            HideExposedSock();
            _state.Reset();
            _openingUntil = 0f;
            _basket.transform.position = _context.Bounds.center;
            _basket.SetActive(true);
            SetBasketClosed("LAUNDRY BASKET - ONE DOG TIP, PARTNER DIVE!");
        }

        public void Tick(float deltaTime, float now)
        {
            if (!_state.BasketOpen || now < _openingUntil) return;
            _state.ExpireOpening();
            RegisterFumble("FUMBLE! The basket flopped shut on the runaway sock.");
        }

        public bool HandleBark(int dogIndex) => false;

        public bool HandleInteract(int dogIndex)
        {
            TryTipBasket(dogIndex, false);
            return true;
        }

        public bool HandleTreatCollected(Treat treat, int dogIndex)
        {
            if (treat == null) return false;
            if (treat != _exposedSock)
            {
                _context.MarkFailedInteraction(DogIdAt(dogIndex), "tip the laundry basket first");
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
                RegisterFumble("DECOY! The basket-tipper needs their partner to dive.");
                return true;
            }

            _exposedSock = null;
            SetBasketClosed("LAUNDRY BASKET - TIP AGAIN!");
            _context.AddScore(ScoreEventCatalog.SockDive.Points, ScoreEventCatalog.SockDive.Label);
            string scoreLabel = $"+{ScoreEventCatalog.SockDive.Points} {ScoreEventCatalog.SockDive.Label}";
            _context.SetCue($"{DogName(dogIndex)} recovered a dramatic sock!");
            _context.Pulse(dogIndex >= 0 && dogIndex < _context.Dogs.Length ? _context.Dogs[dogIndex].gameObject : null, 1.2f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.ScoreDelta, scoreLabel);
            _context.SpawnWorldPop(treat.transform.position, scoreLabel, new Color(0.62f, 0.9f, 1f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            _context.RecoverCollectible(treat);
            _context.LogEvent("Collection", $"{DogName(dogIndex)} collected a sock {_state.SuccessfulDives}/{_context.ObjectiveGoal}");
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
                copy = "DIVE FOR SOCK";
                hideDistance = 1.2f;
            }
            else
            {
                target = _basket != null ? _basket.transform : null;
                copy = _state.BasketOpen ? "HOLD BASKET" : "TIP BASKET";
                hideDistance = BasketInteractRange;
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("sock_panic", score, timeRemaining, _state.SuccessfulDives, _context.ObjectiveGoal, _state.Fumbles,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceTip(DogId dogId) => TryTipBasket(_context.IndexOfDog(dogId), true);

        public void ForceTimeout()
        {
            if (!_state.ExpireOpening()) return;
            RegisterFumble("FUMBLE! The basket flopped shut on the runaway sock.");
        }

        private void TryTipBasket(int dogIndex, bool force)
        {
            if (dogIndex < 0 || _basket == null) return;
            if (!force && Vector2.Distance(_context.Dogs[dogIndex].transform.position, _basket.transform.position) > BasketInteractRange)
            {
                _context.MarkFailedInteraction(DogIdAt(dogIndex), "too far from laundry basket");
                return;
            }
            if (!_state.TryOpen(dogIndex))
            {
                _context.MarkFailedInteraction(DogIdAt(dogIndex), "basket already held open");
                return;
            }

            _exposedSock = _context.AcquireHiddenTreat();
            if (_exposedSock == null)
            {
                _state.ExpireOpening();
                return;
            }

            _exposedSock.transform.position = _basket.transform.position + Vector3.right * 2f;
            _exposedSock.gameObject.SetActive(true);
            _openingUntil = _context.Now() + OpeningSeconds;
            _context.AddScore(ScoreEventCatalog.BasketTipped.Points, ScoreEventCatalog.BasketTipped.Label);
            _context.SetCue($"{DogName(dogIndex)} tipped the basket - partner dive for the sock!");
            _context.SetActorState(_basket, "BASKET HELD OPEN - PARTNER DIVE NOW!", new Color(0.96f, 0.72f, 0.32f), 0.22f);
            MissionPropArt.SetSprite(_basket.GetComponent<MissionPropArtAttachment>(), FinalGameplayArt.MissionLaundryBasketOpen);
            _context.SpawnWorldPop(_basket.transform.position, "TIP! PARTNER DIVE!", new Color(0.62f, 0.9f, 1f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
            _context.LogEvent("SockBasket", $"{DogName(dogIndex)} tipped the basket");
            _context.LogObjectiveChanged();
        }

        private void RegisterFumble(string cue)
        {
            HideExposedSock();
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
                MissionPropArt.SetSprite(_basket.GetComponent<MissionPropArtAttachment>(), FinalGameplayArt.MissionLaundryBasket);
            }
        }

        private DogId DogIdAt(int dogIndex) => dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
            _context.Dogs[dogIndex] != null && _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                ? identity.Id : DogId.Cheddar;

        private string DogName(int dogIndex) => dogIndex >= 0 && dogIndex < _context.Dogs.Length && _context.Dogs[dogIndex] != null
            ? _context.Dogs[dogIndex].name : "Dog";
    }
}
