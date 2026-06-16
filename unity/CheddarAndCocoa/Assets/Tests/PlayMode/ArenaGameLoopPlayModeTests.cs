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
    /// Runtime proof of the first PLAYABLE loop (separate from the controller-movement baseline in
    /// <see cref="ControllerCoopPlayModeTests"/>). Loads the real ArenaScene and asserts the round
    /// rules hold:
    ///   1. the scene loads and builds both dogs (Cheddar + Cocoa);
    ///   2. the shared score starts at 0;
    ///   3. collecting a treat increments the score (and the treat is respawned);
    ///   4. the countdown can reach game-over;
    ///   5. restart resets score + timer + clears the game-over state.
    ///
    /// Headless: <c>unity/run-playmode-tests.sh</c> (needs a licensed editor).
    /// </summary>
    public sealed class ArenaGameLoopPlayModeTests
    {
        [UnityTest]
        public IEnumerator ArenaLoop_Scores_TimesOut_AndRestarts()
        {
            // 1) Scene loads + builds itself (ArenaBootstrap.Start runs over a couple frames).
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            // Both dogs exist.
            DogController cheddar = null, cocoa = null;
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                var dc = id.GetComponent<DogController>();
                if (id.Id == DogId.Cheddar) cheddar = dc;
                else if (id.Id == DogId.Cocoa) cocoa = dc;
            }
            Assert.IsNotNull(cheddar, "ArenaScene did not build a Cheddar dog.");
            Assert.IsNotNull(cocoa, "ArenaScene did not build a Cocoa dog.");

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game, "ArenaScene has no GameManager.");

            // 2) Score starts at 0; round is playing with time on the clock.
            Assert.AreEqual(0, game.Score, "Score should start at 0.");
            Assert.IsFalse(game.IsGameOver, "Round should start in the Playing state.");
            Assert.Greater(game.TimeRemaining, 0f, "Timer should start above 0.");

            // 3) Collecting a treat increments the shared score and respawns a replacement.
            var treats = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None);
            Assert.Greater(treats.Length, 0, "Arena should spawn treats to collect.");
            int treatCountBefore = treats.Length;

            int scoreBefore = game.Score;
            treats[0].CollectBy(cheddar); // the same path OnTriggerEnter2D drives when a dog touches it
            Assert.AreEqual(scoreBefore + 1, game.Score, "Collecting a treat should increment the score.");
            yield return null; // let the destroyed treat clear + replacement spawn
            Assert.AreEqual(treatCountBefore, Object.FindObjectsByType<Treat>(FindObjectsSortMode.None).Length,
                "A collected treat should be replaced so the count stays constant.");

            // Bark now has a small co-op purpose: both dogs must be close and bark within the
            // united-front window to earn a teamwork point. Barking far apart should not score.
            cocoa.transform.position = cheddar.transform.position + Vector3.right * 6f;
            int scoreAfterTreat = game.Score;
            cheddar.Bark();
            cocoa.Bark();
            Assert.AreEqual(scoreAfterTreat, game.Score, "Barking far apart should stay cosmetic only.");
            Assert.AreEqual(0, game.UnitedBarks, "Far-apart barks should not count as a united bark.");

            cocoa.transform.position = cheddar.transform.position + Vector3.right * 1f;
            cheddar.Bark();
            cocoa.Bark();
            Assert.AreEqual(scoreAfterTreat + 1, game.Score, "Close synchronized barks should score once.");
            Assert.AreEqual(1, game.UnitedBarks, "Close synchronized barks should count as a united bark.");

            // 4) Countdown can reach game over. Shorten the round and let it tick down.
            game.SetRoundDuration(0.3f);
            float guard = 0f;
            while (!game.IsGameOver && guard < 5f) { guard += Time.deltaTime; yield return null; }
            Assert.IsTrue(game.IsGameOver, "Round did not reach game-over when the timer expired.");
            Assert.LessOrEqual(game.TimeRemaining, 0f, "Time should be spent at game-over.");

            // 5) Restart resets score + timer + clears game-over.
            game.SetRoundDuration(60f);
            game.Restart();
            Assert.IsFalse(game.IsGameOver, "Restart should leave the Playing state.");
            Assert.AreEqual(0, game.Score, "Restart should reset the score to 0.");
            Assert.AreEqual(60f, game.TimeRemaining, 0.0001f, "Restart should refill the timer.");
        }
    }
}
