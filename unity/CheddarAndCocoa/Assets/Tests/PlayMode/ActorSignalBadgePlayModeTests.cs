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
            Assert.AreEqual(0f, icon.rotation.eulerAngles.z, 0.5f,
                "The badge must stay upright while the actor root sways.");

            var renderer = icon.GetComponent<SpriteRenderer>();
            float worldHeight = renderer.sprite.bounds.size.y * icon.lossyScale.y;
            Assert.AreEqual(0.72f, worldHeight, 0.12f,
                "The badge must hold a constant readable world size regardless of actor scale/pulse.");
        }

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
            _game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == DogId.Cheddar) _cheddar = id.GetComponent<DogController>();
                else if (id.Id == DogId.Cocoa) _cocoa = id.GetComponent<DogController>();
            }
        }
    }
}
