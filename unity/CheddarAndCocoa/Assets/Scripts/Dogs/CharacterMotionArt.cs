using CheddarAndCocoa.Game;
using UnityEngine;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>Stable path contract for direction-aware dog frames with single-pose fallback.</summary>
    public static class CharacterMotionArt
    {
        public enum Facing8 { E, SE, S, SW, W, NW, N, NE }
        public enum Clip
        {
            Idle, Run, RunStorybook, Bark, BarkStorybook, Tug, Dig, Carry, Herd, Hide,
            Comfort, Groom, Stunned, Rescued, Proud, Sad, Sniff, Jump, Wrestle, Interact,
            Swim, Flop, Beg, HeadTilt, PawTap, PushPull, WetShake, Trapped, Sleepy
        }

        public static string ResourcePath(DogId dog, Clip clip, Facing8 facing, int frame)
        {
            string dogName = dog == DogId.Cheddar ? "cheddar" : "cocoa";
            string folder = dog == DogId.Cheddar ? "Cheddar" : "Cocoa";
            string clipName = clip switch
            {
                Clip.RunStorybook => "run_storybook",
                Clip.BarkStorybook => "bark_storybook",
                _ => clip.ToString().ToLowerInvariant()
            };
            return $"{FinalGameplayArt.Root}/Characters/Dogs/{folder}/Motion/" +
                   $"{dogName}_{clipName}_{facing.ToString().ToLowerInvariant()}_{Mathf.Max(0, frame):00}";
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
                case DogReadabilityFeedback.Pose.Run: clip = Clip.RunStorybook; return true;
                case DogReadabilityFeedback.Pose.Bark: clip = Clip.BarkStorybook; return true;
                case DogReadabilityFeedback.Pose.Tug: clip = Clip.Tug; return true;
                case DogReadabilityFeedback.Pose.Dig: clip = Clip.Dig; return true;
                case DogReadabilityFeedback.Pose.Carry: clip = Clip.Carry; return true;
                case DogReadabilityFeedback.Pose.Stunned: clip = Clip.Stunned; return true;
                case DogReadabilityFeedback.Pose.Rescued: clip = Clip.Rescued; return true;
                case DogReadabilityFeedback.Pose.Proud: clip = Clip.Proud; return true;
                case DogReadabilityFeedback.Pose.Sad: clip = Clip.Sad; return true;
                case DogReadabilityFeedback.Pose.Sniff: clip = Clip.Sniff; return true;
                case DogReadabilityFeedback.Pose.Jump: clip = Clip.Jump; return true;
                case DogReadabilityFeedback.Pose.Wrestle: clip = Clip.Wrestle; return true;
                case DogReadabilityFeedback.Pose.Interact: clip = Clip.Interact; return true;
                case DogReadabilityFeedback.Pose.Swim: clip = Clip.Swim; return true;
                case DogReadabilityFeedback.Pose.Comfort: clip = Clip.Comfort; return true;
                case DogReadabilityFeedback.Pose.Flop: clip = Clip.Flop; return true;
                case DogReadabilityFeedback.Pose.Beg: clip = Clip.Beg; return true;
                case DogReadabilityFeedback.Pose.HeadTilt: clip = Clip.HeadTilt; return true;
                case DogReadabilityFeedback.Pose.PawTap: clip = Clip.PawTap; return true;
                case DogReadabilityFeedback.Pose.PushPull: clip = Clip.PushPull; return true;
                case DogReadabilityFeedback.Pose.Hide: clip = Clip.Hide; return true;
                case DogReadabilityFeedback.Pose.WetShake: clip = Clip.WetShake; return true;
                case DogReadabilityFeedback.Pose.Trapped: clip = Clip.Trapped; return true;
                case DogReadabilityFeedback.Pose.Sleepy: clip = Clip.Sleepy; return true;
                default: clip = default; return false;
            }
        }

        public static int FrameAtTime(DogId dog, Clip clip, float elapsedSeconds)
        {
            float fps = clip switch
            {
                Clip.Idle => dog == DogId.Cheddar ? 3.2f : 2.5f,
                Clip.Run => dog == DogId.Cheddar ? 10f : 8.5f,
                Clip.RunStorybook => dog == DogId.Cheddar ? 10f : 8f,
                Clip.Bark => 11f,
                Clip.BarkStorybook => dog == DogId.Cheddar ? 10f : 7f,
                Clip.Tug => dog == DogId.Cheddar ? 9f : 7f,
                Clip.Dig => dog == DogId.Cheddar ? 10f : 8f,
                Clip.Carry => dog == DogId.Cheddar ? 5f : 4f,
                Clip.Groom => dog == DogId.Cheddar ? 7f : 5.5f,
                Clip.Stunned => 6f,
                Clip.Rescued => dog == DogId.Cheddar ? 6f : 4.5f,
                Clip.Proud => dog == DogId.Cheddar ? 5f : 3.5f,
                Clip.Sad => 2.5f,
                Clip.Sniff => dog == DogId.Cheddar ? 4f : 3f, // deliberate/investigative, slower than dig
                Clip.Jump => dog == DogId.Cheddar ? 11f : 8.5f,
                Clip.Wrestle => dog == DogId.Cheddar ? 12f : 9f,
                Clip.Interact => dog == DogId.Cheddar ? 13f : 10f,
                Clip.WetShake => dog == DogId.Cheddar ? 12f : 9f,
                _ => 1f
            };
            int frame = Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) * fps));
            if (IsCompletionPose(clip)) return 0;
            if (clip == Clip.Bark) return Mathf.Min(3, frame);
            if (clip == Clip.Jump || clip == Clip.Wrestle || clip == Clip.Interact)
                return Mathf.Min(3, frame);
            int frameCount = clip == Clip.Tug ? 3 :
                clip == Clip.BarkStorybook ? 2 :
                clip == Clip.Groom ? 2 :
                clip == Clip.Carry || clip == Clip.Stunned || clip == Clip.Rescued || clip == Clip.Proud || clip == Clip.Sad ? 2 : 4;
            return frame % frameCount;
        }

        private static bool IsCompletionPose(Clip clip) =>
            clip == Clip.Swim || clip == Clip.Comfort || clip == Clip.Flop ||
            clip == Clip.Beg || clip == Clip.HeadTilt || clip == Clip.PawTap ||
            clip == Clip.PushPull || clip == Clip.Hide || clip == Clip.WetShake ||
            clip == Clip.Trapped || clip == Clip.Sleepy;

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

        public static string FacingLabel(Vector2 direction)
        {
            Facing8 facing = FacingForDirection(direction, out bool mirror);
            if (!mirror) return facing.ToString();
            return facing switch
            {
                Facing8.E => Facing8.W.ToString(),
                Facing8.NE => Facing8.NW.ToString(),
                Facing8.SE => Facing8.SW.ToString(),
                _ => facing.ToString()
            };
        }

        private static DogReadabilityFeedback.Pose FallbackPose(Clip clip) => clip switch
        {
            Clip.Run => DogReadabilityFeedback.Pose.Run,
            Clip.RunStorybook => DogReadabilityFeedback.Pose.Run,
            Clip.Bark => DogReadabilityFeedback.Pose.Bark,
            Clip.BarkStorybook => DogReadabilityFeedback.Pose.Bark,
            Clip.Tug => DogReadabilityFeedback.Pose.Tug,
            Clip.Dig => DogReadabilityFeedback.Pose.Dig,
            Clip.Carry => DogReadabilityFeedback.Pose.Carry,
            Clip.Stunned => DogReadabilityFeedback.Pose.Stunned,
            Clip.Rescued => DogReadabilityFeedback.Pose.Rescued,
            Clip.Proud => DogReadabilityFeedback.Pose.Proud,
            Clip.Sad => DogReadabilityFeedback.Pose.Sad,
            Clip.Sniff => DogReadabilityFeedback.Pose.Sniff,
            Clip.Jump => DogReadabilityFeedback.Pose.Jump,
            Clip.Wrestle => DogReadabilityFeedback.Pose.Wrestle,
            Clip.Interact => DogReadabilityFeedback.Pose.Interact,
            Clip.Swim => DogReadabilityFeedback.Pose.Swim,
            Clip.Comfort => DogReadabilityFeedback.Pose.Comfort,
            Clip.Flop => DogReadabilityFeedback.Pose.Flop,
            Clip.Beg => DogReadabilityFeedback.Pose.Beg,
            Clip.HeadTilt => DogReadabilityFeedback.Pose.HeadTilt,
            Clip.PawTap => DogReadabilityFeedback.Pose.PawTap,
            Clip.PushPull => DogReadabilityFeedback.Pose.PushPull,
            Clip.Hide => DogReadabilityFeedback.Pose.Hide,
            Clip.WetShake => DogReadabilityFeedback.Pose.WetShake,
            Clip.Trapped => DogReadabilityFeedback.Pose.Trapped,
            Clip.Sleepy => DogReadabilityFeedback.Pose.Sleepy,
            _ => DogReadabilityFeedback.Pose.Idle
        };
    }
}
