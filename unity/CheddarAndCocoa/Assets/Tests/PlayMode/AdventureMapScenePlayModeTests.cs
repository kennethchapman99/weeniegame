using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// AdventureMapScene is the only scene in EditorBuildSettings.asset that no PlayMode test ever
    /// loaded by name - AdventureMapController/AdventureProgressService are well covered via direct
    /// instantiation (AdventureMapControllerPlayModeTests, AdventureProgressionPlayModeTests), but
    /// nothing proved the real scene bootstrap (AdventureMapBootstrap -> AdventureMapHud) actually
    /// builds without error when the scene loads for real.
    /// </summary>
    public sealed class AdventureMapScenePlayModeTests
    {
        [UnityTest]
        public IEnumerator AdventureMapScene_LoadsAndBuildsTheMapHudWithoutError()
        {
            yield return SceneManager.LoadSceneAsync("AdventureMapScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var hud = Object.FindFirstObjectByType<AdventureMapHud>();
            Assert.IsNotNull(hud, "AdventureMapBootstrap should have created the AdventureMapHud.");
            Assert.IsNotNull(hud.Controller, "AdventureMapHud should build its AdventureMapController on Awake.");
            Assert.Greater(hud.Controller.Locations.Count, 0,
                "The map should have at least one location loaded from the catalog.");
            Assert.IsNotNull(hud.Controller.SelectedLocation);
            Assert.AreEqual(AdventureLocationCatalog.BackyardId, hud.Controller.SelectedLocation.Id,
                "A fresh map should default-select the always-unlocked Backyard.");

            // Leave the suite back on ArenaScene so later tests don't inherit this scene's state.
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
        }
    }
}
