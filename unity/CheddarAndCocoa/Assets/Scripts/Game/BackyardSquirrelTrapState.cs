using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Deterministic state for Backyard Rescue's two-pass squirrel trap. The first pass teaches
    /// Cheddar pressure / Cocoa gap control; the second reverses both roles. The dog that did not
    /// cause the redirect is the only dog allowed to recover the dropped weenie.
    /// </summary>
    public sealed class BackyardSquirrelTrapState
    {
        public enum RedirectResult { Success, WrongPressureDog, GapOpen, AlreadyDropped, Complete }
        public enum RecoveryResult { Success, NoDroppedWeenie, WrongDog, Complete }

        public const int RequiredRecoveries = 2;

        public int Recoveries { get; private set; }
        public int Redirects { get; private set; }
        public int Fumbles { get; private set; }
        public bool WeenieDropped { get; private set; }
        public bool Complete => Recoveries >= RequiredRecoveries;
        public DogId PressureDog => Recoveries == 0 ? DogId.Cheddar : DogId.Cocoa;
        public DogId GapDog => Recoveries == 0 ? DogId.Cocoa : DogId.Cheddar;
        public DogId RecoveryDog => GapDog;

        public RedirectResult TryRedirect(DogId pressureDog, bool gapHeld)
        {
            if (Complete) return RedirectResult.Complete;
            if (WeenieDropped) return RedirectResult.AlreadyDropped;
            if (pressureDog != PressureDog)
            {
                Fumbles++;
                return RedirectResult.WrongPressureDog;
            }
            if (!gapHeld)
            {
                Fumbles++;
                return RedirectResult.GapOpen;
            }

            Redirects++;
            WeenieDropped = true;
            return RedirectResult.Success;
        }

        public RecoveryResult TryRecover(DogId dog)
        {
            if (Complete) return RecoveryResult.Complete;
            if (!WeenieDropped) return RecoveryResult.NoDroppedWeenie;
            if (dog != RecoveryDog)
            {
                Fumbles++;
                return RecoveryResult.WrongDog;
            }

            Recoveries++;
            WeenieDropped = false;
            return RecoveryResult.Success;
        }

        public void Reset()
        {
            Recoveries = 0;
            Redirects = 0;
            Fumbles = 0;
            WeenieDropped = false;
        }
    }
}
