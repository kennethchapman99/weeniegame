using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Stable Resources path contract for final transparent gameplay sprites. Artists/tools can export
    /// PNGs into these locations and the runtime art layer will prefer them over draft-sheet crops.
    /// </summary>
    public static class FinalGameplayArt
    {
        public const string Root = "ArenaFinal";

        public static string PathFor(RuntimeArtSpriteFactory.RuntimeSpriteId id)
        {
            switch (id)
            {
                case RuntimeArtSpriteFactory.RuntimeSpriteId.Squirrel:
                    return Root + "/Characters/Squirrel/squirrel_idle";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.SquirrelSteal:
                    return Root + "/Characters/Squirrel/squirrel_steal";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.SquirrelScared:
                    return Root + "/Characters/Squirrel/squirrel_scared";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.EagleThreat:
                    return Root + "/Characters/Eagle/eagle_threat";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.PredatorAttack:
                    return Root + "/Characters/Eagle/eagle_action";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.CoyoteThreat:
                    return Root + "/Characters/Coyote/coyote_threat";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.BackyardBush:
                    return Root + "/Props/Backyard/bush";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.BackyardFence:
                    return Root + "/Props/Backyard/fence_section";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.BackyardRock:
                    return Root + "/Props/Backyard/rock";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.GrassPatch:
                    return Root + "/Props/Backyard/grass_patch";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.DigSpot:
                    return Root + "/Props/Backyard/dig_spot";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.DogBowl:
                    return Root + "/Props/Mission/dog_bowl";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.BarkBurst:
                    return Root + "/VFX/bark_burst";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.BarkRing:
                    return Root + "/VFX/bark_ring";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.PickupSparkle:
                    return Root + "/VFX/pickup_sparkle";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.SuccessPop:
                    return Root + "/VFX/success_pop";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert:
                    return Root + "/VFX/warning_alert";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.RescueBurst:
                    return Root + "/VFX/rescue_burst";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.FailPuff:
                    return Root + "/VFX/fail_puff";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.RopeToy:
                    return Root + "/Props/Mission/rope_tug";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.RopeComplete:
                    return Root + "/Props/Mission/rope_complete";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.WeenieCollectible:
                    return Root + "/Props/Mission/weenie_collectible";
                default:
                    return string.Empty;
            }
        }

        public static Sprite Load(RuntimeArtSpriteFactory.RuntimeSpriteId id)
        {
            string path = PathFor(id);
            return string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(path);
        }
    }
}
