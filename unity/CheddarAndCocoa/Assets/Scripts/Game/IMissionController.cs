using System;
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
        public Func<System.Random> Random { get; }
        public Func<float> Now { get; }

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

        public MissionContext(
            DogController[] dogs,
            DogReadabilityFeedback[] dogFeedback,
            Rect bounds,
            Sprite actorSprite,
            Sprite rangeSprite,
            Func<System.Random> random,
            Func<float> now,
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
            Func<GameObject, string, Vector3, int, Color, TextMesh> addWorldLabel)
        {
            Dogs = dogs ?? throw new ArgumentNullException(nameof(dogs));
            DogFeedback = dogFeedback ?? throw new ArgumentNullException(nameof(dogFeedback));
            if (dogs.Length != dogFeedback.Length)
                throw new ArgumentException("Dog feedback must have one entry per dog.", nameof(dogFeedback));

            Bounds = bounds;
            ActorSprite = actorSprite;
            RangeSprite = rangeSprite;
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Now = now ?? throw new ArgumentNullException(nameof(now));
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
