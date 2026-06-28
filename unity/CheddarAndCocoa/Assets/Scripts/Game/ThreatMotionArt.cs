using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Stable Resources path contract for animated non-dog mission characters.</summary>
    public static class ThreatMotionArt
    {
        public enum Actor { Unknown, Squirrel, Eagle, Coyote }
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
            float fps = (actor, clip) switch
            {
                (Actor.Squirrel, Clip.Run) => 10f,
                (Actor.Squirrel, Clip.Steal) => 8f,
                (Actor.Squirrel, Clip.Scared) => 7f,
                (Actor.Eagle, Clip.Attack) => 9f,
                (Actor.Eagle, Clip.Sweep) => 7f,
                (Actor.Coyote, Clip.Threaten) => 8f,
                (Actor.Coyote, Clip.Retreat) => 9f,
                (Actor.Coyote, Clip.Patrol) => 6f,
                _ => 4f
            };
            int frame = Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) * fps));
            int count = FrameCount(actor, clip);
            return count <= 1 ? 0 : frame % count;
        }

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

            if (actor == Actor.Squirrel)
            {
                if (!upper.Contains("SQUIRREL"))
                {
                    clip = default;
                    return false;
                }
                clip = upper.Contains("STEAL") || upper.Contains("HEIST") || upper.Contains("STOLE")
                    ? Clip.Steal
                    : upper.Contains("SCARED") || upper.Contains("DROPPED") || upper.Contains("FAKE") || upper.Contains("TAUNT")
                        ? Clip.Scared
                        : upper.Contains("ROUTE") || upper.Contains("HERD") || upper.Contains("CONSPIRACY")
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
                clip = upper.Contains("DRIVEN BACK") || upper.Contains("BLOCKED")
                    ? Clip.Retreat
                    : upper.Contains("BARK") || upper.Contains("PRESSURE") || upper.Contains("BREACH") || upper.Contains("LURE")
                        ? Clip.Threaten
                        : Clip.Patrol;
                return true;
            }

            clip = default;
            return false;
        }
    }
}
