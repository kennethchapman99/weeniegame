using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel; // GamepadState — full-state injection
using CheddarAndCocoa.Bootstrap;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Automated runtime proof of the "first playable" acceptance criteria — WITHOUT physical
    /// controllers or a human pressing Play. It spins up the real <see cref="GameBootstrap"/> (the
    /// exact thing the ControllerTestScene runs), injects TWO VIRTUAL gamepads through the Input
    /// System test fixture, and asserts:
    ///   1. each pad drives ITS OWN dog (Cheddar=pad0, Cocoa=pad1) — they move in opposite
    ///      directions, i.e. independently, not as one rig;
    ///   2. the bark button (X / buttonWest) on pad0 fires <see cref="DogController.OnBark"/>.
    ///
    /// Run headlessly: <c>unity/run-playmode-tests.sh</c> (needs a licensed editor — a Unity
    /// Personal seat whose offline period hasn't lapsed; sign in via the Hub once to refresh).
    /// This is the repeatable substitute for "press Play and watch the dogs move", so the milestone
    /// can be re-verified on any machine with one command and zero hardware.
    /// </summary>
    public sealed class ControllerCoopPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator TwoPads_DriveTwoDogs_Independently_AndBarkFires()
        {
            // Two virtual controllers — the Input System reports them in Gamepad.all[0]/[1],
            // which is exactly what GamepadPlayerInput.ResolvePad() reads for P1/P2.
            var pad0 = InputSystem.AddDevice<Gamepad>();
            var pad1 = InputSystem.AddDevice<Gamepad>();

            var boot = new GameObject("Boot").AddComponent<GameBootstrap>();
            yield return null; // GameBootstrap.Start() builds floor/walls/camera/dogs
            yield return null;

            DogController cheddar = null, cocoa = null;
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                var dc = id.GetComponent<DogController>();
                if (id.Id == DogId.Cheddar) cheddar = dc;
                else if (id.Id == DogId.Cocoa) cocoa = dc;
            }
            Assert.IsNotNull(cheddar, "GameBootstrap did not build a Cheddar dog.");
            Assert.IsNotNull(cocoa, "GameBootstrap did not build a Cocoa dog.");

            Vector2 cheddarStart = cheddar.transform.position;
            Vector2 cocoaStart = cocoa.transform.position;

            bool cheddarBarked = false;
            cheddar.OnBark += _ => cheddarBarked = true;

            // Push the dogs APART (Cheddar starts left, Cocoa right) so they can't collide mid-test:
            // pad0 → LEFT, pad1 → RIGHT. Inject FULL GamepadState events (delta-state Set() can't
            // target the composite leftStick control on this Input System version). The stick value
            // persists until the next event, so one injection holds the dogs moving; velocity set
            // in GamepadPlayerInput.Update then integrates over the physics steps below.
            InputSystem.QueueStateEvent(pad0, new GamepadState { leftStick = new Vector2(-1f, 0f) });
            InputSystem.QueueStateEvent(pad1, new GamepadState { leftStick = new Vector2(1f, 0f) });
            InputSystem.Update();
            yield return null; // let GamepadPlayerInput.Update sample the sticks and set velocity
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();

            Vector2 cheddarEnd = cheddar.transform.position;
            Vector2 cocoaEnd = cocoa.transform.position;

            float cheddarDx = cheddarEnd.x - cheddarStart.x;
            float cocoaDx = cocoaEnd.x - cocoaStart.x;

            Assert.Less(cheddarDx, -0.25f, "Cheddar (pad0) did not move left under stick input.");
            Assert.Greater(cocoaDx, 0.25f, "Cocoa (pad1) did not move right under stick input.");
            Assert.AreNotEqual(Mathf.Sign(cheddarDx), Mathf.Sign(cocoaDx),
                "Dogs moved the same direction — input is not independent per pad.");

            // Bark: press X (buttonWest) on pad0. wasPressedThisFrame is a single-frame edge. We
            // must let the PLAYER LOOP process the queued press (no manual InputSystem.Update here),
            // because this coroutine resumes AFTER MonoBehaviour.Update — a manual update would land
            // the edge a frame before GamepadPlayerInput samples it and the player-loop update would
            // then clear it. Queue + yield lets the press be live on the frame GamepadPlayerInput
            // reads it. Retry the release→press cycle a few frames so the edge reliably lands.
            for (int i = 0; i < 12 && !cheddarBarked; i++)
            {
                InputSystem.QueueStateEvent(pad0, new GamepadState()); // release (sticks neutral too)
                yield return null;
                InputSystem.QueueStateEvent(pad0, new GamepadState().WithButton(GamepadButton.West)); // X down
                yield return null;
            }

            Assert.IsTrue(cheddarBarked,
                "Bark input (pad0 X / buttonWest) did not produce a response (OnBark never fired).");

            Object.Destroy(boot.gameObject);
        }
    }
}
