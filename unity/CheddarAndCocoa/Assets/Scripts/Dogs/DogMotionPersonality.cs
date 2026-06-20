using UnityEngine;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>
    /// Deterministic procedural motion layered over authored dog frames. The authored sprites carry
    /// likeness; this profile makes Cheddar read as springy chaos and Cocoa as a grounded queen even
    /// when both dogs are using the same pose family.
    /// </summary>
    public static class DogMotionPersonality
    {
        public readonly struct Sample
        {
            public Sample(Vector2 scale, float rotationDegrees, float verticalOffset, string signature)
            {
                Scale = scale;
                RotationDegrees = rotationDegrees;
                VerticalOffset = verticalOffset;
                Signature = signature;
            }

            public Vector2 Scale { get; }
            public float RotationDegrees { get; }
            public float VerticalOffset { get; }
            public string Signature { get; }
        }

        public static Sample At(DogId dog, DogReadabilityFeedback.Pose pose, float time,
            float speed01, bool zoomies)
        {
            speed01 = Mathf.Clamp01(speed01);
            return dog == DogId.Cheddar
                ? Cheddar(pose, time, speed01, zoomies)
                : Cocoa(pose, time, speed01);
        }

        private static Sample Cheddar(DogReadabilityFeedback.Pose pose, float t, float speed01, bool zoomies)
        {
            float chaos = zoomies ? 1.65f : 1f;
            switch (pose)
            {
                case DogReadabilityFeedback.Pose.Idle:
                    return Make(1f + Wave(t, 7.5f) * 0.025f, 1f - Wave(t, 7.5f) * 0.018f,
                        Wave(t, 5.5f) * 1.8f, Mathf.Abs(Wave(t, 7.5f)) * 0.025f, "CHAOS-PUPPY READY");
                case DogReadabilityFeedback.Pose.Run:
                    return Make(1f + speed01 * 0.08f, 1f - speed01 * 0.045f,
                        Wave(t, 18f * chaos) * 4.5f * chaos,
                        Mathf.Abs(Wave(t, 18f * chaos)) * 0.07f * chaos, zoomies ? "ZOOMIES SCRAMBLE" : "PUPPY SCRAMBLE");
                case DogReadabilityFeedback.Pose.Bark:
                    return Make(1.16f, 1.12f, Wave(t, 30f) * 8f,
                        Mathf.Abs(Wave(t, 30f)) * 0.055f, "YAPPY LAUNCH");
                case DogReadabilityFeedback.Pose.Tug:
                    return Make(1.08f, 0.96f, Wave(t, 22f) * 6f, 0.015f, "FRANTIC TUG");
                case DogReadabilityFeedback.Pose.Carry:
                    return Make(1.04f, 1f, Wave(t, 11f) * 2.5f,
                        Mathf.Abs(Wave(t, 11f)) * 0.045f, "BOUNCY LOOT");
                case DogReadabilityFeedback.Pose.Proud:
                case DogReadabilityFeedback.Pose.Rescued:
                    return Make(1.1f, 1.1f, Wave(t, 6f) * 2.5f,
                        Mathf.Abs(Wave(t, 6f)) * 0.035f, "PUPPY VICTORY");
                case DogReadabilityFeedback.Pose.Sad:
                    return Make(1.05f, 0.88f, -7f, -0.035f, "DRAMATIC FLOP");
                case DogReadabilityFeedback.Pose.Stunned:
                    return Make(0.94f, 0.84f, 12f + Wave(t, 9f) * 2f, -0.025f, "PUPPY SPINOUT");
                default:
                    return Make(1f, 1f, 0f, 0f, "CHAOS-PUPPY");
            }
        }

        private static Sample Cocoa(DogReadabilityFeedback.Pose pose, float t, float speed01)
        {
            switch (pose)
            {
                case DogReadabilityFeedback.Pose.Idle:
                    return Make(1.025f, 0.985f + Wave(t, 2.4f) * 0.012f,
                        Wave(t, 2.4f) * 0.6f, 0f, "QUEENLY WATCH");
                case DogReadabilityFeedback.Pose.Run:
                    return Make(1f + speed01 * 0.055f, 0.96f - speed01 * 0.025f,
                        Wave(t, 12f) * 1.8f, Mathf.Abs(Wave(t, 12f)) * 0.028f, "VETERAN TROT");
                case DogReadabilityFeedback.Pose.Bark:
                    return Make(1.2f, 0.98f, Wave(t, 18f) * 3f, 0f, "ROYAL COMMAND");
                case DogReadabilityFeedback.Pose.Tug:
                    return Make(1.18f, 0.9f, Wave(t, 9f) * 1.5f, -0.025f, "QUEEN ANCHOR");
                case DogReadabilityFeedback.Pose.Carry:
                    return Make(1.1f, 0.96f, Wave(t, 5f) * 0.8f, 0f, "STEADY HAUL");
                case DogReadabilityFeedback.Pose.Proud:
                case DogReadabilityFeedback.Pose.Rescued:
                    return Make(1.12f, 1.03f, Wave(t, 3f) * 0.8f, 0.015f, "REGAL VICTORY");
                case DogReadabilityFeedback.Pose.Sad:
                    return Make(1.1f, 0.9f, -3f, -0.025f, "DIGNIFIED SULK");
                case DogReadabilityFeedback.Pose.Stunned:
                    return Make(1.04f, 0.88f, 7f + Wave(t, 5f), -0.02f, "RUFFLED QUEEN");
                default:
                    return Make(1f, 1f, 0f, 0f, "VETERAN-QUEEN");
            }
        }

        private static Sample Make(float x, float y, float rotation, float verticalOffset, string signature) =>
            new(new Vector2(x, y), rotation, verticalOffset, signature);

        private static float Wave(float time, float frequency) => Mathf.Sin(time * frequency);
    }
}
