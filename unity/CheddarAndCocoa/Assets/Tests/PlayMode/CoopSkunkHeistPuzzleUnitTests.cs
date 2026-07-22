using NUnit.Framework;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the Skunk Blast Mayhem heist beat: the tail-lift telegraph is a
    /// shared clock that resolves against whoever is actually in blast range (not who's luring), a
    /// grab only lands mid-lure with the tail down and the grabber clean, a sprayed dog can't
    /// re-enter the heist until rubbed (or slowly air-dried) clean, and the laundry economy depletes
    /// and refills exactly once per rub/haul.
    /// </summary>
    public sealed class CoopSkunkHeistPuzzleUnitTests
    {
        private static CoopSkunkHeistPuzzle Make(
            float tailLiftInterval = 4.5f, float telegraphSeconds = 1.3f, int rubsToClean = 3,
            int pileCapacity = 3, int basketCapacity = 5, float airDryRubsPerSecond = 1f / 20f)
        {
            var p = new CoopSkunkHeistPuzzle();
            p.Configure(tailLiftInterval, telegraphSeconds, rubsToClean, pileCapacity, basketCapacity, airDryRubsPerSecond);
            return p;
        }

        [Test]
        public void StartState_NoDogStinkyAndLaundryIsFull()
        {
            var p = Make(pileCapacity: 3, basketCapacity: 5);
            Assert.IsFalse(p.CheddarStinky);
            Assert.IsFalse(p.CocoaStinky);
            Assert.IsFalse(p.AnyDogStinky);
            Assert.AreEqual(3, p.PileFresh);
            Assert.AreEqual(0, p.PileFunky);
            Assert.AreEqual(5, p.BasketSupply);
            Assert.IsFalse(p.PrizeSecured);
            Assert.IsFalse(p.TailUp);
            Assert.AreEqual(0, p.SkunkEvents);
        }

        [Test]
        public void TailLiftTelegraph_FiresAfterTheCalmIntervalAndIsReadable()
        {
            var p = Make(tailLiftInterval: 2f, telegraphSeconds: 1f);
            p.Advance(1.9f, false, false);
            Assert.IsFalse(p.TailUp, "The telegraph should not fire before the calm interval elapses.");
            p.Advance(0.2f, false, false);
            Assert.IsTrue(p.TailUp, "The tail-lift telegraph is the shared danger clock - it must fire on its own clock.");
            Assert.Greater(p.TailWindowRemaining, 0f);
        }

        [Test]
        public void ForceTailLift_TestHookSkipsTheCalmTimer()
        {
            var p = Make();
            p.ForceTailLift();
            Assert.IsTrue(p.TailUp);
            Assert.AreEqual(p.TelegraphSeconds, p.TailWindowRemaining);
        }

        [Test]
        public void SprayResolution_OnlySkunksWhoeverIsActuallyInBlastRangeAtTheDeadline()
        {
            var p = Make(telegraphSeconds: 1f);
            p.ForceTailLift();
            p.Advance(1.1f, cheddarInBlastRange: true, cocoaInBlastRange: false);

            Assert.IsTrue(p.CheddarStinky, "Cheddar was still in range when the window closed.");
            Assert.IsFalse(p.CocoaStinky, "Cocoa had already bailed out of range - she must stay clean.");
            Assert.AreEqual(1, p.SkunkEvents);
            Assert.IsFalse(p.TailUp, "The telegraph resolves (up or down) exactly once per window.");
        }

        [Test]
        public void SprayResolution_BothDogsInRangeSkunksBoth()
        {
            var p = Make(telegraphSeconds: 1f);
            p.ForceTailLift();
            p.Advance(1.1f, cheddarInBlastRange: true, cocoaInBlastRange: true);

            Assert.IsTrue(p.CheddarStinky);
            Assert.IsTrue(p.CocoaStinky);
            Assert.IsTrue(p.BothDogsStinky, "Both dogs skunked at once is a valid, funny outcome, not a bug.");
            Assert.AreEqual(2, p.SkunkEvents);
        }

        [Test]
        public void SprayResolution_NeitherDogInRangeIsACleanBail()
        {
            var p = Make(telegraphSeconds: 1f);
            p.ForceTailLift();
            p.Advance(1.1f, cheddarInBlastRange: false, cocoaInBlastRange: false);

            Assert.IsFalse(p.AnyDogStinky);
            Assert.AreEqual(0, p.SkunkEvents);
        }

        [Test]
        public void Grab_OnlySucceedsForCocoaMidLureWithTailDownAndClean()
        {
            var p = Make();
            Assert.IsFalse(p.TryGrab(grabberIsCocoa: false, lureHolding: true), "Cheddar cannot make the snatch - that's Cocoa's role.");
            Assert.IsFalse(p.TryGrab(grabberIsCocoa: true, lureHolding: false), "No lure, no safe window - the skunk is still watching the prize.");

            p.ForceTailLift();
            Assert.IsFalse(p.TryGrab(grabberIsCocoa: true, lureHolding: true), "The tail is up - grabbing now should never succeed.");

            p.Advance(p.TelegraphSeconds + 0.1f, false, false); // clean bail, tail back down
            Assert.IsTrue(p.TryGrab(grabberIsCocoa: true, lureHolding: true));
            Assert.IsTrue(p.PrizeSecured);
            Assert.IsFalse(p.TryGrab(grabberIsCocoa: true, lureHolding: true), "Nothing left to grab once secured.");
        }

        [Test]
        public void Grab_StinkyCocoaCannotMakeTheCleanSnatch()
        {
            var p = Make(telegraphSeconds: 1f);
            p.ForceTailLift();
            p.Advance(1.1f, cheddarInBlastRange: false, cocoaInBlastRange: true);
            Assert.IsTrue(p.CocoaStinky);

            Assert.IsFalse(p.TryGrab(grabberIsCocoa: true, lureHolding: true),
                "The skunk can smell a stinky dog coming - she can't sneak up until she's clean.");
        }

        [Test]
        public void StinkyDog_RubsCleanAfterConfiguredRubCount()
        {
            var p = Make(rubsToClean: 3, pileCapacity: 3);
            p.ForceTailLift();
            p.Advance(p.TelegraphSeconds + 0.1f, true, false);
            Assert.IsTrue(p.CheddarStinky);

            Assert.IsTrue(p.Rub(DogId.Cheddar));
            Assert.IsTrue(p.CheddarStinky, "Two more rubs still needed.");
            Assert.AreEqual(1, p.CheddarRubProgress);
            Assert.IsTrue(p.Rub(DogId.Cheddar));
            Assert.IsTrue(p.Rub(DogId.Cheddar));
            Assert.IsFalse(p.CheddarStinky, "Three rubs (the configured RubsToClean) should fully clean him.");
            Assert.AreEqual(0, p.CheddarRubProgress);

            Assert.IsFalse(p.Rub(DogId.Cheddar), "A clean dog has nothing to rub off.");
            Assert.IsFalse(p.Rub(DogId.Cocoa), "Cocoa was never sprayed - nothing for her to rub either.");
        }

        [Test]
        public void LaundryEconomy_RubDepletesPileAndHaulRestocksFromTheBasket()
        {
            var p = Make(pileCapacity: 3, basketCapacity: 5);
            p.ForceTailLift();
            p.Advance(p.TelegraphSeconds + 0.1f, true, false);

            Assert.IsTrue(p.Rub(DogId.Cheddar));
            Assert.AreEqual(2, p.PileFresh, "One rub should consume exactly one fresh piece.");
            Assert.AreEqual(1, p.PileFunky, "The used piece becomes funky, not just vanish.");

            Assert.IsTrue(p.HaulLaundry());
            Assert.AreEqual(4, p.BasketSupply, "Hauling drags exactly one piece out of the basket's reserve.");
            Assert.AreEqual(3, p.PileFresh, "...and restocks the pile back toward full.");

            Assert.IsFalse(p.HaulLaundry(), "The pile is already full again - nothing more to haul yet.");
        }

        [Test]
        public void LaundryEconomy_RunningCompletelyDry_StillClearsViaSlowAirDry()
        {
            var p = Make(rubsToClean: 3, pileCapacity: 1, basketCapacity: 0, airDryRubsPerSecond: 1f);
            p.ForceTailLift();
            p.Advance(p.TelegraphSeconds + 0.1f, true, false);
            Assert.IsTrue(p.CheddarStinky);

            Assert.IsTrue(p.Rub(DogId.Cheddar), "The single fresh piece in the pile is still usable once.");
            Assert.AreEqual(0, p.PileFresh);
            Assert.IsFalse(p.HaulLaundry(), "An empty basket (capacity 0) has nothing left to haul.");
            Assert.IsFalse(p.Rub(DogId.Cheddar), "Stuck airing out slowly - no fresh laundry left to rub with.");
            Assert.IsTrue(p.CheddarStinky, "Still stinky - only one of the three required rubs has landed.");

            // airDryRubsPerSecond=1 means one full second of waiting equals one rub-equivalent.
            p.Advance(1f, false, false);
            Assert.AreEqual(2, p.CheddarRubProgress, "The slow air-dry trickle should still count as progress.");
            p.Advance(1f, false, false);
            Assert.IsFalse(p.CheddarStinky, "Running completely dry is a costly detour, not a dead end - it still finishes clean.");
        }

        [Test]
        public void Reset_ClearsEverythingBackToStartState()
        {
            var p = Make(pileCapacity: 3, basketCapacity: 5);
            p.ForceTailLift();
            p.Advance(p.TelegraphSeconds + 0.1f, true, true);
            p.Rub(DogId.Cheddar);
            p.HaulLaundry();
            Assert.Greater(p.SkunkEvents, 0);

            p.Reset();
            Assert.IsFalse(p.CheddarStinky);
            Assert.IsFalse(p.CocoaStinky);
            Assert.AreEqual(0, p.SkunkEvents);
            Assert.AreEqual(3, p.PileFresh);
            Assert.AreEqual(0, p.PileFunky);
            Assert.AreEqual(5, p.BasketSupply);
            Assert.IsFalse(p.PrizeSecured);
            Assert.IsFalse(p.TailUp);
        }
    }
}
