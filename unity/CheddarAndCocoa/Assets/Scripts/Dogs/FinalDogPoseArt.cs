using CheddarAndCocoa.Game;
using UnityEngine;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>
    /// Stable Resources path contract for final transparent Cheddar/Cocoa gameplay pose sprites.
    /// </summary>
    public static class FinalDogPoseArt
    {
        public const string Root = "ArenaFinal/Characters/Dogs";

        public static string PathFor(DogId dog, DogReadabilityFeedback.Pose pose)
        {
            string folder = dog == DogId.Cheddar ? "Cheddar" : "Cocoa";
            string prefix = dog == DogId.Cheddar ? "cheddar" : "cocoa";
            return $"{Root}/{folder}/{prefix}_{PoseSuffix(pose)}";
        }

        public static Sprite Load(DogId dog, DogReadabilityFeedback.Pose pose)
        {
            return Resources.Load<Sprite>(PathFor(dog, pose));
        }

        /// <summary>Compatibility alias for the motion layer; identical to <see cref="Load"/>.</summary>
        public static Sprite For(DogId dog, DogReadabilityFeedback.Pose pose) => Load(dog, pose);

        private static string PoseSuffix(DogReadabilityFeedback.Pose pose)
        {
            switch (pose)
            {
                case DogReadabilityFeedback.Pose.Run: return "run";
                case DogReadabilityFeedback.Pose.Bark: return "bark";
                case DogReadabilityFeedback.Pose.Tug: return "tug";
                case DogReadabilityFeedback.Pose.Stunned: return "stunned";
                case DogReadabilityFeedback.Pose.Rescued: return "rescued";
                case DogReadabilityFeedback.Pose.Proud: return "proud";
                case DogReadabilityFeedback.Pose.Sad: return "sad";
                default: return "idle";
            }
        }
    }
}
