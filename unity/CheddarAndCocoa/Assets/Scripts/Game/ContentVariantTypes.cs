using System;

namespace CheddarAndCocoa.Game
{
    public enum ContentVariantKind
    {
        Layout,
        Objective,
        Comedy,
        Modifier
    }

    [Serializable]
    public readonly struct ContentVariantSpec
    {
        public readonly string Id;
        public readonly string Label;
        public readonly ContentVariantKind Kind;
        public readonly string Description;

        public ContentVariantSpec(string id, string label, ContentVariantKind kind, string description)
        {
            Id = id;
            Label = label;
            Kind = kind;
            Description = description;
        }
    }

    public static class ContentVariantCatalog
    {
        public static readonly ContentVariantSpec SquirrelRouteShuffle = new(
            "squirrel_route_shuffle",
            "Squirrel Route Shuffle",
            ContentVariantKind.Layout,
            "Changes the squirrel route order while preserving tested route nodes.");

        public static readonly ContentVariantSpec ZeroFakeOutChallenge = new(
            "zero_fakeout_challenge",
            "Zero Fake-Out Challenge",
            ContentVariantKind.Objective,
            "Adds a replay objective for clearing without triggering fake-outs.");

        public static readonly ContentVariantSpec FakeSnackComedy = new(
            "fake_snack_comedy",
            "Fake Snack Comedy",
            ContentVariantKind.Comedy,
            "Adds a funny fake snack lure event without changing clear conditions.");

        public static readonly ContentVariantSpec WetFloorsModifier = new(
            "wet_floors_modifier",
            "Wet Floors",
            ContentVariantKind.Modifier,
            "Adds temporary slippery movement after pool or shake-off events.");
    }
}
