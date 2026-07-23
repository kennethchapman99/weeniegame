using NUnit.Framework;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the Tick Invasion survival beat: both dogs accumulate ticks
    /// continuously at asymmetric rates, mutual grooming trades ticks between partners (with
    /// Cheddar's faster/stronger action vs Cocoa's still distinct amount), an erratic dog resists a
    /// normal groom until barked calm or held, a pool dive instantly resets but leaves the diver wet
    /// and double-accumulating for an asymmetric recovery window, a Super Tick locks onto one dog and
    /// resists grooming from either direction, and either dog maxing out ticks fails the run with a
    /// distinct per-dog reason.
    /// </summary>
    public sealed class CoopTickInvasionPuzzleUnitTests
    {
        private static CoopTickInvasionPuzzle Make(
            float cheddarTickRate = 0.026f, float cocoaTickRate = 0.017f, float erraticThreshold = 0.62f,
            float cheddarGroomAmount = 0.24f, float cocoaGroomAmount = 0.16f, float selfIncreaseAmount = 0.05f,
            float barkCalmAmount = 0.12f, float barkHoldSeconds = 3.5f,
            float cheddarWetSeconds = 10f, float cocoaWetSeconds = 6f, float wetTickMultiplier = 2f,
            float superTickTriggerSeconds = 45f, float surviveSeconds = 82f)
        {
            var p = new CoopTickInvasionPuzzle();
            p.Configure(cheddarTickRate, cocoaTickRate, erraticThreshold, cheddarGroomAmount, cocoaGroomAmount,
                selfIncreaseAmount, barkCalmAmount, barkHoldSeconds, cheddarWetSeconds, cocoaWetSeconds,
                wetTickMultiplier, superTickTriggerSeconds, surviveSeconds);
            return p;
        }

        [Test]
        public void StartState_BothDogsCleanNoPanicNoWetNoSuperTick()
        {
            var p = Make();
            Assert.AreEqual(0f, p.CheddarTicks);
            Assert.AreEqual(0f, p.CocoaTicks);
            Assert.IsFalse(p.CheddarErratic);
            Assert.IsFalse(p.CocoaErratic);
            Assert.IsFalse(p.IsCheddarWet);
            Assert.IsFalse(p.IsCocoaWet);
            Assert.IsNull(p.SuperTickTarget);
            Assert.IsFalse(p.SuperTickTriggered);
            Assert.IsNull(p.FailedDog);
            Assert.IsFalse(p.Cleared);
            Assert.AreEqual(0, p.Mistakes);
            Assert.AreEqual(0f, p.ElapsedSeconds);
        }

        [Test]
        public void TickAccumulation_CheddarRisesFasterThanCocoaOverTheSameTime()
        {
            var p = Make(cheddarTickRate: 0.02f, cocoaTickRate: 0.01f);
            p.Advance(10f);
            Assert.AreEqual(0.2f, p.CheddarTicks, 0.001f);
            Assert.AreEqual(0.1f, p.CocoaTicks, 0.001f);
            Assert.Greater(p.CheddarTicks, p.CocoaTicks, "Cheddar's chaos-puppy energy should attract ticks faster than Cocoa.");
        }

        [Test]
        public void Groom_ReducesTargetAndSlightlyIncreasesGroomer()
        {
            var p = Make();
            p.Advance(5f); // both dogs pick up some ticks first
            float cheddarBefore = p.CheddarTicks;
            float cocoaBefore = p.CocoaTicks;

            Assert.IsTrue(p.Groom(DogId.Cocoa), "Cocoa grooms Cheddar clean.");
            Assert.Less(p.CheddarTicks, cheddarBefore, "The groom target's ticks should drop.");
            Assert.Greater(p.CocoaTicks, cocoaBefore, "The groomer should pick up a small transfer.");
        }

        [Test]
        public void Groom_CheddarAndCocoaHaveDistinctGroomAmounts()
        {
            // Same starting ticks for both dogs, isolate one groom action each and compare the drop.
            var p = Make(cheddarGroomAmount: 0.3f, cocoaGroomAmount: 0.1f, selfIncreaseAmount: 0f);
            p.Advance(20f); // push both dogs well above any single groom amount
            float cocoaBeforeCheddarGrooms = p.CocoaTicks;
            p.Groom(DogId.Cheddar); // Cheddar grooms Cocoa
            float cocoaDropFromCheddar = cocoaBeforeCheddarGrooms - p.CocoaTicks;

            var q = Make(cheddarGroomAmount: 0.3f, cocoaGroomAmount: 0.1f, selfIncreaseAmount: 0f);
            q.Advance(20f);
            float cheddarBeforeCocoaGrooms = q.CheddarTicks;
            q.Groom(DogId.Cocoa); // Cocoa grooms Cheddar
            float cheddarDropFromCocoa = cheddarBeforeCocoaGrooms - q.CheddarTicks;

            Assert.AreNotEqual(cocoaDropFromCheddar, cheddarDropFromCocoa,
                "Cheddar's and Cocoa's groom amounts must be mechanically distinct, not just differently named.");
            Assert.Greater(cocoaDropFromCheddar, cheddarDropFromCocoa,
                "Cheddar's faster/stronger groom action should clear more ticks per action than Cocoa's.");
        }

        [Test]
        public void Erratic_TriggersAboveThresholdAndClearsBelowItViaBarkThenGroom()
        {
            var p = Make(cheddarTickRate: 1f, erraticThreshold: 0.5f, cocoaGroomAmount: 0.9f, selfIncreaseAmount: 0f);
            p.Advance(0.9f); // Cheddar crosses 0.5 with room to spare
            Assert.IsTrue(p.CheddarErratic);
            Assert.AreEqual(1, p.Mistakes);

            // Erratic Cheddar is too hard to lock down for a bare groom - Cocoa barks him calm first,
            // matching the design's "barked-calm or groomed enough" clearing pathway. The bark's own
            // small calm amount isn't enough by itself to drop him under the threshold.
            Assert.IsTrue(p.Bark(DogId.Cocoa));
            Assert.IsTrue(p.CheddarErratic, "The bark's small calm amount alone shouldn't fully clear erratic.");
            Assert.IsTrue(p.Groom(DogId.Cocoa)); // Cocoa grooms Cheddar back down under the threshold
            Assert.IsFalse(p.CheddarErratic, "A big enough held groom should drop Cheddar back under the erratic threshold.");
        }

        [Test]
        public void Erratic_BlocksNormalGroomUntilBarkedCalm()
        {
            var p = Make(cheddarTickRate: 1f, erraticThreshold: 0.5f);
            p.Advance(0.6f);
            Assert.IsTrue(p.CheddarErratic);

            Assert.IsFalse(p.Groom(DogId.Cocoa), "An erratic partner is too hard to lock down for a normal groom.");
        }

        [Test]
        public void Bark_HoldsErraticPartnerSoTheNextGroomLands()
        {
            // Push Cheddar well past the erratic threshold so the bark's own small calm amount
            // doesn't coincidentally clear erratic by itself - isolates the hold mechanism.
            var p = Make(cheddarTickRate: 1f, erraticThreshold: 0.5f, barkCalmAmount: 0.1f);
            p.Advance(0.9f);
            Assert.IsTrue(p.CheddarErratic);
            Assert.IsFalse(p.Groom(DogId.Cocoa), "Without a bark hold, the groom should still fail.");

            Assert.IsTrue(p.Bark(DogId.Cocoa), "Cocoa barks Cheddar calm.");
            Assert.IsTrue(p.CheddarErratic, "The bark's calm amount alone shouldn't fully clear erratic here.");
            Assert.IsTrue(p.Groom(DogId.Cocoa), "With the bark hold active, the same groom attempt should now land despite still being erratic.");
        }

        [Test]
        public void Bark_NoOpWhenPartnerIsNotErratic()
        {
            var p = Make();
            Assert.IsFalse(p.Bark(DogId.Cocoa), "Nothing to calm when the partner isn't erratic.");
        }

        [Test]
        public void Bark_AlsoAppliesAnImmediateCalmReduction()
        {
            var p = Make(cheddarTickRate: 1f, erraticThreshold: 0.5f, barkCalmAmount: 0.2f);
            p.Advance(0.6f);
            float before = p.CheddarTicks;
            p.Bark(DogId.Cocoa);
            Assert.Less(p.CheddarTicks, before, "Barking calm should also directly reduce some ticks.");
        }

        [Test]
        public void PoolDive_InstantResetPlusWetSlowAndDoubledAccumulation()
        {
            var p = Make(cheddarTickRate: 0.02f, cheddarWetSeconds: 4f, wetTickMultiplier: 2f);
            p.Advance(10f);
            Assert.Greater(p.CheddarTicks, 0f);

            Assert.IsTrue(p.PoolDive(DogId.Cheddar));
            Assert.AreEqual(0f, p.CheddarTicks, "Pool dive should instantly reset ticks to zero.");
            Assert.IsTrue(p.IsCheddarWet, "Cheddar should be wet immediately after diving.");

            float before = p.CheddarTicks;
            p.Advance(1f);
            float wetGain = p.CheddarTicks - before;
            Assert.AreEqual(0.02f * 2f * 1f, wetGain, 0.0005f, "Wet accumulation should run at double rate.");
        }

        [Test]
        public void PoolDive_WetDogCannotEffectivelyGroom()
        {
            var p = Make();
            p.PoolDive(DogId.Cheddar);
            Assert.IsTrue(p.IsCheddarWet);
            Assert.IsFalse(p.Groom(DogId.Cheddar), "A wet, soggy groomer cannot groom effectively right now.");
        }

        [Test]
        public void PoolDive_CocoaRecoversFasterThanCheddar()
        {
            var p = Make(cheddarWetSeconds: 10f, cocoaWetSeconds: 6f);
            p.PoolDive(DogId.Cheddar);
            p.PoolDive(DogId.Cocoa);

            p.Advance(7f); // past Cocoa's 6s window, still inside Cheddar's 10s window
            Assert.IsTrue(p.IsCheddarWet, "Cheddar's longer recovery window should still be active.");
            Assert.IsFalse(p.IsCocoaWet, "Cocoa's shorter recovery window should have already cleared.");
        }

        [Test]
        public void FailCondition_CheddarMaxingOutTripsFailedDog()
        {
            var p = Make(cheddarTickRate: 1f);
            p.Advance(1.1f);
            Assert.AreEqual(DogId.Cheddar, p.FailedDog);
        }

        [Test]
        public void FailCondition_CocoaMaxingOutTripsFailedDog()
        {
            var p = Make(cocoaTickRate: 1f, cheddarTickRate: 0f);
            p.Advance(1.1f);
            Assert.AreEqual(DogId.Cocoa, p.FailedDog);
        }

        [Test]
        public void SuperTick_TriggersDeterministicallyAtConfiguredTime()
        {
            var p = Make(superTickTriggerSeconds: 5f, cheddarTickRate: 0.01f, cocoaTickRate: 0.01f);
            p.Advance(4.9f);
            Assert.IsFalse(p.SuperTickTriggered);
            p.Advance(0.2f);
            Assert.IsTrue(p.SuperTickTriggered);
            Assert.IsNotNull(p.SuperTickTarget);
        }

        [Test]
        public void SuperTick_TargetsExactlyOneDogAndResistsGroomingFromEitherDirection()
        {
            var p = Make();
            p.ForceSuperTick(DogId.Cheddar);
            Assert.AreEqual(DogId.Cheddar, p.SuperTickTarget);

            // Neither direction of grooming can touch the Super Tick dog.
            Assert.IsFalse(p.Groom(DogId.Cocoa), "Grooming the Super Tick target from the partner must fail.");
            Assert.IsFalse(p.Groom(DogId.Cheddar), "The Super Tick dog is too overwhelmed to groom their partner either.");
        }

        [Test]
        public void SuperTick_OtherDogHasNoGroomReliefWhileItIsActive_TicksContinueUnmitigated()
        {
            var p = Make(cocoaTickRate: 0.02f);
            p.ForceSuperTick(DogId.Cheddar);
            float before = p.CocoaTicks;

            Assert.IsFalse(p.Groom(DogId.Cheddar), "Cheddar can't groom Cocoa while he's the Super Tick target.");
            p.Advance(5f);
            Assert.AreEqual(before + 0.02f * 5f, p.CocoaTicks, 0.0005f,
                "With no groom relief available, Cocoa's own ticks should climb completely unmitigated.");
        }

        [Test]
        public void SuperTick_OnlyPoolDiveResolvesIt()
        {
            var p = Make();
            p.ForceSuperTick(DogId.Cocoa);
            Assert.IsTrue(p.PoolDive(DogId.Cocoa));
            Assert.IsNull(p.SuperTickTarget, "A pool dive should clear the Super Tick.");
            Assert.AreEqual(0f, p.CocoaTicks);
        }

        [Test]
        public void ClearPath_SurvivingTheFullDurationWithoutMaxingOutClears()
        {
            var p = Make(cheddarTickRate: 0.001f, cocoaTickRate: 0.001f, surviveSeconds: 10f);
            p.Advance(9.9f);
            Assert.IsFalse(p.Cleared);
            p.Advance(0.2f);
            Assert.IsTrue(p.Cleared);
            Assert.IsNull(p.FailedDog);
        }

        [Test]
        public void Reset_ClearsEverythingBackToStartState()
        {
            var p = Make();
            p.Advance(5f);
            p.Groom(DogId.Cheddar);
            p.Bark(DogId.Cocoa);
            p.PoolDive(DogId.Cheddar);
            p.ForceSuperTick(DogId.Cocoa);

            p.Reset();
            Assert.AreEqual(0f, p.CheddarTicks);
            Assert.AreEqual(0f, p.CocoaTicks);
            Assert.IsFalse(p.IsCheddarWet);
            Assert.IsFalse(p.IsCocoaWet);
            Assert.IsNull(p.SuperTickTarget);
            Assert.IsFalse(p.SuperTickTriggered);
            Assert.IsNull(p.FailedDog);
            Assert.IsFalse(p.Cleared);
            Assert.AreEqual(0, p.Mistakes);
            Assert.AreEqual(0f, p.ElapsedSeconds);
        }
    }
}
