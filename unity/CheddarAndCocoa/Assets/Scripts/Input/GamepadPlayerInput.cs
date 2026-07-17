using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls; // KeyControl (keyboard fallback)
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Input
{
    /// <summary>
    /// Reads one player's gamepad each frame and feeds a <see cref="DogController.MoveIntent"/> to
    /// its dog. Couch co-op: two of these exist, each bound to a distinct controller slot — P1
    /// drives Cheddar and P2 drives Cocoa. If a controller disconnects, its dog stays unbound until
    /// that device returns or an unclaimed replacement controller appears; it never steals the
    /// sibling's still-connected pad.
    ///
    /// PROTOTYPE MAP: src/core/gamepad.ts (GamepadSource per slot) + src/core/input.ts
    /// (computeIntent: stick -> ax/ay/arrive; A/B -> wrestle/jump). The deadzone below mirrors
    /// balance.ts INPUT.gamepadDeadzone = 0.25.
    ///
    /// A future front-end may replace slot assignment with PlayerInputManager join flow. This
    /// component keeps the current fixed Cheddar/Cocoa ownership safe and reconnectable in the
    /// meantime. Use the Input System (not legacy Input).
    /// </summary>
    [RequireComponent(typeof(DogController))]
    [RequireComponent(typeof(DogIdentity))]
    public sealed class GamepadPlayerInput : MonoBehaviour
    {
        /// <summary>Keyboard fallback layouts so the game is playable with no controllers. P1 = WASD,
        /// Space (bark), E (interact), Left-Shift (jump), Q (wrestle); P2 = arrow keys, Enter (bark),
        /// Right-Shift (interact), Right-Ctrl (jump), Right-Alt (wrestle). <see cref="None"/> =
        /// controller only.</summary>
        public enum KeyboardScheme { None, WasdSpace, ArrowsEnter }

        [SerializeField, Range(0f, 0.9f)] private float deadzone = 0.25f; // default mirrors balance.ts INPUT.gamepadDeadzone
        [Tooltip("Leave null to use the most-recently-paired gamepad; set for explicit P1/P2 assignment.")]
        [SerializeField] private int gamepadSlot = -1;
        [Tooltip("Keyboard fallback layout for this player (so the game plays without controllers).")]
        [SerializeField] private KeyboardScheme keyboardScheme = KeyboardScheme.None;

        private DogController _dog;
        private Gamepad _boundPad;
        private bool _hasEverBoundPad;

        public float Deadzone => deadzone;
        public bool HasBoundGamepad => _boundPad != null && _boundPad.added;
        public bool HasDisconnectedGamepad => _hasEverBoundPad && !HasBoundGamepad;
        public int BoundGamepadDeviceId => HasBoundGamepad ? _boundPad.deviceId : -1;
        public string BoundGamepadDisplayName => HasBoundGamepad ? _boundPad.displayName : string.Empty;
        public int GamepadSlot => gamepadSlot;
        public KeyboardScheme AssignedKeyboardScheme => keyboardScheme;

        private void Awake()
        {
            _dog = GetComponent<DogController>();
            var identity = GetComponent<DogIdentity>();
            if (identity != null && identity.Tuning != null)
                deadzone = Mathf.Clamp(identity.Tuning.inputDeadzone, 0f, 0.9f);
        }

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
                wrestle |= pad.buttonSouth.wasPressedThisFrame; // B on Switch-style pads
                jump |= pad.buttonEast.wasPressedThisFrame;      // A on Switch-style pads
                bark |= pad.buttonWest.wasPressedThisFrame;      // Y on Switch-style pads
                interact |= pad.buttonNorth.wasPressedThisFrame; // X on Switch-style pads
            }

            var kb = Keyboard.current;
            if (kb != null && keyboardScheme != KeyboardScheme.None)
            {
                if (keyboardScheme == KeyboardScheme.WasdSpace)
                {
                    move += ReadKeys(kb.aKey, kb.dKey, kb.sKey, kb.wKey);
                    bark |= kb.spaceKey.wasPressedThisFrame;
                    interact |= kb.eKey.wasPressedThisFrame;
                    jump |= kb.leftShiftKey.wasPressedThisFrame;
                    wrestle |= kb.qKey.wasPressedThisFrame;
                }
                else // ArrowsEnter
                {
                    move += ReadKeys(kb.leftArrowKey, kb.rightArrowKey, kb.downArrowKey, kb.upArrowKey);
                    bark |= kb.enterKey.wasPressedThisFrame;
                    interact |= kb.rightShiftKey.wasPressedThisFrame;
                    jump |= kb.rightCtrlKey.wasPressedThisFrame;
                    wrestle |= kb.rightAltKey.wasPressedThisFrame;
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
            // Once assigned, keep the dog glued to the same device even if another pad becomes the
            // current device or Gamepad.all ordering changes. Some platforms re-add the same object
            // after a transient disconnect, so retain it while absent and prefer it if it returns.
            if (_boundPad != null)
            {
                if (_boundPad.added) return _boundPad;

                // A physically replaced controller usually has a new InputDevice instance. Bind
                // only an unclaimed device; rebinding by shifted Gamepad.all index here could hand
                // Cocoa's controller to Cheddar (or vice versa) when just one pad disconnects.
                Gamepad replacement = FirstUnclaimedPad();
                if (replacement == null) return null;
                _boundPad = replacement;
                _hasEverBoundPad = true;
                return _boundPad;
            }

            if (gamepadSlot >= 0)
            {
                if (!_hasEverBoundPad)
                {
                    if (gamepadSlot >= Gamepad.all.Count) return null;
                    _boundPad = Gamepad.all[gamepadSlot];
                }
                else
                {
                    _boundPad = FirstUnclaimedPad();
                    if (_boundPad == null) return null;
                }

                _hasEverBoundPad = true;
                return _boundPad;
            }

            return Gamepad.current;
        }

        private Gamepad FirstUnclaimedPad()
        {
            foreach (Gamepad candidate in Gamepad.all)
            {
                bool claimed = false;
                foreach (GamepadPlayerInput input in FindObjectsByType<GamepadPlayerInput>(FindObjectsSortMode.None))
                {
                    if (input == this || input._boundPad != candidate) continue;
                    claimed = true;
                    break;
                }

                if (!claimed) return candidate;
            }

            return null;
        }
    }
}
