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
