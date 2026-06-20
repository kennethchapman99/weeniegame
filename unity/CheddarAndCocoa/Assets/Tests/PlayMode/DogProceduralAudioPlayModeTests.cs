using NUnit.Framework;
using UnityEngine;
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

        [Test]
        public void Bark_CuesFollowAnticipationImpactAndRecoveryPhases()
        {
            DogProceduralAudio audio = MakeAudio(DogId.Cheddar, out DogActionFeedback feedback);
            DogActionFeedbackStyle style = DogActionFeedbackProfile.For(DogId.Cheddar, DogFeedbackAction.Bark);

            feedback.Trigger(DogFeedbackAction.Bark);
            Assert.AreEqual(DogFeedbackPhase.Anticipation, audio.LastCuePhase);

            feedback.Tick(style.Anticipation + 0.001f);
            Assert.AreEqual(DogFeedbackPhase.Impact, audio.LastCuePhase);

            feedback.Tick(style.Impact + 0.001f);
            Assert.AreEqual(DogFeedbackPhase.Recovery, audio.LastCuePhase);
            StringAssert.Contains("CHEDDAR BARK SETTLE", audio.LastCueSignature);
        }

        [Test]
        public void SustainedAction_StartsAndStopsLoopOnMatchingPhases()
        {
            DogProceduralAudio audio = MakeAudio(DogId.Cocoa, out DogActionFeedback feedback);
            DogActionFeedbackStyle style = DogActionFeedbackProfile.For(DogId.Cocoa, DogFeedbackAction.Tug);

            feedback.SetSustained(DogFeedbackAction.Tug, true);
            feedback.Tick(style.Anticipation + style.Impact + 0.001f);
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
            Assert.IsTrue(audio.TryPlayPhaseCue(DogFeedbackAction.Carry, DogFeedbackPhase.Impact, 10f));
            Assert.IsFalse(audio.TryPlayPhaseCue(DogFeedbackAction.Rescue, DogFeedbackPhase.Impact, 10f));

            Assert.AreEqual(DogProceduralAudio.VoiceLimit, audio.PeakVoiceCount);
            Assert.AreEqual(DogProceduralAudio.VoiceLimit, audio.ActiveVoiceCount(10.01f));
            Assert.AreEqual(2, audio.TotalCuesRejected);
        }

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
