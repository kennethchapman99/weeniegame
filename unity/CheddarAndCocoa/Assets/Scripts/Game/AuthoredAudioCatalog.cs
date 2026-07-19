using System;
using System.Collections.Generic;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Game
{
    /// <summary>Resource paths for imported couch-test audio under Assets/Audio/Resources.</summary>
    public static class AuthoredAudioCatalog
    {
        public const string Root = "AuthoredSfx";

        public static readonly string[] AllClipResourcePaths =
        {
            Root + "/p0_acceleration_skid",
            Root + "/p0_button_confirm",
            Root + "/p0_cheddar_bark_01",
            Root + "/p0_cheddar_bark_02",
            Root + "/p0_cheddar_bark_03",
            Root + "/p0_cheddar_bark_04",
            Root + "/p0_cheddar_bark_05",
            Root + "/p0_cheddar_bark_06",
            Root + "/p0_cheddar_bark_07",
            Root + "/p0_cheddar_bark_08",
            Root + "/p0_cocoa_bark_01",
            Root + "/p0_cocoa_bark_02",
            Root + "/p0_cocoa_bark_03",
            Root + "/p0_cocoa_bark_04",
            Root + "/p0_cocoa_bark_05",
            Root + "/p0_cocoa_bark_06",
            Root + "/p0_cocoa_bark_07",
            Root + "/p0_cocoa_bark_08",
            Root + "/p0_menu_tile_focus",
            Root + "/p0_score_gain_pop_01",
            Root + "/p0_score_gain_pop_02",
            Root + "/p0_score_gain_pop_03",
            Root + "/p0_score_gain_pop_04",
            Root + "/p0_score_gain_pop_05",
            Root + "/p0_score_penalty_bonk",
            Root + "/p1_eating_gulp_micro_sound",
            Root + "/p1_squirrel_chatter_01",
            Root + "/p1_squirrel_chatter_02",
            Root + "/p1_squirrel_chatter_03",
            Root + "/p1_squirrel_chatter_04",
            Root + "/p1_squirrel_chatter_05",
            Root + "/p1_squirrel_chatter_06",
            Root + "/p1_squirrel_chatter_07",
            Root + "/p1_squirrel_chatter_08",
            Root + "/p1_squirrel_chatter_09",
            Root + "/p1_squirrel_chatter_10",
            Root + "/p1_squirrel_escape_laugh",
            Root + "/p1_squirrel_stunned",
            Root + "/p2_bunny_hop_boing",
            Root + "/p2_toy_squeak",
            Root + "/p3_button_disabled",
            Root + "/p3_menu_close",
            Root + "/p3_menu_open",
            Root + "/p3_star_appear_01",
            Root + "/p3_star_appear_02",
            Root + "/p3_star_appear_03"
        };

        public static readonly string[] CheddarBarks =
        {
            Root + "/p0_cheddar_bark_01",
            Root + "/p0_cheddar_bark_02",
            Root + "/p0_cheddar_bark_03",
            Root + "/p0_cheddar_bark_04",
            Root + "/p0_cheddar_bark_05",
            Root + "/p0_cheddar_bark_06",
            Root + "/p0_cheddar_bark_07",
            Root + "/p0_cheddar_bark_08"
        };

        public static readonly string[] CocoaBarks =
        {
            Root + "/p0_cocoa_bark_01",
            Root + "/p0_cocoa_bark_02",
            Root + "/p0_cocoa_bark_03",
            Root + "/p0_cocoa_bark_04",
            Root + "/p0_cocoa_bark_05",
            Root + "/p0_cocoa_bark_06",
            Root + "/p0_cocoa_bark_07",
            Root + "/p0_cocoa_bark_08"
        };

        private static readonly Dictionary<string, string[]> CueBanks = new Dictionary<string, string[]>
        {
            {
                ArenaFeedbackCatalog.Bark, Combine(CheddarBarks, CocoaBarks)
            },
            {
                ArenaFeedbackCatalog.TugRescueSuccess, new[]
                {
                    Root + "/p2_toy_squeak"
                }
            },
            {
                ArenaFeedbackCatalog.SnackSockCollect, new[]
                {
                    Root + "/p1_eating_gulp_micro_sound"
                }
            },
            {
                ArenaFeedbackCatalog.SquirrelStealMiss, new[]
                {
                    Root + "/p1_squirrel_chatter_01",
                    Root + "/p1_squirrel_chatter_02",
                    Root + "/p1_squirrel_chatter_03",
                    Root + "/p1_squirrel_chatter_04",
                    Root + "/p1_squirrel_chatter_05",
                    Root + "/p1_squirrel_chatter_06",
                    Root + "/p1_squirrel_chatter_07",
                    Root + "/p1_squirrel_chatter_08",
                    Root + "/p1_squirrel_chatter_09",
                    Root + "/p1_squirrel_chatter_10",
                    Root + "/p1_squirrel_escape_laugh",
                    Root + "/p1_squirrel_stunned"
                }
            },
            {
                ArenaFeedbackCatalog.ScoreGain, new[]
                {
                    Root + "/p0_score_gain_pop_01",
                    Root + "/p0_score_gain_pop_02",
                    Root + "/p0_score_gain_pop_03",
                    Root + "/p0_score_gain_pop_04",
                    Root + "/p0_score_gain_pop_05"
                }
            },
            {
                ArenaFeedbackCatalog.ScorePenalty, new[]
                {
                    Root + "/p0_score_penalty_bonk",
                    Root + "/p3_button_disabled"
                }
            },
            {
                ArenaFeedbackCatalog.MissionWin, new[]
                {
                    Root + "/p3_star_appear_01",
                    Root + "/p3_star_appear_02",
                    Root + "/p3_star_appear_03"
                }
            },
            {
                ArenaFeedbackCatalog.MissionFail, new[]
                {
                    Root + "/p0_score_penalty_bonk",
                    Root + "/p3_button_disabled",
                    Root + "/p0_acceleration_skid"
                }
            },
            {
                ArenaFeedbackCatalog.UiReplayNextSelect, new[]
                {
                    Root + "/p0_button_confirm"
                }
            },
            {
                ArenaFeedbackCatalog.UiMenuFocus, new[]
                {
                    Root + "/p0_menu_tile_focus",
                }
            },
            {
                ArenaFeedbackCatalog.UiButtonConfirm, new[]
                {
                    Root + "/p0_button_confirm"
                }
            },
            {
                ArenaFeedbackCatalog.UiMenuOpen, new[]
                {
                    Root + "/p3_menu_open"
                }
            },
            {
                ArenaFeedbackCatalog.UiMenuClose, new[]
                {
                    Root + "/p3_menu_close"
                }
            },
            {
                ArenaFeedbackCatalog.UiButtonDisabled, new[]
                {
                    Root + "/p3_button_disabled"
                }
            },
            {
                ArenaFeedbackCatalog.StarAppear, new[]
                {
                    Root + "/p3_star_appear_01",
                    Root + "/p3_star_appear_02",
                    Root + "/p3_star_appear_03"
                }
            },
            {
                ArenaFeedbackCatalog.AccelerationSkid, new[]
                {
                    Root + "/p0_acceleration_skid"
                }
            },
            {
                ArenaFeedbackCatalog.EatingGulp, new[]
                {
                    Root + "/p1_eating_gulp_micro_sound"
                }
            },
            {
                ArenaFeedbackCatalog.SquirrelChatter, new[]
                {
                    Root + "/p1_squirrel_chatter_01",
                    Root + "/p1_squirrel_chatter_02",
                    Root + "/p1_squirrel_chatter_03",
                    Root + "/p1_squirrel_chatter_04",
                    Root + "/p1_squirrel_chatter_05",
                    Root + "/p1_squirrel_chatter_06",
                    Root + "/p1_squirrel_chatter_07",
                    Root + "/p1_squirrel_chatter_08",
                    Root + "/p1_squirrel_chatter_09",
                    Root + "/p1_squirrel_chatter_10"
                }
            },
            {
                ArenaFeedbackCatalog.SquirrelEscapeLaugh, new[]
                {
                    Root + "/p1_squirrel_escape_laugh"
                }
            },
            {
                ArenaFeedbackCatalog.SquirrelStunned, new[]
                {
                    Root + "/p1_squirrel_stunned"
                }
            },
            {
                ArenaFeedbackCatalog.BunnyHop, new[]
                {
                    Root + "/p2_bunny_hop_boing"
                }
            },
            {
                ArenaFeedbackCatalog.ToySqueak, new[]
                {
                    Root + "/p2_toy_squeak"
                }
            },
            {
                ArenaFeedbackCatalog.ThreatWarning, new[]
                {
                    Root + "/p1_squirrel_chatter_06",
                    Root + "/p1_squirrel_chatter_07",
                    Root + "/p1_squirrel_chatter_08",
                    Root + "/p1_squirrel_chatter_09",
                    Root + "/p1_squirrel_chatter_10",
                    Root + "/p0_acceleration_skid"
                }
            },
            // S5.1: no fresh recordings available in this pass (same tooling gap A2.3/A2.4 hit for
            // art), so these reuse the closest-fitting already-imported clips rather than sharing a
            // bank with an unrelated existing cue. p0_menu_tile_focus is the only imported "draw
            // attention, no success/failure connotation" clip, so it stands in for the Tier-3 rescue
            // call until a dedicated coach-woof take exists (reusing an actual bark here would make
            // the rescue cue indistinguishable from an ordinary bark, defeating the point). The star-
            // appear trio ("something just appeared") is a genuine semantic fit for a badge popping
            // into view, not just a placeholder. The handoff chimes are the one clean case: each dog's
            // own bark takes ARE their identity, so reusing them per-dog is the intended design, not a
            // compromise.
            {
                ArenaFeedbackCatalog.GuidanceRescueCall, new[]
                {
                    Root + "/p0_menu_tile_focus"
                }
            },
            {
                ArenaFeedbackCatalog.RoleTurnBeaconAppear, new[]
                {
                    Root + "/p3_star_appear_01",
                    Root + "/p3_star_appear_02",
                    Root + "/p3_star_appear_03"
                }
            },
            {
                ArenaFeedbackCatalog.HandoffChimeCheddar, CheddarBarks
            },
            {
                ArenaFeedbackCatalog.HandoffChimeCocoa, CocoaBarks
            }
        };

        public static IReadOnlyList<string> CueBankFor(string cueName) =>
            !string.IsNullOrEmpty(cueName) && CueBanks.TryGetValue(cueName, out var bank)
                ? bank
                : Array.Empty<string>();

        public static IReadOnlyList<string> DogBarkBankFor(DogId dog) =>
            dog == DogId.Cocoa ? CocoaBarks : CheddarBarks;

        public static bool IsKnownClipPath(string resourcePath)
        {
            foreach (string path in AllClipResourcePaths)
            {
                if (path == resourcePath) return true;
            }

            return false;
        }

        private static string[] Combine(string[] first, string[] second)
        {
            var combined = new string[first.Length + second.Length];
            Array.Copy(first, combined, first.Length);
            Array.Copy(second, 0, combined, first.Length, second.Length);
            return combined;
        }
    }
}
