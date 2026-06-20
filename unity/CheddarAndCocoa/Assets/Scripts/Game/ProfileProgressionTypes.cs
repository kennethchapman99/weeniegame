using System;

namespace CheddarAndCocoa.Game
{
    // NOTE: the per-mission progress record now lives as the actively-used sealed class in
    // AdventureProgression.cs (with Attempts/BestRank/Completed). A duplicate `MissionProgressRecord`
    // struct previously declared here collided with it (CS0101) and broke compilation; it was unused
    // (LifetimeDogStats and ProgressionTargets below are the live types from this file), so it was
    // removed to keep the project compile-clean.

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
