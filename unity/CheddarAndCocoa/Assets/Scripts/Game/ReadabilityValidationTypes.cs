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
    }
}
