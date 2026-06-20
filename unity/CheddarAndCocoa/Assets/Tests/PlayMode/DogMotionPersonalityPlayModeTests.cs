using NUnit.Framework;
using UnityEngine;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Tests
{
    public sealed class DogMotionPersonalityPlayModeTests
    {
        [Test]
        public void RunAndZoomies_PreserveChaosPuppyVersusVeteranQueenRead()
        {
            const float sampleTime = 0.1f;
            var cheddarRun = DogMotionPersonality.At(DogId.Cheddar,
                DogReadabilityFeedback.Pose.Run, sampleTime, 1f, false);
            var cheddarZoomies = DogMotionPersonality.At(DogId.Cheddar,
                DogReadabilityFeedback.Pose.Run, sampleTime, 1f, true);
            var cocoaRun = DogMotionPersonality.At(DogId.Cocoa,
                DogReadabilityFeedback.Pose.Run, sampleTime, 1f, false);

            Assert.AreEqual("PUPPY SCRAMBLE", cheddarRun.Signature);
            Assert.AreEqual("ZOOMIES SCRAMBLE", cheddarZoomies.Signature);
            Assert.AreEqual("VETERAN TROT", cocoaRun.Signature);
            Assert.Greater(Mathf.Abs(cheddarRun.RotationDegrees),
                Mathf.Abs(cocoaRun.RotationDegrees));
            Assert.Less(cocoaRun.Scale.y, cheddarRun.Scale.y,
                "Cocoa's run should stay lower and more grounded than Cheddar's scramble.");

            float normalPeak = 0f;
            float zoomiesPeak = 0f;
            for (int i = 0; i <= 20; i++)
            {
                float t = i / 20f;
                normalPeak = Mathf.Max(normalPeak, Mathf.Abs(DogMotionPersonality.At(DogId.Cheddar,
                    DogReadabilityFeedback.Pose.Run, t, 1f, false).RotationDegrees));
                zoomiesPeak = Mathf.Max(zoomiesPeak, Mathf.Abs(DogMotionPersonality.At(DogId.Cheddar,
                    DogReadabilityFeedback.Pose.Run, t, 1f, true).RotationDegrees));
            }
            Assert.Greater(zoomiesPeak, normalPeak,
                "Cheddar's zoomies cycle should peak harder than the normal puppy scramble.");
        }

        [Test]
        public void TugAndCarry_GiveCocoaTheGroundedAnchorSilhouette()
        {
            var cheddarTug = DogMotionPersonality.At(DogId.Cheddar,
                DogReadabilityFeedback.Pose.Tug, 0.2f, 0f, false);
            var cocoaTug = DogMotionPersonality.At(DogId.Cocoa,
                DogReadabilityFeedback.Pose.Tug, 0.2f, 0f, false);
            var cheddarCarry = DogMotionPersonality.At(DogId.Cheddar,
                DogReadabilityFeedback.Pose.Carry, 0.2f, 0f, false);
            var cocoaCarry = DogMotionPersonality.At(DogId.Cocoa,
                DogReadabilityFeedback.Pose.Carry, 0.2f, 0f, false);

            Assert.AreEqual("FRANTIC TUG", cheddarTug.Signature);
            Assert.AreEqual("QUEEN ANCHOR", cocoaTug.Signature);
            Assert.Greater(cocoaTug.Scale.x, cheddarTug.Scale.x);
            Assert.Less(cocoaTug.Scale.y, cheddarTug.Scale.y);
            Assert.Greater(Mathf.Abs(cheddarTug.RotationDegrees), Mathf.Abs(cocoaTug.RotationDegrees));
            Assert.AreEqual("BOUNCY LOOT", cheddarCarry.Signature);
            Assert.AreEqual("STEADY HAUL", cocoaCarry.Signature);
            Assert.Greater(Mathf.Abs(cheddarCarry.VerticalOffset), Mathf.Abs(cocoaCarry.VerticalOffset));
        }

        [TestCase(1f, 0f, "E")]
        [TestCase(-1f, 0f, "W")]
        [TestCase(1f, 1f, "NE")]
        [TestCase(-1f, 1f, "NW")]
        [TestCase(1f, -1f, "SE")]
        [TestCase(-1f, -1f, "SW")]
        [TestCase(0f, 1f, "N")]
        [TestCase(0f, -1f, "S")]
        public void FacingLabel_ReportsAllEightReadableDirections(float x, float y, string expected)
        {
            Assert.AreEqual(expected, CharacterMotionArt.FacingLabel(new Vector2(x, y)));
        }
    }
}
