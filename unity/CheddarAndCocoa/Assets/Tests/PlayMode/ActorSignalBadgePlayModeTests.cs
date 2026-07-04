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
        public IEnumerator CarRide_NearSpillTilt_RaisesWarningSignal()
        {
            yield return Load(GameManager.MissionVariant.CarRide);

            var car = GameObject.Find("Car Ride Balance Vehicle");
            Assert.IsNotNull(car, "Car Ride should stage its balance vehicle actor.");
            var feedback = car.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback);
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "A level car is a calm balance readout and must not raise the signal badge.");

            _game.ForceCarBalance(0.85f);

            Assert.That(feedback.Label, Does.Contain("SPILL WARNING"));
            Assert.IsTrue(feedback.SignalBadge.IsShowing,
                "A near-spill tilt must raise the distance signal before the meter maxes out.");
            Assert.That(feedback.SignalBadge.IconSpriteName, Does.Contain("warning"),
                "An imminent spill classifies as a warning skin.");

            _game.ForceCarBalance(0.2f);

            Assert.That(feedback.Label, Does.Contain("CAR TILT"));
            Assert.IsFalse(feedback.SignalBadge.IsShowing,
                "Recovering the lean must clear the urgency signal.");
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

            _cocoa.transform.position = door.transform.position;
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

        private static bool StationSignaling(GameObject marker)
        {
            var badge = marker != null ? marker.GetComponent<ActorSignalBadge>() : null;
            return badge != null && badge.IsShowing;
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
