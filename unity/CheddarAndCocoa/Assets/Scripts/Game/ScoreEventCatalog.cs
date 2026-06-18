namespace CheddarAndCocoa.Game
{
    public static class ScoreEventCatalog
    {
        public static readonly ProductionScoreEvent GoodHerd = new("GOOD HERD", 75);
        public static readonly ProductionScoreEvent Cutoff = new("CUTOFF", 125, true);
        public static readonly ProductionScoreEvent DoubleBarkBlock = new("DOUBLE BARK BLOCK", 150, true);
        public static readonly ProductionScoreEvent FakeOut = new("FAKE OUT", -75);
        public static readonly ProductionScoreEvent StashFound = new("STASH FOUND", 300);
        public static readonly ProductionScoreEvent ConspiracyCracked = new("CONSPIRACY CRACKED", 500, true);

        public static readonly ProductionScoreEvent SafeHide = new("SAFE HIDE", 100);
        public static readonly ProductionScoreEvent ToyRescued = new("TOY RESCUED", 250, true);
        public static readonly ProductionScoreEvent UnitedFront = new("UNITED FRONT", 300, true);

        public static readonly ProductionScoreEvent WeeniePickup = new("WEENIE GRABBED", 25);
        public static readonly ProductionScoreEvent WeenieDelivered = new("WEENIE DELIVERED", 150, true);
        public static readonly ProductionScoreEvent WeenieDropped = new("FUMBLED WEENIE", -50);
        public static readonly ProductionScoreEvent RoundupComplete = new("ROUNDUP COMPLETE", 500, true);

        public static readonly ProductionScoreEvent ScentSniff = new("HOT SNIFF", 20);
        public static readonly ProductionScoreEvent BoneFound = new("BONE DUG UP", 175, true);
        public static readonly ProductionScoreEvent ColdDig = new("COLD DIG", -40);
        public static readonly ProductionScoreEvent ScentSearchComplete = new("SEARCH COMPLETE", 500, true);

        public static readonly ProductionScoreEvent StormWeathered = new("CLAP WEATHERED", 120, true);
        public static readonly ProductionScoreEvent StormComfort = new("COMFORT HUDDLE", 30, true);
        public static readonly ProductionScoreEvent StormCleared = new("STORM PASSED", 500, true);

        public static readonly ProductionScoreEvent ZoneClaimed = new("ZONE MARKED", 90);
        public static readonly ProductionScoreEvent ZoneStolen = new("ZONE STOLEN", -40);
        public static readonly ProductionScoreEvent YardMarked = new("YARD MARKED", 500, true);

        public static readonly ProductionScoreEvent FenceHeld = new("FENCE HELD", 100, true);
        public static readonly ProductionScoreEvent DirtFilled = new("DIRT FILLED", 125);
        public static readonly ProductionScoreEvent YardDefended = new("YARD DEFENDED", 500, true);
    }
}
