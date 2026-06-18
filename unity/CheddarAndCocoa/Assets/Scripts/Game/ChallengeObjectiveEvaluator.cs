namespace CheddarAndCocoa.Game
{
    public static class ChallengeObjectiveEvaluator
    {
        public static bool ScoreAtLeast(ChallengeObjectiveSpec challenge, MissionRuntimeSnapshot snapshot)
        {
            return snapshot.Score >= challenge.TargetValue;
        }

        public static bool CounterAtMost(ChallengeObjectiveSpec challenge, int value)
        {
            return value <= challenge.TargetValue;
        }

        public static bool CounterAtLeast(ChallengeObjectiveSpec challenge, int value)
        {
            return value >= challenge.TargetValue;
        }

        public static bool ClearUnderSeconds(ChallengeObjectiveSpec challenge, MissionRuntimeSnapshot snapshot, float elapsedSeconds)
        {
            return snapshot.IsClear && elapsedSeconds <= challenge.TargetValue;
        }
    }
}
