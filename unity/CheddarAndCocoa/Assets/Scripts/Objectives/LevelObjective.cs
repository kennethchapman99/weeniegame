using UnityEngine;

namespace CheddarAndCocoa.Objectives
{
    /// <summary>Objective kinds — mirrors the prototype mission framework's objective types.</summary>
    public enum ObjectiveKind
    {
        Reach,    // get both dogs to a goal zone
        Collect,  // gather N items together (combined)
        Survive,  // last T seconds (e.g. "Stay Together" hawk mission)
        Escort    // move an item/NPC from A to B
    }

    public enum ObjectiveStatus { Active, Success, Fail }

    /// <summary>
    /// One mission objective. Progress mutates through a SINGLE point (<see cref="AddProgress"/> /
    /// <see cref="Complete"/> / <see cref="Fail"/>) — the prototype's hard-won rule: never scatter
    /// completion flags (mirrors addScore being the one score-mutation point).
    ///
    /// PROTOTYPE MAP: src/systems/mission.ts (completeObjective / addCombined / setProgress,
    /// success-when-all-done + time bonus + 1–3★, fail-on-timeout, retry). balance.ts MISSION
    /// (defaultTime 90, timeBonus 2). A LevelObjectives manager (not stubbed here) owns the set,
    /// the combined score, the star rating, and the success/fail/retry screen.
    /// </summary>
    public sealed class LevelObjective : MonoBehaviour
    {
        [SerializeField] private ObjectiveKind kind = ObjectiveKind.Reach;
        [SerializeField] private string label = "Get to the den together";
        [SerializeField] private int target = 1;       // items to collect / 1 for reach
        [SerializeField] private float surviveSeconds = 45f; // for Survive

        public ObjectiveKind Kind => kind;
        public string Label => label;
        public float SurviveSeconds => surviveSeconds;
        public ObjectiveStatus Status { get; private set; } = ObjectiveStatus.Active;
        public int Progress { get; private set; }

        public event System.Action<LevelObjective> OnChanged;

        /// <summary>The one place progress moves. Auto-completes a Collect objective at target.</summary>
        public void AddProgress(int amount = 1)
        {
            if (Status != ObjectiveStatus.Active) return;
            Progress = Mathf.Clamp(Progress + amount, 0, target);
            OnChanged?.Invoke(this);
            if (kind == ObjectiveKind.Collect && Progress >= target) Complete();
        }

        public void Complete()
        {
            if (Status != ObjectiveStatus.Active) return;
            Status = ObjectiveStatus.Success;
            OnChanged?.Invoke(this);
        }

        public void Fail()
        {
            if (Status != ObjectiveStatus.Active) return;
            Status = ObjectiveStatus.Fail;
            OnChanged?.Invoke(this);
        }

        // TODO: a Survive objective ticks surviveSeconds down in the manager and calls Complete()
        // at zero (unless a fail condition — the hawk grab — calls Fail() first).
    }
}
