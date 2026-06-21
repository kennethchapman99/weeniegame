using NUnit.Framework;
using UnityEngine;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Tests
{
    public sealed class DogActionFeedbackPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (go.name.StartsWith("FeedbackTestDog") || go.name.Contains("_Particle_") || go.name.EndsWith("_Trail"))
                    Object.DestroyImmediate(go);
            }
        }

        [TestCase(DogFeedbackAction.Bark)]
        [TestCase(DogFeedbackAction.Tug)]
        [TestCase(DogFeedbackAction.Carry)]
        [TestCase(DogFeedbackAction.Rescue)]
        [TestCase(DogFeedbackAction.Zoomies)]
        public void Profiles_PreserveDistinctCheddarAndCocoaIdentity(DogFeedbackAction action)
        {
            DogActionFeedbackStyle cheddar = DogActionFeedbackProfile.For(DogId.Cheddar, action);
            DogActionFeedbackStyle cocoa = DogActionFeedbackProfile.For(DogId.Cocoa, action);

            Assert.AreNotEqual(cheddar.Signature, cocoa.Signature);
            Assert.AreNotEqual(cheddar.Primary, cocoa.Primary);
            Assert.AreNotEqual(cheddar.Anticipation, cocoa.Anticipation,
                "Each dog should have a distinct action wind-up.");
            Assert.Greater(cheddar.KickDegrees, cocoa.KickDegrees,
                "Cheddar should read as snappier while Cocoa remains grounded.");
            Assert.Greater(cheddar.ParticleCount, 0);
            Assert.Greater(cocoa.ParticleCount, 0);
        }

        [Test]
        public void Bark_SequencesAnticipationImpactAndRecovery_WithoutChangingColliderRoot()
        {
            DogActionFeedback feedback = MakeFeedback(DogId.Cheddar, out GameObject root, out BoxCollider2D collider);
            root.transform.localScale = new Vector3(1.4f, 0.85f, 1f);
            root.transform.localRotation = Quaternion.Euler(0f, 0f, 17f);
            Vector3 rootScale = root.transform.localScale;
            Quaternion rootRotation = root.transform.localRotation;
            Vector2 colliderSize = collider.size;
            Vector2 colliderOffset = collider.offset;
            DogActionFeedbackStyle style = DogActionFeedbackProfile.For(DogId.Cheddar, DogFeedbackAction.Bark);

            feedback.Trigger(DogFeedbackAction.Bark);
            feedback.Tick(style.Anticipation * 0.5f);
            Assert.AreEqual(DogFeedbackPhase.Anticipation, feedback.CurrentPhase);
            Assert.AreNotEqual(Vector2.one, feedback.VisualScale);

            feedback.Tick(style.Anticipation * 0.5f + 0.001f);
            Assert.AreEqual(DogFeedbackPhase.Impact, feedback.CurrentPhase);
            Assert.AreEqual(style.ParticleCount, feedback.TotalParticlesEmitted);
            Assert.AreEqual(style.Signature, feedback.LastParticleSignature);

            feedback.Tick(style.Impact + style.Recovery + 0.01f);
            Assert.IsTrue(feedback.IsNeutral, "The action should settle fully after recovery.");
            Assert.AreEqual(rootScale, root.transform.localScale);
            Assert.AreEqual(rootRotation, root.transform.localRotation);
            Assert.AreEqual(colliderSize, collider.size);
            Assert.AreEqual(colliderOffset, collider.offset);
        }

        [TestCase(DogFeedbackAction.Tug)]
        [TestCase(DogFeedbackAction.Carry)]
        [TestCase(DogFeedbackAction.Zoomies)]
        public void SustainedActions_HoldAfterImpact_EmitMotionTrails_ThenRecover(DogFeedbackAction action)
        {
            DogActionFeedback feedback = MakeFeedback(DogId.Cocoa, out _, out _);
            DogActionFeedbackStyle style = DogActionFeedbackProfile.For(DogId.Cocoa, action);

            feedback.SetSustained(action, true);
            feedback.Tick(style.Anticipation + style.Impact + 0.01f);
            Assert.AreEqual(DogFeedbackPhase.Sustain, feedback.CurrentPhase);
            Assert.AreEqual(action, feedback.CurrentAction);

            feedback.TrackMotion(Vector2.right * 3f, style.TrailInterval * 2.1f);
            Assert.GreaterOrEqual(feedback.TotalTrailsEmitted, 2);

            feedback.SetSustained(action, false);
            Assert.AreEqual(DogFeedbackPhase.Recovery, feedback.CurrentPhase);
            feedback.Tick(style.Recovery + 0.01f);
            Assert.IsTrue(feedback.IsNeutral);
        }

        [Test]
        public void BarkTemporarilyOverridesCarry_ThenReturnsToCarrySustain()
        {
            DogActionFeedback feedback = MakeFeedback(DogId.Cheddar, out _, out _);
            DogActionFeedbackStyle carry = DogActionFeedbackProfile.For(DogId.Cheddar, DogFeedbackAction.Carry);
            DogActionFeedbackStyle bark = DogActionFeedbackProfile.For(DogId.Cheddar, DogFeedbackAction.Bark);

            feedback.SetSustained(DogFeedbackAction.Carry, true);
            feedback.Tick(carry.Anticipation + carry.Impact + 0.01f);
            feedback.Trigger(DogFeedbackAction.Bark);
            Assert.AreEqual(DogFeedbackAction.Bark, feedback.CurrentAction);

            feedback.Tick(bark.Anticipation + bark.Impact + bark.Recovery + 0.01f);
            Assert.AreEqual(DogFeedbackAction.Carry, feedback.CurrentAction);
            Assert.AreEqual(DogFeedbackPhase.Anticipation, feedback.CurrentPhase);
        }

        private static DogActionFeedback MakeFeedback(DogId id, out GameObject root, out BoxCollider2D collider)
        {
            root = new GameObject($"FeedbackTestDog_{id}");
            root.AddComponent<DogIdentity>().Configure(id, null);
            collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.7f, 0.65f);
            collider.offset = new Vector2(0.1f, -0.08f);
            var visual = new GameObject("VisualOnly");
            visual.transform.SetParent(root.transform, false);
            DogActionFeedback feedback = root.AddComponent<DogActionFeedback>();
            feedback.Initialize(visual.transform, null);
            return feedback;
        }
    }
}
