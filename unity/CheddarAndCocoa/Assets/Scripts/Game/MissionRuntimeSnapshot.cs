using System;

namespace CheddarAndCocoa.Game
{
    [Serializable]
    public readonly struct MissionRuntimeSnapshot
    {
        public readonly string MissionId;
        public readonly int Score;
        public readonly float TimeRemaining;
        public readonly int ObjectiveProgress;
        public readonly int ObjectiveGoal;
        public readonly int Mistakes;
        public readonly bool IsClear;
        public readonly bool IsFailed;

        public MissionRuntimeSnapshot(
            string missionId,
            int score,
            float timeRemaining,
            int objectiveProgress,
            int objectiveGoal,
            int mistakes,
            bool isClear,
            bool isFailed)
        {
            MissionId = missionId;
            Score = score;
            TimeRemaining = timeRemaining;
            ObjectiveProgress = objectiveProgress;
            ObjectiveGoal = objectiveGoal;
            Mistakes = mistakes;
            IsClear = isClear;
            IsFailed = isFailed;
        }

        public float ProgressRatio => ObjectiveGoal <= 0 ? 0f : Math.Min(1f, Math.Max(0f, (float)ObjectiveProgress / ObjectiveGoal));
        public bool IsComplete => IsClear || IsFailed;
    }
}
