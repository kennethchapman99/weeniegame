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
            return $"{FinalGameplayArt.Root}/Characters/Dogs/{folder}/Motion/" +
                   $"{dogName}_{clip.ToString().ToLowerInvariant()}_{facing.ToString().ToLowerInvariant()}_{Mathf.Max(0, frame):00}";
        }

        public static Sprite Load(DogId dog, Clip clip, Facing8 facing, int frame) =>
            FinalGameplayArt.Load(ResourcePath(dog, clip, facing, frame));

        public static Sprite LoadOrFallback(DogId dog, Clip clip, Facing8 facing, int frame)
        {
            Sprite directional = Load(dog, clip, facing, frame);
            return directional != null ? directional : FinalDogPoseArt.For(dog, FallbackPose(clip));
        }

        public static bool TryClip(DogReadabilityFeedback.Pose pose, out Clip clip)
        {
            switch (pose)
            {
                case DogReadabilityFeedback.Pose.Idle: clip = Clip.Idle; return true;
                case DogReadabilityFeedback.Pose.Run: clip = Clip.Run; return true;
                case DogReadabilityFeedback.Pose.Bark: clip = Clip.Bark; return true;
                case DogReadabilityFeedback.Pose.Tug: clip = Clip.Tug; return true;
                default: clip = default; return false;
            }
        }

        public static int FrameAtTime(DogId dog, Clip clip, float elapsedSeconds)
        {
            float fps = clip switch
            {
                Clip.Idle => dog == DogId.Cheddar ? 3.2f : 2.5f,
                Clip.Run => dog == DogId.Cheddar ? 10f : 8.5f,
                Clip.Bark => 11f,
                Clip.Tug => dog == DogId.Cheddar ? 9f : 7f,
                _ => 1f
            };
            int frame = Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) * fps));
            if (clip == Clip.Bark) return Mathf.Min(3, frame);
            return frame % (clip == Clip.Tug ? 3 : 4);
        }

        public static Facing8 FacingForDirection(Vector2 direction, out bool mirror)
        {
            float angle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
            mirror = direction.x < 0f;
            if (angle >= 67.5f) return Facing8.N;
            if (angle <= -67.5f) return Facing8.S;
            if (angle >= 22.5f) return Facing8.NE;
            if (angle <= -22.5f) return Facing8.SE;
            return Facing8.E;
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
