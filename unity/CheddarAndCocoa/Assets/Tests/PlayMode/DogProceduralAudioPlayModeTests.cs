using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Tests
{
    public sealed class DogProceduralAudioPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go.name.StartsWith("ProceduralAudioTestDog")) Object.DestroyImmediate(go);
            }
        }

        [TestCase(DogFeedbackAction.Bark)]
        [TestCase(DogFeedbackAction.Tug)]
        [TestCase(DogFeedbackAction.Carry)]
        [TestCase(DogFeedbackAction.Rescue)]
        [TestCase(DogFeedbackAction.Zoomies)]
        public void Profiles_GiveCheddarAndCocoaDistinctActionVoices(DogFeedbackAction action)
        {
            DogProceduralCueStyle cheddar = DogProceduralAudioProfile.For(
                DogId.Cheddar, action, DogFeedbackPhase.Impact);
            DogProceduralCueStyle cocoa = DogProceduralAudioProfile.For(
                DogId.Cocoa, action, DogFeedbackPhase.Impact);

            Assert.AreNotEqual(cheddar.Signature, cocoa.Signature);
            Assert.AreNotEqual(cheddar.Frequency, cocoa.Frequency);
            Assert.AreNotEqual(cheddar.Harmonic, cocoa.Harmonic);
            Assert.Greater(cheddar.Duration, 0f);
            Assert.Greater(cocoa.Duration, 0f);
        }

        [UnityTest]
        public IEnumerator Bark_CuesFollowAnticipationImpactAndRecoveryPhases()
        {
            DogProceduralAudio audio = MakeAudio(DogId.Cheddar, out DogActionFeedback feedback);
            DogActionFeedbackStyle style = DogActionFeedbackProfile.For(DogId.Cheddar, DogFeedbackAction.Bark);

            feedback.Trigger(DogFeedbackAction.Bark);
            Assert.AreEqual(DogFeedbackPhase.Anticipation, audio.LastCuePhase);

            yield return new WaitForSecondsRealtime(
                DogProceduralAudioProfile.For(DogId.Cheddar, DogFeedbackAction.Bark,
                    DogFeedbackPhase.Anticipation).Duration + 0.01f);
            feedback.Tick(style.Anticipation + 0.001f);
            Assert.AreEqual(DogFeedbackPhase.Impact, audio.LastCuePhase);

            yield return new WaitForSecondsRealtime(
                DogProceduralAudioProfile.For(DogId.Cheddar, DogFeedbackAction.Bark,
                    DogFeedbackPhase.Impact).Duration + 0.01f);
            feedback.Tick(style.Impact + 0.001f);
            Assert.AreEqual(DogFeedbackPhase.Recovery, audio.LastCuePhase);
            StringAssert.Contains("CHEDDAR BARK SETTLE", audio.LastCueSignature);
        }

        [UnityTest]
        public IEnumerator SustainedAction_StartsAndStopsLoopOnMatchingPhases()
        {
            DogProceduralAudio audio = MakeAudio(DogId.Cocoa, out DogActionFeedback feedback);
            DogActionFeedbackStyle style = DogActionFeedbackProfile.For(DogId.Cocoa, DogFeedbackAction.Tug);

            feedback.SetSustained(DogFeedbackAction.Tug, true);
            yield return new WaitForSecondsRealtime(
                DogProceduralAudioProfile.For(DogId.Cocoa, DogFeedbackAction.Tug,
                    DogFeedbackPhase.Anticipation).Duration + 0.01f);
            feedback.Tick(style.Anticipation + 0.001f);
            yield return new WaitForSecondsRealtime(
                DogProceduralAudioProfile.For(DogId.Cocoa, DogFeedbackAction.Tug,
                    DogFeedbackPhase.Impact).Duration + 0.01f);
            feedback.Tick(style.Impact + 0.001f);
            Assert.AreEqual(DogFeedbackPhase.Sustain, audio.LastCuePhase);
            Assert.AreEqual(1, audio.ActiveVoiceCount(float.MaxValue), "Only the sustain loop should remain indefinitely.");

            feedback.SetSustained(DogFeedbackAction.Tug, false);
            Assert.AreEqual(DogFeedbackPhase.Recovery, audio.LastCuePhase);
            Assert.AreEqual(0, audio.ActiveVoiceCount(float.MaxValue), "Recovery must stop the sustain loop.");
        }

        [Test]
        public void CooldownAndVoicePool_RejectSpamWithoutExceedingLimit()
        {
            DogProceduralAudio audio = MakeAudio(DogId.Cheddar, out _);

            Assert.IsTrue(audio.TryPlayPhaseCue(DogFeedbackAction.Bark, DogFeedbackPhase.Impact, 10f));
            Assert.IsFalse(audio.TryPlayPhaseCue(DogFeedbackAction.Bark, DogFeedbackPhase.Impact, 10.01f));
            Assert.IsTrue(audio.TryPlayPhaseCue(DogFeedbackAction.Tug, DogFeedbackPhase.Impact, 10f));
            Assert.IsFalse(audio.TryPlayPhaseCue(DogFeedbackAction.Carry, DogFeedbackPhase.Impact, 10f));

            Assert.AreEqual(DogProceduralAudio.VoiceLimit, audio.PeakVoiceCount);
            Assert.AreEqual(DogProceduralAudio.VoiceLimit, audio.ActiveVoiceCount(10.01f));
            Assert.AreEqual(2, audio.TotalCuesRejected);
        }

        [Test]
        public void Profiles_PrioritizeRescueAndBarkAboveContinuousActionBeds()
        {
            DogProceduralCueStyle rescue = Profile(DogFeedbackAction.Rescue, DogFeedbackPhase.Impact);
            DogProceduralCueStyle bark = Profile(DogFeedbackAction.Bark, DogFeedbackPhase.Impact);
            DogProceduralCueStyle tug = Profile(DogFeedbackAction.Tug, DogFeedbackPhase.Impact);
            DogProceduralCueStyle carry = Profile(DogFeedbackAction.Carry, DogFeedbackPhase.Impact);
            DogProceduralCueStyle zoomies = Profile(DogFeedbackAction.Zoomies, DogFeedbackPhase.Impact);

            Assert.Greater(rescue.Volume, bark.Volume);
            Assert.Greater(bark.Volume, tug.Volume);
            Assert.Greater(tug.Volume, carry.Volume);
            Assert.Greater(carry.Volume, zoomies.Volume);

            Assert.LessOrEqual(Profile(DogFeedbackAction.Tug, DogFeedbackPhase.Sustain).Volume, 0.15f);
            Assert.LessOrEqual(Profile(DogFeedbackAction.Carry, DogFeedbackPhase.Sustain).Volume, 0.15f);
            Assert.LessOrEqual(Profile(DogFeedbackAction.Zoomies, DogFeedbackPhase.Sustain).Volume, 0.15f);
        }

        [Test]
        public void SustainedProfiles_ThrottleRestartsAcrossTransientActionOverrides()
        {
            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            foreach (DogFeedbackAction action in new[]
                     { DogFeedbackAction.Tug, DogFeedbackAction.Carry, DogFeedbackAction.Zoomies })
            {
                DogProceduralCueStyle sustain = DogProceduralAudioProfile.For(
                    dog, action, DogFeedbackPhase.Sustain);
                Assert.GreaterOrEqual(sustain.Cooldown, 1.2f,
                    $"{dog} {action} must not stack a second loop after bark/rescue interruption.");
            }
        }

        [Test]
        public void TwoPlayerMix_AllowsBothDogImpactsButCapsCombinedLocalVoicesAtFour()
        {
            DogProceduralAudio cheddar = MakeAudio(DogId.Cheddar, out _);
            DogProceduralAudio cocoa = MakeAudio(DogId.Cocoa, out _);

            Assert.IsTrue(cheddar.TryPlayPhaseCue(DogFeedbackAction.Carry, DogFeedbackPhase.Sustain, 20f));
            Assert.IsTrue(cocoa.TryPlayPhaseCue(DogFeedbackAction.Carry, DogFeedbackPhase.Sustain, 20f));
            Assert.IsTrue(cheddar.TryPlayPhaseCue(DogFeedbackAction.Bark, DogFeedbackPhase.Impact, 20.1f));
            Assert.IsTrue(cocoa.TryPlayPhaseCue(DogFeedbackAction.Bark, DogFeedbackPhase.Impact, 20.1f));

            Assert.AreEqual(2, cheddar.ActiveVoiceCount(20.11f));
            Assert.AreEqual(2, cocoa.ActiveVoiceCount(20.11f));
            Assert.AreEqual(4, cheddar.ActiveVoiceCount(20.11f) + cocoa.ActiveVoiceCount(20.11f));
            StringAssert.Contains("CHEDDAR BARK HIT", cheddar.LastCueSignature);
            StringAssert.Contains("COCOA BARK HIT", cocoa.LastCueSignature);
        }

        private static DogProceduralCueStyle Profile(DogFeedbackAction action, DogFeedbackPhase phase) =>
            DogProceduralAudioProfile.For(DogId.Cheddar, action, phase);

        private static DogProceduralAudio MakeAudio(DogId id, out DogActionFeedback feedback)
        {
            var root = new GameObject($"ProceduralAudioTestDog_{id}");
            root.AddComponent<DogIdentity>().Configure(id, null);
            feedback = root.AddComponent<DogActionFeedback>();
            feedback.Initialize(root.transform, null);
            DogProceduralAudio audio = root.AddComponent<DogProceduralAudio>();
            audio.Initialize(feedback);
            return audio;
        }
    }
}
