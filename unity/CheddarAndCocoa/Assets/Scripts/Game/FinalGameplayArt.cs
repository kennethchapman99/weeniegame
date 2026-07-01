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
        public const string ChaosLeverReady = Root + "/Props/ChaosMachine/chaos_lever_ready";
        public const string ChaosLeverRunning = Root + "/Props/ChaosMachine/chaos_lever_running";
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
        public const string GreatEscapeStationWaiting = Root + "/Props/GreatEscape/great_escape_station_waiting";
        public const string GreatEscapeStationCheddarActive = Root + "/Props/GreatEscape/great_escape_station_cheddar_active";
        public const string GreatEscapeStationCocoaActive = Root + "/Props/GreatEscape/great_escape_station_cocoa_active";
        public const string GreatEscapeStationCompleted = Root + "/Props/GreatEscape/great_escape_station_completed";
        public const string GreatEscapeStationFumble = Root + "/Props/GreatEscape/great_escape_station_fumble";
        public const string GreatEscapeStationSettle = Root + "/Props/GreatEscape/great_escape_station_settle";
        public const string BlanketCatchSlack = Root + "/Props/BlanketCatch/blanket_catch_slack";
        public const string BlanketCatchTaut = Root + "/Props/BlanketCatch/blanket_catch_taut";
        public const string BlanketCatchRipping = Root + "/Props/BlanketCatch/blanket_catch_ripping";
        public const string BlanketCatchCaught = Root + "/Props/BlanketCatch/blanket_catch_caught";
        public const string BlanketSnackFalling = Root + "/Props/BlanketCatch/blanket_snack_falling";
        public const string BlanketSnackCaught = Root + "/Props/BlanketCatch/blanket_snack_caught";
        public const string BlanketSnackSplat = Root + "/Props/BlanketCatch/blanket_snack_splat";
        public const string KitchenCounterReady = Root + "/Props/KitchenFrenzy/kitchen_counter_ready";
        public const string KitchenCounterBarked = Root + "/Props/KitchenFrenzy/kitchen_counter_barked";
        public const string KitchenSafeBowlEmpty = Root + "/Props/KitchenFrenzy/kitchen_safe_bowl_empty";
        public const string KitchenSafeBowlCatch = Root + "/Props/KitchenFrenzy/kitchen_safe_bowl_catch";
        public const string KitchenFoodGoodFalling = Root + "/Props/KitchenFrenzy/kitchen_food_good_falling";
        public const string KitchenFoodBadFalling = Root + "/Props/KitchenFrenzy/kitchen_food_bad_falling";
        public const string KitchenFoodSplat = Root + "/Props/KitchenFrenzy/kitchen_food_splat";
        public const string BackyardTrapGapOpen = Root + "/Props/BackyardRescue/backyard_trap_gap_open";
        public const string BackyardTrapGapHeld = Root + "/Props/BackyardRescue/backyard_trap_gap_held";
        public const string BackyardTrapGapFakeRoute = Root + "/Props/BackyardRescue/backyard_trap_gap_fake_route";
        public const string BackyardWeenieTargeted = Root + "/Props/BackyardRescue/backyard_weenie_targeted";
        public const string BackyardWeenieDropped = Root + "/Props/BackyardRescue/backyard_weenie_dropped";
        public const string BackyardWeenieSaved = Root + "/Props/BackyardRescue/backyard_weenie_saved";
        public const string BackyardPredatorLaneWarning = Root + "/Props/BackyardRescue/backyard_predator_lane_warning";
        public const string SnackHeistPlateTargeted = Root + "/Props/SnackHeist/snack_heist_plate_targeted";
        public const string SnackHeistPlateStashed = Root + "/Props/SnackHeist/snack_heist_plate_stashed";
        public const string SnackHeistPlateStolen = Root + "/Props/SnackHeist/snack_heist_plate_stolen";
        public const string SnackHeistGuardLane = Root + "/Props/SnackHeist/snack_heist_guard_lane";
        public const string SockPanicBasketClosed = Root + "/Props/SockPanic/sock_panic_basket_closed";
        public const string SockPanicBasketOpen = Root + "/Props/SockPanic/sock_panic_basket_open";
        public const string SockPanicBasketFumble = Root + "/Props/SockPanic/sock_panic_basket_fumble";
        public const string SockPanicSockExposed = Root + "/Props/SockPanic/sock_panic_sock_exposed";
        public const string SockPanicSockDecoy = Root + "/Props/SockPanic/sock_panic_sock_decoy";
        public const string SockPanicSockSaved = Root + "/Props/SockPanic/sock_panic_sock_saved";
        public const string SquirrelConspiracyCutoffOpen = Root + "/Props/SquirrelConspiracy/squirrel_conspiracy_cutoff_open";
        public const string SquirrelConspiracyCutoffHeld = Root + "/Props/SquirrelConspiracy/squirrel_conspiracy_cutoff_held";
        public const string SquirrelConspiracyCutoffFakeout = Root + "/Props/SquirrelConspiracy/squirrel_conspiracy_cutoff_fakeout";
        public const string SquirrelConspiracyStashRevealed = Root + "/Props/SquirrelConspiracy/squirrel_conspiracy_stash_revealed";
        public const string SquirrelConspiracyStashCracked = Root + "/Props/SquirrelConspiracy/squirrel_conspiracy_stash_cracked";
        public const string EagleShadowCoverSafe = Root + "/Props/EagleShadow/eagle_shadow_cover_safe";
        public const string EagleShadowCoverSpotted = Root + "/Props/EagleShadow/eagle_shadow_cover_spotted";
        public const string EagleShadowTalonGripClosed = Root + "/Props/EagleShadow/eagle_shadow_talon_grip_closed";
        public const string EagleShadowTalonGripOpen = Root + "/Props/EagleShadow/eagle_shadow_talon_grip_open";
        public const string EagleShadowTalonGripFreed = Root + "/Props/EagleShadow/eagle_shadow_talon_grip_freed";
        public const string CoyotesFenceGapOpen = Root + "/Props/CoyotesFence/coyotes_fence_gap_open";
        public const string CoyotesFenceGapPinned = Root + "/Props/CoyotesFence/coyotes_fence_gap_pinned";
        public const string CoyotesFenceGapRepaired = Root + "/Props/CoyotesFence/coyotes_fence_gap_repaired";
        public const string CoyotesFenceGapBreached = Root + "/Props/CoyotesFence/coyotes_fence_gap_breached";
        public const string CoyotesFenceFakeSnack = Root + "/Props/CoyotesFence/coyotes_fence_fake_snack";
        public const string WeenieRoundupLoose = Root + "/Props/WeenieRoundup/weenie_roundup_loose";
        public const string WeenieRoundupCarried = Root + "/Props/WeenieRoundup/weenie_roundup_carried";
        public const string WeenieRoundupDropped = Root + "/Props/WeenieRoundup/weenie_roundup_dropped";
        public const string WeenieRoundupBowlEmpty = Root + "/Props/WeenieRoundup/weenie_roundup_bowl_empty";
        public const string WeenieRoundupBowlProgress = Root + "/Props/WeenieRoundup/weenie_roundup_bowl_progress";
        public const string WeenieRoundupBowlFull = Root + "/Props/WeenieRoundup/weenie_roundup_bowl_full";
        public const string ScentSearchDigUnknown = Root + "/Props/ScentSearch/scent_search_dig_unknown";
        public const string ScentSearchScentHot = Root + "/Props/ScentSearch/scent_search_scent_hot";
        public const string ScentSearchScentCold = Root + "/Props/ScentSearch/scent_search_scent_cold";
        public const string ScentSearchBoneFound = Root + "/Props/ScentSearch/scent_search_bone_found";
        public const string ThunderstormCloudWaiting = Root + "/Props/Thunderstorm/thunderstorm_cloud_waiting";
        public const string ThunderstormThunderclap = Root + "/Props/Thunderstorm/thunderstorm_thunderclap";
        public const string ThunderstormComfortHuddle = Root + "/Props/Thunderstorm/thunderstorm_comfort_huddle";
        public const string ThunderstormStormCleared = Root + "/Props/Thunderstorm/thunderstorm_storm_cleared";
        public const string MarkYardZoneUnclaimed = Root + "/Props/MarkTheYard/mark_yard_zone_unclaimed";
        public const string MarkYardZoneClaimed = Root + "/Props/MarkTheYard/mark_yard_zone_claimed";
        public const string MarkYardZoneStolen = Root + "/Props/MarkTheYard/mark_yard_zone_stolen";
        public const string MarkYardSquirrelWatch = Root + "/Props/MarkTheYard/mark_yard_squirrel_watch";
        public const string MarkYardSquirrelSteal = Root + "/Props/MarkTheYard/mark_yard_squirrel_steal";
        public const string LeashWalkCheckpointWaiting = Root + "/Props/LeashWalk/leash_walk_checkpoint_waiting";
        public const string LeashWalkCheckpointReached = Root + "/Props/LeashWalk/leash_walk_checkpoint_reached";
        public const string LeashWalkSnapWarning = Root + "/Props/LeashWalk/leash_walk_snap_warning";
        public const string CarRideLevel = Root + "/Props/CarRide/car_ride_level";
        public const string CarRideLurchLeft = Root + "/Props/CarRide/car_ride_lurch_left";
        public const string CarRideLurchRight = Root + "/Props/CarRide/car_ride_lurch_right";
        public const string CarRideSpill = Root + "/Props/CarRide/car_ride_spill";
        public const string GateCrashGateClosed = Root + "/Props/GateCrash/gate_crash_gate_closed";
        public const string GateCrashGateHeld = Root + "/Props/GateCrash/gate_crash_gate_held";
        public const string GateCrashGateSnap = Root + "/Props/GateCrash/gate_crash_gate_snap";
        public const string GateCrashToyWaiting = Root + "/Props/GateCrash/gate_crash_toy_waiting";
        public const string GateCrashToyClaimed = Root + "/Props/GateCrash/gate_crash_toy_claimed";
        public const string TableStealthHumanWatching = Root + "/Props/TableStealth/table_stealth_human_watching";
        public const string TableStealthHumanDistracted = Root + "/Props/TableStealth/table_stealth_human_distracted";
        public const string TableStealthHumanSpotted = Root + "/Props/TableStealth/table_stealth_human_spotted";
        public const string TableStealthHumanCaught = Root + "/Props/TableStealth/table_stealth_human_caught";
        public const string TableStealthSteakAvailable = Root + "/Props/TableStealth/table_stealth_steak_available";
        public const string TableStealthSteakSneakProgress = Root + "/Props/TableStealth/table_stealth_steak_sneak_progress";
        public const string TableStealthSteakGone = Root + "/Props/TableStealth/table_stealth_steak_gone";
        public const string SwitcherooDecoyGuarded = Root + "/Props/SquirrelSwitcheroo/switcheroo_decoy_guarded";
        public const string SwitcherooDecoyChased = Root + "/Props/SquirrelSwitcheroo/switcheroo_decoy_chased";
        public const string SwitcherooDecoyBackfire = Root + "/Props/SquirrelSwitcheroo/switcheroo_decoy_backfire";
        public const string SwitcherooStashGuarded = Root + "/Props/SquirrelSwitcheroo/switcheroo_stash_guarded";
        public const string SwitcherooStashOpen = Root + "/Props/SquirrelSwitcheroo/switcheroo_stash_open";
        public const string SwitcherooStashRaided = Root + "/Props/SquirrelSwitcheroo/switcheroo_stash_raided";
        public const string WalkCampaignHumanConfused = Root + "/Props/WalkCampaign/walk_campaign_human_confused";
        public const string WalkCampaignHumanGettingIt = Root + "/Props/WalkCampaign/walk_campaign_human_getting_it";
        public const string WalkCampaignHumanMisread = Root + "/Props/WalkCampaign/walk_campaign_human_misread";
        public const string WalkCampaignHumanWalkies = Root + "/Props/WalkCampaign/walk_campaign_human_walkies";
        public const string WalkCampaignHumanGaveUp = Root + "/Props/WalkCampaign/walk_campaign_human_gave_up";
        public const string WalkCampaignLeashWaiting = Root + "/Props/WalkCampaign/walk_campaign_leash_waiting";
        public const string WalkCampaignLeashPresented = Root + "/Props/WalkCampaign/walk_campaign_leash_presented";
        public const string WalkCampaignLeashGrabbed = Root + "/Props/WalkCampaign/walk_campaign_leash_grabbed";
        public const string BoneRelayScentPostIdle = Root + "/Props/BoneRelay/bone_relay_scent_post_idle";
        public const string BoneRelayScentPostCalled = Root + "/Props/BoneRelay/bone_relay_scent_post_called";
        public const string BoneRelayMoundUnknown = Root + "/Props/BoneRelay/bone_relay_mound_unknown";
        public const string BoneRelayMoundCalled = Root + "/Props/BoneRelay/bone_relay_mound_called";
        public const string BoneRelayMoundWrong = Root + "/Props/BoneRelay/bone_relay_mound_wrong";
        public const string BoneRelayMoundFound = Root + "/Props/BoneRelay/bone_relay_mound_found";
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
        public const string LevelAreaKitchenFloor = Root + "/Props/LevelAreas/kitchen_floor_area";
        public const string LevelAreaKitchenCounters = Root + "/Props/LevelAreas/kitchen_counter_wall";
        public const string LevelAreaCarInterior = Root + "/Props/LevelAreas/car_interior_cabin";
        public const string LevelAreaCarBalanceLane = Root + "/Props/LevelAreas/car_balance_lane";
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

        public static readonly string[] LevelAreaPropPack =
        {
            LevelAreaKitchenFloor, LevelAreaKitchenCounters,
            LevelAreaCarInterior, LevelAreaCarBalanceLane
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
            ChaosLeverReady, ChaosLeverRunning,
            ChaosJunctionTowelDrop, ChaosJunctionBasketTip, ChaosJunctionToyLaunch
        };

        public static readonly string[] PeeBreakPropPack =
        {
            PeeBreakCouch, PeeBreakTeenager, PeeBreakPhoneCharger,
            PeeBreakOpenDoor, PeeBreakLeash, PeeBreakHydrantRelief,
            PeeBreakBladderMeter, PeeBreakMisreadTennisBall
        };

        public static readonly string[] BackyardRescueP0Pack =
        {
            BackyardTrapGapOpen, BackyardTrapGapHeld, BackyardTrapGapFakeRoute,
            BackyardWeenieTargeted, BackyardWeenieDropped, BackyardWeenieSaved,
            BackyardPredatorLaneWarning
        };

        public static readonly string[] SnackHeistP0Pack =
        {
            SnackHeistPlateTargeted, SnackHeistPlateStashed,
            SnackHeistPlateStolen, SnackHeistGuardLane
        };

        public static readonly string[] SockPanicP0Pack =
        {
            SockPanicBasketClosed, SockPanicBasketOpen, SockPanicBasketFumble,
            SockPanicSockExposed, SockPanicSockDecoy, SockPanicSockSaved
        };

        public static readonly string[] ThreatConspiracyP0Pack =
        {
            SquirrelConspiracyCutoffOpen, SquirrelConspiracyCutoffHeld,
            SquirrelConspiracyCutoffFakeout, SquirrelConspiracyStashRevealed,
            SquirrelConspiracyStashCracked, EagleShadowCoverSafe,
            EagleShadowCoverSpotted, EagleShadowTalonGripClosed,
            EagleShadowTalonGripOpen, EagleShadowTalonGripFreed,
            CoyotesFenceGapOpen, CoyotesFenceGapPinned, CoyotesFenceGapRepaired,
            CoyotesFenceGapBreached, CoyotesFenceFakeSnack
        };

        public static readonly string[] AdventureP0Pack =
        {
            WeenieRoundupLoose, WeenieRoundupCarried, WeenieRoundupDropped,
            WeenieRoundupBowlEmpty, WeenieRoundupBowlProgress, WeenieRoundupBowlFull,
            ScentSearchDigUnknown, ScentSearchScentHot, ScentSearchScentCold,
            ScentSearchBoneFound, ThunderstormCloudWaiting, ThunderstormThunderclap,
            ThunderstormComfortHuddle, ThunderstormStormCleared,
            MarkYardZoneUnclaimed, MarkYardZoneClaimed, MarkYardZoneStolen,
            MarkYardSquirrelWatch, MarkYardSquirrelSteal
        };

        public static readonly string[] HomeTripP0Pack =
        {
            LeashWalkCheckpointWaiting, LeashWalkCheckpointReached, LeashWalkSnapWarning,
            CarRideLevel, CarRideLurchLeft, CarRideLurchRight, CarRideSpill,
            GateCrashGateClosed, GateCrashGateHeld, GateCrashGateSnap,
            GateCrashToyWaiting, GateCrashToyClaimed,
            TableStealthHumanWatching, TableStealthHumanDistracted,
            TableStealthHumanSpotted, TableStealthHumanCaught,
            TableStealthSteakAvailable, TableStealthSteakSneakProgress,
            TableStealthSteakGone
        };

        public static readonly string[] CoopTricksP0Pack =
        {
            SwitcherooDecoyGuarded, SwitcherooDecoyChased, SwitcherooDecoyBackfire,
            SwitcherooStashGuarded, SwitcherooStashOpen, SwitcherooStashRaided,
            WalkCampaignHumanConfused, WalkCampaignHumanGettingIt,
            WalkCampaignHumanMisread, WalkCampaignHumanWalkies,
            WalkCampaignHumanGaveUp, WalkCampaignLeashWaiting,
            WalkCampaignLeashPresented, WalkCampaignLeashGrabbed,
            BoneRelayScentPostIdle, BoneRelayScentPostCalled,
            BoneRelayMoundUnknown, BoneRelayMoundCalled,
            BoneRelayMoundWrong, BoneRelayMoundFound
        };

        public static readonly string[] EscapeCatchKitchenP0Pack =
        {
            GreatEscapeStationWaiting, GreatEscapeStationCheddarActive,
            GreatEscapeStationCocoaActive, GreatEscapeStationCompleted,
            GreatEscapeStationFumble, GreatEscapeStationSettle,
            BlanketCatchSlack, BlanketCatchTaut, BlanketCatchRipping,
            BlanketCatchCaught, BlanketSnackFalling, BlanketSnackCaught,
            BlanketSnackSplat, KitchenCounterReady, KitchenCounterBarked,
            KitchenSafeBowlEmpty, KitchenSafeBowlCatch,
            KitchenFoodGoodFalling, KitchenFoodBadFalling, KitchenFoodSplat
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
