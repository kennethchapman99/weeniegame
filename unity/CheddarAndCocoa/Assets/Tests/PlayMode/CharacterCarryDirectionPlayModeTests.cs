using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;
using NUnit.Framework;
using UnityEngine;

namespace CheddarAndCocoa.Tests
{
    public sealed class CharacterCarryDirectionPlayModeTests
    {
        [TestCase(DogId.Cheddar)]
        [TestCase(DogId.Cocoa)]
        public void Carry_HasAuthoredStraightFrames(DogId dog)
        {
            foreach (CharacterMotionArt.Facing8 facing in new[]
                     { CharacterMotionArt.Facing8.S, CharacterMotionArt.Facing8.N })
            {
                for (int frame = 0; frame < 2; frame++)
                {
                    Sprite sprite = CharacterMotionArt.Load(dog, CharacterMotionArt.Clip.Carry, facing, frame);
                    Assert.IsNotNull(sprite, $"Missing {dog} carry {facing} frame {frame}.");
                    Assert.AreEqual(512, sprite.texture.width);
                    Assert.AreEqual(384, sprite.texture.height);
                }
            }
        }

        [Test]
        public void Carry_VerticalFacingSelectsStraightArtWithoutMirroring()
        {
            Assert.AreEqual(CharacterMotionArt.Facing8.N,
                CharacterMotionArt.FacingForDirection(Vector2.up, out bool northMirror));
            Assert.IsFalse(northMirror);
            Assert.AreEqual(CharacterMotionArt.Facing8.S,
                CharacterMotionArt.FacingForDirection(Vector2.down, out bool southMirror));
            Assert.IsFalse(southMirror);
        }
    }
}
