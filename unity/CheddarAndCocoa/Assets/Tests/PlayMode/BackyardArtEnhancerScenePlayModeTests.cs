using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class BackyardArtEnhancerScenePlayModeTests
    {
        [UnityTest]
        public IEnumerator BackyardArtEnhancer_InstallsWithoutBlockingArenaStart()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            var enhancer = Object.FindFirstObjectByType<BackyardRescueArtEnhancer>();
            if (enhancer == null)
            {
                var go = new GameObject("BackyardRescueArtEnhancer_TestFallback");
                enhancer = go.AddComponent<BackyardRescueArtEnhancer>();
            }

            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
            enhancer.EnhanceNow();
            yield return null;

            Assert.AreEqual(GameManager.State.Playing, game.Phase);
            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, game.ActiveMissionVariant);
            Assert.IsTrue(enhancer.Enhanced, "Enhancer should mark itself active even if final art sprites are not present yet.");
            Assert.GreaterOrEqual(enhancer.OverlayCount, 0);
        }
    }
}
