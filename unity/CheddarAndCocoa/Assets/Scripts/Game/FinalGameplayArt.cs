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
                case RuntimeArtSpriteFactory.RuntimeSpriteId.EagleThreat:
                    return Root + "/Characters/Eagle/eagle_threat";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.CoyoteThreat:
                    return Root + "/Characters/Coyote/coyote_threat";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.BackyardBush:
                    return Root + "/Props/Backyard/bush";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.BackyardFence:
                    return Root + "/Props/Backyard/fence_section";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.BackyardRock:
                    return Root + "/Props/Backyard/rock";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.BarkBurst:
                    return Root + "/VFX/bark_burst";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.PickupSparkle:
                    return Root + "/VFX/pickup_sparkle";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.SuccessPop:
                    return Root + "/VFX/success_pop";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert:
                    return Root + "/VFX/warning_alert";
                case RuntimeArtSpriteFactory.RuntimeSpriteId.RopeToy:
                    return Root + "/Props/Mission/rope_tug";
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
