using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Fast, scene-free guards on the per-mission state machines' boundary behavior, so the
    /// counters can't underflow/overflow or report clear/fail at the wrong thresholds.
    /// </summary>
    public sealed class MissionStateMachineUnitTests
    {
        [Test]
        public void CarryRoundup_PickupDeliverDropAccounting()
        {
            var s = new CarryRoundupMissionState();
            s.Configure(3);
            Assert.AreEqual(3, s.Loose);

            Assert.IsTrue(s.TryPickup());
            Assert.AreEqual(2, s.Loose);
            s.Deliver();
            Assert.AreEqual(1, s.Delivered);

            // A dropped weenie returns to the yard and counts as a fumble.
            s.Drop();
            Assert.AreEqual(3, s.Loose);
            Assert.AreEqual(1, s.Drops);

            // Cannot pick up past what's loose.
            Assert.IsTrue(s.TryPickup());
            Assert.IsTrue(s.TryPickup());
            Assert.IsTrue(s.TryPickup());
            Assert.IsFalse(s.TryPickup(), "No pickup when nothing is loose.");
            Assert.AreEqual(0, s.Loose);

            s.Reset();
            Assert.AreEqual(0, s.Loose);
            Assert.AreEqual(0, s.Delivered);
            Assert.AreEqual(0, s.Drops);
        }

        [Test]
        public void ScentSearch_FindAndWastedThresholds()
        {
            var s = new ScentSearchMissionState();
            s.Reset();
            Assert.IsFalse(s.ReadyToClear(3));
            s.AddFind();
            s.AddFind();
            Assert.IsFalse(s.ReadyToClear(3));
            s.AddFind();
            Assert.IsTrue(s.ReadyToClear(3));

            Assert.IsFalse(s.TooManyWastedDigs(4));
            for (int i = 0; i < 4; i++) s.AddWastedDig();
            Assert.IsTrue(s.TooManyWastedDigs(4));
        }

        [Test]
        public void Territory_ClaimUnclaimClampsAndReportsAllClaimed()
        {
            var s = new TerritoryMissionState();
            s.Configure(2);
            Assert.IsFalse(s.AllClaimed);

            s.Unclaim(); // nothing claimed yet -> no underflow, no reclaim
            Assert.AreEqual(0, s.Claimed);
            Assert.AreEqual(0, s.Reclaims);

            s.Claim();
            s.Claim();
            s.Claim(); // cannot exceed zone count
            Assert.AreEqual(2, s.Claimed);
            Assert.IsTrue(s.AllClaimed);

            s.Unclaim();
            Assert.AreEqual(1, s.Claimed);
            Assert.AreEqual(1, s.Reclaims);
            Assert.IsFalse(s.AllClaimed);
        }

        [Test]
        public void Thunderstorm_SurvivesUpToRequiredClaps()
        {
            var s = new ThunderstormMissionState();
            s.Configure(3);
            Assert.IsFalse(s.ReadyToClear());
            s.SurviveClap();
            s.SurviveClap();
            Assert.IsFalse(s.ReadyToClear());
            s.SurviveClap();
            Assert.IsTrue(s.ReadyToClear());

            s.Reset();
            Assert.AreEqual(0, s.ClapsSurvived);
            Assert.IsFalse(s.ReadyToClear());
        }

        [Test]
        public void ArenaMissionTuning_BalanceFor_NeverSilentlyFallsThroughToBackyardRescue()
        {
            // BalanceFor's switch defaults unrecognized variants to BackyardRescue's tuning. That
            // default exists for BackyardRescue itself; any other variant hitting it is a mission
            // that was never given its own MissionBalance and is silently inheriting the wrong
            // round length / rank thresholds.
            var tuning = ArenaMissionTuning.CreateDefault();
            foreach (GameManager.MissionVariant variant in System.Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                if (variant == GameManager.MissionVariant.BackyardRescue) continue;
                if (variant == GameManager.MissionVariant.OperationPeeBreak) continue; // builds its own balance directly, never calls BalanceFor

                var balance = tuning.BalanceFor(variant);
                Assert.AreNotSame(tuning.BackyardRescue, balance,
                    $"{variant} has no explicit MissionBalance entry in ArenaMissionTuning and is silently inheriting BackyardRescue's tuning.");
            }
        }

        [Test]
        public void Thunderstorm_TracksExposedClapsAsMistakes()
        {
            var s = new ThunderstormMissionState();
            s.Configure(3);
            Assert.AreEqual(0, s.ExposedClaps);

            s.RegisterExposedClap();
            s.SurviveClap();
            s.RegisterExposedClap();
            Assert.AreEqual(2, s.ExposedClaps, "A clap that lands while the pair isn't huddled should count as a mistake.");

            s.Reset();
            Assert.AreEqual(0, s.ExposedClaps);

            s.Configure(3);
            Assert.AreEqual(0, s.ExposedClaps, "Configure should also clear stale mistakes from a previous attempt.");
        }

        /// <summary>
        /// The guaranteed mission-specific score of a clean clear (required per-step events plus any
        /// completion event), deliberately conservative - optional extras like sniff bonuses, onion
        /// dodges, squirrel scares, and united barks are excluded. Counts mirror each controller's
        /// own goal constants; if a mission's pacing or scoring changes, update its line here.
        /// </summary>
        private static int GuaranteedClearPayoff(GameManager.MissionVariant variant) => variant switch
        {
            // Item missions score through ItemScore * ItemGoal (handled by the caller); their
            // controller-specific extras below are only what a clear *guarantees* on top.
            GameManager.MissionVariant.BackyardRescue => 0,
            GameManager.MissionVariant.SnackHeist => 0,
            GameManager.MissionVariant.SockPanic => ScoreEventCatalog.BasketTipped.Points + ScoreEventCatalog.SockDive.Points,
            GameManager.MissionVariant.SquirrelConspiracy => ScoreEventCatalog.Cutoff.Points * 4
                + ScoreEventCatalog.DoubleBarkBlock.Points + ScoreEventCatalog.StashFound.Points
                + ScoreEventCatalog.ConspiracyCracked.Points,
            GameManager.MissionVariant.EagleShadowPanic => ScoreEventCatalog.SafeHide.Points * 2 // RequiredHides
                + ScoreEventCatalog.UnitedFront.Points + 500, // SHADOW PANIC CLEAR
            GameManager.MissionVariant.CoyotesFence => ScoreEventCatalog.FenceHeld.Points * 3 // RequiredRepairs
                + ScoreEventCatalog.DirtFilled.Points * 3 + ScoreEventCatalog.YardDefended.Points,
            GameManager.MissionVariant.WeenieRoundup => (ScoreEventCatalog.WeeniePickup.Points + ScoreEventCatalog.WeenieDelivered.Points) * 5 // RequiredDeliveries
                + ScoreEventCatalog.RoundupComplete.Points,
            GameManager.MissionVariant.ScentSearch => ScoreEventCatalog.BoneFound.Points * 3 // RequiredFinds
                + ScoreEventCatalog.ScentSearchComplete.Points,
            GameManager.MissionVariant.ThunderstormComfort => ScoreEventCatalog.StormWeathered.Points * 5 // ClapGoal
                + ScoreEventCatalog.StormCleared.Points,
            GameManager.MissionVariant.MarkTheYard => ScoreEventCatalog.ZoneClaimed.Points * 4 // conservative zone count
                + ScoreEventCatalog.YardMarked.Points,
            GameManager.MissionVariant.LeashWalk => ScoreEventCatalog.CheckpointReached.Points * 3 // conservative checkpoint count
                + ScoreEventCatalog.WalkComplete.Points,
            GameManager.MissionVariant.CarRide => ScoreEventCatalog.RoadEventCleared.Points * 7 // RideScript.Length
                + ScoreEventCatalog.BraceHeld.Points * 2 * 3 // both dogs braced through 3 brakes
                + ScoreEventCatalog.RideComplete.Points,
            GameManager.MissionVariant.GateCrash => 0,     // penalties only - the clear package is the whole ceiling
            GameManager.MissionVariant.TableStealth => 0,  // penalties only - the clear package is the whole ceiling
            GameManager.MissionVariant.SquirrelSwitcheroo => ScoreEventCatalog.StashFound.Points * 3, // HitsNeeded
            GameManager.MissionVariant.WalkCampaign => ScoreEventCatalog.HumanGettingIt.Points + ScoreEventCatalog.WalkConned.Points,
            GameManager.MissionVariant.BoneRelay => ScoreEventCatalog.BoneFound.Points * 3, // FindsNeeded
            GameManager.MissionVariant.GreatEscape => ScoreEventCatalog.ContraptionStep.Points * 4, // Owners.Length
            GameManager.MissionVariant.ChaosMachine => ScoreEventCatalog.ContraptionStep.Points * 3, // JunctionSpots.Length
            GameManager.MissionVariant.BlanketCatch => ScoreEventCatalog.WeenieDelivered.Points * 5, // CatchesNeeded
            GameManager.MissionVariant.BabyBirdBedlam => (ScoreEventCatalog.ChickNabbed.Points + ScoreEventCatalog.ChickGulped.Points) * 4 // ChicksNeeded
                + ScoreEventCatalog.NestFeastComplete.Points,
            // Full 5-catch combo chain (70+95+120+145+170) plus the dinner-rush start bonus.
            GameManager.MissionVariant.KitchenFoodFrenzy => 600 + 100,
            _ => 0
        };

        [Test]
        public void EveryMission_PawfectRank_IsReachableByAnExcellentHonestRun()
        {
            // An "excellent honest run" = a flawless clear that banks every guaranteed
            // mission-specific event and still has 60% of the round on the clock. If Pawfect sits
            // above that, the top rank is only reachable through degenerate play (united-bark
            // farming) or not at all - which is exactly the bug GateCrash shipped with (Pawfect
            // 1200 against a hard ceiling of 1050).
            var tuning = ArenaMissionTuning.CreateDefault();
            foreach (GameManager.MissionVariant variant in System.Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                if (variant == GameManager.MissionVariant.OperationPeeBreak) continue; // builds its own balance directly, never calls BalanceFor

                var mission = GameManager.BuildMissionDefinition(variant);
                int excellentRun = mission.ItemScore * mission.ItemGoal
                    + GuaranteedClearPayoff(variant)
                    + (mission.RequiresPredator ? tuning.PredatorDefendedScore : 0)
                    + (mission.RequiresTug ? tuning.TugScore : 0)
                    + tuning.ClearScore + tuning.FlawlessBonus
                    + (int)(0.6f * mission.RoundSeconds) * tuning.TimeBonusMultiplier;

                Assert.GreaterOrEqual(excellentRun, mission.PawfectScore,
                    $"{mission.Name}: Pawfect ({mission.PawfectScore}) is above what an excellent honest run can score ({excellentRun}).");
                Assert.GreaterOrEqual(excellentRun, mission.HeroScore,
                    $"{mission.Name}: Hero ({mission.HeroScore}) is above what an excellent honest run can score ({excellentRun}).");
            }
        }
    }
}
