using System.Collections.Generic;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Replaceable generated feedback slots for ArenaScene. These names are the contract future
    /// authored audio and haptics should preserve even if the generated clips are replaced.
    /// </summary>
    public static class ArenaFeedbackCatalog
    {
        public const string Bark = "bark";
        public const string TugRescueSuccess = "tug_rescue_success";
        public const string SnackSockCollect = "snack_sock_collect";
        public const string SquirrelStealMiss = "squirrel_steal_miss";
        public const string ScoreGain = "score_gain";
        public const string ScorePenalty = "score_penalty";
        public const string MissionWin = "mission_win";
        public const string MissionFail = "mission_fail";
        public const string UiReplayNextSelect = "ui_replay_next_select";
        public const string UiMenuFocus = "ui_menu_focus";
        public const string UiButtonConfirm = "ui_button_confirm";
        public const string UiMenuOpen = "ui_menu_open";
        public const string UiMenuClose = "ui_menu_close";
        public const string UiButtonDisabled = "ui_button_disabled";
        public const string StarAppear = "star_appear";
        public const string AccelerationSkid = "acceleration_skid";
        public const string EatingGulp = "eating_gulp";
        public const string SquirrelChatter = "squirrel_chatter";
        public const string SquirrelEscapeLaugh = "squirrel_escape_laugh";
        public const string SquirrelStunned = "squirrel_stunned";
        public const string BunnyHop = "bunny_hop";
        public const string ToySqueak = "toy_squeak";
        public const string ThreatWarning = "threat_warning";
        public const string BackyardMusicLoop = "backyard_music_loop";

        // S5.1: guidance-ladder Tier 3 rescue, role-turn beacon appearance, and the two dogs'
        // identity-distinct handoff-flip chimes. Each replaces a placeholder cue an earlier signal
        // task reused out of necessity (Bark / UiReplayNextSelect) with its own named slot.
        public const string GuidanceRescueCall = "guidance_rescue_call";
        public const string RoleTurnBeaconAppear = "role_turn_beacon_appear";
        public const string HandoffChimeCheddar = "handoff_chime_cheddar";
        public const string HandoffChimeCocoa = "handoff_chime_cocoa";

        public enum GeneratedSfxKind
        {
            None,
            DogBark,
            TeamSuccess,
            CrunchCollect,
            SquirrelAlarm,
            ScoreSparkle,
            PenaltyThunk,
            VictoryFanfare,
            FailureSigh,
            UiBlip,
            ThreatRattle
        }

        public static readonly AudioCueSlot[] RequiredAudioCues =
        {
            new AudioCueSlot(Bark, GeneratedSfxKind.DogBark, 420f, 0.16f, 0.24f, 0.78f, 0.18f),
            new AudioCueSlot(TugRescueSuccess, GeneratedSfxKind.TeamSuccess, 620f, 0.26f, 0.22f, 0.92f, 0.08f),
            new AudioCueSlot(SnackSockCollect, GeneratedSfxKind.CrunchCollect, 700f, 0.16f, 0.18f, 0.34f, 0.36f),
            new AudioCueSlot(SquirrelStealMiss, GeneratedSfxKind.SquirrelAlarm, 340f, 0.22f, 0.2f, -0.38f, 0.46f),
            new AudioCueSlot(ScoreGain, GeneratedSfxKind.ScoreSparkle, 940f, 0.18f, 0.14f, 0.7f, 0.08f),
            new AudioCueSlot(ScorePenalty, GeneratedSfxKind.PenaltyThunk, 180f, 0.2f, 0.16f, -0.55f, 0.18f),
            new AudioCueSlot(MissionWin, GeneratedSfxKind.VictoryFanfare, 720f, 0.48f, 0.24f, 1.05f, 0.06f),
            new AudioCueSlot(MissionFail, GeneratedSfxKind.FailureSigh, 260f, 0.38f, 0.19f, -0.78f, 0.22f),
            new AudioCueSlot(UiReplayNextSelect, GeneratedSfxKind.UiBlip, 820f, 0.11f, 0.12f, 0.42f, 0.04f),
            new AudioCueSlot(UiMenuFocus, GeneratedSfxKind.UiBlip, 760f, 0.08f, 0.1f, 0.26f, 0.03f),
            new AudioCueSlot(UiButtonConfirm, GeneratedSfxKind.UiBlip, 940f, 0.12f, 0.14f, 0.34f, 0.03f),
            new AudioCueSlot(UiMenuOpen, GeneratedSfxKind.UiBlip, 680f, 0.14f, 0.13f, 0.58f, 0.04f),
            new AudioCueSlot(UiMenuClose, GeneratedSfxKind.UiBlip, 520f, 0.12f, 0.12f, -0.32f, 0.04f),
            new AudioCueSlot(UiButtonDisabled, GeneratedSfxKind.PenaltyThunk, 160f, 0.12f, 0.13f, -0.22f, 0.2f),
            new AudioCueSlot(StarAppear, GeneratedSfxKind.ScoreSparkle, 1100f, 0.22f, 0.16f, 0.82f, 0.05f),
            new AudioCueSlot(AccelerationSkid, GeneratedSfxKind.ThreatRattle, 300f, 0.18f, 0.13f, -0.42f, 0.34f),
            new AudioCueSlot(EatingGulp, GeneratedSfxKind.CrunchCollect, 560f, 0.16f, 0.18f, -0.18f, 0.18f),
            new AudioCueSlot(SquirrelChatter, GeneratedSfxKind.SquirrelAlarm, 430f, 0.16f, 0.16f, 0.7f, 0.32f),
            new AudioCueSlot(SquirrelEscapeLaugh, GeneratedSfxKind.SquirrelAlarm, 520f, 0.22f, 0.17f, 0.88f, 0.26f),
            new AudioCueSlot(SquirrelStunned, GeneratedSfxKind.PenaltyThunk, 220f, 0.18f, 0.16f, -0.46f, 0.18f),
            new AudioCueSlot(BunnyHop, GeneratedSfxKind.TeamSuccess, 840f, 0.13f, 0.13f, 0.72f, 0.04f),
            new AudioCueSlot(ToySqueak, GeneratedSfxKind.TeamSuccess, 1250f, 0.11f, 0.13f, 0.4f, 0.02f),
            new AudioCueSlot(ThreatWarning, GeneratedSfxKind.ThreatRattle, 230f, 0.28f, 0.21f, -0.12f, 0.58f),
            new AudioCueSlot(GuidanceRescueCall, GeneratedSfxKind.UiBlip, 860f, 0.16f, 0.15f, 0.5f, 0.08f),
            new AudioCueSlot(RoleTurnBeaconAppear, GeneratedSfxKind.ScoreSparkle, 980f, 0.18f, 0.13f, 0.66f, 0.06f),
            // Cheddar (chaos-puppy) reads higher/faster/rougher than Cocoa (veteran-queen) here, same
            // identity split A2.2's interact-squash amplitude uses - encoded in the generated-fallback
            // params even though the authored bank (each dog's own bark takes) is what actually plays.
            new AudioCueSlot(HandoffChimeCheddar, GeneratedSfxKind.DogBark, 480f, 0.14f, 0.22f, 0.9f, 0.16f),
            new AudioCueSlot(HandoffChimeCocoa, GeneratedSfxKind.DogBark, 340f, 0.18f, 0.2f, 0.55f, 0.14f)
        };

        public static string SignatureFor(AudioCueSlot cue) => $"{cue.Kind}:{cue.Frequency:0}:{cue.Sweep:0.00}:{cue.Noise:0.00}";

        public static bool ContainsCue(string cueName)
        {
            foreach (var cue in RequiredAudioCues)
            {
                if (cue.Name == cueName) return true;
            }

            return false;
        }

        public static Dictionary<string, AudioCueSlot> BuildLookup()
        {
            var lookup = new Dictionary<string, AudioCueSlot>();
            foreach (var cue in RequiredAudioCues) lookup[cue.Name] = cue;
            return lookup;
        }
    }

    public readonly struct AudioCueSlot
    {
        public readonly string Name;
        public readonly ArenaFeedbackCatalog.GeneratedSfxKind Kind;
        public readonly float Frequency;
        public readonly float Seconds;
        public readonly float Volume;
        public readonly float Sweep;
        public readonly float Noise;

        public AudioCueSlot(string name, ArenaFeedbackCatalog.GeneratedSfxKind kind, float frequency,
            float seconds, float volume, float sweep, float noise)
        {
            Name = name;
            Kind = kind;
            Frequency = frequency;
            Seconds = seconds;
            Volume = volume;
            Sweep = sweep;
            Noise = noise;
        }
    }
}
