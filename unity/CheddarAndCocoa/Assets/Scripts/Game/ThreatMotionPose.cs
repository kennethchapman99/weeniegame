using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Pure procedural pose layer for the four-frame threat strips. The authored frames carry the
    /// silhouette; these poses add the between-frame life the couch tests said the cutouts were
    /// missing: coyote gait/lunge/back-pedal, squirrel hop/snatch/tremble, and eagle wing-synced
    /// lift and dive stretch. Every pose is a pure function of the clip cycle phase so PlayMode
    /// tests can pin the math without driving real time.
    /// </summary>
    public static class ThreatMotionPose
    {
        public readonly struct Pose
        {
            public Pose(Vector2 offset, float rotationDegrees, Vector2 scale)
            {
                Offset = offset;
                RotationDegrees = rotationDegrees;
                Scale = scale;
            }

            public Vector2 Offset { get; }
            public float RotationDegrees { get; }
            public Vector2 Scale { get; }

            public static Pose Identity => new Pose(Vector2.zero, 0f, Vector2.one);
        }

        // Shared bounds contract: offsets stay small enough that labels, range rings, and
        // colliders keep lining up over the actor; rotation stays a lean rather than a spin; any
        // stretch is volume-preserving and never approaches the old half-height skew bug.
        public const float MaxOffset = 0.2f;
        public const float MaxRotationDegrees = 20f;
        public const float MaxStretch = 1.15f;

        private const float TwoPi = Mathf.PI * 2f;

        public static Pose Evaluate(ThreatMotionArt.Actor actor, ThreatMotionArt.Clip clip,
            float cyclePhase, bool mirrored)
        {
            float p = Mathf.Repeat(cyclePhase, 1f);
            float face = mirrored ? -1f : 1f;

            switch (actor, clip)
            {
                case (ThreatMotionArt.Actor.Eagle, ThreatMotionArt.Clip.Sweep):
                {
                    // Lift synced to the flap cycle: the glide bob rises with the downstroke
                    // frames instead of drifting on its own clock. Bank and depth scale are
                    // layered on by the animator from this offset.
                    float lift = Mathf.Sin(p * TwoPi) * 0.07f;
                    return new Pose(new Vector2(0f, lift), 0f, Vector2.one);
                }
                case (ThreatMotionArt.Actor.Eagle, ThreatMotionArt.Clip.Attack):
                {
                    // Talon pounce: a sharp drop with a vertical dive stretch, then recover.
                    float strike = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(p * TwoPi)), 3f);
                    float s = 0.08f * strike;
                    return new Pose(new Vector2(0f, -0.09f * strike), face * -4f * strike,
                        new Vector2(1f / (1f + s), 1f + s));
                }
                case (ThreatMotionArt.Actor.Coyote, ThreatMotionArt.Clip.Patrol):
                {
                    // Prowl gait: two footfall bounces per strip cycle with a slow pace sway.
                    float gait = Mathf.Abs(Mathf.Sin(p * TwoPi * 2f));
                    float sway = Mathf.Sin(p * TwoPi) * 2f;
                    return new Pose(new Vector2(0f, 0.045f * gait), face * sway, Vector2.one);
                }
                case (ThreatMotionArt.Actor.Coyote, ThreatMotionArt.Clip.Threaten):
                {
                    // Coil back and down, then snap forward toward the fence with a lunge
                    // stretch. Lunge peaks in the first half of the cycle, the coil in the second.
                    float lunge = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(p * TwoPi)), 2f);
                    float coil = Mathf.Max(0f, Mathf.Sin(p * TwoPi + Mathf.PI));
                    float s = 0.07f * lunge;
                    return new Pose(
                        new Vector2(face * (0.11f * lunge - 0.05f * coil),
                            0.02f * lunge - 0.03f * coil),
                        face * -6f * lunge,
                        new Vector2(1f + s, 1f / (1f + s)));
                }
                case (ThreatMotionArt.Actor.Coyote, ThreatMotionArt.Clip.Retreat):
                {
                    // Driven back: weight on the haunches, leaning away from the dogs, with
                    // quick nervous back-pedal steps.
                    float pedal = Mathf.Abs(Mathf.Sin(p * TwoPi * 3f));
                    return new Pose(new Vector2(face * -0.04f, 0.03f * pedal), face * 7f,
                        Vector2.one);
                }
                case (ThreatMotionArt.Actor.Squirrel, ThreatMotionArt.Clip.Idle):
                {
                    // Perky perch: a small breath rise and tail-flick tilt. Scale stays uniform
                    // so the resting silhouette never reads deformed.
                    float breathe = Mathf.Sin(p * TwoPi);
                    return new Pose(new Vector2(0f, 0.012f * Mathf.Max(0f, breathe)),
                        breathe * 1.5f, Vector2.one);
                }
                case (ThreatMotionArt.Actor.Squirrel, ThreatMotionArt.Clip.Run):
                {
                    // Bounding scurry: two hop arcs per cycle, leaning into the travel direction,
                    // stretching along the ground while airborne.
                    float hop = Mathf.Abs(Mathf.Sin(p * TwoPi * 2f));
                    float s = 0.05f * hop;
                    return new Pose(new Vector2(0f, 0.09f * hop), face * -7f * hop,
                        new Vector2(1f + s, 1f / (1f + s)));
                }
                case (ThreatMotionArt.Actor.Squirrel, ThreatMotionArt.Clip.Steal):
                {
                    // Grabby snatch: quick head-down grabs with a greedy wiggle.
                    float grab = Mathf.Sin(p * TwoPi * 2f);
                    return new Pose(
                        new Vector2(face * 0.02f * grab, -0.03f * Mathf.Abs(grab)),
                        face * 3f * grab, Vector2.one);
                }
                case (ThreatMotionArt.Actor.Squirrel, ThreatMotionArt.Clip.Scared):
                {
                    // Cowering tremble: crouched low with a fast side-to-side shiver.
                    float tremble = Mathf.Sin(p * TwoPi * 6f);
                    return new Pose(new Vector2(0.02f * tremble, -0.035f), face * 2f * tremble,
                        Vector2.one);
                }
                case (ThreatMotionArt.Actor.Skunk, ThreatMotionArt.Clip.Patrol):
                {
                    // Calm guard stance: a slow watchful breathing sway, tail low.
                    float sway = Mathf.Sin(p * TwoPi);
                    return new Pose(new Vector2(0f, 0.015f * sway), face * 1.5f * sway, Vector2.one);
                }
                case (ThreatMotionArt.Actor.Skunk, ThreatMotionArt.Clip.Threaten):
                {
                    // Tail-lift telegraph: a building shiver that sharpens as the danger clock
                    // closes in - the authored frames already carry the tail rising, this adds
                    // the urgency between them.
                    float shiver = Mathf.Sin(p * TwoPi * 5f) * Mathf.Clamp01(p * 3f);
                    return new Pose(new Vector2(0f, 0.02f * shiver), face * 3f * shiver, Vector2.one);
                }
                case (ThreatMotionArt.Actor.Skunk, ThreatMotionArt.Clip.Retreat):
                {
                    // Gives up and waddles off: hunched, quick nervous back-pedal steps.
                    float pedal = Mathf.Abs(Mathf.Sin(p * TwoPi * 3f));
                    return new Pose(new Vector2(face * -0.03f, 0.025f * pedal), face * 4f, Vector2.one);
                }
                default:
                    return Pose.Identity;
            }
        }
    }
}
