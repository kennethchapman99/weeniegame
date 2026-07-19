using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// G1.3: the role-turn beacon (small paw badge in the owning dog's identity color, floating over
    /// the active objective target). Great Escape is the fixture for "follows the active owner" per
    /// the task's own test spec - it's one of the three hard-handoff puzzles (Great Escape, Chaos
    /// Machine, Bone Relay) that implement IMissionRoleOwner and opt into GuidanceBeaconAlwaysOn,
    /// because (see the G1.2 caveat carried into GameManager.GuidanceOwningDogIndex) their
    /// TryGetObjectiveTarget hands BOTH dogs a target every step - the generic presence/absence
    /// heuristic alone would never resolve an owner for them. Kitchen Food Frenzy (no IMissionRoleOwner,
    /// not opted in) is the fixture for "shared steps show none".
    /// </summary>
    public sealed class RoleTurnBeaconPlayModeTests
    {
        [UnityTest]
        public IEnumerator Beacon_AlwaysOnMission_VisibleAtTierZero_AndFollowsTheAlternatingOwner()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            Assert.AreEqual(0, game.GuidanceTier, "This test is specifically about Tier-0 always-on visibility.");
            Assert.IsTrue(game.GuidanceBeaconVisible, "Great Escape opts into GuidanceBeaconAlwaysOn, so the beacon must show even with no stall.");

            // Owners = { Cocoa, Cheddar, Cocoa, Cheddar } (CoopGreatEscapePlayModeTests documents the same sequence).
            Color cocoaTint = game.GuidanceBeaconTint;
            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner);
            yield return null;
            Assert.IsTrue(game.GuidanceBeaconVisible);
            Color cheddarTint = game.GuidanceBeaconTint;
            Assert.AreNotEqual(cocoaTint, cheddarTint, "The beacon tint must change when the owning dog changes.");

            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner);
            yield return null;
            Assert.AreEqual(cocoaTint, game.GuidanceBeaconTint, "Ownership cycling back to Cocoa must reuse Cocoa's tint.");

            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner);
            yield return null;
            Assert.AreEqual(cheddarTint, game.GuidanceBeaconTint, "Ownership cycling back to Cheddar must reuse Cheddar's tint.");

            // 4 owners total (Cocoa, Cheddar, Cocoa, Cheddar); the loop above only drove 3 steps.
            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner);
            yield return null;
            Assert.IsTrue(game.GreatEscapePuzzle.Solved);
            Assert.IsFalse(game.GuidanceBeaconVisible, "Once the sequence is solved there is no more turn to signal.");
        }

        [UnityTest]
        public IEnumerator Beacon_FiresRoleTurnBeaconAppear_OnceOnTheRisingEdge_NotOnEveryOwnerChange()
        {
            // S5.1: the beacon's first appearance gets its own audio cue, distinct from the handoff
            // chime that already fires when SignalRoleHandoff runs (owner changes here without a
            // SignalRoleHandoff call would otherwise double up on audio for one visual moment).
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            Assert.IsTrue(game.GuidanceBeaconVisible, "Great Escape is always-on, so it should already be showing on mission start.");
            Assert.AreEqual(1, CountRoleTurnBeaconAppearCues(game),
                "The always-on beacon's first appearance must fire exactly one RoleTurnBeaconAppear cue.");

            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(1, CountRoleTurnBeaconAppearCues(game), "Staying visible must not spam the appear cue every frame.");

            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner);
            yield return null;
            Assert.IsTrue(game.GuidanceBeaconVisible);
            Assert.AreEqual(1, CountRoleTurnBeaconAppearCues(game),
                "The beacon stayed visible through the owner change - that's a handoff, not a fresh appearance.");
        }

        [UnityTest]
        public IEnumerator Beacon_SharedStepMission_StaysHidden_EvenWhileStalled()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            Assert.IsFalse(game.GuidanceBeaconVisible, "Kitchen hands both dogs a target every beat; no beacon at Tier 0.");

            game.ForceGuidanceStall(45f);
            yield return null;

            Assert.AreEqual(3, game.GuidanceTier);
            Assert.IsNull(game.GuidanceOwningDogIndex);
            Assert.IsFalse(game.GuidanceBeaconVisible, "Sharing a step must stay beacon-free even at the highest stall tier (avoid noise).");
        }

        [UnityTest]
        public IEnumerator Beacon_Replay_ClearsVisibility()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;
            Assert.IsTrue(game.GuidanceBeaconVisible);

            game.Restart();
            yield return null;

            Assert.IsTrue(game.GuidanceBeaconVisible, "A fresh attempt still has a first owner and is still always-on.");
            // The real assertion is that this is the NEW attempt's owner, not a stale reference to a
            // destroyed station from the previous one; Great Escape resets to step 0 (Cocoa) on replay.
            Assert.AreEqual(0, game.GreatEscapePuzzle.Step);
        }

        private static int CountRoleTurnBeaconAppearCues(GameManager game)
        {
            int count = 0;
            foreach (string cue in game.AudioCueRequests)
                if (cue == ArenaFeedbackCatalog.RoleTurnBeaconAppear) count++;
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
