using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Minimal bootstrap for a future AdventureMap scene. Add this to an empty scene to get the generated map HUD.
    /// </summary>
    public sealed class AdventureMapBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (FindFirstObjectByType<AdventureMapHud>() != null) return;
            var go = new GameObject("AdventureMapHud");
            go.AddComponent<AdventureMapHud>();
        }
    }
}
