using System.Collections;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;
using CheddarAndCocoa.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Locks the blocking Tier-3 anti-stuck tutorial contract: the progressive Tier-1/Tier-2 nudges
    /// remain non-blocking, a sustained no-progress stall freezes the world, the card snapshots only
    /// current-beat guidance, and the resume input cannot leak into live gameplay on the same frame.
    /// </summary>
    public sealed class StruggleTutorialPlayModeTests
    {
        private GameManager _game;

        [UnityTest]
        public IEnumerator TierTwo_RemainsNonBlocking_TierThreeFreezesOnClearStruggle()
        {
            yield return LoadKitchen();

            _game.ForceGuidanceStall(MissionGuidanceEscalation.DefaultTier2Seconds);
            Assert.AreEqual(2, _game.GuidanceTier);
            Assert.IsFalse(_game.StruggleTutorialVisible,
                "The progressive nudge/coach tiers should not interrupt play before clear struggle.");
            Assert.AreEqual(1f, Time.timeScale);

            _game.ForceGuidanceStall(
                MissionGuidanceEscalation.DefaultTier3Seconds -
                MissionGuidanceEscalation.DefaultTier2Seconds);

            Assert.AreEqual(3, _game.GuidanceTier);
            Assert.IsTrue(_game.StruggleTutorialVisible);
            Assert.AreEqual(0f, Time.timeScale, "The visual lesson must genuinely stop simulation time.");
            Assert.AreEqual(1, _game.StruggleTutorialActivationCount);
            Assert.IsFalse(_game.ButtonCoachVisible,
                "The blocking lesson owns the coaching slot; the ambient coach must not stack under it.");
        }

        [UnityTest]
        public IEnumerator Tutorial_SnapshotsOnlyCurrentObjectiveAndTruthfulCurrentActions()
        {
            yield return LoadKitchen();

            string objectiveAtTrigger = _game.ObjectiveLabel;
            DogId scout = _game.KitchenState.ScoutDog;
            DogId sweeper = _game.KitchenState.SweeperDog;
            _game.ForceGuidanceStall(MissionGuidanceEscalation.DefaultTier3Seconds);

            Assert.AreEqual(objectiveAtTrigger, _game.StruggleTutorialObjectiveLabel,
                "The overlay may mirror the current beat, never a future beat or payoff.");
            Assert.AreEqual(GameManager.TutorialActionStep.Bark,
                _game.StruggleTutorialActionFor(scout),
                "Kitchen's current scout ask is truthfully BARK.");
            Assert.IsNull(_game.StruggleTutorialActionFor(sweeper),
                "The positional sweeper ask must render movement, never invent a face-button answer.");
            Assert.IsNotEmpty(_game.StruggleTutorialInstructionFor(scout));
            Assert.IsNotEmpty(_game.StruggleTutorialInstructionFor(sweeper));
            Assert.That(_game.StruggleTutorialObjectiveLabel, Does.Not.Contain("CLEAR"));
            Assert.That(_game.StruggleTutorialObjectiveLabel, Does.Not.Contain("FINALE"));
        }

        [UnityTest]
        public IEnumerator FrozenTutorial_HoldsMissionClock_AndResumeConsumesItsInputFrame()
        {
            yield return LoadKitchen();

            _game.ForceGuidanceStall(MissionGuidanceEscalation.DefaultTier3Seconds);
            float remainingAtFreeze = _game.TimeRemaining;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(remainingAtFreeze, _game.TimeRemaining,
                "Round pressure must not drain while players study the lesson.");

            var inputs = Object.FindObjectsByType<GamepadPlayerInput>(FindObjectsSortMode.None);
            Assert.That(inputs.Length, Is.GreaterThanOrEqualTo(2));
            foreach (var input in inputs)
                Assert.IsFalse(input.enabled, "Dog input stays disabled behind the blocking lesson.");

            _game.ResumeStruggleTutorial();
            Assert.IsFalse(_game.StruggleTutorialVisible);
            Assert.AreEqual(1f, Time.timeScale);
            foreach (var input in inputs)
                Assert.IsFalse(input.enabled,
                    "The resume press must be consumed instead of also firing the coached action.");

            yield return null;
            foreach (var input in inputs)
                Assert.IsTrue(input.enabled, "Normal controls return on the clean frame after resume.");
        }

        [UnityTest]
        public IEnumerator Resume_DoesNotRepeatUntilProgressResetsTheStruggleLadder()
        {
            yield return LoadKitchen();

            _game.ForceGuidanceStall(MissionGuidanceEscalation.DefaultTier3Seconds);
            _game.ResumeStruggleTutorial();
            yield return null;

            _game.ForceGuidanceStall(10f);
            Assert.IsFalse(_game.StruggleTutorialVisible,
                "Remaining at Tier 3 must not nag repeatedly after the players resume.");
            Assert.AreEqual(1, _game.StruggleTutorialActivationCount);

            _game.Restart();
            yield return null;
            Assert.AreEqual(0, _game.GuidanceTier);
            Assert.IsFalse(_game.StruggleTutorialVisible);
            Assert.AreEqual(0, _game.StruggleTutorialActivationCount);
        }

        private IEnumerator LoadKitchen()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
            _game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;
        }
    }
}
