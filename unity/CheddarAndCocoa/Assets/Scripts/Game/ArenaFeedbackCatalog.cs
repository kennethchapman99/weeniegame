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
        public const string ThreatWarning = "threat_warning";
        public const string BackyardMusicLoop = "backyard_music_loop";

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
            new AudioCueSlot(ThreatWarning, GeneratedSfxKind.ThreatRattle, 230f, 0.28f, 0.21f, -0.12f, 0.58f)
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
