using NUnit.Framework;
using UnityEngine;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class AdventureMapControllerPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            AdventureMissionLaunch.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            AdventureMissionLaunch.Clear();
        }

        [Test]
        public void FreshMap_SelectsUnlockedBackyardAndQueuesMission()
        {
            var controller = new AdventureMapController(AdventureProgressService.CreateInMemoryForTests());

            Assert.AreEqual(AdventureLocationCatalog.BackyardId, controller.SelectedLocation.Id);
            Assert.IsTrue(controller.SelectedLocationUnlocked);
            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, controller.SelectedMission.Value);
            Assert.IsTrue(controller.CanLaunchSelectedMission);

            Assert.IsTrue(controller.TryQueueSelectedMissionLaunch());
            Assert.IsTrue(AdventureMissionLaunch.TryPeek(out var mission, out var locationId));
            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, mission);
            Assert.AreEqual(AdventureLocationCatalog.BackyardId, locationId);
        }

        [Test]
        public void LockedLocation_DoesNotQueueMissionLaunch()
        {
            var controller = new AdventureMapController(AdventureProgressService.CreateInMemoryForTests());

            controller.SelectLocation(1);

            Assert.AreEqual(AdventureLocationCatalog.FrontYardId, controller.SelectedLocation.Id);
            Assert.IsFalse(controller.SelectedLocationUnlocked);
            Assert.IsFalse(controller.CanLaunchSelectedMission);
            Assert.IsFalse(controller.TryQueueSelectedMissionLaunch());
            Assert.IsFalse(AdventureMissionLaunch.HasPendingMission);
        }

        [Test]
        public void UnlockedLocation_QueuesSelectedMission()
        {
            var progress = AdventureProgressService.CreateInMemoryForTests();
            progress.RecordMissionResult(GameManager.MissionVariant.BackyardRescue, 1500, 3, "Pawfect Yard", true);
            progress.RecordMissionResult(GameManager.MissionVariant.SquirrelConspiracy, 1200, 3, "Conspiracy Cracked", true);
            var controller = new AdventureMapController(progress);

            controller.SelectLocation(1);
            controller.SelectMission(1);

            Assert.AreEqual(AdventureLocationCatalog.FrontYardId, controller.SelectedLocation.Id);
            Assert.IsTrue(controller.SelectedLocationUnlocked);
            Assert.AreEqual(GameManager.MissionVariant.LeashWalk, controller.SelectedMission.Value);
            Assert.IsTrue(controller.TryQueueSelectedMissionLaunch());
            Assert.IsTrue(AdventureMissionLaunch.TryConsume(out var mission, out var locationId));
            Assert.AreEqual(GameManager.MissionVariant.LeashWalk, mission);
            Assert.AreEqual(AdventureLocationCatalog.FrontYardId, locationId);
            Assert.IsFalse(AdventureMissionLaunch.HasPendingMission);
        }

        [Test]
        public void MissionRows_ShowBestProgressFromSave()
        {
            var progress = AdventureProgressService.CreateInMemoryForTests();
            progress.RecordMissionResult(GameManager.MissionVariant.BackyardRescue, 1500, 3, "Pawfect Yard", true);
            var controller = new AdventureMapController(progress);

            string row = controller.BuildMissionRowLabel(0);

            Assert.That(row, Does.Contain("Backyard Rescue"));
            Assert.That(row, Does.Contain("3★"));
            Assert.That(row, Does.Contain("1500"));
            Assert.That(row, Does.Contain("Pawfect Yard"));
        }

        [Test]
        public void LocationSelection_WrapsAndResetsMissionSelection()
        {
            var controller = new AdventureMapController(AdventureProgressService.CreateInMemoryForTests());

            controller.SelectMission(3);
            controller.SelectPreviousLocation();

            Assert.AreEqual(AdventureLocationCatalog.NeighborhoodParkId, controller.SelectedLocation.Id);
            Assert.AreEqual(0, controller.SelectedMissionIndex);
        }

        [Test]
        public void AdventureMapPreviews_UseRealSprites_NotPrimitiveSquares()
        {
            var controller = new AdventureMapController(AdventureProgressService.CreateInMemoryForTests());

            for (int i = 0; i < controller.Locations.Count; i++)
            {
                var location = controller.Locations[i];
                AssertRealPreviewSprite(
                    AdventureMapHud.LoadLocationPreviewSprite(location),
                    $"{location.DisplayName} location card");

                if (location.Missions == null) continue;
                for (int m = 0; m < location.Missions.Length; m++)
                {
                    var mission = location.Missions[m];
                    AssertRealPreviewSprite(
                        AdventureMapHud.LoadMissionPreviewSprite(mission),
                        $"{AdventureMapController.DisplayNameForMission(mission)} mission card");
                }
            }
        }

        private static void AssertRealPreviewSprite(Sprite sprite, string label)
        {
            Assert.IsNotNull(sprite, $"{label} should load an authored ArenaFinal preview sprite.");
            Assert.AreNotSame(SpriteShapeCache.WhiteSquare, sprite, $"{label} must not use SpriteShapeCache.WhiteSquare.");
            Assert.That(sprite.name, Does.Not.Contain("RuntimeWhiteSquare"), $"{label} must not be the runtime primitive square.");
            Assert.IsNotNull(sprite.texture, $"{label} should be backed by an imported texture.");
            Assert.That(sprite.texture.name, Does.Not.Contain("RuntimeWhiteSquare"), $"{label} texture must not be the runtime primitive square.");
        }
    }
}
