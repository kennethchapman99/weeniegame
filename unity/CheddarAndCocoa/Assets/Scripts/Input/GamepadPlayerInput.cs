using UnityEngine;
using UnityEngine.InputSystem;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Input
{
    /// <summary>
    /// Reads one player's gamepad each frame and feeds a <see cref="DogController.MoveIntent"/> to
    /// its dog. Couch co-op: two of these exist, each paired (PlayerInput device pairing) to a
    /// distinct controller — P1 drives Cheddar, P2 drives Cocoa (assignable in the lobby).
    ///
    /// PROTOTYPE MAP: src/core/gamepad.ts (GamepadSource per slot) + src/core/input.ts
    /// (computeIntent: stick -> ax/ay/arrive; A/B -> wrestle/jump). The deadzone below mirrors
    /// balance.ts INPUT.gamepadDeadzone = 0.25.
    ///
    /// Recommended wiring: a PlayerInputManager ("join players" / "press a button to join")
    /// instantiates a dog prefab per controller; this script lives on that prefab and resolves
    /// its <see cref="DogController"/> locally. Use the Input System (not legacy Input).
    /// </summary>
    [RequireComponent(typeof(DogController))]
    public sealed class GamepadPlayerInput : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.9f)] private float deadzone = 0.25f; // balance.ts INPUT.gamepadDeadzone
        [Tooltip("Leave null to use the most-recently-paired gamepad; set for explicit P1/P2 assignment.")]
        [SerializeField] private int gamepadSlot = -1;

        private DogController _dog;

        private void Awake() => _dog = GetComponent<DogController>();

        // Read in FixedUpdate so intent lands on the same cadence as DogController.Tick (the
        // prototype reads the pad in the fixed-step input phase, never in render).
        private void FixedUpdate()
        {
            Gamepad pad = ResolvePad();
            if (pad == null) return;

            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.magnitude < deadzone) stick = Vector2.zero;

            var intent = new DogController.MoveIntent
            {
                move = stick,
                wrestle = pad.buttonSouth.wasPressedThisFrame, // A
                jump = pad.buttonEast.wasPressedThisFrame,      // B
                bark = pad.buttonWest.wasPressedThisFrame,      // X
                interact = pad.buttonNorth.wasPressedThisFrame, // Y
            };

            _dog.Tick(intent, Time.fixedDeltaTime);
        }

        private Gamepad ResolvePad()
        {
            if (gamepadSlot >= 0 && gamepadSlot < Gamepad.all.Count) return Gamepad.all[gamepadSlot];
            return Gamepad.current;
            // TODO: replace with PlayerInput device pairing so P1/P2 stay glued to their controller
            // across scene loads (mirrors the prototype's P1=pad0 / P2=pad1 slot assignment).
        }
    }
}
