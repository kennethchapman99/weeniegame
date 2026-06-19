using UnityEngine;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>Maps dog identity/pose state to extracted transparent final sprite resources.</summary>
    public static class FinalDogPoseArt
    {
        public static Sprite For(DogId dog, DogReadabilityFeedback.Pose pose)
        {
            string dogPath = dog == DogId.Cheddar ? "Cheddar/cheddar_" : "Cocoa/cocoa_";
            string posePath = pose switch
            {
                DogReadabilityFeedback.Pose.Idle => "idle",
                DogReadabilityFeedback.Pose.Run => "run",
                DogReadabilityFeedback.Pose.Bark => "bark",
                DogReadabilityFeedback.Pose.Tug => "tug",
                DogReadabilityFeedback.Pose.Stunned => "stunned",
                DogReadabilityFeedback.Pose.Rescued => "rescued",
                DogReadabilityFeedback.Pose.Proud => "proud",
                DogReadabilityFeedback.Pose.Sad => "sad",
                _ => "idle"
            };
            return FinalGameplayArt.Load($"{FinalGameplayArt.Root}Characters/Dogs/{dogPath}{posePath}");
        }
    }
}
