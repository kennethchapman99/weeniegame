using System;

namespace CheddarAndCocoa.Game
{
    [Serializable]
    public struct MissionProgressRecord
    {
        public string MissionId;
        public int BestScore;
        public int BestStars;
        public int Clears;
        public int Failures;
    }

    [Serializable]
    public struct LifetimeDogStats
    {
        public int Barks;
        public int UnitedBarks;
        public int Rescues;
        public int SquirrelsChased;
        public int ToysRecovered;
        public int PoolFalls;
        public int FakeSnacksEaten;
    }

    public static class ProgressionTargets
    {
        public const int BackyardPackUnlockStars = 0;
        public const int PoolPackUnlockStars = 8;
        public const int HouseChaosUnlockStars = 16;
        public const int RoadTripUnlockStars = 24;
    }
}
