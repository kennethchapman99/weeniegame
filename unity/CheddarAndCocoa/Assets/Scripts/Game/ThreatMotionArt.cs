using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Stable Resources path contract for animated non-dog mission characters.</summary>
    public static class ThreatMotionArt
    {
        public enum Actor { Unknown, Squirrel, Eagle, Coyote, Skunk }
        public enum Clip { Idle, Run, Steal, Scared, Sweep, Attack, Patrol, Threaten, Retreat }

        public static string ResourcePath(Actor actor, Clip clip, int frame)
        {
            string folder = actor.ToString();
            string prefix = actor.ToString().ToLowerInvariant();
            return $"{FinalGameplayArt.Root}/Characters/{folder}/Motion/" +
                   $"{prefix}_{clip.ToString().ToLowerInvariant()}_e_{Mathf.Max(0, frame):00}";
        }

        public static Sprite Load(Actor actor, Clip clip, int frame) =>
            actor == Actor.Unknown ? null : FinalGameplayArt.Load(ResourcePath(actor, clip, frame));

        public static int FrameAtTime(Actor actor, Clip clip, float elapsedSeconds)
        {
            int frame = Mathf.Max(0, Mathf.FloorToInt(FractionalFrameAtTime(actor, clip, elapsedSeconds)));
            int count = FrameCount(actor, clip);
            return count <= 1 ? 0 : frame % count;
        }

        /// <summary>Continuous frame position; the integer part is the FrameAtTime frame.</summary>
        public static float FractionalFrameAtTime(Actor actor, Clip clip, float elapsedSeconds) =>
            Mathf.Max(0f, elapsedSeconds) * FramesPerSecond(actor, clip);

        /// <summary>How far the current frame has progressed toward the next one, 0-1.</summary>
        public static float SubFrameFractionAtTime(Actor actor, Clip clip, float elapsedSeconds)
        {
            float fractionalFrame = FractionalFrameAtTime(actor, clip, elapsedSeconds);
            return fractionalFrame - Mathf.Floor(fractionalFrame);
        }

        /// <summary>Continuous 0-1 phase through one full loop of the strip, for procedural poses.</summary>
        public static float CyclePhaseAtTime(Actor actor, Clip clip, float elapsedSeconds)
        {
            int count = Mathf.Max(1, FrameCount(actor, clip));
            return Mathf.Repeat(FractionalFrameAtTime(actor, clip, elapsedSeconds) / count, 1f);
        }

        private static float FramesPerSecond(Actor actor, Clip clip) => (actor, clip) switch
        {
            (Actor.Squirrel, Clip.Run) => 10f,
            (Actor.Squirrel, Clip.Steal) => 8f,
            (Actor.Squirrel, Clip.Scared) => 7f,
            (Actor.Eagle, Clip.Attack) => 9f,
            (Actor.Eagle, Clip.Sweep) => 7f,
            (Actor.Coyote, Clip.Threaten) => 8f,
            (Actor.Coyote, Clip.Retreat) => 9f,
            (Actor.Coyote, Clip.Patrol) => 6f,
            (Actor.Skunk, Clip.Threaten) => 8f,
            (Actor.Skunk, Clip.Retreat) => 6f,
            (Actor.Skunk, Clip.Patrol) => 5f,
            _ => 4f
        };

        public static int FrameCount(Actor actor, Clip clip) =>
            actor == Actor.Squirrel && clip == Clip.Scared ? 2 : 4;

        public static bool TryInfer(string label, Actor defaultActor, out Actor actor, out Clip clip)
        {
            string upper = string.IsNullOrEmpty(label) ? string.Empty : label.ToUpperInvariant();
            actor = defaultActor;

            if (upper.Contains("EAGLE") || upper.Contains("SHADOW") || upper.Contains("TALON"))
                actor = Actor.Eagle;
            else if (upper.Contains("COYOTE"))
                actor = Actor.Coyote;
            else if (upper.Contains("SQUIRREL"))
                actor = Actor.Squirrel;
            else if (upper.Contains("SKUNK") || upper.Contains("TAIL UP"))
                actor = Actor.Skunk;

            if (actor == Actor.Squirrel)
            {
                // Deliberately gated on the literal word, not just defaultActor: the shared squirrel
                // actor gets repurposed as a non-squirrel marker in other missions (Coyotes Fence's
                // dirt/weak-spot, Eagle Shadow Panic's ally-huddle marker), and those must keep
                // falling back to the static placeholder rather than idle-breathing like a squirrel.
                // Every genuine Squirrel Conspiracy/Backyard Rescue/Snack Heist label says the word.
                if (!upper.Contains("SQUIRREL"))
                {
                    clip = default;
                    return false;
                }
                clip = upper.Contains("STEAL") || upper.Contains("HEIST") || upper.Contains("STOLE") || upper.Contains("WEENIE")
                    ? Clip.Steal
                    : upper.Contains("SCARED") || upper.Contains("DROPPED") || upper.Contains("FAKE")
                        ? Clip.Scared
                        : upper.Contains("ROUTE") || upper.Contains("HERD") || upper.Contains("CONSPIRACY") || upper.Contains("TAUNT")
                            ? Clip.Run
                            : Clip.Idle;
                return true;
            }

            if (actor == Actor.Eagle)
            {
                clip = upper.Contains("TALON") || upper.Contains("GRIP") || upper.Contains("SNATCH") || upper.Contains("PULL")
                    ? Clip.Attack
                    : Clip.Sweep;
                return true;
            }

            if (actor == Actor.Coyote)
            {
                clip = upper.Contains("DRIVEN BACK") || upper.Contains("BLOCKED") || upper.Contains("RETREAT")
                    ? Clip.Retreat
                    : upper.Contains("BARK") || upper.Contains("PRESSURE") || upper.Contains("BREACH") ||
                      upper.Contains("LURE") || upper.Contains("BAIT")
                        ? Clip.Threaten
                        : Clip.Patrol;
                return true;
            }

            if (actor == Actor.Skunk)
            {
                // Skunk Blast Mayhem's SetActorState labels: "TAIL UP" is the spray telegraph
                // (Threaten reuses its urgent, fast-cycling clip for the tail-lift raise), "GIVES
                // UP"/retreat covers the post-heist retreat, everything else (calm guard/lured
                // watch) is the idle patrol loop.
                clip = upper.Contains("TAIL UP")
                    ? Clip.Threaten
                    : upper.Contains("GIVES UP") || upper.Contains("RETREAT")
                        ? Clip.Retreat
                        : Clip.Patrol;
                return true;
            }

            clip = default;
            return false;
        }
    }
}
