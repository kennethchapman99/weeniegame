using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// AdventureArenaProgressBridge is the only piece of the Adventure meta-progression loop with no
    /// direct test coverage - AdventureMapController and AdventureProgressService are both unit-tested
    /// via direct instantiation, but nothing proved the bridge actually starts a queued mission and
    /// records its result back to progress when a real Arena round plays out. This constructs the
    /// bridge manually (bypassing the automatic scene-load hook, which would touch the real save file
    /// via AdventureProgressService.LoadDefault()) so the whole flow can be exercised against an
    /// in-memory progress service with no filesystem side effects.
    /// </summary>
    public sealed class AdventureArenaProgressBridgePlayModeTests
    {
        [UnityTest]
        public IEnumerator Bridge_StartsQueuedMissionAndRecordsClearResultToProgress()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            var progress = AdventureProgressService.CreateInMemoryForTests();
            var bridgeGo = new GameObject("TestAdventureArenaProgressBridge");
            var bridge = bridgeGo.AddComponent<AdventureArenaProgressBridge>();
            // Car Ride is one of Front Yard's real catalog missions (AdventureLocationCatalog), unlike
            // an arbitrary variant, so this exercises the same mission/location pairing Adventure mode
            // actually queues.
            bridge.Init(GameManager.MissionVariant.CarRide, AdventureLocationCatalog.FrontYardId, progress);

            // StartWhenArenaReady finds the already-loaded GameManager on its first tick and starts the
            // mission in the same tick (no extra frames needed since GameManager already exists).
            yield return null;
            Assert.AreEqual(GameManager.MissionVariant.CarRide, game.ActiveMissionVariant,
                "The bridge should have started the queued mission once GameManager was found.");

            for (int i = 0; i < 7; i++) game.ForceCarEventSurvived(); // CarRideMissionState.RequiredEvents
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);

            // WatchForMissionEnd polls once per frame; give it a couple of ticks to observe the end
            // screen and record the result.
            yield return null;
            yield return null;

            Assert.IsTrue(progress.TryGetMissionProgress(GameManager.MissionVariant.CarRide, out var record),
                "The bridge should have recorded a progress entry for the cleared mission.");
            Assert.AreEqual(1, record.Attempts);
            Assert.AreEqual(1, record.Clears);
            Assert.Greater(record.BestScore, 0);
            Assert.IsTrue(progress.IsLocationUnlocked(AdventureLocationCatalog.BackyardId),
                "The always-unlocked Backyard should remain unlocked after recording progress.");
        }
    }
}
