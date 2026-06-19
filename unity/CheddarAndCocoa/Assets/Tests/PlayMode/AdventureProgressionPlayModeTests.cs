using System;
using System.IO;
using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class AdventureProgressionPlayModeTests
    {
        [Test]
        public void FreshProgress_StartsWithOnlyBackyardUnlocked()
        {
            var progress = AdventureProgressService.CreateInMemoryForTests();

            Assert.AreEqual(0, progress.Snapshot.TotalStars);
            Assert.IsTrue(progress.IsLocationUnlocked(AdventureLocationCatalog.BackyardId));
            Assert.IsFalse(progress.IsLocationUnlocked(AdventureLocationCatalog.FrontYardId));
            Assert.IsFalse(progress.IsLocationUnlocked(AdventureLocationCatalog.HouseInteriorId));
            Assert.IsFalse(progress.IsLocationUnlocked(AdventureLocationCatalog.NeighborhoodParkId));
            Assert.Greater(progress.GetUnlockedMissions().Count, 0);
        }

        [Test]
        public void RecordingMissionResult_StoresBestAndUnlocksByStars()
        {
            var progress = AdventureProgressService.CreateInMemoryForTests();

            progress.RecordMissionResult(GameManager.MissionVariant.BackyardRescue, 900, 2, "Backyard Heroes", true);
            progress.RecordMissionResult(GameManager.MissionVariant.SquirrelConspiracy, 1200, 3, "Conspiracy Cracked", true);
            Assert.AreEqual(5, progress.Snapshot.TotalStars);
            Assert.IsFalse(progress.IsLocationUnlocked(AdventureLocationCatalog.FrontYardId));

            progress.RecordMissionResult(GameManager.MissionVariant.WeenieRoundup, 800, 1, "Snack Survivors", true);
            Assert.AreEqual(6, progress.Snapshot.TotalStars);
            Assert.IsTrue(progress.IsLocationUnlocked(AdventureLocationCatalog.FrontYardId));
            Assert.IsFalse(progress.IsLocationUnlocked(AdventureLocationCatalog.HouseInteriorId));

            progress.RecordMissionResult(GameManager.MissionVariant.ScentSearch, 1300, 3, "Master Sniffers", true);
            Assert.AreEqual(9, progress.Snapshot.TotalStars);
            Assert.IsTrue(progress.IsLocationUnlocked(AdventureLocationCatalog.HouseInteriorId));
        }

        [Test]
        public void WeakerReplay_DoesNotLowerBestResult()
        {
            var progress = AdventureProgressService.CreateInMemoryForTests();

            progress.RecordMissionResult(GameManager.MissionVariant.BackyardRescue, 1500, 3, "Pawfect Yard", true);
            progress.RecordMissionResult(GameManager.MissionVariant.BackyardRescue, 400, 1, "Needs More Bark", true);

            Assert.IsTrue(progress.TryGetMissionProgress(GameManager.MissionVariant.BackyardRescue, out var record));
            Assert.AreEqual(2, record.Attempts);
            Assert.AreEqual(2, record.Clears);
            Assert.AreEqual(1500, record.BestScore);
            Assert.AreEqual(3, record.BestStars);
            Assert.AreEqual("Pawfect Yard", record.BestRank);
        }

        [Test]
        public void SaveLoad_RoundTripsMissionProgressAndUnlocks()
        {
            string path = TempSavePath();
            try
            {
                var progress = AdventureProgressService.Load(path);
                progress.RecordMissionResult(GameManager.MissionVariant.BackyardRescue, 1500, 3, "Pawfect Yard", true);
                progress.RecordMissionResult(GameManager.MissionVariant.SquirrelConspiracy, 1200, 3, "Conspiracy Cracked", true);
                Assert.IsTrue(progress.Save());

                var loaded = AdventureProgressService.Load(path);
                Assert.AreEqual(6, loaded.Snapshot.TotalStars);
                Assert.IsTrue(loaded.IsLocationUnlocked(AdventureLocationCatalog.FrontYardId));
                Assert.IsTrue(loaded.TryGetMissionProgress(GameManager.MissionVariant.BackyardRescue, out var record));
                Assert.AreEqual(1500, record.BestScore);
                Assert.AreEqual(3, record.BestStars);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void CorruptSave_FallsBackToFreshProgress()
        {
            string path = TempSavePath();
            try
            {
                File.WriteAllText(path, "this is not json");

                var progress = AdventureProgressService.Load(path);

                Assert.AreEqual(0, progress.Snapshot.TotalStars);
                Assert.IsTrue(progress.IsLocationUnlocked(AdventureLocationCatalog.BackyardId));
                Assert.IsFalse(progress.IsLocationUnlocked(AdventureLocationCatalog.FrontYardId));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void EighteenStars_UnlocksNeighborhoodPark()
        {
            var progress = AdventureProgressService.CreateInMemoryForTests();
            var missions = new[]
            {
                GameManager.MissionVariant.BackyardRescue,
                GameManager.MissionVariant.SquirrelConspiracy,
                GameManager.MissionVariant.WeenieRoundup,
                GameManager.MissionVariant.ScentSearch,
                GameManager.MissionVariant.ThunderstormComfort,
                GameManager.MissionVariant.MarkTheYard
            };

            for (int i = 0; i < missions.Length; i++)
            {
                progress.RecordMissionResult(missions[i], 1000 + i, 3, "Pawfect Yard", true);
            }

            Assert.AreEqual(18, progress.Snapshot.TotalStars);
            Assert.IsTrue(progress.IsLocationUnlocked(AdventureLocationCatalog.NeighborhoodParkId));
        }

        private static string TempSavePath()
        {
            return Path.Combine(Path.GetTempPath(), $"cheddar-cocoa-progress-{Guid.NewGuid():N}.json");
        }
    }
}
