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

        public static readonly ProductionScoreEvent CheckpointReached = new("CHECKPOINT", 120, true);
        public static readonly ProductionScoreEvent LeashSnap = new("LEASH SNAP", -45);
        public static readonly ProductionScoreEvent WalkComplete = new("WALK COMPLETE", 500, true);

        public static readonly ProductionScoreEvent RoadEventCleared = new("SMOOTH!", 110, true);
        public static readonly ProductionScoreEvent BraceHeld = new("BRACED", 90, true);
        public static readonly ProductionScoreEvent CarTumble = new("TUMBLE", -45);
        public static readonly ProductionScoreEvent RideComplete = new("RIDE COMPLETE", 500, true);

        public static readonly ProductionScoreEvent FenceHeld = new("FENCE HELD", 100, true);
        public static readonly ProductionScoreEvent DirtFilled = new("DIRT FILLED", 125);
        public static readonly ProductionScoreEvent YardDefended = new("YARD DEFENDED", 500, true);

        public static readonly ProductionScoreEvent BasketTipped = new("BASKET TIPPED", 20, true);
        public static readonly ProductionScoreEvent SockDive = new("PARTNER SOCK DIVE", 40, true);
        public static readonly ProductionScoreEvent SockDecoy = new("DECOY SOCK FUMBLE", -15);

        public static readonly ProductionScoreEvent HumanGettingIt = new("THEY'RE GETTING IT", 120, true);
        public static readonly ProductionScoreEvent HumanMisread = new("HUMAN CONFUSED", -55);
        public static readonly ProductionScoreEvent WalkConned = new("WALK CONNED", 500, true);

        public static readonly ProductionScoreEvent ContraptionStep = new("CONTRAPTION STEP", 90, true);
        public static readonly ProductionScoreEvent ContraptionFumble = new("CONTRAPTION FUMBLE", -40);

        public static readonly ProductionScoreEvent ChickNabbed = new("CHICK NABBED", 25);
        public static readonly ProductionScoreEvent ChickGulped = new("CHICK GULPED", 150, true);
        public static readonly ProductionScoreEvent ParentRepelled = new("PARENT REPELLED", 125, true);
        public static readonly ProductionScoreEvent ParentPeck = new("PECKED", -50);
        public static readonly ProductionScoreEvent ChickAirlifted = new("CHICK AIRLIFTED", -25);
        public static readonly ProductionScoreEvent NestFeastComplete = new("NEST FEAST COMPLETE", 500, true);

        public static readonly ProductionScoreEvent SkunkBailed = new("CLEAN BAIL", 60, true);
        public static readonly ProductionScoreEvent SkunkSprayed = new("SKUNKED", -60);
        public static readonly ProductionScoreEvent SkunkDeSkunked = new("FRESH AGAIN", 80, true);
        public static readonly ProductionScoreEvent LaundryHauled = new("LAUNDRY HAULED", 20, true);
        public static readonly ProductionScoreEvent BirdSecured = new("BIRD SECURED", 500, true);

        public static readonly ProductionScoreEvent GroomLanded = new("GROOMED CLEAN", 25, true);
        public static readonly ProductionScoreEvent TickOverload = new("TICK OVERLOAD", -50);
        public static readonly ProductionScoreEvent PoolDiveRinse = new("POOL DIVE", 15, true);
        public static readonly ProductionScoreEvent SuperTickShaken = new("SUPER TICK SHAKEN OFF", 150, true);
        public static readonly ProductionScoreEvent InfestationCleared = new("INFESTATION CLEARED", 500, true);

        public static readonly ProductionScoreEvent BurrMazeCheckpoint = new("CHECKPOINT REACHED", 90, true);
        public static readonly ProductionScoreEvent BurrPickedClean = new("BURRS PICKED CLEAN", 60, true);
        public static readonly ProductionScoreEvent BurrMazeCleared = new("MAZE CLEARED", 500, true);
    }
}
