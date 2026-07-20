using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Signal-language contract for urgent actor states: urgency is carried by an icon-only
    /// authored skin badge that stays visible at any distance, while sentence text remains a
    /// close-range/debug support prompt.
    /// </summary>
    public sealed class ActorSignalBadgePlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator SquirrelSteal_ShowsDistanceIconSignal_WhileTextStaysContextual()
        {
            yield return Load();

            var feedback = _game.SquirrelObject.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback);
            Assert.IsNotNull(feedback.SignalBadge,
                "Mission actors with state feedback should carry a signal badge slot.");
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "A waiting squirrel is not urgent and must not raise the signal badge.");

            // Move both dogs far away so the contextual text prompt is hidden.
            _cheddar.transform.position = _game.SquirrelObject.transform.position + Vector3.right * 30f;
            _cocoa.transform.position = _game.SquirrelObject.transform.position + Vector3.right * 30f;
            yield return null;
            yield return null;

            _game.ForceSquirrelStealAttempt();
            yield return null;
            yield return null;

            Assert.That(feedback.Label, Does.Contain("STEALING"),
                "The deterministic state string must remain the gameplay contract.");
            Assert.IsFalse(feedback.TextVisible,
                "Sentence text stays a close-range prompt even during urgent states.");
            Assert.IsTrue(feedback.SignalBadge.IsShowing,
                "An actively stealing squirrel must raise a distance-readable icon signal.");
            Assert.That(feedback.SignalBadge.IconSpriteName, Does.StartWith("world_label"),
                "The badge must use the authored world-label skin art, not placeholder geometry.");

            // Trapping the squirrel resolves the urgency into a calm recover prompt.
            _game.ForceBackyardTrapRedirect(DogId.Cheddar, true);
            yield return null;
            yield return null;

            Assert.That(feedback.Label, Does.Contain("TRAPPED"));
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "The signal badge must drop as soon as the urgent state resolves.");
        }

        [UnityTest]
        public IEnumerator PredatorWarning_ShowsWarningIcon_UntilHuddleBarkResolves()
        {
            yield return Load();

            var feedback = _game.PredatorObject.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback);
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "An offscreen predator must not raise the signal badge.");

            _game.ForcePredatorWarning();
            yield return null;
            yield return null;

            Assert.That(feedback.Label, Does.Contain("HUDDLE"));
            Assert.IsTrue(feedback.SignalBadge.IsShowing,
                "The predator warning must raise a distance-readable icon signal.");
            Assert.That(feedback.SignalBadge.IconSpriteName, Does.Contain("warning"),
                "A predator threat classifies as a warning skin, not a command skin.");

            _cheddar.transform.position = _cocoa.transform.position + Vector3.right;
            _cheddar.Bark();
            _cocoa.Bark();
            yield return null;
            yield return null;

            Assert.IsTrue(_game.PredatorResolved);
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "Yeeting the predator must clear the urgency signal.");
        }

        [UnityTest]
        public IEnumerator SignalBadge_KeepsFixedUprightWorldPose()
        {
            yield return Load();

            _game.ForceSquirrelStealAttempt();
            yield return null;
            yield return null;

            var feedback = _game.SquirrelObject.GetComponent<MissionActorFeedback>();
            Assert.IsTrue(feedback.SignalBadge.IsShowing);

            var icon = _game.SquirrelObject.transform.Find(ActorSignalBadge.BadgeName);
            Assert.IsNotNull(icon);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, icon.rotation.eulerAngles.z), 0.5f,
                "The badge must stay upright while the actor root sways.");

            var renderer = icon.GetComponent<SpriteRenderer>();
            float worldHeight = renderer.sprite.bounds.size.y * icon.lossyScale.y;
            Assert.AreEqual(0.72f, worldHeight, 0.12f,
                "The badge must hold a constant readable world size regardless of actor scale/pulse.");
        }

        [UnityTest]
        public IEnumerator MarkTheYard_StealingProwl_RaisesDistanceSignal()
        {
            yield return Load(GameManager.MissionVariant.MarkTheYard);

            var squirrel = GameObject.Find("MarkTheYardSquirrel");
            Assert.IsNotNull(squirrel, "Mark the Yard should stage its controller-local squirrel.");
            var feedback = squirrel.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback);
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "A watching squirrel is not urgent and must not raise the signal badge.");

            // One claimed zone gives the squirrel a steal target; the prowl starts after its
            // short reaction window (0.45s) passes.
            _game.ForceClaimZone(DogId.Cheddar);
            yield return new WaitForSeconds(0.6f);
            yield return null;

            Assert.That(feedback.Label, Does.Contain("STEALING"));
            Assert.IsTrue(feedback.SignalBadge.IsShowing,
                "A squirrel actively prowling for a claimed zone must raise the distance signal.");

            // The completed re-mark is an aftermath state; the zone art carries the recovery job.
            _game.ForceSquirrelReclaim();
            yield return null;
            yield return null;

            Assert.That(feedback.Label, Does.Contain("STOLE"));
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "The signal badge must drop once the steal resolves.");
        }

        [UnityTest]
        public IEnumerator SockPanic_OpenBasketDiveWindow_RaisesCommandSignal()
        {
            yield return Load(GameManager.MissionVariant.SockPanic);

            var basket = _game.SockPanicController.BasketObject;
            Assert.IsNotNull(basket, "Sock Panic should stage its laundry basket actor.");
            var feedback = basket.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback);
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "A closed basket is an ambient prompt and must not raise the signal badge.");

            _game.ForceSockBasketTip(DogId.Cocoa);
            yield return null;

            Assert.That(feedback.Label, Does.Contain("DIVE NOW"));
            Assert.IsTrue(feedback.SignalBadge.IsShowing,
                "The open-basket dive window is a closing timer and must raise the distance signal.");
            Assert.That(feedback.SignalBadge.IconSpriteName, Does.Contain("command"),
                "A partner-dive window is a command moment, not a threat warning.");

            _game.ForceSockBasketTimeout();
            yield return null;

            Assert.That(feedback.Label, Does.Contain("TIP AGAIN"));
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "The signal badge must drop when the basket flops shut.");
        }

        [UnityTest]
        public IEnumerator EagleShadow_TalonRescue_SignalsWigglePhaseThenPullWindow()
        {
            yield return Load(GameManager.MissionVariant.EagleShadowPanic);

            _game.ForceEagleShadowSafeHide();
            _game.ForceEagleShadowSafeHide();
            yield return null;
            yield return null;

            Assert.IsTrue(_game.EagleShadowPanicState.RescueObjectiveActive);
            var feedback = _game.SquirrelObject.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback);
            Assert.That(feedback.Label, Does.Contain("WIGGLE"));
            Assert.IsTrue(feedback.SignalBadge.IsShowing,
                "A snatched Cheddar must raise a distance signal over the talon grip.");
            Assert.That(feedback.SignalBadge.IconSpriteName, Does.Contain("warning"),
                "The closed talon grip reads as a threat warning while Cheddar wiggles.");

            _game.ForceEagleShadowWiggle();

            Assert.IsTrue(_game.EagleRescuePuzzle.WindowOpen);
            Assert.That(feedback.Label, Does.Contain("PULL NOW"));
            Assert.That(feedback.SignalBadge.IconSpriteName, Does.Contain("command"),
                "The cracked-grip pull window is Cocoa's command moment.");

            _game.ForceEagleShadowPull();
            int remaining = _game.EagleRescuePuzzle.PullsNeeded - _game.EagleRescuePuzzle.Pulls;
            for (int i = 0; i < remaining; i++)
            {
                _game.ForceEagleShadowWiggle();
                _game.ForceEagleShadowPull();
            }
            yield return null;

            Assert.IsTrue(_game.EagleRescuePuzzle.Freed);
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "Freeing Cheddar resolves the urgency; the huddle prompt stays calm.");
        }

        [UnityTest]
        public IEnumerator CarRide_DriverTelegraph_RaisesWarningSignal()
        {
            yield return Load(GameManager.MissionVariant.CarRide);

            var driver = GameObject.Find("Car Ride Driver");
            Assert.IsNotNull(driver, "Car Ride should stage its dashboard driver actor.");
            var feedback = driver.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback);
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "A cruising driver is a calm readout and must not raise the signal badge.");

            var controller = (CarRideMissionController)_game.ActiveMissionController;
            controller.ForceBeginRoadEvent(CarRideMissionController.RoadEventKind.Brake);

            Assert.That(feedback.Label, Does.Contain("BRAKES AHEAD"));
            Assert.IsTrue(feedback.SignalBadge.IsShowing,
                "A brake telegraph must raise the distance signal before the slam lands.");
            Assert.That(feedback.SignalBadge.IconSpriteName, Does.Contain("warning"),
                "An incoming brake classifies as a warning skin.");

            controller.ForceBrace(0);
            controller.ForceBrace(1);
            controller.ForceResolveRoadEvent();

            Assert.That(feedback.Label, Does.Contain("cruising"));
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "Riding out the event must return the driver to a calm signal.");
        }

        [UnityTest]
        public IEnumerator GreatEscape_ActiveStation_CarriesTheDistanceSignal()
        {
            yield return Load(GameManager.MissionVariant.GreatEscape);

            var first = GameObject.Find("EscapeStation_0");
            var second = GameObject.Find("EscapeStation_1");
            Assert.IsNotNull(first, "Great Escape should stage its contraption stations.");
            Assert.IsNotNull(second);
            Assert.IsTrue(StationSignaling(first),
                "The chain's active station must carry a distance-readable signal.");
            Assert.That(StationIcon(first), Does.Contain("command"),
                "A go-here-now station is a command moment, not a threat warning.");
            Assert.IsFalse(StationSignaling(second),
                "Stations later in the chain are not urgent yet and must stay quiet.");

            _game.ForceEscapeStep(ChainActor.Cocoa);
            yield return null;

            Assert.IsFalse(StationSignaling(first),
                "A completed station's signal must drop the moment the chain advances.");
            Assert.IsTrue(StationSignaling(second),
                "The signal must follow the chain to the next active station.");
        }

        [UnityTest]
        public IEnumerator ChaosMachine_DistanceSignalFollowsTheCascade()
        {
            yield return Load(GameManager.MissionVariant.ChaosMachine);

            var lever = GameObject.Find("ChaosMachineLever");
            var firstJunction = GameObject.Find("ChaosJunction_0");
            Assert.IsNotNull(lever);
            Assert.IsNotNull(firstJunction);
            Assert.IsTrue(StationSignaling(lever),
                "Before the pull, the lever is the act-now objective and must signal at distance.");
            Assert.That(StationIcon(lever), Does.Contain("command"));
            Assert.IsFalse(StationSignaling(firstJunction),
                "Pre-pull junctions are pre-positioning spots, not act-now signals.");

            _game.ForceChaosTrigger();
            yield return null;

            Assert.IsFalse(StationSignaling(lever),
                "Once the cascade is rolling the lever's job is done.");
            Assert.IsTrue(StationSignaling(firstJunction),
                "The live cascade's current junction must signal its assist window at distance.");
            Assert.That(StationIcon(firstJunction), Does.Contain("command"));

            _game.ForceChaosAdvance(3.5f, assisting: false);
            yield return null;

            Assert.IsTrue(StationSignaling(firstJunction),
                "A misfire must keep the jammed junction signalling.");
            Assert.That(StationIcon(firstJunction), Does.Contain("warning"),
                "A jam reads as a warning skin, not a calm command.");
            Assert.IsTrue(StationSignaling(lever),
                "The re-pull becomes the command once the machine jams.");
        }

        [UnityTest]
        public IEnumerator BoneRelay_SignalMovesFromScentPostToCalledMound()
        {
            yield return Load(GameManager.MissionVariant.BoneRelay);

            var post = GameObject.Find("ScentPost");
            Assert.IsNotNull(post, "Bone Relay should stage its scent post.");
            Assert.IsTrue(StationSignaling(post),
                "While Cocoa owes a sniff, the scent post is the act-now objective.");
            Assert.That(StationIcon(post), Does.Contain("command"));

            _game.ForceBoneReveal();
            yield return null;

            int call = _game.BoneRelayPuzzle.RevealedTarget;
            Assert.GreaterOrEqual(call, 0, "The reveal should call a real mound.");
            var mound = GameObject.Find($"BoneMound_{call}");
            Assert.IsNotNull(mound);
            Assert.IsFalse(StationSignaling(post),
                "The post's signal must hand off once the call is out.");
            Assert.IsTrue(StationSignaling(mound),
                "The called mound must carry the distance signal for Cheddar's dig.");
        }

        [UnityTest]
        public IEnumerator SquirrelConspiracy_ActiveCutoff_CarriesTheDistanceSignal()
        {
            yield return Load(GameManager.MissionVariant.SquirrelConspiracy);

            int route = _game.SquirrelConspiracyState.RouteIndex;
            var marker = GameObject.Find($"SquirrelCutoff_{route}");
            Assert.IsNotNull(marker, "The active cutoff marker should be staged and visible.");
            Assert.IsTrue(StationSignaling(marker),
                "The hold-cutoff zone must signal at distance while it is the active objective.");
            Assert.That(StationIcon(marker), Does.Contain("command"));
        }

        [UnityTest]
        public IEnumerator BackyardRescue_EscapeGap_SignalsUntilTheGapDogHoldsIt()
        {
            yield return Load();

            var gap = GameObject.Find("BackyardSquirrelTrapEscapeGap");
            Assert.IsNotNull(gap, "Backyard Rescue should stage the trap's escape gap.");

            var holder = _game.BackyardTrapState.GapDog == DogId.Cheddar ? _cheddar : _cocoa;
            holder.transform.position = Vector3.zero; // yard center, well outside the gap radius
            yield return null;
            yield return null;

            Assert.IsTrue(StationSignaling(gap),
                "An unheld escape gap must signal for its holder at any distance.");
            Assert.That(StationIcon(gap), Does.Contain("command"));

            holder.transform.position = _game.BackyardTrapGapPosition;
            yield return null;
            yield return null;

            Assert.IsFalse(StationSignaling(gap),
                "A held gap is the resolved state - the signal must drop while the dog stands in it.");
        }

        [UnityTest]
        public IEnumerator PeeBreak_BeatStations_SignalUntilTheirDogHoldsThem()
        {
            yield return Load(GameManager.MissionVariant.OperationPeeBreak);

            var door = GameObject.Find("PeeBreakDoor");
            var coach = GameObject.Find("PeeBreakCheddarCoach");
            Assert.IsNotNull(door, "Pee Break should stage the door station.");
            Assert.IsNotNull(coach, "Pee Break should stage Cheddar's watch pad.");

            // Park both dogs far from every station so beat 1's jobs are all unheld.
            _cheddar.transform.position = new Vector3(-20f, -10f, 0f);
            _cocoa.transform.position = new Vector3(-22f, -10f, 0f);
            yield return null;
            yield return null;

            Assert.IsTrue(StationSignaling(door),
                "Beat 1's door-stare station must signal for Cocoa at any distance.");
            Assert.That(StationIcon(door), Does.Contain("command"));
            Assert.IsTrue(StationSignaling(coach),
                "Beat 1's watch pad must signal for Cheddar at any distance.");

            // CF1.6: the door-stare stimulus (and the cocoaAtDoor read this signal badge follows)
            // now anchors to the doormat's floor position, not the door art's own tall-wall
            // center - see PeeBreakMissionController.DoorStareAnchor's XML doc.
            _cocoa.transform.position = _game.PeeBreakController.DoorStareAnchor;
            yield return null;
            yield return null;

            Assert.IsFalse(StationSignaling(door),
                "Cocoa holding the door stare resolves that station's signal.");
            Assert.IsTrue(StationSignaling(coach),
                "Cheddar's unheld watch pad must keep signalling independently.");
        }

        [UnityTest]
        public IEnumerator LeashWalk_CommandSignalRidesTheActiveCheckpoint()
        {
            yield return Load(GameManager.MissionVariant.LeashWalk);

            var first = GameObject.Find("LeashCheckpoint_0");
            Assert.IsNotNull(first, "Leash Walk should stage its checkpoint route.");
            Assert.IsTrue(StationSignaling(first),
                "The route's current checkpoint must signal at distance.");
            Assert.That(StationIcon(first), Does.Contain("command"));

            _game.ForceReachCheckpoint();
            yield return null;

            var second = GameObject.Find("LeashCheckpoint_1");
            Assert.IsNotNull(second, "The next checkpoint should be staged after the first is reached.");
            Assert.IsFalse(StationSignaling(first),
                "A reached checkpoint's signal must drop immediately.");
            Assert.IsTrue(StationSignaling(second),
                "The signal must walk the route with the dogs.");
        }

        [UnityTest]
        public IEnumerator GateCrash_HoldReleaseRhythm_AlternatesStationSignals()
        {
            yield return Load(GameManager.MissionVariant.GateCrash);

            var gate = GameObject.Find("GateCrashGate");
            var toy = GameObject.Find("GateCrashToy");
            Assert.IsNotNull(gate);
            Assert.IsNotNull(toy);

            _game.ForceGateHold(false);
            Assert.IsTrue(StationSignaling(gate),
                "An unbraced gate must signal for Cocoa at any distance.");
            Assert.That(StationIcon(gate), Does.Contain("command"));
            Assert.IsFalse(StationSignaling(toy),
                "The toy is not urgent while the gate is shut.");

            _game.ForceGateHold(true);
            Assert.IsFalse(StationSignaling(gate),
                "A braced gate is resolved - its signal must hand off.");
            Assert.IsTrue(StationSignaling(toy),
                "The open squeeze window must signal Cheddar's cross at distance.");
        }

        [UnityTest]
        public IEnumerator TableStealth_SneakWindow_MovesSignalFromHumanToSteak()
        {
            yield return Load(GameManager.MissionVariant.TableStealth);

            var human = GameObject.Find("TableStealthHuman");
            var steak = GameObject.Find("TableStealthSteak");
            Assert.IsNotNull(human);
            Assert.IsNotNull(steak);

            _game.ForceTableFlop(false);
            Assert.IsTrue(StationSignaling(human),
                "With no distraction running, Cocoa's flop station is the act-now objective.");
            Assert.IsFalse(StationSignaling(steak),
                "The steak must stay quiet while the human is watching the table.");

            _game.ForceTableFlop(true);
            Assert.IsFalse(StationSignaling(human),
                "A running belly-flop resolves the distraction signal.");
            Assert.IsTrue(StationSignaling(steak),
                "The open sneak window must signal the steak at distance.");
        }

        [UnityTest]
        public IEnumerator Switcheroo_CommitWindow_MovesSignalFromDecoyToStash()
        {
            yield return Load(GameManager.MissionVariant.SquirrelSwitcheroo);

            var decoy = GameObject.Find("SwitcherooDecoy");
            var stash = GameObject.Find("SwitcherooStash");
            Assert.IsNotNull(decoy);
            Assert.IsNotNull(stash);

            _game.ForceSwitcherooBait(0.05f, false);
            Assert.IsTrue(StationSignaling(decoy),
                "A guarding squirrel means the decoy feint is the act-now objective.");
            Assert.IsFalse(StationSignaling(stash),
                "The stash must stay quiet while the squirrel guards it.");

            _game.ForceSwitcherooBait(0.7f);
            Assert.IsFalse(StationSignaling(decoy),
                "A committed squirrel resolves the feint signal.");
            Assert.IsTrue(StationSignaling(stash),
                "The open raid window must signal the stash at distance.");
        }

        [UnityTest]
        public IEnumerator WalkCampaign_MessageHalves_SignalUntilTheirDogSendsThem()
        {
            yield return Load(GameManager.MissionVariant.WalkCampaign);

            var human = GameObject.Find("WalkCampaignHuman");
            var leash = GameObject.Find("WalkCampaignLeash");
            Assert.IsNotNull(human);
            Assert.IsNotNull(leash);

            // Park both dogs away from their message stations.
            _cheddar.transform.position = new Vector3(-20f, -10f, 0f);
            _cocoa.transform.position = new Vector3(-22f, -10f, 0f);
            yield return null;
            yield return null;

            Assert.IsTrue(StationSignaling(human),
                "Cocoa's unsent door stare must signal at distance.");
            Assert.IsTrue(StationSignaling(leash),
                "Cheddar's unpresented leash must signal at distance.");

            _cheddar.transform.position = leash.transform.position;
            _cheddar.Interact();
            yield return null;
            yield return null;

            Assert.IsFalse(StationSignaling(leash),
                "Cheddar's deliberate leash Interact resolves that half of the message.");
            Assert.IsTrue(StationSignaling(human),
                "Cocoa's half must keep signalling independently.");
        }

        [UnityTest]
        public IEnumerator WeenieRoundup_CarriedWeenie_RaisesBringItHomeSignal()
        {
            yield return Load(GameManager.MissionVariant.WeenieRoundup);

            var bowl = GameObject.Find("HomeBowl");
            Assert.IsNotNull(bowl, "Weenie Roundup should stage the home bowl.");
            var feedback = bowl.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback);
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "An idle bowl is a calm tally and must not raise the signal badge.");

            _game.ForceWeeniePickup(DogId.Cheddar);
            yield return null;

            Assert.That(feedback.Label, Does.Contain("BRING IT"));
            Assert.IsTrue(feedback.SignalBadge.IsShowing,
                "A weenie in transit must signal the delivery target at distance.");
            Assert.That(feedback.SignalBadge.IconSpriteName, Does.Contain("command"),
                "Bring-it-home is a command moment, not a threat warning.");

            _game.ForceWeenieDeliver(DogId.Cheddar);
            yield return null;

            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "Delivering the weenie must drop the bowl's urgency signal.");
        }

        [UnityTest]
        public IEnumerator EagleShadow_CoverPads_SignalUntilADogTucksIn()
        {
            yield return Load(GameManager.MissionVariant.EagleShadowPanic);

            var first = GameObject.Find("EagleCover_0");
            var second = GameObject.Find("EagleCover_1");
            Assert.IsNotNull(first, "Eagle Shadow Panic should stage its cover pads.");
            Assert.IsNotNull(second);

            // Park both dogs in the open, outside every cover radius.
            _cheddar.transform.position = new Vector3(0f, -5f, 0f);
            _cocoa.transform.position = new Vector3(3f, -5f, 0f);
            yield return null;
            yield return null;

            Assert.IsTrue(StationSignaling(first),
                "During the hide phase every open cover must signal at distance.");
            Assert.That(StationIcon(first), Does.Contain("command"),
                "A hide-here pad is a command moment; the eagle actor carries the threat warning.");
            Assert.IsTrue(StationSignaling(second),
                "All open covers are valid hides and must signal together.");

            _cheddar.transform.position = first.transform.position;
            yield return null;
            yield return null;

            Assert.IsFalse(StationSignaling(first),
                "A cover with a dog tucked inside is resolved and must drop its signal.");
            Assert.IsTrue(StationSignaling(second),
                "The other open covers must keep signalling for the partner.");

            _game.ForceEagleShadowSafeHide();
            _game.ForceEagleShadowSafeHide(); // the second hide opens the snatch/rescue beat
            yield return null;
            yield return null;

            Assert.IsFalse(StationSignaling(second),
                "The snatch/rescue beat moves the urgency to the talons - covers must go quiet.");
        }

        [UnityTest]
        public IEnumerator CoyotesFence_ActiveWeakSpot_WarnsThenCommandsWhenPinned()
        {
            yield return Load(GameManager.MissionVariant.CoyotesFence);

            var first = GameObject.Find("FenceGap_0");
            var second = GameObject.Find("FenceGap_1");
            Assert.IsNotNull(first, "Coyotes at the Fence should stage its weak-spot markers.");
            Assert.IsNotNull(second);
            Assert.IsTrue(StationSignaling(first),
                "The coyote's target weak spot must signal at distance.");
            Assert.That(StationIcon(first), Does.Contain("warning"),
                "An unpinned coyote closing on the gap reads as a threat warning.");
            Assert.IsFalse(StationSignaling(second),
                "Idle gaps are not urgent and must stay quiet.");

            _game.ForceCoyoteBarkPressure(DogId.Cocoa);
            yield return null;

            Assert.IsTrue(StationSignaling(first),
                "A pinned coyote keeps the active gap signalling for the partner's repair.");
            Assert.That(StationIcon(first), Does.Contain("command"),
                "A bark-pinned coyote flips the gap into the partner's fill-dirt command.");

            _game.ForceCoyoteRepair(DogId.Cheddar);
            yield return null;

            Assert.IsFalse(StationSignaling(first),
                "A filled weak spot is resolved and must drop its signal.");
            Assert.IsTrue(StationSignaling(second),
                "The signal must move with the coyote to the next active weak spot.");
            Assert.That(StationIcon(second), Does.Contain("warning"),
                "The fresh gap starts back in the loose-coyote threat state.");
        }

        [UnityTest]
        public IEnumerator GateCrash_StationText_StaysIdentityOnly_AcrossTheHoldFlip()
        {
            yield return Load(GameManager.MissionVariant.GateCrash);

            var gate = GameObject.Find("GateCrashGate");
            var toy = GameObject.Find("GateCrashToy");
            Assert.AreEqual("GATE", StationText(gate),
                "Close-range station text is identity-only; the instruction lives on the badge, sprites, and HUD line.");
            Assert.AreEqual("TOY", StationText(toy));

            _game.ForceGateHold(true);
            _game.ForceGateCross(0.3f);
            yield return null;

            Assert.AreEqual("GATE", StationText(gate),
                "The hold flip must move to the badge handoff and held sprite, not back into shouting text.");
            Assert.AreEqual("TOY", StationText(toy),
                "Squeeze progress lives on the HUD objective line, not in world text.");
        }

        [UnityTest]
        public IEnumerator BoneRelay_CalledMoundText_StaysDigQuestion_TheBadgeCarriesTheCall()
        {
            yield return Load(GameManager.MissionVariant.BoneRelay);

            _game.ForceBoneReveal();
            yield return null;

            int call = _game.BoneRelayPuzzle.RevealedTarget;
            Assert.GreaterOrEqual(call, 0);
            var mound = GameObject.Find($"BoneMound_{call}");
            Assert.IsNotNull(mound);
            Assert.IsTrue(StationSignaling(mound),
                "The called mound must carry the distance signal.");
            Assert.AreEqual("DIG?", StationText(mound),
                "The call reads through the badge, gold tint, and called sprite - the identity text must not flip to DIG HERE!.");
        }

        private static bool StationSignaling(GameObject marker)
        {
            var badge = marker != null ? marker.GetComponent<ActorSignalBadge>() : null;
            return badge != null && badge.IsShowing;
        }

        private static string StationText(GameObject marker)
        {
            var label = marker != null ? marker.GetComponentInChildren<TextMesh>(true) : null;
            return label != null ? label.text : string.Empty;
        }

        private static string StationIcon(GameObject marker) =>
            marker.GetComponent<ActorSignalBadge>().IconSpriteName;

        private IEnumerator Load(GameManager.MissionVariant variant = GameManager.MissionVariant.BackyardRescue)
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
            _game.StartMission(variant);
            yield return null;

            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == DogId.Cheddar) _cheddar = id.GetComponent<DogController>();
                else if (id.Id == DogId.Cocoa) _cocoa = id.GetComponent<DogController>();
            }
        }
    }
}
