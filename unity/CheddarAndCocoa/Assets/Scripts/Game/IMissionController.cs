using System;
using System.Collections.Generic;
using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Mission-owned behavior invoked by the shared session orchestrator.</summary>
    public interface IMissionController
    {
        GameManager.MissionVariant Variant { get; }
        bool IsComplete { get; }

        /// <summary>True once the controller's own (non-timeout) fail condition has tripped.</summary>
        bool IsFailed { get; }

        /// <summary>Controller-owned fail-reason text, or null to defer to the shared/default reasons.</summary>
        string FailReason { get; }
        string ObjectiveLabel { get; }
        Vector2 EntryTarget { get; }

        /// <summary>Controller-owned end-of-round summary phrase, or null to use the shared default.</summary>
        string OutcomeSummary { get; }

        void Initialize(MissionContext context);
        void StartMission();
        void Tick(float deltaTime, float now);
        bool HandleBark(int dogIndex);
        void Cleanup();
        void StageDogsForEntry();
        bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance);
        MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome);
    }

    /// <summary>Optional input surface for missions whose primary verb is the shared interact action.</summary>
    public interface IMissionInteractionController
    {
        bool HandleInteract(int dogIndex);
    }

    /// <summary>Optional collection surface for controllers that interpret shared Treat actors.</summary>
    public interface IMissionTreatCollector
    {
        bool SpawnTreatsHidden { get; }
        bool HandleTreatCollected(Treat treat, int dogIndex);
    }

    /// <summary>
    /// Narrow shared-services bundle for mission controllers. It intentionally exposes dogs,
    /// arena presentation services, scoring, and session-safe callbacks—not GameManager itself.
    /// </summary>
    public sealed class MissionContext
    {
        public DogController[] Dogs { get; }
        public DogReadabilityFeedback[] DogFeedback { get; }
        public Rect Bounds { get; }
        public Sprite ActorSprite { get; }
        public Sprite RangeSprite { get; }
        public GameObject SquirrelObject { get; }
        public float SquirrelMoveSpeed { get; }
        public float SingleBarkSquirrelRange { get; }
        public float SingleBarkScareSeconds { get; }
        public float FirstSquirrelBaseDelay { get; }
        public float FirstSquirrelTroubleDelay { get; }
        public float SquirrelBaseDelay { get; }
        public float SquirrelTroubleDelay { get; }
        public int ItemScore { get; }
        public int MaxStolenFood { get; }
        public int SquirrelPenalty { get; }
        public int SquirrelScareScore { get; }
        public int PancakeSquirrelPenalty { get; }
        /// <summary>Shared panic/co-regulation state owned by GameManager; null on non-panic missions.</summary>
        public PanicMeter PanicMeter { get; }
        public Func<System.Random> Random { get; }
        public Func<float> Now { get; }
        public Func<GameManager.RoundModifier> ActiveModifier { get; }
        public Func<IReadOnlyList<Treat>> ActiveTreats { get; }
        /// <summary>True once the shared predator sequence has been resolved for this round.</summary>
        public Func<bool> IsPredatorResolved { get; }
        /// <summary>True once the shared tug-of-war sequence has been completed for this round.</summary>
        public Func<bool> IsTugComplete { get; }

        public Action<int, string> AddScore { get; }
        public Action<int> CreditDog { get; }
        public Action<string> SetCue { get; }
        public Action<GameManager.FeedbackKind> SetFeedback { get; }
        public Action<GameManager.JuiceFeedbackKind, string> SetJuice { get; }
        public Action<Vector2, string, Color> SpawnWorldPop { get; }
        public Action<string> RequestAudioCue { get; }
        public Action<string, float, float, float> RequestRumble { get; }
        public Action<string, string> LogEvent { get; }
        public Action LogObjectiveChanged { get; }
        public Action<DogId, string> MarkFailedInteraction { get; }
        public Func<GameObject, string, Vector3, int, Color, TextMesh> AddWorldLabel { get; }
        public int ObjectiveGoal { get; }
        public Func<ArenaArtCatalog.ActorKind, GameObject> CreateActor { get; }
        public Func<Treat> AcquireHiddenTreat { get; }
        public Action<Treat> RecoverCollectible { get; }
        public Action<Treat> ReplaceCollectible { get; }
        public Action<GameObject, string, Color, float> SetActorState { get; }
        public Action<GameObject, float> Pulse { get; }

        public MissionContext(
            DogController[] dogs,
            DogReadabilityFeedback[] dogFeedback,
            Rect bounds,
            Sprite actorSprite,
            Sprite rangeSprite,
            GameObject squirrelObject,
            float squirrelMoveSpeed,
            float singleBarkSquirrelRange,
            float singleBarkScareSeconds,
            float firstSquirrelBaseDelay,
            float firstSquirrelTroubleDelay,
            float squirrelBaseDelay,
            float squirrelTroubleDelay,
            int itemScore,
            int maxStolenFood,
            int squirrelPenalty,
            int squirrelScareScore,
            int pancakeSquirrelPenalty,
            PanicMeter panicMeter,
            Func<System.Random> random,
            Func<float> now,
            Func<GameManager.RoundModifier> activeModifier,
            Func<IReadOnlyList<Treat>> activeTreats,
            Func<bool> isPredatorResolved,
            Func<bool> isTugComplete,
            Action<int, string> addScore,
            Action<int> creditDog,
            Action<string> setCue,
            Action<GameManager.FeedbackKind> setFeedback,
            Action<GameManager.JuiceFeedbackKind, string> setJuice,
            Action<Vector2, string, Color> spawnWorldPop,
            Action<string> requestAudioCue,
            Action<string, float, float, float> requestRumble,
            Action<string, string> logEvent,
            Action logObjectiveChanged,
            Action<DogId, string> markFailedInteraction,
            Func<GameObject, string, Vector3, int, Color, TextMesh> addWorldLabel,
            int objectiveGoal,
            Func<ArenaArtCatalog.ActorKind, GameObject> createActor,
            Func<Treat> acquireHiddenTreat,
            Action<Treat> recoverCollectible,
            Action<Treat> replaceCollectible,
            Action<GameObject, string, Color, float> setActorState,
            Action<GameObject, float> pulse)
        {
            Dogs = dogs ?? throw new ArgumentNullException(nameof(dogs));
            DogFeedback = dogFeedback ?? throw new ArgumentNullException(nameof(dogFeedback));
            if (dogs.Length != dogFeedback.Length)
                throw new ArgumentException("Dog feedback must have one entry per dog.", nameof(dogFeedback));

            Bounds = bounds;
            ActorSprite = actorSprite;
            RangeSprite = rangeSprite;
            SquirrelObject = squirrelObject ?? throw new ArgumentNullException(nameof(squirrelObject));
            SquirrelMoveSpeed = squirrelMoveSpeed;
            SingleBarkSquirrelRange = singleBarkSquirrelRange;
            SingleBarkScareSeconds = singleBarkScareSeconds;
            FirstSquirrelBaseDelay = firstSquirrelBaseDelay;
            FirstSquirrelTroubleDelay = firstSquirrelTroubleDelay;
            SquirrelBaseDelay = squirrelBaseDelay;
            SquirrelTroubleDelay = squirrelTroubleDelay;
            ItemScore = itemScore;
            MaxStolenFood = maxStolenFood;
            SquirrelPenalty = squirrelPenalty;
            SquirrelScareScore = squirrelScareScore;
            PancakeSquirrelPenalty = pancakeSquirrelPenalty;
            PanicMeter = panicMeter; // nullable — only thunderstorm/comfort missions use it
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Now = now ?? throw new ArgumentNullException(nameof(now));
            ActiveModifier = activeModifier ?? throw new ArgumentNullException(nameof(activeModifier));
            ActiveTreats = activeTreats ?? throw new ArgumentNullException(nameof(activeTreats));
            IsPredatorResolved = isPredatorResolved ?? throw new ArgumentNullException(nameof(isPredatorResolved));
            IsTugComplete = isTugComplete ?? throw new ArgumentNullException(nameof(isTugComplete));
            AddScore = addScore ?? throw new ArgumentNullException(nameof(addScore));
            CreditDog = creditDog ?? throw new ArgumentNullException(nameof(creditDog));
            SetCue = setCue ?? throw new ArgumentNullException(nameof(setCue));
            SetFeedback = setFeedback ?? throw new ArgumentNullException(nameof(setFeedback));
            SetJuice = setJuice ?? throw new ArgumentNullException(nameof(setJuice));
            SpawnWorldPop = spawnWorldPop ?? throw new ArgumentNullException(nameof(spawnWorldPop));
            RequestAudioCue = requestAudioCue ?? throw new ArgumentNullException(nameof(requestAudioCue));
            RequestRumble = requestRumble ?? throw new ArgumentNullException(nameof(requestRumble));
            LogEvent = logEvent ?? throw new ArgumentNullException(nameof(logEvent));
            LogObjectiveChanged = logObjectiveChanged ?? throw new ArgumentNullException(nameof(logObjectiveChanged));
            MarkFailedInteraction = markFailedInteraction ?? throw new ArgumentNullException(nameof(markFailedInteraction));
            AddWorldLabel = addWorldLabel ?? throw new ArgumentNullException(nameof(addWorldLabel));
            ObjectiveGoal = objectiveGoal;
            CreateActor = createActor ?? throw new ArgumentNullException(nameof(createActor));
            AcquireHiddenTreat = acquireHiddenTreat ?? throw new ArgumentNullException(nameof(acquireHiddenTreat));
            RecoverCollectible = recoverCollectible ?? throw new ArgumentNullException(nameof(recoverCollectible));
            ReplaceCollectible = replaceCollectible ?? throw new ArgumentNullException(nameof(replaceCollectible));
            SetActorState = setActorState ?? throw new ArgumentNullException(nameof(setActorState));
            Pulse = pulse ?? throw new ArgumentNullException(nameof(pulse));
        }

        public int IndexOfDog(DogId dogId)
        {
            if (Dogs == null) return -1;
            for (int i = 0; i < Dogs.Length; i++)
            {
                if (Dogs[i] != null && Dogs[i].TryGetComponent<DogIdentity>(out var identity) && identity.Id == dogId)
                    return i;
            }
            return -1;
        }
    }
}
