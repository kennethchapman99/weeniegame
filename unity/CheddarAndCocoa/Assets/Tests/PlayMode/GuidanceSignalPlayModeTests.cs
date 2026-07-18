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
    /// G1.2: the guidance-escalation ladder becomes visible/audible. Kitchen Food Frenzy is the
    /// fixture mission because both its opening-beat markers carry a child world label from the
    /// same NewMarker() call that TryGetObjectiveTarget returns, so both Tier 2's label-gate-lift
    /// and Tier 1's arrow emphasis are exercisable without any mission-specific plumbing. Kitchen
    /// also always hands BOTH dogs a valid target (scout/sweeper), so it cannot exercise the
    /// single-owner path (GuidanceOwningDogIndex/partner-chip-pulse/HUD dog-naming) end-to-end - that
    /// derivation is covered separately by the pure ComputeGuidanceOwningDogIndex test below, since
    /// no mission in the current roster hands only one dog a target (see the caveat on
    /// GameManager.GuidanceOwningDogIndex).
    /// </summary>
    public sealed class GuidanceSignalPlayModeTests
    {
        [Test]
        public void ComputeGuidanceOwningDogIndex_CoversAllFourCombinations()
        {
            Assert.AreEqual(0, GameManager.ComputeGuidanceOwningDogIndex(true, false));
            Assert.AreEqual(1, GameManager.ComputeGuidanceOwningDogIndex(false, true));
            Assert.IsNull(GameManager.ComputeGuidanceOwningDogIndex(true, true));
            Assert.IsNull(GameManager.ComputeGuidanceOwningDogIndex(false, false));
        }

        [UnityTest]
        public IEnumerator Tier0_NoArrowEmphasis()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            Assert.AreEqual(0, game.GuidanceTier);
            foreach (var arrow in game.ObjectiveArrows)
                Assert.IsFalse(arrow.IsEmphasized, "Normal play must stay Tier-0-quiet: no arrow emphasis.");
        }

        [UnityTest]
        public IEnumerator Tier1_EmphasizesBothArrows_KitchenGivesBothDogsATarget()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            game.ForceGuidanceStall(12f);
            yield return null;

            Assert.AreEqual(1, game.GuidanceTier);
            foreach (var arrow in game.ObjectiveArrows)
                Assert.IsTrue(arrow.IsEmphasized, "Tier 1 must brighten the active objective arrows.");
        }

        [UnityTest]
        public IEnumerator ProgressReset_DropsEmphasisBackToQuiet()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            game.ForceGuidanceStall(12f);
            yield return null;
            Assert.AreEqual(1, game.GuidanceTier);

            // An objective-copy change is a generic progress signal; forcing a drop flips both dogs'
            // roles (scout/sweeper -> catch/reset), which changes their objective text.
            ((KitchenFoodFrenzyMissionController)game.ActiveMissionController).ForceDrop(KitchenFoodFrenzyMissionState.FoodKind.Good);
            yield return null; // LogObjectiveIfChanged (and NotifyProgress) run at the tail of this frame
            yield return null; // presentation catches up to the now-reset tier on the following frame

            Assert.AreEqual(0, game.GuidanceTier);
            foreach (var arrow in game.ObjectiveArrows)
                Assert.IsFalse(arrow.IsEmphasized, "A progress signal must return the ladder to quiet Tier 0.");
        }

        [UnityTest]
        public IEnumerator Tier2_WidensBothTargetLabels_AndRevertsOnProgress()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            game.ForceGuidanceStall(25f);
            yield return null;
            Assert.AreEqual(2, game.GuidanceTier);

            var counterLabel = GameObject.Find("KitchenCounterRoute").GetComponentInChildren<TextMesh>();
            var safeZoneLabel = GameObject.Find("KitchenSafeBowl").GetComponentInChildren<TextMesh>();
            Assert.IsNotNull(counterLabel);
            Assert.IsNotNull(safeZoneLabel);
            var counterVisibility = counterLabel.GetComponent<WorldLabelVisibility>();
            var safeZoneVisibility = safeZoneLabel.GetComponent<WorldLabelVisibility>();
            Assert.IsNotNull(counterVisibility);
            Assert.IsNotNull(safeZoneVisibility);
            Assert.Greater(counterVisibility.PromptRange, WorldLabelVisibility.DefaultPromptRange,
                "Tier 2 must widen the acting dog's objective label proximity gate.");
            Assert.Greater(safeZoneVisibility.PromptRange, WorldLabelVisibility.DefaultPromptRange,
                "Tier 2 must widen the partner's objective label proximity gate too (Kitchen hands both dogs a target).");

            ((KitchenFoodFrenzyMissionController)game.ActiveMissionController).ForceDrop(KitchenFoodFrenzyMissionState.FoodKind.Good);
            yield return null; // LogObjectiveIfChanged (and NotifyProgress) run at the tail of this frame
            yield return null; // presentation catches up to the now-reset tier on the following frame

            Assert.AreEqual(0, game.GuidanceTier);
            Assert.AreEqual(WorldLabelVisibility.DefaultPromptRange, counterVisibility.PromptRange,
                "Dropping back to Tier 0 must restore the label's normal proximity gate.");
            Assert.AreEqual(WorldLabelVisibility.DefaultPromptRange, safeZoneVisibility.PromptRange);
        }

        [UnityTest]
        public IEnumerator Tier3_FlagsRescueActive_AndFiresOneAudioCueOnTheEdge()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            Assert.IsFalse(game.GuidanceRescueActive);
            int barksBefore = CountBarkCues(game);

            game.ForceGuidanceStall(45f);
            yield return null;

            Assert.AreEqual(3, game.GuidanceTier);
            Assert.IsTrue(game.GuidanceRescueActive, "Tier 3 must flag the HUD objective line for a flash.");
            // Kitchen always hands both dogs a target, so the owning dog is undeterminable here (see
            // the class-level note) - the flash must still activate, just without a dog name.
            Assert.IsNull(game.GuidanceOwningDogIndex);
            Assert.IsEmpty(game.GuidanceRescueDogName);
            Assert.AreEqual(barksBefore + 1, CountBarkCues(game), "Tier 3 must fire exactly one placeholder audio cue on the tier-up edge.");

            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(barksBefore + 1, CountBarkCues(game), "Staying at Tier 3 must not spam the audio cue every frame.");
        }

        [UnityTest]
        public IEnumerator Replay_ClearsGuidancePresentationState()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            game.ForceGuidanceStall(25f);
            yield return null;
            Assert.AreEqual(2, game.GuidanceTier);

            game.Restart();
            yield return null;

            Assert.AreEqual(0, game.GuidanceTier);
            foreach (var arrow in game.ObjectiveArrows)
                Assert.IsFalse(arrow.IsEmphasized, "Replay must not carry emphasis over from the previous attempt.");
            var counterLabel = GameObject.Find("KitchenCounterRoute")?.GetComponentInChildren<TextMesh>();
            if (counterLabel != null)
            {
                var visibility = counterLabel.GetComponent<WorldLabelVisibility>();
                if (visibility != null)
                    Assert.AreEqual(WorldLabelVisibility.DefaultPromptRange, visibility.PromptRange,
                        "Replay must not carry a widened label range over from the previous attempt.");
            }
        }

        private static int CountBarkCues(GameManager game)
        {
            int count = 0;
            foreach (string cue in game.AudioCueRequests)
                if (cue == ArenaFeedbackCatalog.Bark) count++;
            return count;
        }

        private static IEnumerator LoadArena()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }
    }
}
