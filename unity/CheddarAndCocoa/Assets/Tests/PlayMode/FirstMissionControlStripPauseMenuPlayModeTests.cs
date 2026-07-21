using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using CheddarAndCocoa.Bootstrap;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// CF2.1 (roster audit: auto-dismissing info UI): the F4.1 ambient control-reminder strip used
    /// to have no way back once it timed out or was skipped - a real gap against the audit's rule
    /// that even an ambient, allowed-to-stay-timed reminder must be re-summonable from pause.
    /// <see cref="FirstMissionControlStripPlayModeTests"/> covers the new
    /// <c>GameManager.ReplayFirstMissionControlStrip</c>/<c>FirstMissionControlStripAvailable</c>
    /// seam at the controller-method level. Per trap #2 in
    /// docs/AGENT-WORK-QUEUE-COUCHFIX.md ("force-hook tests bypass real dispatch"), this file
    /// additionally drives the fix through the REAL pause-menu input path - the same
    /// <c>ArenaHud.Update()</c> D-pad/keyboard navigation and <c>ActivatePauseOption</c> a couch
    /// player actually uses - not just a direct method call.
    /// </summary>
    public sealed class FirstMissionControlStripPauseMenuPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator RealPauseMenuInput_ReplaysTheSkippedControlStrip()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
            yield return null;

            var keyboard = InputSystem.AddDevice<Keyboard>();

            new GameObject("Boot").AddComponent<ArenaBootstrap>();
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game, "ArenaBootstrap did not build a GameManager.");
            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            Assert.IsTrue(game.FirstMissionControlStripVisible,
                "SnackHeist as the session's first mission should auto-show the ambient control strip.");
            game.SkipFirstMissionControlStrip();
            Assert.IsFalse(game.FirstMissionControlStripVisible,
                "Precondition: the strip must be gone before we try to bring it back through pause.");

            // Real dispatch: open pause, navigate "previous" onto the control-strip re-summon row
            // (it sits directly above Resume whenever the row is present), and confirm - exactly
            // what a couch player does, through ArenaHud's actual Update()/ActivatePauseOption
            // path, not a direct GameManager method call. Wrapped in a small, fully-safe retry: each
            // attempt reopens the pause menu fresh (so a missed single-frame InputSystem edge - the
            // same caveat ControllerCoopPlayModeTests documents for gamepad edges - can never
            // accumulate into the wrong row; at most one "previous" press is ever injected per
            // attempt, so the worst case is staying on Resume, never overshooting the target).
            for (int attempt = 0; attempt < 4 && !game.FirstMissionControlStripVisible; attempt++)
            {
                if (!game.IsPaused) game.TogglePause();
                Assert.IsTrue(game.IsPaused);
                yield return null; // ArenaHud.Update()'s first paused frame only seeds _pauseSelection at Resume.
                yield return null; // extra buffer frame in case pause was toggled the same frame as this call.

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.UpArrow));
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;

                if (!game.FirstMissionControlStripVisible && game.IsPaused)
                    game.TogglePause(); // close so the next attempt reopens onto a freshly-seeded Resume.
            }

            Assert.IsTrue(game.FirstMissionControlStripVisible,
                "Selecting the pause menu's re-summon row through real keyboard dispatch should bring " +
                "the ambient control reminder back.");
            Assert.AreEqual(1f, game.FirstMissionControlStripAlpha,
                "Re-summoning through the real pause menu should restart the strip at full strength, " +
                "not resume a stale fade.");
        }
    }
}
