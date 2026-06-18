using System.Collections.Generic;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Replaceable placeholder feedback slots for ArenaScene. These names are the contract future
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

        public enum PlaceholderWave { Tone, Noise }

        public static readonly AudioCueSlot[] RequiredAudioCues =
        {
            new AudioCueSlot(Bark, 520f, 0.08f, 0.18f, PlaceholderWave.Tone),
            new AudioCueSlot(TugRescueSuccess, 760f, 0.14f, 0.2f, PlaceholderWave.Tone),
            new AudioCueSlot(SnackSockCollect, 680f, 0.07f, 0.16f, PlaceholderWave.Tone),
            new AudioCueSlot(SquirrelStealMiss, 210f, 0.14f, 0.17f, PlaceholderWave.Noise),
            new AudioCueSlot(ScoreGain, 900f, 0.05f, 0.12f, PlaceholderWave.Tone),
            new AudioCueSlot(ScorePenalty, 170f, 0.09f, 0.13f, PlaceholderWave.Tone),
            new AudioCueSlot(MissionWin, 840f, 0.22f, 0.22f, PlaceholderWave.Tone),
            new AudioCueSlot(MissionFail, 150f, 0.2f, 0.17f, PlaceholderWave.Tone),
            new AudioCueSlot(UiReplayNextSelect, 610f, 0.05f, 0.12f, PlaceholderWave.Tone),
            new AudioCueSlot(ThreatWarning, 260f, 0.12f, 0.14f, PlaceholderWave.Noise)
        };

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
        public readonly float Frequency;
        public readonly float Seconds;
        public readonly float Volume;
        public readonly ArenaFeedbackCatalog.PlaceholderWave Wave;

        public AudioCueSlot(string name, float frequency, float seconds, float volume,
            ArenaFeedbackCatalog.PlaceholderWave wave)
        {
            Name = name;
            Frequency = frequency;
            Seconds = seconds;
            Volume = volume;
            Wave = wave;
        }
    }
}
