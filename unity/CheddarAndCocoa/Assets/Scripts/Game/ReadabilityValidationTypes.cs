using System;

namespace CheddarAndCocoa.Game
{
    [Flags]
    public enum ReadabilityRequirement
    {
        None = 0,
        ObjectiveVisible = 1,
        ScoreVisible = 2,
        RoleLabelVisible = 4,
        WarningVisible = 8,
        ReplayVisible = 16,
        DogIdentityReadable = 32
    }

    public readonly struct ReadabilityValidationResult
    {
        public readonly ReadabilityRequirement Missing;
        public bool Passed => Missing == ReadabilityRequirement.None;

        public ReadabilityValidationResult(ReadabilityRequirement missing)
        {
            Missing = missing;
        }
    }

    public static class ReadabilityValidator
    {
        public static ReadabilityValidationResult Validate(ReadabilityRequirement present, ReadabilityRequirement required)
        {
            return new ReadabilityValidationResult(required & ~present);
        }

        public static ReadabilityValidationResult ValidateMissionDefinition(GameManager.MissionDefinition definition)
        {
            if (definition == null)
                return new ReadabilityValidationResult(ReadabilityRequirement.ObjectiveVisible |
                                                       ReadabilityRequirement.ScoreVisible |
                                                       ReadabilityRequirement.RoleLabelVisible |
                                                       ReadabilityRequirement.ReplayVisible |
                                                       ReadabilityRequirement.DogIdentityReadable);

            var present = ReadabilityRequirement.None;
            if (!string.IsNullOrWhiteSpace(definition.IntroPrompt) &&
                !string.IsNullOrWhiteSpace(definition.CollectObjectiveFormat) &&
                !string.IsNullOrWhiteSpace(definition.WaitingObjectiveText))
                present |= ReadabilityRequirement.ObjectiveVisible;
            if (!string.IsNullOrWhiteSpace(definition.ReadyScoreLabel) &&
                !string.IsNullOrWhiteSpace(definition.ClearScoreLabel))
                present |= ReadabilityRequirement.ScoreVisible;
            if (!string.IsNullOrWhiteSpace(definition.RoleHint) &&
                !string.IsNullOrWhiteSpace(definition.MechanicTag) &&
                !string.IsNullOrWhiteSpace(definition.SceneCue))
                present |= ReadabilityRequirement.RoleLabelVisible;
            if (!string.IsNullOrWhiteSpace(definition.SquirrelStealingCue) &&
                !string.IsNullOrWhiteSpace(definition.SquirrelMissPopLabel))
                present |= ReadabilityRequirement.WarningVisible;
            if (!string.IsNullOrWhiteSpace(definition.ReplayPrompt) &&
                !string.IsNullOrWhiteSpace(definition.ClearObjectiveText) &&
                !string.IsNullOrWhiteSpace(definition.FailObjectiveText))
                present |= ReadabilityRequirement.ReplayVisible;
            if (!string.IsNullOrWhiteSpace(definition.ReusablePresentation) &&
                definition.ReusablePresentation.Contains("Cheddar") &&
                definition.ReusablePresentation.Contains("Cocoa"))
                present |= ReadabilityRequirement.DogIdentityReadable;

            return Validate(present, definition.RequiredReadability);
        }
    }
}
