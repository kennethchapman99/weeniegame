using System;

namespace CheddarAndCocoa.Game
{
    public enum CollectibleSetKind
    {
        SquirrelEvidence,
        LostToys,
        FamousSticks,
        MysteriousSocks,
        NeighborhoodSmells
    }

    [Serializable]
    public readonly struct CollectibleSetSpec
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly CollectibleSetKind Kind;
        public readonly int RequiredCount;

        public CollectibleSetSpec(string id, string displayName, CollectibleSetKind kind, int requiredCount)
        {
            Id = id;
            DisplayName = displayName;
            Kind = kind;
            RequiredCount = requiredCount;
        }
    }

    public static class CollectibleSetCatalog
    {
        public static readonly CollectibleSetSpec SquirrelEvidence = new(
            "squirrel_evidence",
            "Squirrel Evidence",
            CollectibleSetKind.SquirrelEvidence,
            12);

        public static readonly CollectibleSetSpec LostToys = new(
            "lost_toys",
            "Lost Toys",
            CollectibleSetKind.LostToys,
            20);

        public static readonly CollectibleSetSpec NeighborhoodSmells = new(
            "neighborhood_smells",
            "Neighborhood Smells",
            CollectibleSetKind.NeighborhoodSmells,
            30);
    }
}
