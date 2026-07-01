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

        // Stable string-path contract used by the motion/juice/bark layers (resource-path based).
        public const string SquirrelIdle = Root + "/Characters/Squirrel/squirrel_mischief_v02";
        public const string SquirrelSteal = Root + "/Characters/Squirrel/squirrel_mischief_v02";
        public const string SquirrelScared = Root + "/Characters/Squirrel/squirrel_mischief_v02";
        public const string EagleThreat = Root + "/Characters/Eagle/eagle_banking_v02";
        public const string EagleAction = Root + "/Characters/Eagle/eagle_banking_v02";
        public const string CoyoteThreat = Root + "/Characters/Coyote/coyote_threat";
        public const string BunnyIdle = Root + "/Characters/Bunny/bunny_idle";
        public const string Weenie = Root + "/Props/Mission/weenie_collectible";
        public const string RopeTug = Root + "/Props/Mission/rope_tug";
        public const string RopeComplete = Root + "/Props/Mission/rope_complete";
        public const string DogBowl = Root + "/Props/Mission/dog_bowl";
        public const string Bush = Root + "/Props/Backyard/bush";
        public const string Fence = Root + "/Props/Backyard/fence_section";
        public const string Rock = Root + "/Props/Backyard/rock";
        public const string Grass = Root + "/Props/Backyard/grass_patch";
        public const string DigSpot = Root + "/Props/Backyard/dig_spot";
        public const string BarkBurst = Root + "/VFX/bark_burst";
        public const string BarkRing = Root + "/VFX/bark_ring";
        public const string PickupSparkle = Root + "/VFX/pickup_sparkle";
        public const string SuccessPop = Root + "/VFX/success_pop";
        public const string WarningAlert = Root + "/VFX/warning_alert";
        public const string RescueBurst = Root + "/VFX/rescue_burst";
        public const string FailPuff = Root + "/VFX/fail_puff";
        public const string CueObjectiveArrow = Root + "/UI/Cues/cue_objective_arrow";
        public const string CueTargetPaw = Root + "/UI/Cues/cue_target_paw";
        public const string CueBarkRange = Root + "/UI/Cues/cue_bark_range";
        public const string CueRescueRange = Root + "/UI/Cues/cue_rescue_range";
        public const string CueTugRange = Root + "/UI/Cues/cue_tug_range";
        public const string DogFxChaosSpark = Root + "/VFX/Dog/dog_fx_chaos_spark";
        public const string DogFxCollarGlint = Root + "/VFX/Dog/dog_fx_collar_glint";
        public const string DogFxGroundGlow = Root + "/VFX/Dog/dog_fx_ground_glow";
        public const string DogFxPawCheddar = Root + "/VFX/Dog/dog_fx_paw_cheddar";
        public const string DogFxPawCocoa = Root + "/VFX/Dog/dog_fx_paw_cocoa";
        public const string DogFxQueenGlint = Root + "/VFX/Dog/dog_fx_queen_glint";
        public const string KitchenCueTelegraphGold = Root + "/UI/KitchenCues/kitchen_telegraph_gold";
        public const string KitchenCueTelegraphPurple = Root + "/UI/KitchenCues/kitchen_telegraph_purple";
        public const string KitchenCueLandingGold = Root + "/UI/KitchenCues/kitchen_landing_gold";
        public const string KitchenCueLandingPurple = Root + "/UI/KitchenCues/kitchen_landing_purple";
        public const string WowCheddarCocoaDuo = Root + "/Props/Wow/cheddar_cocoa_duo";
        public const string WowPeeBreakMotif = Root + "/Props/Wow/motif_pee_break";
        public const string WowFoodHeistMotif = Root + "/Props/Wow/motif_food_heist";
        public const string WowThreatWatchMotif = Root + "/Props/Wow/motif_threat_watch";
        public const string WowAdventureRouteMotif = Root + "/Props/Wow/motif_adventure_route";
        public const string WowBackyardPropsMotif = Root + "/Props/Wow/motif_backyard_props";
        public const string PeeBreakCouch = Root + "/Props/PeeBreak/pee_break_couch";
        public const string PeeBreakTeenager = Root + "/Props/PeeBreak/pee_break_teenager";
        public const string PeeBreakPhoneCharger = Root + "/Props/PeeBreak/pee_break_phone_charger";
        public const string PeeBreakOpenDoor = Root + "/Props/PeeBreak/pee_break_open_door";
        public const string PeeBreakLeash = Root + "/Props/PeeBreak/pee_break_leash";
        public const string PeeBreakHydrantRelief = Root + "/Props/PeeBreak/pee_break_hydrant_relief";
        public const string PeeBreakBladderMeter = Root + "/Props/PeeBreak/pee_break_bladder_meter";
        public const string PeeBreakMisreadTennisBall = Root + "/Props/PeeBreak/pee_break_misread_tennis_ball";
        public const string MissionSnackPlate = Root + "/Props/Missions/snack_plate";
        public const string MissionSockBundle = Root + "/Props/Missions/sock_bundle";
        public const string MissionLaundryBasket = Root + "/Props/Missions/laundry_basket";
        public const string MissionLaundryBasketOpen = Root + "/Props/Missions/laundry_basket_open";
        public const string MissionSquirrelStash = Root + "/Props/Missions/squirrel_stash";
        public const string MissionEscapeGap = Root + "/Props/Missions/escape_gap";
        public const string MissionGate = Root + "/Props/Missions/gate";
        public const string MissionSqueakyToy = Root + "/Props/Missions/squeaky_toy";
        public const string MissionSteakPlate = Root + "/Props/Missions/steak_plate";
        public const string MissionTableHuman = Root + "/Props/Missions/table_human";
        public const string MissionDecoyToy = Root + "/Props/Missions/decoy_toy";
        public const string MissionWalkHuman = Root + "/Props/Missions/walk_human";
        public const string MissionWalkLeash = Root + "/Props/Missions/walk_leash";
        public const string MissionCarBalance = Root + "/Props/Missions/car_balance";
        public const string MissionDigMound = Root + "/Props/Missions/dig_mound";
        public const string MissionBuriedBone = Root + "/Props/Missions/buried_bone";
        public const string MissionScentPost = Root + "/Props/Missions/scent_post";
        public const string MissionTerritoryZone = Root + "/Props/Missions/territory_zone";
        public const string MissionLeashCheckpoint = Root + "/Props/Missions/leash_checkpoint";
        public const string MissionBoneMound = Root + "/Props/Missions/bone_mound";
        public const string MissionChaosLever = Root + "/Props/Missions/chaos_lever";
        public const string MissionChaosJunction = Root + "/Props/Missions/chaos_junction";
        public const string ChaosJunctionTowelDrop = Root + "/Props/ChaosMachine/chaos_junction_towel_drop";
        public const string ChaosJunctionBasketTip = Root + "/Props/ChaosMachine/chaos_junction_basket_tip";
        public const string ChaosJunctionToyLaunch = Root + "/Props/ChaosMachine/chaos_junction_toy_launch";
        public const string MissionEscapeStation = Root + "/Props/Missions/escape_station";
        public const string MissionCatchBlanket = Root + "/Props/Missions/catch_blanket";
        public const string MissionFallingSnack = Root + "/Props/Missions/falling_snack";
        public const string MissionKitchenCounter = Root + "/Props/Missions/kitchen_counter";
        public const string MissionKitchenSafeBowl = Root + "/Props/Missions/kitchen_safe_bowl";
        public const string MissionKitchenGoodFood = Root + "/Props/Missions/kitchen_good_food";
        public const string MissionKitchenBadFood = Root + "/Props/Missions/kitchen_bad_food";
        public const string MissionKitchenWarning = Root + "/Props/Missions/kitchen_warning";
        public const string EnvironmentBackDoor = Root + "/Props/Environment/yard_back_door";
        public const string EnvironmentBackyardPlate = Root + "/Props/Environment/yard_backyard_plate_v02";
        public const string EnvironmentFenceRun = Root + "/Props/Environment/yard_fence_run";
        public const string EnvironmentFlowerPatch = Root + "/Props/Environment/yard_flower_patch";
        public const string EnvironmentGardenBed = Root + "/Props/Environment/yard_garden_bed";
        public const string EnvironmentHousePatio = Root + "/Props/Environment/yard_house_patio";
        public const string EnvironmentLaundryCorner = Root + "/Props/Environment/yard_laundry_corner";
        public const string EnvironmentLawnLandmarks = Root + "/Props/Environment/yard_lawn_landmarks";
        public const string EnvironmentLeashRoute = Root + "/Props/Environment/yard_leash_route";
        public const string EnvironmentPicnicBlanket = Root + "/Props/Environment/yard_picnic_blanket";
        public const string EnvironmentPond = Root + "/Props/Environment/yard_pond";
        public const string EnvironmentPeeBreakPath = Root + "/Props/Environment/yard_pee_break_path";
        public const string EnvironmentSandbox = Root + "/Props/Environment/yard_sandbox";
        public const string EnvironmentScentTrail = Root + "/Props/Environment/yard_scent_trail";
        public const string EnvironmentShadeTree = Root + "/Props/Environment/yard_shade_tree";
        public const string EnvironmentSnackTable = Root + "/Props/Environment/yard_snack_table";
        public const string EnvironmentSteppingStone = Root + "/Props/Environment/yard_stepping_stone";
        public const string EnvironmentThreatLane = Root + "/Props/Environment/yard_threat_lane";
        public const string BuildingHomeExterior = Root + "/Props/Buildings/home_exterior_facade";
        public const string BuildingBackPorchEntry = Root + "/Props/Buildings/back_porch_entry";
        public const string BuildingYardShedStorage = Root + "/Props/Buildings/yard_shed_storage";
        public const string HudPanelFrame = Root + "/UI/Hud/hud_panel_frame";
        public const string HudMissionTile = Root + "/UI/Hud/hud_mission_tile";
        public const string HudMissionTileSelected = Root + "/UI/Hud/hud_mission_tile_selected";
        public const string HudBadgeFrame = Root + "/UI/Hud/hud_badge_frame";
        public const string HudButtonPrimary = Root + "/UI/Hud/hud_button_primary";
        public const string HudOverlayPanel = Root + "/UI/Hud/hud_overlay_panel";
        public const string WorldLabelBubble = Root + "/UI/WorldLabels/world_label_bubble";
        public const string WorldLabelCommand = Root + "/UI/WorldLabels/world_label_command";
        public const string WorldLabelWarning = Root + "/UI/WorldLabels/world_label_warning";
        public const string WorldPopBurst = Root + "/UI/WorldLabels/world_pop_burst";

        public static readonly string[] MissionPropPackPass2 =
        {
            MissionSnackPlate, MissionSockBundle, MissionLaundryBasket, MissionLaundryBasketOpen,
            MissionSquirrelStash, MissionEscapeGap, MissionGate, MissionSqueakyToy,
            MissionSteakPlate, MissionTableHuman, MissionDecoyToy, MissionWalkHuman,
            MissionWalkLeash, MissionCarBalance, MissionDigMound, MissionBuriedBone,
            MissionScentPost, MissionTerritoryZone, MissionLeashCheckpoint, MissionBoneMound,
            MissionChaosLever, MissionChaosJunction, MissionEscapeStation, MissionCatchBlanket,
            MissionFallingSnack, MissionKitchenCounter, MissionKitchenSafeBowl,
            MissionKitchenGoodFood, MissionKitchenBadFood, MissionKitchenWarning
        };

        public static readonly string[] EnvironmentPropPack =
        {
            EnvironmentBackDoor, EnvironmentFenceRun, EnvironmentFlowerPatch, EnvironmentGardenBed,
            EnvironmentHousePatio, EnvironmentLaundryCorner, EnvironmentLawnLandmarks,
            EnvironmentLeashRoute, EnvironmentPicnicBlanket, EnvironmentPond,
            EnvironmentPeeBreakPath, EnvironmentSandbox, EnvironmentScentTrail,
            EnvironmentShadeTree, EnvironmentSnackTable, EnvironmentSteppingStone,
            EnvironmentThreatLane
        };

        public static readonly string[] BuildingPropPack =
        {
            BuildingHomeExterior, BuildingBackPorchEntry, BuildingYardShedStorage
        };

        public static readonly string[] HudSkinPack =
        {
            HudPanelFrame, HudMissionTile, HudMissionTileSelected,
            HudBadgeFrame, HudButtonPrimary, HudOverlayPanel
        };

        public static readonly string[] WorldLabelSkinPack =
        {
            WorldLabelBubble, WorldLabelCommand, WorldLabelWarning, WorldPopBurst
        };

        public static readonly string[] GameplayCuePack =
        {
            CueObjectiveArrow, CueTargetPaw, CueBarkRange, CueRescueRange, CueTugRange
        };

        public static readonly string[] DogFxPack =
        {
            DogFxChaosSpark, DogFxCollarGlint, DogFxGroundGlow,
            DogFxPawCheddar, DogFxPawCocoa, DogFxQueenGlint
        };

        public static readonly string[] KitchenCuePack =
        {
            KitchenCueTelegraphGold, KitchenCueTelegraphPurple,
            KitchenCueLandingGold, KitchenCueLandingPurple
        };

        public static readonly string[] ChaosMachinePropPack =
        {
            ChaosJunctionTowelDrop, ChaosJunctionBasketTip, ChaosJunctionToyLaunch
        };

        public static Sprite Load(string path) => string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(path);
        public static bool Has(string path) => Load(path) != null;

        public static string PathFor(RuntimeArtSpriteFactory.RuntimeSpriteId id)
        {
            switch (id)
            {
                case RuntimeArtSpriteFactory.RuntimeSpriteId.Squirrel:
                    return SquirrelIdle;
                case RuntimeArtSpriteFactory.RuntimeSpriteId.SquirrelSteal:
                    return SquirrelSteal;
                case RuntimeArtSpriteFactory.RuntimeSpriteId.SquirrelScared:
                    return SquirrelScared;
                case RuntimeArtSpriteFactory.RuntimeSpriteId.EagleThreat:
                    return EagleThreat;
                case RuntimeArtSpriteFactory.RuntimeSpriteId.PredatorAttack:
                    return EagleAction;
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
