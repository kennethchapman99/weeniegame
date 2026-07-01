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
                default:
                    return string.Empty;
            }
        }

        public static string HowToPlayFor(GameManager.MissionVariant variant)
        {
            switch (variant)
            {
                case GameManager.MissionVariant.BackyardRescue:
                    return "Collect Breakfast/Weenies scattered around the yard. When a label reads SQUIRREL STEALING - BARK!, bark near the squirrel before it reaches its target. When the Predator Warning appears, huddle both dogs together and bark to drive it off - or bark-rescue a grabbed partner. Complete the Squirrel Trap twice: one dog bark-pressures the squirrel while the other holds the blue ESCAPE GAP marker, then only the gap-holder can recover the dropped weenie (roles reverse for pass two). Finish by standing together at the striped Rope/Tug prop until BOTH TUGGING hits 100%.";
                case GameManager.MissionVariant.SnackHeist:
                    return "Walk into snack-plate props to stash them (Stash snacks 0/4). When the squirrel targets a snack and the label reads SQUIRREL SNACK HEIST - BARK!, bark near it to scare it off. Two successful steals ends the run, so don't let it reach a snack twice.";
                case GameManager.MissionVariant.SockPanic:
                    return "One dog interacts at the LAUNDRY BASKET to tip it open (TIP BASKET); the other then has 6 seconds to dive onto the exposed sock (DIVE FOR SOCK) - the tipper can't grab their own sock, so trade roles. Return 5 socks before time runs out; a missed dive just flops the basket shut, nothing is lost for good.";
                case GameManager.MissionVariant.SquirrelConspiracy:
                    return "Herd the squirrel around its route - whichever dog is nearest gets BARK HERD guidance while the partner runs to the glowing HOLD CUTOFF zone. A cutoff only scores if the partner is actually standing in that zone when you bark. Build up control to reveal the hidden stash, then interact there to crack the case before the squirrel racks up 3 taunts.";
                case GameManager.MissionVariant.EagleShadowPanic:
                    return "When the eagle's shadow sweeps the yard, get into one of the three HIDE HERE cover zones before it passes over you (2 clean hides opens the rescue). Then one dog distracts while the other frees the stranded toy caught in the open. Finally bring both dogs together and bark for the united-front finish. Three exposures caught in the open ends the mission.";
                case GameManager.MissionVariant.CoyotesFence:
                    return "When a coyote is testing a fence gap, one dog bark-pins it (hold the bark) while the partner interacts at the WEAK SPOT to fill the dirt - fills only count while the pin is held. Bark away the fake snack lure instead of taking the bait. After enough repairs, both dogs bark together to block the final push; 3 breaches ends the mission.";
                case GameManager.MissionVariant.WeenieRoundup:
                    return "Walk into any loose WEENIE marker to pick it up, then carry it to the HOME BOWL in the back corner. Both dogs can carry at once, so split the yard between you. A fumble bounces a dropped weenie a short distance - just chase it down again. Deliver all 5 before the timer runs out.";
                case GameManager.MissionVariant.ScentSearch:
                    return "Bark near a DIG? mound for a heat readout (RED HOT / WARM / COLD) toward the buried bone, then move toward hotter readings. Interact to dig the mound you think is hottest - a correct dig yields a bone and re-buries the next one elsewhere; a wrong dig wastes one of your four allowed misses. Find 3 bones to clear it.";
                case GameManager.MissionVariant.ThunderstormComfort:
                    return "There's nothing to collect - just stay close together. When the STORM CLOUD flashes HUDDLE! for a thunderclap, keep both dogs huddled close so panic drains instead of climbing (Cheddar spooks harder than Cocoa, so watch his meter). Weather 5 claps without either dog's panic maxing out.";
                case GameManager.MissionVariant.MarkTheYard:
                    return "Split up and stand in each of the 5 CLAIM zones to mark them green. The squirrel periodically re-marks whichever claimed zone is nearest it, so you have to hold ground across the whole yard. Win the instant all five zones glow green at the same time.";
                case GameManager.MissionVariant.LeashWalk:
                    return "Cheddar and Cocoa share one leash, so stay close as you walk. Move together through each of the four CHECKPOINT markers in order - both dogs must stand on the current checkpoint to bank it. Drift too far apart and the leash snaps taut as a penalty; four snaps fails the walk.";
                case GameManager.MissionVariant.CarRide:
                    return "The car cabin tilts as it lurches. Watch the car indicator and move both dogs to the side opposite the current tilt to bring it level (LEVEL / tipping LEFT / tipping RIGHT). Ride out 6 lurches without tipping; 4 spills fails the drive.";
                case GameManager.MissionVariant.GateCrash:
                    return "Cocoa braces the heavy gate open (anchor) while Cheddar squeezes through the gap to reach the toy on the other side (crosser). If Cocoa lets go mid-squeeze, the gate snaps shut and Cheddar has to start his crossing over - hold steady until he's through.";
                case GameManager.MissionVariant.TableStealth:
                    return "A steak is dropped under the dinner table and a human is watching. One dog holds the human's attention (Cocoa flops belly-up for a rub, or Cheddar burps a distraction cloud) while the partner sneaks the steak along the safe lane in segments. Sneak while the human is actually watching and you get spotted, sent back to your last checkpoint.";
                case GameManager.MissionVariant.SquirrelSwitcheroo:
                    return "The squirrel is guarding its buried stash. Cheddar feints toward a decoy nut pile to bait the squirrel into committing to the chase; only while it's fully committed can Cocoa slip in and raid the real stash. Hold the feint too long and the squirrel wises up (or Cheddar chases his own decoy) and the window snaps shut - feather off and try again.";
                case GameManager.MissionVariant.WalkCampaign:
                    return "Neither signal works alone. Cocoa holds a dignified door-stare while Cheddar presents the leash at the same time - the human only gets the message when both stations are held together. Cover only one station (or wander off) and the human gets confused and grabs the wrong thing; too many misreads calls off the walk.";
                case GameManager.MissionVariant.BoneRelay:
                    return "Four look-alike dirt mounds hide one real bone. Cocoa noses the scent post to call which mound is real; only Cheddar can dig, but he can't tell the mounds apart, so he has to wait for Cocoa's call before digging. Digging blind or digging a decoy wastes a dig - waste too many and the team gives up. Each bone found re-buries the next one somewhere new.";
                case GameManager.MissionVariant.GreatEscape:
                    return "Run an ordered contraption chain, taking turns by role: Cocoa paws the latch, Cheddar shoulders the gate, Cocoa drags the cooler into place, Cheddar squeezes through. Only the glowing station's owner can advance it - wrong dog or wrong order is a harmless fumble, and dawdling eases the chain back a step. Botch it too many times and the breakout fails.";
                case GameManager.MissionVariant.ChaosMachine:
                    return "Pre-position both dogs at their junctions, then pull the lever - the cascade (towel drop, basket tip, toy launch) runs itself. Each junction has a brief window where its owner dog must be in place, or the machine visibly jams there; re-pull the lever to resume from the jam. Too many misfires fails the run.";
                case GameManager.MissionVariant.BlanketCatch:
                    return "Both dogs grip opposite corners of a blanket and stretch it between them to catch food falling from the counter. Too close together and it sags (slack); too far apart and it RIPS - find the taut middle band, then keep the blanket's midpoint under the falling snack. Rip the blanket too many times and the mission fails.";
                case GameManager.MissionVariant.KitchenFoodFrenzy:
                    return "Cheddar is the counter scout - reach the marked COUNTER route and bark to knock the next item loose (watch for the colored telegraph flash before it drops). Cocoa is the floor sweeper - catch gold food in the SAFE BOWL and dodge purple onions instead of catching them. After 3 good catches, survive the DINNER RUSH finale's GOOD-BAD-GOOD sequence to clear it.";
                case GameManager.MissionVariant.OperationPeeBreak:
                    return "Four escalating beats with the distracted Teenager. Beat 1: Cocoa holds a door stare. Beat 2: Cocoa keeps staring while Cheddar presents the leash at the same time - neither alone is enough. Beat 3 (role-flip): Cheddar blocks the hallway while Cocoa unplugs the charger, draining the phone. Beat 4: hold the door and leash positions, then bark together near the door for the united-front finish. Barking too early just gets misread with a funny gag - it doesn't fail the run.";
                default:
                    return string.Empty;
            }
        }
    }
}
