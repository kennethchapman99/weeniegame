namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Level-select flavor copy: a one-line premise plus a grounded "how to play" readout for every
    /// mission variant. Content describes real controller-owned mechanics/labels (see
    /// docs/ARENA-PLAYABLE.md and docs/COOP-PUZZLE-PRIMITIVES.md), not aspirational design text, so
    /// it stays accurate as a couch-test player reads it before starting a mission.
    /// </summary>
    public static class MissionInstructionCatalog
    {
        public static string DescriptionFor(GameManager.MissionVariant variant)
        {
            switch (variant)
            {
                case GameManager.MissionVariant.BackyardRescue:
                    return "Cheddar and Cocoa team up to protect the yard's breakfast weenies from a thieving squirrel and a diving predator, then finish the job with a shared rope tug.";
                case GameManager.MissionVariant.SnackHeist:
                    return "Cheddar and Cocoa smell a forbidden snack stash on the counter and need to stash it away before the squirrel union notices and steals it out from under them.";
                case GameManager.MissionVariant.SockPanic:
                    return "It's laundry day chaos - Cheddar and Cocoa tip open the laundry basket and dive for every escaping sock before the humans get home.";
                case GameManager.MissionVariant.SquirrelConspiracy:
                    return "A suspicious squirrel has been plotting in the yard. Cheddar and Cocoa herd it around its route, cut off its escape, and crack open its hidden stash.";
                case GameManager.MissionVariant.EagleShadowPanic:
                    return "A huge eagle shadow sweeps over the yard. Cheddar and Cocoa dive for cover, rescue a stranded toy caught in the open, then face the shadow down together.";
                case GameManager.MissionVariant.CoyotesFence:
                    return "Coyotes are testing the fence line. Cheddar and Cocoa split up to pin the intruder with bark pressure and patch every weak spot before the yard is breached.";
                case GameManager.MissionVariant.WeenieRoundup:
                    return "A whole litter of scattered weenies needs herding home. Cheddar and Cocoa carry them one by one back to the home bowl before time runs out.";
                case GameManager.MissionVariant.ScentSearch:
                    return "Cheddar and Cocoa follow their noses through the yard's buried-bone puzzle, sniffing out hot and cold trails to find every bone without wasting a dig.";
                case GameManager.MissionVariant.ThunderstormComfort:
                    return "A summer storm is rolling in and it's spooking the pups, especially Cheddar. Cheddar and Cocoa huddle close together to comfort each other through every clap.";
                case GameManager.MissionVariant.MarkTheYard:
                    return "Territory matters. Cheddar and Cocoa race to claim every zone in the yard and hold them all at once before a scheming squirrel steals them back.";
                case GameManager.MissionVariant.LeashWalk:
                    return "Cheddar and Cocoa share a single leash for a proper neighborhood walk, staying close enough that it never snaps taut as they hit every checkpoint.";
                case GameManager.MissionVariant.CarRide:
                    return "The dogs are along for an exciting, lurching car ride. Cheddar and Cocoa lean opposite the tilt to keep the back seat level all the way home.";
                case GameManager.MissionVariant.GateCrash:
                    return "Cheddar and Cocoa spot a heavy gate standing between them and a toy on the other side. One braces it open while the other squeezes through before it slams shut.";
                case GameManager.MissionVariant.TableStealth:
                    return "A steak has dropped under the dinner table during a meal. Cheddar and Cocoa turn spy - one holds the human's attention while the other sneaks the prize.";
                case GameManager.MissionVariant.SquirrelSwitcheroo:
                    return "The squirrel is camped on its buried stash and won't budge. Cheddar baits it away with a decoy while Cocoa raids the real stash the moment it's committed.";
                case GameManager.MissionVariant.WalkCampaign:
                    return "Cheddar and Cocoa stage a coordinated campaign for more walks, holding a door-stare and a presented leash together until the human finally gets the message.";
                case GameManager.MissionVariant.BoneRelay:
                    return "A mysterious bone is buried somewhere in a field of look-alike mounds. Cocoa sniffs out the real one while Cheddar does the digging on her call.";
                case GameManager.MissionVariant.GreatEscape:
                    return "The gate's latched and the yard is calling. Cheddar and Cocoa work an escape contraption in careful turns to break out into the sun.";
                case GameManager.MissionVariant.ChaosMachine:
                    return "Cheddar and Cocoa rig up a backyard contraption of their own - a towel drop, a tipped basket, and a launched toy - and hold their junctions while the chain reaction runs.";
                case GameManager.MissionVariant.BlanketCatch:
                    return "It's raining snacks off the counter. Cheddar and Cocoa stretch a blanket taut between them and chase falling treats to keep the midpoint under every drop.";
                case GameManager.MissionVariant.KitchenFoodFrenzy:
                    return "It's raining food in the kitchen. Cheddar knocks items loose from the counter while Cocoa catches the good stuff and dodges the bad.";
                case GameManager.MissionVariant.OperationPeeBreak:
                    return "The Teenager is glued to the phone and somebody really needs to go outside. Cheddar and Cocoa escalate through four signal beats to finally get that door open.";
                case GameManager.MissionVariant.BabyBirdBedlam:
                    return "Chicks are tumbling out of the big oak nest and dinner is falling from the sky. Cheddar shake-gulps each one down while Cocoa bark-repels the furious parent birds dive-bombing the feast.";
                default:
                    return string.Empty;
            }
        }

        /// <summary>Legacy single-paragraph readout: the bullet steps joined back together.</summary>
        public static string HowToPlayFor(GameManager.MissionVariant variant)
        {
            return string.Join(" ", HowToPlayStepsFor(variant));
        }

        /// <summary>
        /// Couch feedback: the how-to paragraph was a wall of text. Each mission now reads as short
        /// numbered-feeling steps, and any on-screen label (SQUIRREL STEALING - BARK!, ESCAPE GAP,
        /// HIDE HERE...) is written exactly as it appears in the world so players recognize it live.
        /// </summary>
        public static string[] HowToPlayStepsFor(GameManager.MissionVariant variant)
        {
            switch (variant)
            {
                case GameManager.MissionVariant.BackyardRescue:
                    return new[]
                    {
                        "Collect the Breakfast/Weenies scattered around the yard.",
                        "SQUIRREL STEALING - BARK! means bark near the squirrel before it reaches its target.",
                        "On the Predator Warning, huddle both dogs together and bark to drive it off - or bark-rescue a grabbed partner.",
                        "Squirrel Trap (twice): one dog bark-pressures the squirrel while the other holds the blue ESCAPE GAP marker.",
                        "Only the gap-holder can recover the dropped weenie - roles reverse for pass two.",
                        "Finish together at the striped Rope/Tug prop until BOTH TUGGING hits 100%.",
                        "The pool is open: run the floaties, fall in and you swim, and you shake off at the deck."
                    };
                case GameManager.MissionVariant.SnackHeist:
                    return new[]
                    {
                        "Walk into snack-plate props to stash them (Stash snacks 0/4).",
                        "SQUIRREL SNACK HEIST - BARK! means bark near the squirrel to scare it off the targeted snack.",
                        "Two successful steals ends the run, so don't let it reach a snack twice."
                    };
                case GameManager.MissionVariant.SockPanic:
                    return new[]
                    {
                        "One dog interacts at the LAUNDRY BASKET to tip it open (TIP BASKET).",
                        "The other then has 6 seconds to dive onto the exposed sock (DIVE FOR SOCK) - the tipper can't grab their own sock, so trade roles.",
                        "Return 5 socks before time runs out; a missed dive just flops the basket shut, nothing is lost for good."
                    };
                case GameManager.MissionVariant.SquirrelConspiracy:
                    return new[]
                    {
                        "Herd the squirrel around its route - the nearest dog gets BARK HERD guidance while the partner runs to the glowing HOLD CUTOFF zone.",
                        "A cutoff only scores if the partner is actually standing in that zone when you bark.",
                        "Build up control to reveal the hidden stash, then interact there to crack the case before the squirrel racks up 3 taunts."
                    };
                case GameManager.MissionVariant.EagleShadowPanic:
                    return new[]
                    {
                        "When the eagle's shadow sweeps the yard, get into one of the three HIDE HERE cover zones before it passes over you (2 clean hides opens the rescue).",
                        "Then one dog distracts while the other frees the stranded toy caught in the open.",
                        "Finally bring both dogs together and bark for the united-front finish.",
                        "Three exposures caught in the open ends the mission."
                    };
                case GameManager.MissionVariant.CoyotesFence:
                    return new[]
                    {
                        "When a coyote is testing a fence gap, one dog bark-pins it (hold the bark) while the partner interacts at the WEAK SPOT to fill the dirt.",
                        "Fills only count while the pin is held.",
                        "Bark away the fake snack lure instead of taking the bait.",
                        "After enough repairs, both dogs bark together to block the final push; 3 breaches ends the mission."
                    };
                case GameManager.MissionVariant.WeenieRoundup:
                    return new[]
                    {
                        "Walk into any loose WEENIE marker to pick it up, then carry it to the HOME BOWL in the back corner.",
                        "Both dogs can carry at once, so split the yard between you.",
                        "A fumble bounces a dropped weenie a short distance - just chase it down again.",
                        "Deliver all 5 before the timer runs out."
                    };
                case GameManager.MissionVariant.ScentSearch:
                    return new[]
                    {
                        "Bark near a DIG? mound for a heat readout (RED HOT / WARM / COLD) toward the buried bone.",
                        "Move toward hotter readings, then interact to dig the mound you think is hottest.",
                        "A correct dig yields a bone and re-buries the next one elsewhere; a wrong dig wastes one of your four allowed misses.",
                        "Find 3 bones to clear it."
                    };
                case GameManager.MissionVariant.ThunderstormComfort:
                    return new[]
                    {
                        "There's nothing to collect - just stay close together.",
                        "When the STORM CLOUD flashes HUDDLE! for a thunderclap, keep both dogs huddled close so panic drains instead of climbing.",
                        "Cheddar spooks harder than Cocoa, so watch his meter.",
                        "Weather 5 claps without either dog's panic maxing out."
                    };
                case GameManager.MissionVariant.MarkTheYard:
                    return new[]
                    {
                        "Split up and stand in each of the 5 CLAIM zones to mark them green.",
                        "The squirrel periodically re-marks whichever claimed zone is nearest it, so hold ground across the whole yard.",
                        "Win the instant all five zones glow green at the same time."
                    };
                case GameManager.MissionVariant.LeashWalk:
                    return new[]
                    {
                        "Cheddar and Cocoa share one leash, so stay close as you walk.",
                        "Move together through each of the four CHECKPOINT markers in order - both dogs must stand on the current checkpoint to bank it.",
                        "Drift too far apart and the leash snaps taut as a penalty; four snaps fails the walk."
                    };
                case GameManager.MissionVariant.CarRide:
                    return new[]
                    {
                        "The car cabin tilts as it lurches.",
                        "Watch the car indicator and move both dogs to the side opposite the current tilt to bring it level (LEVEL / tipping LEFT / tipping RIGHT).",
                        "Ride out 6 lurches without tipping; 4 spills fails the drive."
                    };
                case GameManager.MissionVariant.GateCrash:
                    return new[]
                    {
                        "Cocoa braces the heavy gate open (anchor) while Cheddar squeezes through the gap to reach the toy on the other side (crosser).",
                        "If Cocoa lets go mid-squeeze, the gate snaps shut and Cheddar has to start his crossing over - hold steady until he's through."
                    };
                case GameManager.MissionVariant.TableStealth:
                    return new[]
                    {
                        "A steak is dropped under the dinner table and a human is watching.",
                        "One dog holds the human's attention: Cocoa flops belly-up for a rub, or Cheddar burps a distraction cloud.",
                        "The partner sneaks the steak along the safe lane in segments.",
                        "Sneak while the human is actually watching and you get spotted, sent back to your last checkpoint."
                    };
                case GameManager.MissionVariant.SquirrelSwitcheroo:
                    return new[]
                    {
                        "The squirrel is guarding its buried stash.",
                        "Cheddar feints toward a decoy nut pile to bait the squirrel into committing to the chase.",
                        "Only while it's fully committed can Cocoa slip in and raid the real stash.",
                        "Hold the feint too long and the squirrel wises up (or Cheddar chases his own decoy) - feather off and try again."
                    };
                case GameManager.MissionVariant.WalkCampaign:
                    return new[]
                    {
                        "Neither signal works alone: Cocoa holds a dignified door-stare while Cheddar presents the leash at the same time.",
                        "The human only gets the message when both stations are held together.",
                        "Cover only one station (or wander off) and the human grabs the wrong thing; too many misreads calls off the walk."
                    };
                case GameManager.MissionVariant.BoneRelay:
                    return new[]
                    {
                        "Four look-alike dirt mounds hide one real bone.",
                        "Cocoa noses the scent post to call which mound is real; only Cheddar can dig, so he waits for Cocoa's call before digging.",
                        "Digging blind or digging a decoy wastes a dig - waste too many and the team gives up.",
                        "Each bone found re-buries the next one somewhere new."
                    };
                case GameManager.MissionVariant.GreatEscape:
                    return new[]
                    {
                        "Run the contraption chain in role order: Cocoa paws the latch, Cheddar shoulders the gate, Cocoa drags the cooler, Cheddar squeezes through.",
                        "Only the glowing station's owner can advance it - wrong dog or wrong order is a harmless fumble.",
                        "Dawdling eases the chain back a step; botch it too many times and the breakout fails."
                    };
                case GameManager.MissionVariant.ChaosMachine:
                    return new[]
                    {
                        "Pre-position both dogs at their junctions, then pull the lever - the cascade (towel drop, basket tip, toy launch) runs itself.",
                        "Each junction has a brief window where its owner dog must be in place, or the machine visibly jams there.",
                        "Re-pull the lever to resume from the jam; too many misfires fails the run."
                    };
                case GameManager.MissionVariant.BlanketCatch:
                    return new[]
                    {
                        "Both dogs grip opposite corners of a blanket and stretch it between them to catch food falling from the counter.",
                        "Too close together and it sags (slack); too far apart and it RIPS - find the taut middle band.",
                        "Keep the blanket's midpoint under the falling snack.",
                        "Rip the blanket too many times and the mission fails."
                    };
                case GameManager.MissionVariant.BabyBirdBedlam:
                    return new[]
                    {
                        "Chicks drop from THE NEST - Cheddar runs to the GRAB IT! marker and grabs the chick (Tug/Rescue) before the parents airlift it back.",
                        "Cheddar then shakes it down (Tug/Rescue x3) - the last shake is the GULP. He can't defend himself while shaking.",
                        "When PARENT BIRD DIVE flashes, Cocoa gets under the diving parent and BARKS to repel it.",
                        "An un-repelled dive PECKS Cheddar and the chick escapes - three pecks fail the mission. Eat 4 chicks to clear."
                    };
                case GameManager.MissionVariant.KitchenFoodFrenzy:
                    return new[]
                    {
                        "Cheddar is the counter scout - reach the marked COUNTER route and bark to knock the next item loose (watch the colored telegraph flash).",
                        "Cocoa is the floor sweeper - catch gold food in the SAFE BOWL and dodge purple onions instead of catching them.",
                        "After 3 good catches, survive the DINNER RUSH finale's GOOD-BAD-GOOD sequence to clear it."
                    };
                case GameManager.MissionVariant.OperationPeeBreak:
                    return new[]
                    {
                        "Four escalating beats with the distracted Teenager.",
                        "Beat 1: Cocoa holds a door stare.",
                        "Beat 2: Cocoa keeps staring while Cheddar presents the leash at the same time - neither alone is enough.",
                        "Beat 3 (role-flip): Cheddar blocks the hallway while Cocoa unplugs the charger, draining the phone.",
                        "Beat 4: hold the door and leash positions, then bark together near the door for the united-front finish.",
                        "Barking too early just gets misread with a funny gag - it doesn't fail the run."
                    };
                default:
                    return System.Array.Empty<string>();
            }
        }

        /// <summary>
        /// Wraps runs of ALL-CAPS copy (the exact text of in-world labels like SQUIRREL STEALING -
        /// BARK! or ESCAPE GAP) in rich-text gold/bold so the mission-select steps show labels the
        /// way they appear on-screen during play. Pure string transform for deterministic tests.
        /// </summary>
        public static string HighlightOnScreenLabels(string step)
        {
            if (string.IsNullOrEmpty(step)) return string.Empty;

            string[] tokens = step.Split(' ');
            bool[] caps = new bool[tokens.Length];
            for (int i = 0; i < tokens.Length; i++) caps[i] = IsCapsToken(tokens[i]);
            // " - " and " / " connect two halves of one on-screen label (STEALING - BARK!).
            for (int i = 1; i < tokens.Length - 1; i++)
            {
                if ((tokens[i] == "-" || tokens[i] == "/") && caps[i - 1] && caps[i + 1]) caps[i] = true;
            }

            var builder = new System.Text.StringBuilder(step.Length + 64);
            int index = 0;
            while (index < tokens.Length)
            {
                if (!caps[index])
                {
                    if (builder.Length > 0) builder.Append(' ');
                    builder.Append(tokens[index]);
                    index++;
                    continue;
                }

                int end = index;
                while (end + 1 < tokens.Length && caps[end + 1]) end++;
                if (builder.Length > 0) builder.Append(' ');
                builder.Append("<b><color=#ffd75e>");
                for (int i = index; i <= end; i++)
                {
                    if (i > index) builder.Append(' ');
                    builder.Append(tokens[i]);
                }
                builder.Append("</color></b>");
                index = end + 1;
            }

            return builder.ToString();
        }

        private static bool IsCapsToken(string token)
        {
            string core = token.Trim('(', ')', ',', '.', ';', ':');
            int upper = 0;
            foreach (char c in core)
            {
                if (char.IsLower(c)) return false;
                if (char.IsUpper(c)) upper++;
            }
            return upper >= 2;
        }
    }
}
