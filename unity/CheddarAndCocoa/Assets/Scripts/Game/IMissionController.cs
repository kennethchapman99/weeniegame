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
        string ObjectiveLabel { get; }
        Vector2 EntryTarget { get; }

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
        public DogController[] Dogs { get; set; }
        public DogReadabilityFeedback[] DogFeedback { get; set; }
        public Rect Bounds { get; set; }
        public Sprite ActorSprite { get; set; }
        public Sprite RangeSprite { get; set; }
        public Func<System.Random> Random { get; set; }
        public Func<float> Now { get; set; }

        public Action<int, string> AddScore { get; set; }
        public Action<string> SetCue { get; set; }
        public Action<GameManager.FeedbackKind> SetFeedback { get; set; }
        public Action<GameManager.JuiceFeedbackKind, string> SetJuice { get; set; }
        public Action<Vector2, string, Color> SpawnWorldPop { get; set; }
        public Action<string> RequestAudioCue { get; set; }
        public Action<string, float, float, float> RequestRumble { get; set; }
        public Action<string, string> LogEvent { get; set; }
        public Action LogObjectiveChanged { get; set; }
        public Action<DogId, string> MarkFailedInteraction { get; set; }
        public Func<GameObject, string, Vector3, int, Color, TextMesh> AddWorldLabel { get; set; }

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
