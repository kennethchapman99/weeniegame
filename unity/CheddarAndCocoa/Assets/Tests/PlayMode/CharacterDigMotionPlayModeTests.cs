using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;
using NUnit.Framework;
using UnityEngine;

namespace CheddarAndCocoa.Tests
{
    public sealed class CharacterDigMotionPlayModeTests
    {
        [TestCase(DogId.Cheddar)]
        [TestCase(DogId.Cocoa)]
        public void Dig_HasAuthoredEastAndSouthFrames(DogId dog)
        {
            foreach (CharacterMotionArt.Facing8 facing in new[]
                     { CharacterMotionArt.Facing8.E, CharacterMotionArt.Facing8.S })
            {
                for (int frame = 0; frame < 4; frame++)
                {
                    Sprite sprite = CharacterMotionArt.Load(dog, CharacterMotionArt.Clip.Dig, facing, frame);
                    Assert.IsNotNull(sprite, $"Missing {dog} dig {facing} frame {frame}.");
                    Assert.AreEqual(512, sprite.texture.width);
                    Assert.AreEqual(384, sprite.texture.height);
                }
            }
        }

        [Test]
        public void DigPose_MapsToDigClipAndUsesFourFrameLoop()
        {
            Assert.IsTrue(CharacterMotionArt.TryClip(DogReadabilityFeedback.Pose.Dig, out var clip));
            Assert.AreEqual(CharacterMotionArt.Clip.Dig, clip);
            Assert.AreEqual(0, CharacterMotionArt.FrameAtTime(DogId.Cheddar, clip, 0f));
            Assert.AreEqual(3, CharacterMotionArt.FrameAtTime(DogId.Cheddar, clip, 0.3f));
            Assert.AreEqual(0, CharacterMotionArt.FrameAtTime(DogId.Cheddar, clip, 0.4f));
        }
    }
}
