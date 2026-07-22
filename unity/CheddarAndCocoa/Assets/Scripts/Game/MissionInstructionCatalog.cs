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
                    return "A huge eagle shadow sweeps the whole yard. Both dogs dive for cover, Cheddar wiggles free through Cocoa's timed pulls, then they face the eagle down together.";
                case GameManager.MissionVariant.CoyotesFence:
                    return "Coyotes are testing the fence line. Cheddar and Cocoa split up to pin the intruder with bark pressure and patch every weak spot before the yard is breached.";
                case GameManager.MissionVariant.WeenieRoundup:
                    return "A whole litter of scattered weenies needs herding home. Cheddar and Cocoa carry them one by one back to the home bowl before time runs out.";
                case GameManager.MissionVariant.ScentSearch:
                    return "Cheddar catches the broad direction of a buried bone, Cocoa tracks down the exact red-hot patch, and Cheddar digs only after her bark call.";
                case GameManager.MissionVariant.ThunderstormComfort:
                    return "A summer storm is spooking Cheddar. The dogs huddle, Cocoa gives the steady reassurance bark, and Cheddar answers before each thunderclap.";
                case GameManager.MissionVariant.MarkTheYard:
                    return "Territory matters. Cheddar and Cocoa race to claim every zone in the yard and hold them all at once before a scheming squirrel steals them back.";
                case GameManager.MissionVariant.LeashWalk:
                    return "Cheddar and Cocoa share a single leash for a proper neighborhood walk, staying close enough that it never snaps taut as they hit every checkpoint.";
                case GameManager.MissionVariant.CarRide:
                    return "The dogs are riding the back seat home. Turns send pups and junk sliding; at each brake Cocoa plants first so Cheddar can tuck safely behind her.";
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
                case GameManager.MissionVariant.SkunkBlastMayhem:
                    return "A skunk is guarding a prized dead bird in the yard. Cheddar barks loud to hold its attention while Cocoa sneaks in for the clean snatch - but when the tail lifts, both dogs must bail before the spray.";
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
                        "Finish together at the striped Rope/Tug prop until it pops complete.",
                        "The pool is open: run the floaties, fall in and you swim, and you shake off at the deck."
                    };
                case GameManager.MissionVariant.SnackHeist:
                    return new[]
                    {
                        "Cheddar is the snack thief: run him into snack plates to stash 4 forbidden snacks.",
                        "Cocoa is the guard: when SQUIRREL SNACK HEIST - BARK! appears, bark near the squirrel to open Cheddar's safe stealing window.",
                        "The watched final snack will not bank until Cocoa has stopped at least one active heist.",
                        "Wrong roles cause a harmless snack audit or mouth-full bark, so swap jobs and recover.",
                        "Two successful steals ends the run, so don't let it reach a snack twice."
                    };
                case GameManager.MissionVariant.SockPanic:
                    return new[]
                    {
                        "Cocoa is the anchor: Interact at the LAUNDRY BASKET to tip it open, then stay beside it and HOLD.",
                        "Cheddar is the chaos diver: once Cocoa opens the basket, he has 6 seconds to dive onto the exposed sock.",
                        "If Cocoa leaves, Cocoa grabs the sock, or time expires, the basket flops shut; recover by resetting the same hold-and-dive beat.",
                        "Rescue 5 socks before time runs out to build Cheddar's glorious sock mountain."
                    };
                case GameManager.MissionVariant.SquirrelConspiracy:
                    return new[]
                    {
                        "Cheddar pressures the squirrel with BARK HERD while Cocoa runs ahead to the glowing HOLD CUTOFF zone.",
                        "Cheddar's bark cannot advance the route unless Cocoa is physically holding the active cutoff; a solo herd escapes but remains recoverable.",
                        "Complete 4 handoffs to reveal the hidden stash, then Cocoa reaches it and Interacts while Cheddar guards the culprit.",
                        "Crack the case before the squirrel racks up 3 taunts."
                    };
                case GameManager.MissionVariant.EagleShadowPanic:
                    return new[]
                    {
                        "A completed sweep crosses the whole yard: both dogs must be inside HIDE HERE cover when it resolves (2 clean hides opens the rescue).",
                        "The eagle then snatches Cheddar. He Interacts to WIGGLE the grip open; Cocoa gets close and Interacts to PULL during that short window.",
                        "Repeat the wiggle/pull handoff three times, then huddle both dogs and bark together for the united-front finish.",
                        "Three exposures caught in the open ends the mission."
                    };
                case GameManager.MissionVariant.CoyotesFence:
                    return new[]
                    {
                        "Cocoa is the steady sentinel: get near the coyote and BARK to pin it for a short opening.",
                        "Cheddar is the digger: reach the active WEAK SPOT and Interact before Cocoa's pin expires.",
                        "Neither dog can perform both jobs, and out-of-range barks do not pin the coyote; repin and recover if the opening closes.",
                        "Cocoa barks away the fake snack lure instead of letting Cheddar take the bait.",
                        "After enough repairs, both dogs bark together to block the final push; 3 breaches ends the mission."
                    };
                case GameManager.MissionVariant.WeenieRoundup:
                    return new[]
                    {
                        "Split up for the four small WEENIES: walk into one to pick it up, then carry it to the HOME BOWL.",
                        "The JUMBO is a team haul: Cocoa stands beside it to steady, then Cheddar grabs it.",
                        "Stay together all the way to the bowl; separation makes the jumbo fumble, but it can be grabbed again immediately.",
                        "Deliver all 5 before the timer runs out, then enjoy the live bowl-full payoff."
                    };
                case GameManager.MissionVariant.ScentSearch:
                    return new[]
                    {
                        "Cheddar can bark for a broad compass direction, but Cocoa is the precise tracker.",
                        "Move Cocoa between DIG? patches and bark for COLD / WARM / RED HOT; a red-hot bark calls the exact mound.",
                        "Cheddar follows Cocoa's call and Interacts at the glowing mound. Cocoa cannot dig, and premature role attempts coach without spending a miss.",
                        "A called wrong mound costs one of four cold digs. Find 3 bones, then enjoy the live cache reveal."
                    };
                case GameManager.MissionVariant.ThunderstormComfort:
                    return new[]
                    {
                        "Huddle close before each clap; passive proximity calms panic but does not prepare the dogs for thunder.",
                        "Cocoa BARKS first with a steady reassurance, then Cheddar BARKS back before her short window closes.",
                        "Hold the physical huddle after COMFORT READY. A missed order, expired answer, or separation spikes panic but the next clap is immediately recoverable.",
                        "Prepare and weather 5 claps without either dog's panic maxing out, then enjoy the live storm-passed beat."
                    };
                case GameManager.MissionVariant.MarkTheYard:
                    return new[]
                    {
                        "Cheddar runs the route: enter a grey zone and press Interact to deliberately mark it green.",
                        "Each mark attracts the reclaim squirrel; Cocoa tracks it down and BARKS nearby to drive it off.",
                        "Cocoa's defense creates a short opening for Cheddar to reach another zone before the squirrel returns.",
                        "A stolen mark can be reclaimed safely; hold all five green at once to own the yard."
                    };
                case GameManager.MissionVariant.LeashWalk:
                    return new[]
                    {
                        "Cheddar and Cocoa share one leash, so stay close as you walk.",
                        "The named scout alternates each checkpoint: reach the marker and BARK the route call so your partner knows where to join.",
                        "After the call, both dogs must stand on the current checkpoint together to bank it.",
                        "Drift too far apart and the leash snaps taut as a penalty; four snaps fails the walk."
                    };
                case GameManager.MissionVariant.CarRide:
                    return new[]
                    {
                        "Watch the driver: the dashboard telegraphs every TURN and BRAKE before it hits.",
                        "Turns tilt the cabin and slide dogs and seat junk sideways - fight the slide, and jump over the cooler and toy bin as they sweep past.",
                        "When BRAKES flash, Cocoa Interacts to plant first; Cheddar gets beside her and Interacts to tuck behind the anchor. Hold together until the stop.",
                        "Ride out all 7 road events; 5 tumbles fails the drive. Bark together and the driver eases up."
                    };
                case GameManager.MissionVariant.GateCrash:
                    return new[]
                    {
                        "Cocoa creates the opening: reach the heavy gate and press Interact to plant as the anchor.",
                        "Cheddar turns that opening into progress by squeezing through the gap toward the toy while Cocoa stays beside the gate.",
                        "If Cocoa leaves, the gate snaps shut, Cheddar loses the crossing, and Cocoa must Interact again to re-anchor.",
                        "Hold steady until Cheddar claims the toy; wrong-dog and out-of-range attempts coach the pair without costing the round."
                    };
                case GameManager.MissionVariant.TableStealth:
                    return new[]
                    {
                        "A steak is dropped under the dinner table and a human is watching.",
                        "Choose the opening: Cocoa reaches the human and Interacts to hold a belly-rub flop, or Cheddar Barks beside the human to fire a short burp cloud.",
                        "Cocoa's sustained flop sends Cheddar to the steak; Cheddar's burst burp sends Cocoa. The partner turns the distraction into sneak progress.",
                        "Leaving a flop ends that hold, while a burp fades quickly. Sneaking without real attention gets the pair spotted, but the setup is recoverable.",
                        "Secure the steak before four exposures; the stolen-steak gag holds in the live world before the result card."
                    };
                case GameManager.MissionVariant.SquirrelSwitcheroo:
                    return new[]
                    {
                        "The squirrel is guarding its buried stash.",
                        "Cheddar reaches the decoy and Barks to start the feint, then feathers away once the squirrel commits.",
                        "Only during that chase window can Cocoa reach the real stash and press Interact for one raid.",
                        "Proximity alone does nothing. A guarded raid bonks harmlessly; over-baiting makes the squirrel wise up; both failures explain the reset.",
                        "Pull three clean switcheroos; the cracked-stash payoff holds in the live world before results."
                    };
                case GameManager.MissionVariant.WalkCampaign:
                    return new[]
                    {
                        "Neither signal works alone: Cocoa reaches the door and presses Interact for the dignified stare; Cheddar reaches the leash and presses Interact to present it.",
                        "Both dogs must stay beside their station so the two deliberate poses overlap long enough for the human to understand.",
                        "Leaving breaks that pose and requires another Interact. One signal held alone eventually makes the human fetch a funny wrong item.",
                        "After three misreads the walk is off; a clean campaign holds the live WALKIES payoff before results."
                    };
                case GameManager.MissionVariant.BoneRelay:
                    return new[]
                    {
                        "Four look-alike dirt mounds hide one real bone.",
                        "Cocoa reaches the scent post and BARKS to call which mound is real; proximity alone does not reveal it.",
                        "Only Cheddar can dig, so he waits for Cocoa's glowing call before committing to a mound.",
                        "A blind/decoy dig wastes one chance; each find re-buries the next bone, and three finds expose the stash."
                    };
                case GameManager.MissionVariant.GreatEscape:
                    return new[]
                    {
                        "Run the contraption chain in role order: Cocoa paws the latch, Cheddar shoulders the gate, Cocoa drags the cooler, Cheddar squeezes through.",
                        "The glowing station's named owner must reach it and press Interact; proximity alone never fires a step.",
                        "A deliberate wrong-dog Interact makes a harmless, readable CLANK. Dawdling eases the chain back one station, which can be repeated normally.",
                        "Six total fumbles/settles fails the breakout; a clean final squeeze holds the completed contraption and FREE DOGS payoff before results."
                    };
                case GameManager.MissionVariant.ChaosMachine:
                    return new[]
                    {
                        "Cheddar reaches the lever and presses Interact to start or resume the towel-drop, basket-tip, toy-launch cascade; Cocoa pre-positions at the first junction.",
                        "While it rolls, the named owner must reach the glowing junction and Interact before its brief window expires. The other dog gets a head start toward the next station.",
                        "Proximity alone never pulls or fires anything. Wrong paws coach; a missed timer visibly jams at that exact junction.",
                        "Cheddar re-pulls after a jam; four misfires fails. A clean toy launch holds the completed glorious mess before results."
                    };
                case GameManager.MissionVariant.BlanketCatch:
                    return new[]
                    {
                        "Cheddar and Cocoa spread apart to pull the blanket taut; too close sags and too far RIPS.",
                        "Once the blanket is taut, Cocoa BARKS to call the next snack down from the counter.",
                        "Both dogs slide the blanket's midpoint under the falling snack and hold the catch together.",
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
                case GameManager.MissionVariant.SkunkBlastMayhem:
                    return new[]
                    {
                        "Cheddar barks near the skunk to hold its attention (LURED!) so Cocoa can sneak in and Interact on the guarded dead bird for the clean snatch.",
                        "Watch the TAIL UP telegraph - both dogs must clear the blast range before the window closes or they get SKUNKED.",
                        "A skunked dog goes STINKY and can't lure/snatch until clean - Interact at the laundry pile to rub off the stink.",
                        "The clean partner hauls fresh laundry from the basket to the pile to keep the stinky dog's scrub-clean supply stocked."
                    };
                case GameManager.MissionVariant.KitchenFoodFrenzy:
                    return new[]
                    {
                        "Cheddar is the counter scout - reach the marked COUNTER route and bark to knock the next item loose (watch the colored telegraph flash).",
                        "Cocoa is the floor sweeper - catch gold food in the SAFE BOWL and dodge purple onions instead of catching them.",
                        "After 3 good catches, survive the DINNER RUSH finale (catch gold, dodge purple, catch gold) to clear it."
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
