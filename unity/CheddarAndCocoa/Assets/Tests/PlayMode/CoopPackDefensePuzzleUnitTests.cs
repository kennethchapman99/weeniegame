using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for Coyotes at the Fence's Pack Defense: seeded weak-spot layout on the
    /// fence (never in the pool, never at corners), prowl-then-dig with a visible meter, Cocoa-only
    /// pin that freezes the dig, Cheddar-only fill while pinned, the scout that only Cheddar can chase,
    /// the bait lure Cocoa barks him out of, the final push window, and a scripted pacing check.
    /// </summary>
    public sealed class CoopPackDefensePuzzleUnitTests
    {
        private static readonly Rect Yard = new Rect(-60f, -34f, 120f, 68f);
        private static readonly Rect Pool = new Rect(-35f, 3f, 34f, 23f);

        private static CoopPackDefensePuzzle Make(int seed = 3)
        {
            var p = new CoopPackDefensePuzzle();
            p.Configure(Yard, Pool, 4, 3, new[] { 8f, 6.5f, 5f });
            p.Reset(seed);
            return p;
        }

        private static List<CoopPackDefensePuzzle.PackEvent> Drain(CoopPackDefensePuzzle p)
        {
            var list = new List<CoopPackDefensePuzzle.PackEvent>();
            p.DrainEvents(list);
            return list;
        }

        private static bool Has(List<CoopPackDefensePuzzle.PackEvent> events, CoopPackDefensePuzzle.EventKind kind)
        {
            foreach (var e in events) if (e.Kind == kind) return true;
            return false;
        }

        private static void RunUntilDigging(CoopPackDefensePuzzle p)
        {
            for (int i = 0; i < 600 && p.Coyote != CoopPackDefensePuzzle.CoyoteState.Digging; i++)
                p.Advance(0.05f, Vector2.zero, Vector2.zero);
            Assert.AreEqual(CoopPackDefensePuzzle.CoyoteState.Digging, p.Coyote);
        }

        [Test]
        public void Gaps_SitOnTheFence_ApartFromEachOther_OutOfThePool_AwayFromCorners()
        {
            for (int seed = 0; seed < 60; seed++)
            {
                var p = Make(seed);
                Assert.AreEqual(4, p.GapCount);
                for (int i = 0; i < p.GapCount; i++)
                {
                    Vector2 g = p.GapPosition(i);
                    Assert.IsTrue(Yard.Contains(g), $"seed {seed}: gap {i} must be inside the yard.");
                    bool onSide = Mathf.Abs(g.x - Yard.xMin) < 5f || Mathf.Abs(g.x - Yard.xMax) < 5f;
                    bool onBack = Mathf.Abs(g.y - Yard.yMax) < 5f;
                    Assert.IsTrue(onSide || onBack, $"seed {seed}: gap {i} at {g} should be on the fence line.");
                    Assert.IsFalse(Pool.Contains(g), $"seed {seed}: gap {i} must not be in the pool.");
                    if (i > 0)
                        Assert.Greater(Vector2.Distance(g, p.GapPosition(i - 1)), 18f,
                            $"seed {seed}: consecutive weak spots should be a real run apart.");
                }
            }
        }

        [Test]
        public void Gaps_SpanTheWholeFence_EverySeed()
        {
            // The pair must defend both side fences and the back - never a single clustered corner.
            for (int seed = 0; seed < 60; seed++)
            {
                var p = Make(seed);
                float min = float.MaxValue, max = float.MinValue;
                for (int i = 0; i < p.GapCount; i++)
                {
                    min = Mathf.Min(min, p.GapPosition(i).x);
                    max = Mathf.Max(max, p.GapPosition(i).x);
                }
                Assert.GreaterOrEqual(max - min, Yard.width * 0.9f, $"seed {seed}: weak spots should span the yard.");
            }
        }

        [Test]
        public void SameSeed_SameLayout_DifferentSeeds_Vary()
        {
            var a = Make(10);
            var b = Make(10);
            for (int i = 0; i < 4; i++) Assert.AreEqual(a.GapPosition(i), b.GapPosition(i));
            int distinct = 0;
            for (int seed = 0; seed < 10; seed++)
                if (Make(seed).GapPosition(0) != a.GapPosition(0)) distinct++;
            Assert.Greater(distinct, 6, "Seeds should move the weak spots around the fence.");
        }

        [Test]
        public void Coyote_ProwlsToTheWeakSpot_ThenDigs_ThenBreaches()
        {
            var p = Make();
            Assert.AreEqual(CoopPackDefensePuzzle.CoyoteState.Prowling, p.Coyote);
            RunUntilDigging(p);
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.DigStarted));
            p.Advance(4f, Vector2.zero, Vector2.zero);
            Assert.AreEqual(0.5f, p.Dig, 0.01f, "Wave one digs through in 8 seconds.");
            p.Advance(4.1f, Vector2.zero, Vector2.zero);
            Assert.AreEqual(1, p.Breaches);
            Assert.AreEqual(1, p.ActiveGap, "After a breach the coyote moves on to the next weak spot.");
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.Breached));
        }

        [Test]
        public void OnlyCocoaPins_AndOnlyInRange_ThePinFreezesTheDigWithoutUndoingIt()
        {
            var p = Make();
            RunUntilDigging(p);
            p.Advance(2f, Vector2.zero, Vector2.zero);
            float dug = p.Dig;
            Vector2 coyote = p.CoyotePosition;

            Assert.AreEqual(CoopPackDefensePuzzle.BarkResult.WrongDog, p.Bark(DogId.Cheddar, coyote, coyote));
            Assert.AreEqual(CoopPackDefensePuzzle.BarkResult.OutOfRange, p.Bark(DogId.Cocoa, coyote + Vector2.right * 20f, Vector2.zero));
            Assert.AreEqual(CoopPackDefensePuzzle.BarkResult.Pinned, p.Bark(DogId.Cocoa, coyote, Vector2.zero));
            Assert.IsTrue(p.PinHeld);
            p.Advance(1f, Vector2.zero, Vector2.zero);
            Assert.AreEqual(dug, p.Dig, 0.0001f, "Pinned coyotes don't dig.");
            p.Advance(2f, Vector2.zero, Vector2.zero);
            Assert.IsFalse(p.PinHeld, "The pin wears off.");
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.PinExpired));
            RunUntilDigging(p);
            Assert.AreEqual(dug, p.Dig, 0.0001f, "The hole is still as deep as it was.");
        }

        [Test]
        public void CocoaStandingGuard_SendsTheCowardToAnotherWeakSpot_CheddarDoesNot()
        {
            var p = Make();
            Vector2 guardSpot = p.GapPosition(0);
            for (int i = 0; i < 600 && p.Guarded == 0; i++) p.Advance(0.05f, guardSpot, guardSpot);
            Assert.AreEqual(1, p.Guarded);
            Assert.AreNotEqual(0, p.ActiveGap, "Cocoa on guard: the coyote picks a different hole.");
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.SpotGuarded));

            var q = Make();
            Vector2 cheddarOnGuard = q.GapPosition(0);
            RunUntilDiggingWith(q, cheddarOnGuard, Vector2.zero);
            Assert.AreEqual(0, q.ActiveGap, "Cheddar standing there doesn't scare it one bit.");
        }

        [Test]
        public void ACocoaInterceptOnTheProwl_StillPins()
        {
            var p = Make();
            Assert.AreEqual(CoopPackDefensePuzzle.CoyoteState.Prowling, p.Coyote);
            Assert.AreEqual(CoopPackDefensePuzzle.BarkResult.Pinned, p.Bark(DogId.Cocoa, p.CoyotePosition, Vector2.zero));
            Assert.IsTrue(p.PinHeld);
        }

        [Test]
        public void FromWaveTwo_TheCoyoteSometimesFeints()
        {
            int feints = 0;
            for (int seed = 0; seed < 20; seed++)
            {
                var p = Make(seed);
                p.ForcePin();
                p.Fill(DogId.Cheddar, p.ActiveGapPosition);
                for (int i = 0; i < 400 && p.Coyote == CoopPackDefensePuzzle.CoyoteState.Prowling; i++)
                    p.Advance(0.05f, Vector2.zero, Vector2.zero);
                feints += p.Feints;
                Assert.AreEqual(1, p.ActiveGap, $"seed {seed}: a feint still ends at the real weak spot.");
            }
            Assert.Greater(feints, 5, "Feints should be common enough to matter.");
            Assert.Less(feints, 20, "...but not every time.");
        }

        private static void RunUntilDiggingWith(CoopPackDefensePuzzle p, Vector2 cheddar, Vector2 cocoa)
        {
            for (int i = 0; i < 600 && p.Coyote != CoopPackDefensePuzzle.CoyoteState.Digging; i++)
                p.Advance(0.05f, cheddar, cocoa);
            Assert.AreEqual(CoopPackDefensePuzzle.CoyoteState.Digging, p.Coyote);
        }

        [Test]
        public void OnlyCheddarFills_OnlyWhilePinned_AndOnlyAtTheHole()
        {
            var p = Make();
            RunUntilDigging(p);
            Vector2 hole = p.ActiveGapPosition;
            Assert.AreEqual(CoopPackDefensePuzzle.FillResult.NotPinned, p.Fill(DogId.Cheddar, hole));
            p.ForcePin();
            Assert.AreEqual(CoopPackDefensePuzzle.FillResult.WrongDog, p.Fill(DogId.Cocoa, hole));
            Assert.AreEqual(CoopPackDefensePuzzle.FillResult.TooFar, p.Fill(DogId.Cheddar, hole + Vector2.one * 5f));
            Assert.AreEqual(CoopPackDefensePuzzle.FillResult.Filled, p.Fill(DogId.Cheddar, hole));
            Assert.AreEqual(1, p.Repairs);
            Assert.AreEqual(1, p.ActiveGap);
            Assert.AreEqual(0f, p.Dig);
            Assert.AreEqual(CoopPackDefensePuzzle.CoyoteState.Prowling, p.Coyote);
            Assert.AreEqual(6.5f, p.DigSecondsThisWave, 0.001f, "Wave two digs faster.");
        }

        [Test]
        public void ForceReach_PinnedDrivesBack_UnpinnedBreaches()
        {
            var p = Make();
            RunUntilDigging(p);
            p.ForcePin();
            p.ForceReach();
            Assert.AreEqual(0, p.Breaches);
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.DrivenBack));
            p.ForceReach();
            Assert.AreEqual(1, p.Breaches);
        }

        [Test]
        public void Scout_ArrivesInWaveTwo_AtADifferentGap_AndOnlyCheddarChasesIt()
        {
            var p = Make();
            RunUntilDigging(p);
            p.ForcePin();
            p.Fill(DogId.Cheddar, p.ActiveGapPosition);
            Drain(p);
            Assert.IsFalse(p.ScoutActive);
            for (int i = 0; i < 80 && !p.ScoutActive; i++) p.Advance(0.05f, Vector2.zero, Vector2.zero);
            Assert.IsTrue(p.ScoutActive);
            Assert.AreNotEqual(p.ActiveGap, p.ScoutGap);
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.ScoutArrived));

            Vector2 scout = p.ScoutPosition;
            Assert.AreNotEqual(CoopPackDefensePuzzle.BarkResult.ScoutChased, p.Bark(DogId.Cocoa, scout, Vector2.zero),
                "Cocoa will not be pulled off the big coyote by a scrawny scout.");
            Assert.AreEqual(CoopPackDefensePuzzle.BarkResult.ScoutChased, p.Bark(DogId.Cheddar, scout, scout));
            Assert.IsFalse(p.ScoutActive);
            Assert.AreEqual(1, p.ScoutsChased);
        }

        [Test]
        public void Scout_LeftAlone_DigsThroughForABreach()
        {
            var p = Make();
            p.ForceScout();
            Assert.IsTrue(p.ScoutActive);
            // Keep the big coyote pinned so only the scout can do damage.
            for (int i = 0; i < 400 && p.Breaches == 0; i++)
            {
                if (!p.PinHeld) p.ForcePin();
                p.Advance(0.1f, Vector2.zero, Vector2.zero);
            }
            Assert.AreEqual(1, p.Breaches);
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.ScoutBreached));
        }

        [Test]
        public void Bait_LuresCheddarOnly_AndCocoasLeaveItSnapsHimOut()
        {
            var p = Make();
            p.ForceLure();
            Assert.IsTrue(p.LureActive);
            Vector2 near = p.BaitPosition + Vector2.right * 3f;
            p.Advance(0.05f, near, Vector2.zero);
            Assert.IsTrue(p.CheddarLured);
            Assert.Less(p.SpeedMultiplierFor(DogId.Cheddar), 1f);
            Assert.AreEqual(1f, p.SpeedMultiplierFor(DogId.Cocoa), "Cocoa is above bait.");
            Vector2 drift = p.LureDrift(near, 0.5f);
            Assert.Less(Vector2.Distance(near + drift, p.BaitPosition), Vector2.Distance(near, p.BaitPosition),
                "The bait drags Cheddar toward it.");

            Assert.AreEqual(CoopPackDefensePuzzle.BarkResult.LeaveIt, p.Bark(DogId.Cocoa, near + Vector2.up * 2f, near));
            Assert.IsFalse(p.CheddarLured);
            Assert.IsFalse(p.LureActive);
            var events = Drain(p);
            Assert.IsTrue(Has(events, CoopPackDefensePuzzle.EventKind.LeaveIt));
            Assert.IsTrue(Has(events, CoopPackDefensePuzzle.EventKind.LureResolved));
        }

        [Test]
        public void Bait_EatenByCheddar_GivesTheCoyoteAHeadStart()
        {
            var p = Make();
            RunUntilDigging(p);
            p.ForcePin(); // hold the dig steady while Cheddar wanders
            float before = p.Dig;
            p.ForceLure();
            p.Advance(0.05f, p.BaitPosition, Vector2.zero);
            Assert.IsFalse(p.LureActive);
            Assert.Greater(p.Dig, before + 0.3f);
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.BaitEaten));
        }

        [Test]
        public void PinningTheCoyote_CallsTheBaitsBluff()
        {
            var p = Make();
            p.ForceLure();
            p.ForcePin();
            Assert.IsFalse(p.LureActive);
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.LureResolved));
        }

        [Test]
        public void SecondRepair_ThrowsTheBait_ThirdRepair_StartsTheFinalPush()
        {
            var p = Make();
            for (int i = 0; i < 3; i++)
            {
                p.ForcePin();
                Assert.AreEqual(CoopPackDefensePuzzle.FillResult.Filled, p.Fill(DogId.Cheddar, p.ActiveGapPosition));
                if (i == 1) Assert.IsTrue(p.LureActive, "The bait comes before the last repair.");
            }
            Assert.IsTrue(p.ReadyForFinalPush);
            Assert.IsTrue(p.FinalPushActive);
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.FinalPushStarted));
        }

        [Test]
        public void FinalPush_RunsOut_Breaches_ThenComesAgain_UnitedBarkWins()
        {
            var p = Make();
            for (int i = 0; i < 3; i++) { p.ForcePin(); p.Fill(DogId.Cheddar, p.ActiveGapPosition); }
            Drain(p);
            p.Advance(p.FinalPushSeconds + 0.1f, Vector2.zero, Vector2.zero);
            Assert.AreEqual(1, p.Breaches);
            Assert.IsFalse(p.FinalPushActive);
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.FinalPushBroke));
            p.Advance(2.5f, Vector2.zero, Vector2.zero);
            Assert.IsTrue(p.FinalPushActive, "The pack regroups and charges again.");
            Assert.IsTrue(p.UnitedBark());
            Assert.IsTrue(p.Complete);
            Assert.IsTrue(Has(Drain(p), CoopPackDefensePuzzle.EventKind.FinalPushBlocked));
        }

        [Test]
        public void UnitedBark_BeforeTheFenceIsPatched_DoesNothing()
        {
            var p = Make();
            Assert.IsFalse(p.UnitedBark());
            Assert.IsFalse(p.Complete);
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            var p = Make();
            p.ForceScout();
            p.ForceLure();
            p.ForcePin();
            p.Fill(DogId.Cheddar, p.ActiveGapPosition);
            p.ForceBreach();
            p.Reset(4);
            Assert.AreEqual(0, p.Repairs);
            Assert.AreEqual(0, p.Breaches);
            Assert.AreEqual(0, p.Pins);
            Assert.AreEqual(0, p.ActiveGap);
            Assert.IsFalse(p.ScoutActive);
            Assert.IsFalse(p.LureActive);
            Assert.IsFalse(p.FinalPushActive);
            Assert.IsFalse(p.Complete);
            Assert.IsEmpty(Drain(p));
        }

        /// <summary>
        /// Pacing guard: a sensible pair (Cocoa shadows the coyote and pins it on arrival, Cheddar
        /// waits at the hole to fill, breaks off to chase a scout that has dug a while, Cocoa barks him
        /// off the bait, both huddle for the final push) must clear inside the 80s round without
        /// losing. The old coyote crawled to each gap for ~40s.
        /// </summary>
        [Test]
        public void Pacing_ASensiblePair_ClearsInsideTheRound()
        {
            float worst = 0f;
            for (int seed = 0; seed < 30; seed++)
            {
                float t = SimulateSensiblePair(seed, out var p);
                Assert.IsTrue(p.Complete, $"seed {seed}: a sensible pair should clear (repairs {p.Repairs}, breaches {p.Breaches}).");
                Assert.Less(p.Breaches, 3, $"seed {seed}: a sensible pair should not lose.");
                worst = Mathf.Max(worst, t);
            }
            Assert.Less(worst, 72f, "A sensible clear must fit an 80s round with room to spare.");
        }

        public static float SimulateSensiblePair(int seed, out CoopPackDefensePuzzle p)
        {
            const float dt = 1f / 30f;
            const float cheddarSpeed = 6.2f / 50f * 60f;
            const float cocoaSpeed = 5.9f / 50f * 60f;
            p = new CoopPackDefensePuzzle();
            p.Configure(Yard, Pool, 4, 3, new[] { 8f, 6.5f, 5f });
            p.Reset(seed);
            Vector2 cheddar = Vector2.zero, cocoa = Vector2.right;
            float t = 0f;
            float barkCooldown = 0f;
            while (!p.Complete && t < 120f && p.Breaches < 3)
            {
                barkCooldown -= dt;
                Vector2 hole = p.ActiveGapPosition;
                Vector2 cheddarGoal, cocoaGoal;
                if (p.ReadyForFinalPush)
                {
                    Vector2 mid = (cheddar + cocoa) * 0.5f;
                    cheddarGoal = mid; cocoaGoal = mid;
                    if (p.FinalPushActive && Vector2.Distance(cheddar, cocoa) < 2f) p.UnitedBark();
                }
                else
                {
                    bool chaseScout = p.ScoutActive && p.ScoutDig > 0.35f && !p.PinHeld;
                    cheddarGoal = chaseScout ? p.ScoutPosition : hole + (Vector2.zero - hole).normalized * 1f;
                    // (Cheddar can camp at the hole: the coyote isn't scared of him.)
                    Vector2 inward = (Vector2.zero - hole).normalized;
                    bool digging = p.Coyote == CoopPackDefensePuzzle.CoyoteState.Digging;
                    // Hang back outside the guard radius so the coward commits, then pounce.
                    Vector2 lurk = hole + inward * (p.GuardRadius + 1.5f);
                    cocoaGoal = p.CheddarLured ? cheddar : digging ? p.DigSpot(p.ActiveGap) + inward * 2.5f : lurk;
                    if (p.CheddarLured && barkCooldown <= 0f && Vector2.Distance(cocoa, cheddar) <= p.LeaveItRange)
                    { p.Bark(DogId.Cocoa, cocoa, cheddar); barkCooldown = 0.4f; }
                    else if (!p.PinHeld && p.Coyote == CoopPackDefensePuzzle.CoyoteState.Digging && barkCooldown <= 0f &&
                        Vector2.Distance(cocoa, p.CoyotePosition) <= p.PinRange)
                    { p.Bark(DogId.Cocoa, cocoa, cheddar); barkCooldown = 0.4f; }
                    if (chaseScout && Vector2.Distance(cheddar, p.ScoutPosition) <= p.ScoutRange) p.Bark(DogId.Cheddar, cheddar, cheddar);
                    if (p.PinHeld) p.Fill(DogId.Cheddar, cheddar);
                }
                cheddar = Vector2.MoveTowards(cheddar, cheddarGoal, cheddarSpeed * p.SpeedMultiplierFor(DogId.Cheddar) * dt);
                cheddar += p.LureDrift(cheddar, dt);
                cocoa = Vector2.MoveTowards(cocoa, cocoaGoal, cocoaSpeed * dt);
                p.Advance(dt, cheddar, cocoa);
                p.DrainEvents(null);
                t += dt;
            }
            return t;
        }
    }
}
