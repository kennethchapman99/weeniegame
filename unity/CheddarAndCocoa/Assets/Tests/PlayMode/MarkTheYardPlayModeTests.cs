using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class MarkTheYardPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator MarkTheYard_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(21, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.MarkTheYard)
                {
                    found = true;
                    break;
                }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Mark the Yard should be reachable from mission select.");
            Assert.AreEqual("Mark the Yard", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator MarkTheYard_ClearPath_ClaimEveryZone()
        {
            yield return LoadArena();
            var game = _game;

            // Park the dogs off in a corner so the real-time tick doesn't auto-claim mid-test.
            _cheddar.transform.position = new Vector3(0f, 0f, 0f);
            _cocoa.transform.position = new Vector3(0f, 0f, 0f);

            game.StartMission(GameManager.MissionVariant.MarkTheYard);
            yield return null;

            Assert.AreEqual("mark_the_yard", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Claim"));
            int zones = game.RuntimeSnapshot.ObjectiveGoal;
            Assert.Greater(zones, 0);

            int guard = 0;
            while (game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 40)
            {
                game.ForceClaimZone(DogId.Cheddar);
                yield return null;
            }

            Assert.IsTrue(game.MarkTheYardState.AllClaimed);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Yard Is Ours"));
            Assert.That(game.MvpLabel, Does.Contain("Cheddar"), "Cheddar claimed every zone, so should be MVP.");
        }

        [UnityTest]
        public IEnumerator MarkTheYard_SquirrelReclaim_StealsAClaimedZone()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.MarkTheYard);
            yield return null;

            game.ForceClaimZone(DogId.Cheddar);
            yield return null;
            int afterClaim = game.MarkTheYardState.Claimed;
            Assert.Greater(afterClaim, 0);

            game.ForceSquirrelReclaim();
            yield return null;

            Assert.AreEqual(afterClaim - 1, game.MarkTheYardState.Claimed);
            Assert.AreEqual(1, game.MarkTheYardState.Reclaims);
        }

        [UnityTest]
        public IEnumerator MarkTheYard_Replay_ResetsTerritoryState()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.MarkTheYard);
            yield return null;
            game.ForceClaimZone(DogId.Cheddar);
            game.ForceSquirrelReclaim();
            yield return null;

            Assert.Greater(game.MarkTheYardState.Reclaims, 0);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.MarkTheYard, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.MarkTheYardState.Claimed);
            Assert.AreEqual(0, game.MarkTheYardState.Reclaims);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator MarkTheYard_FirstClear_RecordsSessionBestButNotANewBest()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.MarkTheYard);
            yield return null;

            int guard = 0;
            while (game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 40)
            {
                game.ForceClaimZone(DogId.Cheddar);
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.AreEqual(game.Score, game.BestScoreForMission(GameManager.MissionVariant.MarkTheYard));
            Assert.IsFalse(game.LastRoundWasBest, "First time playing a mission is not a 'new best'.");
            Assert.IsTrue(game.LastRoundFlawless, "Claiming every zone with no squirrel steals is a flawless run.");
            Assert.AreEqual(1, game.SessionFlawlessClears, "A flawless clear should count toward the session tally.");
            Assert.That(game.MvpLabel, Does.Contain("Chaos Crown"));
            Assert.That(game.FlawlessRivalryLabel, Does.Contain("Cheddar"));
        }

        [UnityTest]
        public IEnumerator MarkTheYard_CocoaLeadership_UsesQueenRivalryCopy()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.MarkTheYard);
            yield return null;

            int guard = 0;
            while (_game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 40)
            {
                _game.ForceClaimZone(DogId.Cocoa);
                yield return null;
            }

            Assert.That(_game.MvpLabel, Does.Contain("Cocoa"));
            Assert.That(_game.MvpLabel, Does.Contain("Queen of the Yard"));
            Assert.That(_game.FlawlessRivalryLabel, Does.Contain("Cocoa"));
        }

        private IEnumerator LoadArena()
        {
            _game = null;
            _cheddar = null;
            _cocoa = null;
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            _game = Object.FindFirstObjectByType<GameManager>();
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == DogId.Cheddar) _cheddar = id.GetComponent<DogController>();
                if (id.Id == DogId.Cocoa) _cocoa = id.GetComponent<DogController>();
            }

            Assert.IsNotNull(_game);
            Assert.IsNotNull(_cheddar);
            Assert.IsNotNull(_cocoa);
        }
    }
}
