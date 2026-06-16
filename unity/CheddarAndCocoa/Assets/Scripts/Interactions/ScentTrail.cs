using System.Collections.Generic;
using UnityEngine;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Interactions
{
    /// <summary>
    /// A sniffable scent trail — a breadcrumb path a dog follows nose-down to find a hidden
    /// objective (treat, lost toy, the squirrel's route). Supports the "sniff" core verb that the
    /// web prototype only implied; this is a NET-NEW co-op mechanic to design + sim, not a port.
    ///
    /// Co-op angle: one dog sniffs out the trail while the other handles a threat/gate — pairs
    /// naturally with <see cref="CoopInteraction"/>. Reveal nodes progressively as a dog reaches
    /// each, optionally gating the final node behind both dogs (e.g. dig it up together).
    ///
    /// DESIGN TODO (owner): does sniffing slow the dog (nose-down state) as a tradeoff? Does the
    /// trail decay over time? Add the resulting constants to balance.ts and a Unity DataAsset.
    /// </summary>
    public sealed class ScentTrail : MonoBehaviour
    {
        [SerializeField] private List<Transform> nodes = new();
        [SerializeField] private float sniffRadius = 0.75f;
        [SerializeField] private bool finalNodeNeedsBothDogs = false;

        private int _revealed; // index of the next node to discover

        /// <summary>Raised as each node is sniffed out (UI/VFX hook); int = node index.</summary>
        public event System.Action<int> OnNodeFound;
        /// <summary>Raised when the trail is fully followed to its end.</summary>
        public event System.Action OnTrailComplete;

        private void Update()
        {
            if (_revealed >= nodes.Count) return;

            // TODO: query the actual dogs from the level instead of FindObjectsByType each frame.
            var dogs = FindObjectsByType<DogIdentity>(FindObjectsSortMode.None);
            Transform next = nodes[_revealed];

            int dogsAtNode = 0;
            foreach (var d in dogs)
                if (Vector2.Distance(d.transform.position, next.position) <= sniffRadius) dogsAtNode++;

            bool isFinal = _revealed == nodes.Count - 1;
            int needed = (isFinal && finalNodeNeedsBothDogs) ? 2 : 1;
            if (dogsAtNode < needed) return;

            OnNodeFound?.Invoke(_revealed);
            _revealed++;
            if (_revealed >= nodes.Count) OnTrailComplete?.Invoke();
        }
    }
}
