using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for Eagle Shadow Panic's Shadow Run: seeded toy layout, the sweeping
    /// shadow column and its edge banking, spotting only in the open, live grab + partner wiggle/pull
    /// rescue vs. carry-off, toy pickup/stash and run progression, the two-dog rope toy, and the
    /// asymmetric bark decoy (Cocoa holds the eagle longer, barking in the open backfires).
    /// </summary>
    public sealed class CoopShadowRunPuzzleUnitTests
    {
        private static readonly Rect Yard = new Rect(-60f, -34f, 120f, 68f);
        private static readonly Vector2[] Covers =
        {
            new Vector2(-42f, -21.76f), new Vector2(42f, -21.76f), new Vector2(0f, 23.12f),
            new Vector2(-21.6f, 3.4f), new Vector2(21.6f, 3.4f)
        };

        private static CoopShadowRunPuzzle Make(int seed = 7, float edgePause = 0f)
        {
            var p = new CoopShadowRunPuzzle();
            p.Configure(Yard, -58.5f, 58.5f, Covers, 3f,
                new[] { 2, 3 }, new[] { false, true }, new[] { 11f, 13.5f });
            p.ConfigureFeel(edgePauseSeconds: edgePause);
            p.Reset(seed);
            return p;
        }

        private static List<CoopShadowRunPuzzle.RunEvent> Drain(CoopShadowRunPuzzle p)
        {
            var list = new List<CoopShadowRunPuzzle.RunEvent>();
            p.DrainEvents(list);
            return list;
        }

        private static bool Has(List<CoopShadowRunPuzzle.RunEvent> events, CoopShadowRunPuzzle.RunEventKind kind)
        {
            foreach (var e in events) if (e.Kind == kind) return true;
            return false;
        }

        // A spot on open lawn far from every cover, at the given x.
        private static Vector2 Open(float x) => new Vector2(x, 0f);

        [Test]
        public void SameSeed_SameToyLayout_DifferentSeed_DifferentLayout()
        {
            var a = Make(seed: 42);
            var b = Make(seed: 42);
            var c = Make(seed: 43);
            Assert.AreEqual(a.ToyCount, b.ToyCount);
            bool anyDifferent = false;
            for (int i = 0; i < a.ToyCount; i++)
            {
                Assert.AreEqual(a.ToyPosition(i), b.ToyPosition(i), "Same seed must replay the same layout.");
                if (a.ToyPosition(i) != c.ToyPosition(i)) anyDifferent = true;
            }
            Assert.IsTrue(anyDifferent, "A different seed should scatter the toys differently.");
        }

        [Test]
        public void Toys_SpawnOnTheLawn_AFairDashFromCover()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var p = Make(seed);
                for (int run = 0; run < 2; run++)
                {
                    for (int i = 0; i < p.ToyCount; i++)
                    {
                        Vector2 toy = p.ToyPosition(i);
                        Assert.IsFalse(p.IsInCover(toy), $"seed {seed}: a toy must never start already in cover.");
                        Assert.Greater(p.NearestCoverDistance(toy), p.CoverRadius + 3f,
                            $"seed {seed}: toys should sit a real dash out from cover.");
                        Assert.IsTrue(Yard.Contains(toy), $"seed {seed}: toys must stay inside the yard.");
                    }
                    p.ForceClearRun();
                }
            }
        }

        [Test]
        public void FirstRun_HasNoRope_SecondRun_HasExactlyOneRope()
        {
            var p = Make();
            Assert.AreEqual(2, p.ToyCount);
            for (int i = 0; i < p.ToyCount; i++) Assert.IsFalse(p.ToyIsRope(i));
            p.ForceClearRun();
            Assert.AreEqual(1, p.RunIndex);
            int ropes = 0;
            for (int i = 0; i < p.ToyCount; i++) if (p.ToyIsRope(i)) ropes++;
            Assert.AreEqual(1, ropes);
            Assert.AreEqual(3, p.ToyCount);
        }

        [Test]
        public void Shadow_BanksBeforeTheFirstPass_ThenSweepsAndTurnsAtTheEdge()
        {
            var p = Make(edgePause: 1f);
            Vector2 hide = Covers[0];
            Assert.IsTrue(p.ShadowBanking);
            p.Advance(0.5f, hide, hide);
            Assert.AreEqual(-58.5f, p.ShadowX, 0.001f, "The shadow holds off-yard while the eagle banks in.");
            p.Advance(0.6f, hide, hide);
            Assert.IsFalse(p.ShadowBanking);
            float before = p.ShadowX;
            p.Advance(1f, hide, hide);
            Assert.Greater(p.ShadowX, before);

            for (int i = 0; i < 40 && p.ShadowDirection > 0; i++) p.Advance(0.5f, hide, hide);
            Assert.AreEqual(-1, p.ShadowDirection, "The shadow turns around at the far edge.");
            Assert.AreEqual(1, p.CleanPasses, "A pass with both dogs in cover is clean.");
            Assert.IsTrue(Has(Drain(p), CoopShadowRunPuzzle.RunEventKind.CleanPass));
        }

        [Test]
        public void DogInTheOpen_UnderTheShadow_IsGrabbed()
        {
            var p = Make();
            Vector2 hide = Covers[0];
            Vector2 open = Open(p.ShadowX + 0.5f);
            p.Advance(0.25f, open, hide);
            Assert.IsTrue(p.GrabActive);
            Assert.AreEqual(DogId.Cheddar, p.GrabbedDog);
            Assert.AreEqual(1, p.TotalGrabs);
            Assert.IsTrue(Has(Drain(p), CoopShadowRunPuzzle.RunEventKind.DogGrabbed));
        }

        [Test]
        public void DogInCover_UnderTheShadow_IsSafe()
        {
            var p = Make();
            Vector2 hide = Covers[0];
            // Walk the whole sweep across the cover; neither dog ever leaves it.
            for (int i = 0; i < 60; i++) p.Advance(0.25f, hide, hide);
            Assert.IsFalse(p.GrabActive);
            Assert.AreEqual(0, p.TotalGrabs);
        }

        [Test]
        public void BriefCrossing_UnderSpotDwell_IsNotAGrab()
        {
            var p = Make();
            p.ConfigureFeel(spotDwellSeconds: 0.3f);
            Vector2 hide = Covers[0];
            Vector2 open = Open(p.ShadowX);
            p.Advance(0.1f, open, hide);
            p.Advance(0.1f, hide, hide);
            p.Advance(0.1f, open, hide);
            Assert.IsFalse(p.GrabActive, "Darting through the shadow edge faster than the dwell should be forgiven.");
        }

        [Test]
        public void Grab_WiggleThenPullInRange_FreesTheDog()
        {
            var p = Make();
            Vector2 open = Open(p.ShadowX);
            p.Advance(0.25f, open, Covers[0]);
            Assert.IsTrue(p.GrabActive);
            Drain(p);

            Assert.AreEqual(CoopShadowRunPuzzle.PullResult.WrongDog, p.Pull(DogId.Cheddar, open, open));
            Assert.AreEqual(CoopShadowRunPuzzle.PullResult.TooFar, p.Pull(DogId.Cocoa, Covers[0], open));
            Assert.AreEqual(CoopShadowRunPuzzle.PullResult.Mistimed, p.Pull(DogId.Cocoa, open, open));
            Assert.IsFalse(p.Wiggle(DogId.Cocoa), "Only the grabbed dog can wiggle.");

            Assert.IsTrue(p.Wiggle(DogId.Cheddar));
            Assert.AreEqual(CoopShadowRunPuzzle.PullResult.Landed, p.Pull(DogId.Cocoa, open, open));
            Assert.IsTrue(p.Wiggle(DogId.Cheddar));
            Assert.AreEqual(CoopShadowRunPuzzle.PullResult.Freed, p.Pull(DogId.Cocoa, open, open));
            Assert.IsFalse(p.GrabActive);
            Assert.AreEqual(0, p.TotalCarryOffs);
            Assert.IsTrue(Has(Drain(p), CoopShadowRunPuzzle.RunEventKind.GrabRescued));
        }

        [Test]
        public void Grab_NobodyComes_CarriesTheDogOffIntoCover_AsAStrike()
        {
            var p = Make();
            p.ConfigureFeel(carryOffSeconds: 2f);
            Vector2 open = Open(p.ShadowX);
            p.Advance(0.25f, open, Covers[1]);
            Assert.IsTrue(p.GrabActive);
            float shadowWhileHeld = p.ShadowX;
            p.Advance(1f, open, Covers[1]);
            Assert.AreEqual(shadowWhileHeld, p.ShadowX, 0.001f, "The sweep holds while the eagle has a dog.");
            Assert.Greater(p.CarryOffNormalized, 0.4f);
            p.Advance(1.1f, open, Covers[1]);
            Assert.IsFalse(p.GrabActive);
            Assert.AreEqual(1, p.TotalCarryOffs);
            var events = Drain(p);
            Assert.IsTrue(Has(events, CoopShadowRunPuzzle.RunEventKind.CarriedOff));
            foreach (var e in events)
                if (e.Kind == CoopShadowRunPuzzle.RunEventKind.CarriedOff)
                    Assert.IsTrue(p.IsInCover(e.Position), "The eagle drops its catch back in cover.");
        }

        [Test]
        public void SmallToy_PickedUp_ThenStashedInCover()
        {
            var p = Make();
            p.ConfigureFeel(edgePauseSeconds: 100f); // keep the eagle banking so nobody is spotted
            p.Reset(7);
            Vector2 toy = p.ToyPosition(0);
            p.Advance(0.1f, toy, Covers[2]);
            Assert.AreEqual(CoopShadowRunPuzzle.ToyState.Carried, p.ToyStateAt(0));
            Assert.AreEqual(0, p.CarriedToy(DogId.Cheddar));
            Assert.AreEqual(1f, p.SpeedMultiplierFor(DogId.Cheddar), 0.0001f, "Cheddar hauls at full speed.");

            p.Advance(0.1f, Covers[0], Covers[2]);
            Assert.AreEqual(CoopShadowRunPuzzle.ToyState.Stashed, p.ToyStateAt(0));
            Assert.AreEqual(1, p.ToysStashedThisRun);
            Assert.IsTrue(Has(Drain(p), CoopShadowRunPuzzle.RunEventKind.ToyStashed));
        }

        [Test]
        public void CocoaCarriesSlower_ThanCheddar()
        {
            var p = Make();
            p.ConfigureFeel(edgePauseSeconds: 100f);
            p.Reset(7);
            p.Advance(0.1f, Covers[2], p.ToyPosition(0));
            Assert.AreEqual(0, p.CarriedToy(DogId.Cocoa));
            Assert.Less(p.SpeedMultiplierFor(DogId.Cocoa), 1f);
        }

        [Test]
        public void Grab_DropsTheToyBackOnTheLawn()
        {
            var p = Make();
            p.ConfigureFeel(spotDwellSeconds: 0f);
            p.Reset(7);
            Vector2 toy = p.ToyPosition(0);
            // Pick the toy up while the eagle is still banking in.
            p.Advance(0.05f, toy, Covers[2]);
            Assert.AreEqual(CoopShadowRunPuzzle.ToyState.Carried, p.ToyStateAt(0));
            p.ForceGrab(DogId.Cheddar, toy);
            Assert.AreEqual(CoopShadowRunPuzzle.ToyState.OnLawn, p.ToyStateAt(0));
            Assert.IsTrue(Has(Drain(p), CoopShadowRunPuzzle.RunEventKind.ToyDropped));
        }

        [Test]
        public void StashingEveryToy_ClearsTheRun_AndSpawnsTheNext()
        {
            var p = Make();
            p.ConfigureFeel(edgePauseSeconds: 100f);
            p.Reset(11);
            for (int i = 0; i < 2; i++)
            {
                p.Advance(0.05f, p.ToyPosition(i), Covers[2]);
                p.Advance(0.05f, Covers[2], Covers[2]);
            }
            Assert.AreEqual(1, p.RunIndex);
            Assert.AreEqual(2, p.TotalToysStashed);
            Assert.IsTrue(Has(Drain(p), CoopShadowRunPuzzle.RunEventKind.RunCleared));
            Assert.AreEqual(3, p.ToyCount);
            for (int i = 0; i < p.ToyCount; i++)
                Assert.AreEqual(CoopShadowRunPuzzle.ToyState.OnLawn, p.ToyStateAt(i));
        }

        [Test]
        public void Rope_NeedsBothDogs_OneDogAloneOnlyCallsForThePartner()
        {
            var p = Make();
            p.ConfigureFeel(edgePauseSeconds: 100f);
            p.Reset(5);
            p.ForceClearRun();
            int rope = -1;
            for (int i = 0; i < p.ToyCount; i++) if (p.ToyIsRope(i)) rope = i;
            Vector2 at = p.ToyPosition(rope);

            p.Advance(0.05f, at, Covers[2]);
            Assert.AreEqual(CoopShadowRunPuzzle.ToyState.OnLawn, p.ToyStateAt(rope));
            Assert.IsTrue(p.HoldsRope(DogId.Cheddar));
            var events = Drain(p);
            Assert.IsTrue(Has(events, CoopShadowRunPuzzle.RunEventKind.RopeNeedsPartner));
            p.Advance(0.05f, at, Covers[2]);
            Assert.IsFalse(Has(Drain(p), CoopShadowRunPuzzle.RunEventKind.RopeNeedsPartner),
                "The partner call fires once, not every frame.");

            p.Advance(0.05f, at, at + Vector2.right);
            Assert.AreEqual(CoopShadowRunPuzzle.ToyState.Carried, p.ToyStateAt(rope));
            Assert.IsTrue(p.RopeLifted);
            Assert.IsTrue(Has(Drain(p), CoopShadowRunPuzzle.RunEventKind.RopeLifted));
            Assert.Less(p.SpeedMultiplierFor(DogId.Cheddar), 1f, "Hauling the rope slows both dogs.");
            Assert.Less(p.SpeedMultiplierFor(DogId.Cocoa), 1f);
        }

        [Test]
        public void Rope_DriftingApart_DropsIt_CarryingItTogether_StashesIt()
        {
            var p = Make();
            p.ConfigureFeel(edgePauseSeconds: 100f);
            p.Reset(5);
            p.ForceClearRun();
            int rope = -1;
            for (int i = 0; i < p.ToyCount; i++) if (p.ToyIsRope(i)) rope = i;
            Vector2 at = p.ToyPosition(rope);
            p.Advance(0.05f, at, at + Vector2.right);
            Assert.IsTrue(p.RopeLifted);
            Drain(p);

            p.Advance(0.05f, at, at + Vector2.right * 10f);
            Assert.IsFalse(p.RopeLifted);
            Assert.AreEqual(CoopShadowRunPuzzle.ToyState.OnLawn, p.ToyStateAt(rope));
            Assert.IsTrue(Has(Drain(p), CoopShadowRunPuzzle.RunEventKind.ToyDropped));

            Vector2 dropped = p.ToyPosition(rope);
            p.Advance(0.05f, dropped, dropped + Vector2.left);
            Assert.IsTrue(p.RopeLifted);
            Vector2 cover = Covers[2];
            p.Advance(0.05f, cover + Vector2.left, cover + Vector2.right);
            Assert.AreEqual(CoopShadowRunPuzzle.ToyState.Stashed, p.ToyStateAt(rope));
        }

        [Test]
        public void Rope_CannotBeLiftedByADogAlreadyCarryingAToy()
        {
            var p = Make();
            p.ConfigureFeel(edgePauseSeconds: 100f);
            p.Reset(5);
            p.ForceClearRun();
            int rope = -1, small = -1;
            for (int i = 0; i < p.ToyCount; i++) { if (p.ToyIsRope(i)) rope = i; else if (small < 0) small = i; }
            p.Advance(0.05f, p.ToyPosition(small), Covers[2]);
            Assert.AreEqual(small, p.CarriedToy(DogId.Cheddar));
            Vector2 at = p.ToyPosition(rope);
            p.Advance(0.05f, at, at + Vector2.right);
            Assert.IsFalse(p.RopeLifted, "Mouth's full - Cheddar has to stash his toy first.");
        }

        [Test]
        public void BarkFromCover_IsADecoy_CocoaHoldsTheEagleLongerThanCheddar()
        {
            var cocoaRun = Make();
            Vector2 hide = Covers[0];
            Assert.AreEqual(CoopShadowRunPuzzle.BarkResult.Decoy, cocoaRun.Bark(DogId.Cocoa, hide));
            float start = cocoaRun.ShadowX;
            cocoaRun.Advance(2f, hide, hide);
            Assert.AreEqual(start, cocoaRun.ShadowX, 0.001f, "Cocoa's bark should still be holding the eagle at 2s.");

            var cheddarRun = Make();
            Assert.AreEqual(CoopShadowRunPuzzle.BarkResult.Decoy, cheddarRun.Bark(DogId.Cheddar, hide));
            float cheddarStart = cheddarRun.ShadowX;
            cheddarRun.Advance(2f, hide, hide);
            Assert.Greater(cheddarRun.ShadowX, cheddarStart, "Cheddar's bark is shorter; the shadow moves again by 2s.");
        }

        [Test]
        public void DecoyBark_HasACooldown()
        {
            var p = Make();
            Vector2 hide = Covers[0];
            Assert.AreEqual(CoopShadowRunPuzzle.BarkResult.Decoy, p.Bark(DogId.Cocoa, hide));
            Assert.AreEqual(CoopShadowRunPuzzle.BarkResult.OnCooldown, p.Bark(DogId.Cheddar, hide));
            p.Advance(6f, hide, hide);
            Assert.AreEqual(CoopShadowRunPuzzle.BarkResult.Decoy, p.Bark(DogId.Cheddar, hide));
        }

        [Test]
        public void BarkInTheOpen_AlertsTheEagle_AndSpeedsTheShadowUp()
        {
            var p = Make();
            float calm = p.ShadowSpeed;
            Assert.AreEqual(CoopShadowRunPuzzle.BarkResult.Alerted, p.Bark(DogId.Cheddar, Open(30f)));
            Assert.IsTrue(p.EagleAlerted);
            Assert.Greater(p.ShadowSpeed, calm);
            Assert.IsTrue(Has(Drain(p), CoopShadowRunPuzzle.RunEventKind.EagleAlerted));
            p.Advance(3f, Covers[0], Covers[0]);
            Assert.IsFalse(p.EagleAlerted, "The alert wears off.");
        }

        [Test]
        public void ClearingEveryRun_CompletesTheShadowRun()
        {
            var p = Make();
            p.ForceClearRun();
            p.ForceClearRun();
            Assert.IsTrue(p.Complete);
            Assert.AreEqual(5, p.TotalToysStashed);
            Assert.AreEqual(0f, p.ShadowSpeed);
            Vector2 open = Open(0f);
            p.Advance(1f, open, open);
            Assert.IsFalse(p.GrabActive, "A finished run never grabs anyone.");
        }

        /// <summary>
        /// Pacing guard: a careful pair that waits in cover, reads the shadow, and only dashes when
        /// the trip clears it must finish both runs well inside the round. The old sweep crawled
        /// across the 120-wide yard so slowly that two required passes could not fit the clock.
        /// </summary>
        [Test]
        public void Pacing_ACarefulPair_ClearsBothRuns_InsideTheRoundBudget()
        {
            float worst = 0f;
            for (int seed = 0; seed < 25; seed++)
            {
                float t = SimulateCarefulPair(seed, 13f, 15f, out var p, 2f); // the shipped tuning
                Assert.IsTrue(p.Complete, $"seed {seed}: a careful pair should clear both runs (stuck at run {p.RunIndex}).");
                Assert.AreEqual(0, p.TotalCarryOffs, $"seed {seed}: a pair that rescues promptly should never be carried off.");
                worst = Mathf.Max(worst, t);
            }
            Assert.Less(worst, 62f, "Both runs must leave room in a 90s round for the snatch rescue and the united front.");
        }

        /// <summary>
        /// Scripted "careful couch pair": Cheddar fetches small toys bush-to-bush and only dashes when
        /// the round trip clears the shadow; Cocoa holds a bush and barks a decoy when the shadow
        /// closes on him; both go out for the rope; a grabbed dog is rescued at once. Returns seconds
        /// taken (capped at 150).
        /// </summary>
        public static float SimulateCarefulPair(int seed, float run1Speed, float run2Speed, out CoopShadowRunPuzzle p,
            float edgePause = -1f)
        {
            const float dt = 1f / 30f;
            const float cheddarSpeed = 6.2f / 50f * 60f; // DogController's base speed conversion
            const float cocoaSpeed = 5.9f / 50f * 60f;
            p = new CoopShadowRunPuzzle();
            p.Configure(Yard, -58.5f, 58.5f, Covers, 3f,
                new[] { 2, 3 }, new[] { false, true }, new[] { run1Speed, run2Speed });
            p.ConfigureFeel(edgePauseSeconds: edgePause);
            p.Reset(seed);
            Vector2 cheddar = Covers[2];
            Vector2 cocoa = Covers[2] + Vector2.right;
            Vector2 goal = cheddar;
            bool replan = true;
            float t = 0f;
            while (!p.Complete && t < 150f)
            {
                // Commit to each waypoint (a bush or a toy) and only re-plan on arrival.
                if (replan || Vector2.Distance(cheddar, goal) < 0.05f) goal = BotGoal(p, cheddar, cheddarSpeed);
                replan = false;
                cheddar = Vector2.MoveTowards(cheddar, goal, cheddarSpeed * p.SpeedMultiplierFor(DogId.Cheddar) * dt);
                bool ropeJob = RopeIsNext(p);
                Vector2 cocoaGoal = ropeJob ? goal + Vector2.right * 1.2f
                    : p.IsInCover(cocoa) ? cocoa : p.NearestCover(cocoa) + Vector2.right;
                cocoa = Vector2.MoveTowards(cocoa, cocoaGoal, cocoaSpeed * p.SpeedMultiplierFor(DogId.Cocoa) * dt);
                float gap = (cheddar.x - p.ShadowX) * p.ShadowDirection;
                if (!p.IsInCover(cheddar) && gap > 0f && gap < 14f && p.IsInCover(cocoa))
                    p.Bark(DogId.Cocoa, cocoa); // "HEY! Over here!"
                p.Advance(dt, cheddar, cocoa);
                if (p.GrabActive)
                {
                    Vector2 held = p.GrabbedDog == DogId.Cheddar ? cheddar : cocoa;
                    DogId free = p.GrabbedDog == DogId.Cheddar ? DogId.Cocoa : DogId.Cheddar;
                    p.Wiggle(p.GrabbedDog.Value);
                    p.Pull(free, held, held);
                    replan = true;
                }
                p.DrainEvents(null);
                t += dt;
            }
            return t;
        }

        private static bool RopeIsNext(CoopShadowRunPuzzle p)
        {
            if (p.RopeLifted) return true;
            for (int i = 0; i < p.ToyCount; i++)
                if (!p.ToyIsRope(i) && p.ToyStateAt(i) != CoopShadowRunPuzzle.ToyState.Stashed) return false;
            return true;
        }

        // Next waypoint for the lead dog: the nearest un-stashed toy if the round trip clears the
        // shadow, otherwise the nearest cover to wait in.
        private static Vector2 BotGoal(CoopShadowRunPuzzle p, Vector2 at, float speed)
        {
            int carried = p.CarriedToy(DogId.Cheddar);
            if (carried >= 0 || p.RopeLifted) return p.NearestCover(at);
            int target = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < p.ToyCount; i++)
            {
                if (p.ToyStateAt(i) == CoopShadowRunPuzzle.ToyState.Stashed) continue;
                if (p.ToyIsRope(i) && !RopeIsNext(p)) continue; // small toys first; the rope needs both dogs
                float d = Vector2.Distance(at, p.ToyPosition(i));
                if (d < best) { best = d; target = i; }
            }
            if (target < 0) return p.NearestCover(at);
            Vector2 toy = p.ToyPosition(target);
            // Hop bush-to-bush toward the toy when another cover is a better launch point.
            Vector2 launch = at;
            float launchCost = Vector2.Distance(at, toy);
            for (int c = 0; c < Covers.Length; c++)
            {
                float cost = Vector2.Distance(Covers[c], toy);
                if (cost + 4f < launchCost && Vector2.Distance(at, Covers[c]) < 50f) { launch = Covers[c]; launchCost = cost; }
            }
            if (launch != at)
            {
                float hop = Vector2.Distance(at, launch) / speed;
                return TripClearsShadow(p, at, launch, launch, hop, 0.01f) ? launch : at;
            }
            Vector2 back = p.NearestCover(toy);
            float leg1 = Vector2.Distance(at, toy) / speed;
            float leg2 = Vector2.Distance(toy, back) / (speed * 0.8f);
            return TripClearsShadow(p, at, toy, back, leg1, leg2) ? toy : at;
        }

        private static bool TripClearsShadow(CoopShadowRunPuzzle p, Vector2 from, Vector2 toy, Vector2 back, float leg1, float leg2)
        {
            float total = leg1 + leg2;
            for (float tau = 0f; tau <= total; tau += 0.1f)
            {
                Vector2 dog = tau < leg1 ? Vector2.Lerp(from, toy, tau / leg1) : Vector2.Lerp(toy, back, (tau - leg1) / leg2);
                if (p.IsInCover(dog)) continue;
                float sx = p.PredictShadowX(tau);
                bool parked = Mathf.Approximately(sx, -58.5f) || Mathf.Approximately(sx, 58.5f);
                if (!parked && Mathf.Abs(sx - dog.x) < p.ShadowHalfWidth + 1.5f) return false;
            }
            return true;
        }

        [Test]
        public void PredictShadowX_MatchesTheRealSweep_IncludingTheTurnPause()
        {
            var p = Make(edgePause: 1.1f);
            var twin = Make(edgePause: 1.1f);
            Vector2 hide = Covers[0];
            float predicted = p.PredictShadowX(6f);
            for (int i = 0; i < 60; i++) twin.Advance(0.1f, hide, hide);
            Assert.AreEqual(twin.ShadowX, predicted, 0.05f);

            // Across a turn-around the wobble re-rolls, so only demand a near match.
            float farPrediction = p.PredictShadowX(14f);
            for (int i = 0; i < 80; i++) twin.Advance(0.1f, hide, hide);
            Assert.AreEqual(twin.ShadowX, farPrediction, 9f);
        }

        [Test]
        public void SecondsUntilShadowAt_CountsDownAsTheShadowApproaches()
        {
            var p = Make();
            float far = p.SecondsUntilShadowAt(30f);
            float near = p.SecondsUntilShadowAt(-30f);
            Assert.Greater(far, near, "The shadow starts on the left, so the right side has longer.");
            Assert.AreEqual(0f, p.SecondsUntilShadowAt(p.ShadowX));
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            var p = Make();
            p.ForceGrab(DogId.Cocoa, Open(0f));
            p.ForceCarryOff(Open(0f));
            p.ForceClearRun();
            p.Reset(3);
            Assert.AreEqual(0, p.RunIndex);
            Assert.AreEqual(0, p.TotalGrabs);
            Assert.AreEqual(0, p.TotalCarryOffs);
            Assert.AreEqual(0, p.TotalToysStashed);
            Assert.IsFalse(p.GrabActive);
            Assert.AreEqual(1, p.ShadowDirection);
            Assert.IsEmpty(Drain(p));
        }
    }
}
