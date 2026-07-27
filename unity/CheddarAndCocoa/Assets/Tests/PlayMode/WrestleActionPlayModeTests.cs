using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Coverage for the wrestle verb: the A button read `intent.wrestle` every frame but nothing ever
    /// consumed it (same dead-input bug class jump had). Wrestle is now a real minimal flip - an
    /// asymmetric reversal-odds coin flip that stuns and knocks back the loser - gated the same way
    /// the prototype's canWrestle/doWrestle (src/systems/wrestle.ts) gates it: both dogs must be
    /// free/non-immune and in range, or the attempt whiffs/blocks instead of resolving.
    ///
    /// The win/lose side is random (attacker's own wrestleWinChance), so these tests assert the
    /// outcome structurally - exactly one of the two dogs ends up stunned - rather than which dog
    /// wins, so they don't depend on the mission's RNG seed.
    /// </summary>
    public sealed class WrestleActionPlayModeTests
    {
        private static (DogController cheddar, DogController cocoa, GameManager game) SetUpDogs()
        {
            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);

            var cheddarGo = GameObject.Find("Cheddar");
            var cocoaGo = GameObject.Find("Cocoa");
            Assert.IsNotNull(cheddarGo);
            Assert.IsNotNull(cocoaGo);
            cheddarGo.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            cocoaGo.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;

            return (cheddarGo.GetComponent<DogController>(), cocoaGo.GetComponent<DogController>(), game);
        }

        [UnityTest]
        public IEnumerator Wrestle_ResolvesWhenDogsAreClose_StunsExactlyOneDogThenBothRecover()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var (cheddar, cocoa, _) = SetUpDogs();
            yield return null;

            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = new Vector3(0.3f, 0f, 0f); // well within wrestleRange (1.8 units)

            Assert.IsFalse(cheddar.IsWrestleStunned);
            Assert.IsFalse(cocoa.IsWrestleStunned);

            cheddar.Wrestle();

            Assert.IsTrue(cheddar.WrestleOnCooldown, "A resolved attempt must cooldown the attacker.");
            Assert.AreNotEqual(cheddar.IsWrestleStunned, cocoa.IsWrestleStunned,
                "Exactly one dog should be stunned by a resolved wrestle - never both, never neither.");
            Assert.AreEqual(DogReadabilityFeedback.Pose.Wrestle,
                cheddar.GetComponent<DogReadabilityFeedback>().CurrentPose);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Wrestle,
                cocoa.GetComponent<DogReadabilityFeedback>().CurrentPose,
                "Both dogs should act the flip before the loser settles into the stunned loop.");

            float guard = 0f;
            while ((cheddar.IsWrestleStunned || cocoa.IsWrestleStunned) && guard < 3f)
            {
                cheddar.Tick(default, Time.deltaTime);
                cocoa.Tick(default, Time.deltaTime);
                guard += Time.deltaTime;
                yield return null;
            }

            Assert.IsFalse(cheddar.IsWrestleStunned, "The loser's stun must expire on its own (no rescue needed).");
            Assert.IsFalse(cocoa.IsWrestleStunned);
            Assert.AreEqual(MovementMode.Free, cheddar.Mode);
            Assert.AreEqual(MovementMode.Free, cocoa.Mode);
        }

        [UnityTest]
        public IEnumerator Wrestle_WhiffsWhenDogsAreFarApart_NoStunButShortCooldownApplies()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var (cheddar, cocoa, _) = SetUpDogs();
            yield return null;

            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = new Vector3(10f, 0f, 0f); // far outside wrestleRange

            cheddar.Wrestle();

            Assert.IsFalse(cheddar.IsWrestleStunned, "A whiff must not stun anyone.");
            Assert.IsFalse(cocoa.IsWrestleStunned);
            Assert.IsTrue(cheddar.WrestleOnCooldown, "Even a whiff applies a short cooldown so the button can't be spammed.");
        }

        [UnityTest]
        public IEnumerator Wrestle_IsBlockedWhileBusy()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var (cheddar, cocoa, _) = SetUpDogs();
            yield return null;

            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = new Vector3(0.3f, 0f, 0f);
            cheddar.SetMode(MovementMode.Stunned);

            cheddar.Wrestle();

            Assert.IsFalse(cheddar.WrestleOnCooldown, "A rooted dog (stunned/tug/shaking/transit) can't even attempt a wrestle.");
            Assert.IsFalse(cocoa.IsWrestleStunned, "Nothing should happen to the sibling when the attacker never actually attempted.");
        }

        [UnityTest]
        public IEnumerator Wrestle_ResolvesWhenDogsAreClose_EmitsDustParticlesOnBothDogs()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var (cheddar, cocoa, game) = SetUpDogs();
            yield return null;

            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = new Vector3(0.3f, 0f, 0f);

            var cheddarFeedback = game.DogFeedback[0].ActionFeedback;
            var cocoaFeedback = game.DogFeedback[1].ActionFeedback;
            int cheddarParticlesBefore = cheddarFeedback.TotalParticlesEmitted;
            int cocoaParticlesBefore = cocoaFeedback.TotalParticlesEmitted;

            cheddar.Wrestle();

            // DogReadabilityFeedback.Update() ticks _actionFeedback on its own every real frame; the
            // wrestle style's anticipation phase is a few frames long before EmitImpactParticles fires,
            // so just wait real frames rather than double-driving Tick() ourselves.
            float guard = 0f;
            while (guard < 1f &&
                   (cheddarFeedback.TotalParticlesEmitted == cheddarParticlesBefore ||
                    cocoaFeedback.TotalParticlesEmitted == cocoaParticlesBefore))
            {
                guard += Time.deltaTime;
                yield return null;
            }

            Assert.Greater(cheddarFeedback.TotalParticlesEmitted, cheddarParticlesBefore,
                "The attacker should get a dust burst when a wrestle resolves, win or lose.");
            Assert.Greater(cocoaFeedback.TotalParticlesEmitted, cocoaParticlesBefore,
                "The defender should get a dust burst too - the flip happens to both dogs.");
        }

        [UnityTest]
        public IEnumerator Wrestle_WhiffsWhenDogsAreFarApart_LungesAttackerTowardDefender()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var (cheddar, cocoa, _) = SetUpDogs();
            yield return null;

            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = new Vector3(10f, 0f, 0f); // far outside wrestleRange, same direction as +X

            cheddar.Wrestle();

            Assert.Greater(cheddar.CurrentVelocity.x, 0f,
                "A whiff should lunge the attacker toward the sibling instead of leaving it dead still.");
            Assert.AreEqual(cheddar.WrestleLungeSpeed, cheddar.CurrentVelocity.magnitude, 0.01f,
                "The lunge should be a clean one-shot kick at the tuned lunge speed.");
        }

        [UnityTest]
        public IEnumerator Wrestle_WhiffsWhenDogsAreFarApart_PlaysAMissReadOnTheAttacker()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var (cheddar, cocoa, game) = SetUpDogs();
            yield return null;

            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = new Vector3(10f, 0f, 0f); // far outside wrestleRange

            cheddar.Wrestle();

            Assert.AreEqual(DogFeedbackAction.WrestleMiss, game.DogFeedback[0].ActionFeedback.CurrentAction,
                "A whiff must still play a readable attempt, not silence.");
        }

        [UnityTest]
        public IEnumerator Wrestle_AgainstABusyDefender_PlaysAMissReadOnTheAttacker()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var (cheddar, cocoa, game) = SetUpDogs();
            yield return null;

            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = new Vector3(0.3f, 0f, 0f); // in range, but rooted
            cocoa.SetMode(MovementMode.Stunned);

            cheddar.Wrestle();

            Assert.AreEqual(DogFeedbackAction.WrestleMiss, game.DogFeedback[0].ActionFeedback.CurrentAction,
                "Nothing to wrestle right now must still play a readable attempt, not silence.");
        }

        [UnityTest]
        public IEnumerator Wrestle_IsConsumedFromMoveIntent()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var (cheddar, cocoa, _) = SetUpDogs();
            yield return null;

            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = new Vector3(0.3f, 0f, 0f);

            cheddar.Tick(new DogController.MoveIntent { wrestle = true }, Time.deltaTime);

            Assert.AreNotEqual(cheddar.IsWrestleStunned, cocoa.IsWrestleStunned,
                "Tick() must resolve intent.wrestle into a real attempt, not silently drop it.");
        }
    }
}
