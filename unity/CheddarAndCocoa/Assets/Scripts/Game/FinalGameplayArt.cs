using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Stable Resources paths for extracted transparent ArenaFinal gameplay sprites.</summary>
    public static class FinalGameplayArt
    {
        public const string Root = "ArenaFinal/";
        public const string SquirrelIdle = Root + "Characters/Squirrel/squirrel_idle";
        public const string SquirrelSteal = Root + "Characters/Squirrel/squirrel_steal";
        public const string SquirrelScared = Root + "Characters/Squirrel/squirrel_scared";
        public const string EagleThreat = Root + "Characters/Eagle/eagle_threat";
        public const string EagleAction = Root + "Characters/Eagle/eagle_action";
        public const string CoyoteThreat = Root + "Characters/Coyote/coyote_threat";
        public const string BunnyIdle = Root + "Characters/Bunny/bunny_idle";
        public const string Weenie = Root + "Props/Mission/weenie_collectible";
        public const string RopeTug = Root + "Props/Mission/rope_tug";
        public const string RopeComplete = Root + "Props/Mission/rope_complete";
        public const string DogBowl = Root + "Props/Mission/dog_bowl";
        public const string Bush = Root + "Props/Backyard/bush";
        public const string Fence = Root + "Props/Backyard/fence_section";
        public const string Rock = Root + "Props/Backyard/rock";
        public const string Grass = Root + "Props/Backyard/grass_patch";
        public const string DigSpot = Root + "Props/Backyard/dig_spot";
        public const string BarkBurst = Root + "VFX/bark_burst";
        public const string BarkRing = Root + "VFX/bark_ring";
        public const string PickupSparkle = Root + "VFX/pickup_sparkle";
        public const string SuccessPop = Root + "VFX/success_pop";
        public const string WarningAlert = Root + "VFX/warning_alert";
        public const string RescueBurst = Root + "VFX/rescue_burst";
        public const string FailPuff = Root + "VFX/fail_puff";

        public static Sprite Load(string path) => string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(path);
        public static bool Has(string path) => Load(path) != null;
    }
}
