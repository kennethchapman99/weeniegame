using CheddarAndCocoa.Game;
using UnityEngine;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>Stable path contract for direction-aware dog frames with single-pose fallback.</summary>
    public static class CharacterMotionArt
    {
        public enum Facing8 { E, SE, S, SW, W, NW, N, NE }
        public enum Clip { Idle, Run, Bark, Tug, Dig, Carry, Herd, Hide, Comfort, Stunned, Rescued, Proud, Sad }

        public static string ResourcePath(DogId dog, Clip clip, Facing8 facing, int frame)
        {
            string dogName = dog == DogId.Cheddar ? "cheddar" : "cocoa";
            string folder = dog == DogId.Cheddar ? "Cheddar" : "Cocoa";
            return $"{FinalGameplayArt.Root}Characters/Dogs/{folder}/Motion/" +
                   $"{dogName}_{clip.ToString().ToLowerInvariant()}_{facing.ToString().ToLowerInvariant()}_{Mathf.Max(0, frame):00}";
        }

        public static Sprite Load(DogId dog, Clip clip, Facing8 facing, int frame) =>
            FinalGameplayArt.Load(ResourcePath(dog, clip, facing, frame));

        public static Sprite LoadOrFallback(DogId dog, Clip clip, Facing8 facing, int frame)
        {
            Sprite directional = Load(dog, clip, facing, frame);
            return directional != null ? directional : FinalDogPoseArt.For(dog, FallbackPose(clip));
        }

        private static DogReadabilityFeedback.Pose FallbackPose(Clip clip) => clip switch
        {
            Clip.Run => DogReadabilityFeedback.Pose.Run,
            Clip.Bark => DogReadabilityFeedback.Pose.Bark,
            Clip.Tug => DogReadabilityFeedback.Pose.Tug,
            Clip.Stunned => DogReadabilityFeedback.Pose.Stunned,
            Clip.Rescued => DogReadabilityFeedback.Pose.Rescued,
            Clip.Proud => DogReadabilityFeedback.Pose.Proud,
            Clip.Sad => DogReadabilityFeedback.Pose.Sad,
            _ => DogReadabilityFeedback.Pose.Idle
        };
    }
}
