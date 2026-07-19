using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// F4.2: the end-card "Next" button (<see cref="GameManager.ChooseNextMission"/>) and the
    /// session-summary "Continue" button (<see cref="GameManager.ContinueSession"/>) both route
    /// through <c>NextUnfinishedMissionIndex</c> - showcase order for the first five, library order
    /// after that, skipping anything already attempted this session (clear or fail; see
    /// <see cref="SessionResetPlayModeTests.SessionUniqueMissionsCleared_OnlyCountsActualClearsNotAttempts"/>
    /// for the separate cleared-vs-attempted counting contract this task must not disturb).
    /// </summary>
    public sealed class PostClearRoutingPlayModeTests
    {
        private sealed class Rig
        {
            public GameManager Game;
        }

        private static IEnumerator Boot(Rig rig)
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            rig.Game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(rig.Game);
        }

        [UnityTest]
        public IEnumerator FreshSession_NextAfterEachClear_FollowsShowcaseOrderThenLibraryOrder()
        {
            var rig = new Rig();
            yield return Boot(rig);
            GameManager game = rig.Game;

            // Routing only cares whether a mission was attempted, not whether it was cleared (that
            // distinction is SessionUniqueMissionsCleared's job, untouched here) - ForceGameOver is
            // the cheapest universal way to mark a mission "attempted" without per-mission choreography.
            game.StartMission(GameManager.MissionVariant.OperationPeeBreak);
            game.ForceGameOver();
            game.ChooseNextMission();
            Assert.AreEqual(GameManager.MissionVariant.KitchenFoodFrenzy, game.ActiveMissionVariant);

            game.ForceGameOver();
            game.ChooseNextMission();
            Assert.AreEqual(GameManager.MissionVariant.CarRide, game.ActiveMissionVariant);

            // The 3rd unique completion opens the session-summary milestone screen instead of
            // routing directly - existing, intentional behavior (SessionSummaryReady). "Next"
            // becomes the summary's own Continue button from here, same as a real player would see.
            game.ForceGameOver();
            Assert.IsTrue(game.SessionSummaryReady);
            game.ShowSessionSummary();
            game.ContinueSession();
            Assert.AreEqual(GameManager.MissionVariant.BabyBirdBedlam, game.ActiveMissionVariant);

            game.ForceGameOver();
            game.ChooseNextMission();
            Assert.AreEqual(GameManager.MissionVariant.GateCrash, game.ActiveMissionVariant,
                "Fifth and last showcase mission.");

            game.ForceGameOver();
            game.ChooseNextMission();
            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, game.ActiveMissionVariant,
                "All five showcase missions attempted - Next should now hand out the first library mission.");
        }

        [UnityTest]
        public IEnumerator ManuallyPickingALaterShowcaseMissionFirst_ContinuesInArrayOrderNotBackToShowcase()
        {
            var rig = new Rig();
            yield return Boot(rig);
            GameManager game = rig.Game;

            // Considered and deliberately rejected: making Next "exhaust the rest of the showcase
            // five" before library order, regardless of where the player currently sits, sounds
            // appealing from this task's goal text alone. Building and testing it against the FULL
            // suite (not just new tests written to match the new behavior) surfaced that 4 existing
            // tests already encode today's simpler contract - continue circularly from wherever the
            // player currently is - as intentional (e.g. ArenaGameLoopPlayModeTests jumps straight to
            // Snack Heist, a library mission, and asserts Next lands on Sock Panic, the next library
            // mission, not a jump back to an unattempted showcase pick). That, plus this task's own
            // explicit "reuse NextUnfinishedMissionIndex semantics... preserve it" instruction, means
            // the showcase-exhaustion idea is out of scope here, not a bug - this test pins the real,
            // shipped behavior instead of the rejected alternative.
            game.StartMission(GameManager.MissionVariant.GateCrash);
            game.ForceGameOver();
            game.ChooseNextMission();

            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, game.ActiveMissionVariant,
                "Continues to the next MissionOrder entry after Gate Crash, same as any other mission.");
        }

        [UnityTest]
        public IEnumerator ReplayingAnAlreadyAttemptedMission_SkipsOtherAlreadyAttemptedOnesOnNext()
        {
            var rig = new Rig();
            yield return Boot(rig);
            GameManager game = rig.Game;

            game.StartMission(GameManager.MissionVariant.OperationPeeBreak);
            game.ForceGameOver();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            game.ForceGameOver();
            game.StartMission(GameManager.MissionVariant.CarRide);
            game.ForceGameOver();

            // Replay the first mission (e.g. going for a better score) rather than following Next.
            // Three unique missions are already attempted at this point, so ChooseNextMission()
            // itself would divert to the session-summary milestone screen (existing, intentional -
            // see the first test above); ContinueSession() is the summary's own equivalent button
            // and reaches the exact same NextUnfinishedMissionIndex routing without that detour.
            game.StartMission(GameManager.MissionVariant.OperationPeeBreak);
            game.ForceGameOver();
            game.ContinueSession();

            Assert.AreEqual(GameManager.MissionVariant.BabyBirdBedlam, game.ActiveMissionVariant,
                "Kitchen and Car Ride were already attempted and must be skipped, not re-offered.");
        }

        [UnityTest]
        public IEnumerator AllMissionsAttempted_NextStillWrapsCleanlyInsteadOfGettingStuck()
        {
            var rig = new Rig();
            yield return Boot(rig);
            GameManager game = rig.Game;

            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                game.StartMission(game.MissionVariantAt(i));
                game.ForceGameOver();
            }

            Assert.AreEqual(game.MissionSelectOptionCount, game.SessionUniqueMissionsCompleted);
            Assert.IsTrue(game.SessionSummaryReady);
            game.ShowSessionSummary();
            game.ChooseNextMission();

            Assert.AreEqual(GameManager.MissionVariant.OperationPeeBreak, game.ActiveMissionVariant,
                "Once literally everything has been attempted, Next should wrap to the start clean, " +
                "not throw or stall.");
        }
    }
}
