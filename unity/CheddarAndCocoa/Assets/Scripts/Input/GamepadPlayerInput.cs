using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls; // KeyControl (keyboard fallback)
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
        /// <summary>Keyboard fallback layouts so the game is playable with no controllers. P1 = WASD +
        /// Space (bark); P2 = arrow keys + Enter/Right-Shift (bark). <see cref="None"/> = controller only.</summary>
        public enum KeyboardScheme { None, WasdSpace, ArrowsEnter }

        [SerializeField, Range(0f, 0.9f)] private float deadzone = 0.25f; // balance.ts INPUT.gamepadDeadzone
        [Tooltip("Leave null to use the most-recently-paired gamepad; set for explicit P1/P2 assignment.")]
        [SerializeField] private int gamepadSlot = -1;
        [Tooltip("Keyboard fallback layout for this player (so the game plays without controllers).")]
        [SerializeField] private KeyboardScheme keyboardScheme = KeyboardScheme.None;

        private DogController _dog;

        private void Awake() => _dog = GetComponent<DogController>();

        /// <summary>Assign this player's controller slot (0 = P1, 1 = P2). Set by GameBootstrap.</summary>
        public void SetSlot(int slot) => gamepadSlot = slot;

        /// <summary>Assign this player's keyboard fallback layout (set by the bootstrap).</summary>
        public void SetKeyboardScheme(KeyboardScheme scheme) => keyboardScheme = scheme;

        // Read in Update: button edges (wasPressedThisFrame) are sampled on the Input System's
        // default dynamic update, so they're reliable here. Movement is velocity-based, so the
        // Rigidbody2D integrates it on the physics step regardless. Gamepad and keyboard are read
        // together and combined — whichever the player touches drives the dog (controllers preserved).
        private void Update()
        {
            Vector2 move = Vector2.zero;
            bool wrestle = false, jump = false, bark = false, interact = false;

            Gamepad pad = ResolvePad();
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.magnitude < deadzone) stick = Vector2.zero;
                move += stick;
                wrestle |= pad.buttonSouth.wasPressedThisFrame; // A
                jump |= pad.buttonEast.wasPressedThisFrame;      // B
                bark |= pad.buttonWest.wasPressedThisFrame;      // X
                interact |= pad.buttonNorth.wasPressedThisFrame; // Y
            }

            var kb = Keyboard.current;
            if (kb != null && keyboardScheme != KeyboardScheme.None)
            {
                if (keyboardScheme == KeyboardScheme.WasdSpace)
                {
                    move += ReadKeys(kb.aKey, kb.dKey, kb.sKey, kb.wKey);
                    bark |= kb.spaceKey.wasPressedThisFrame;
                }
                else // ArrowsEnter
                {
                    move += ReadKeys(kb.leftArrowKey, kb.rightArrowKey, kb.downArrowKey, kb.upArrowKey);
                    bark |= kb.enterKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame;
                }
            }

            if (move.sqrMagnitude > 1f) move = move.normalized; // keep combined input within analog range

            var intent = new DogController.MoveIntent
            {
                move = move,
                wrestle = wrestle,
                jump = jump,
                bark = bark,
                interact = interact,
            };

            _dog.Tick(intent, Time.deltaTime);
        }

        private static Vector2 ReadKeys(KeyControl left, KeyControl right, KeyControl down, KeyControl up)
        {
            Vector2 v = Vector2.zero;
            if (left.isPressed) v.x -= 1f;
            if (right.isPressed) v.x += 1f;
            if (down.isPressed) v.y -= 1f;
            if (up.isPressed) v.y += 1f;
            return v;
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
