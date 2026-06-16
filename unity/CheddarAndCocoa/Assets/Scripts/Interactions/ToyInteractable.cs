using UnityEngine;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Interactions
{
    /// <summary>
    /// A grabbable toy. Walk onto it to pick it up and score; a rope toy instead starts a
    /// two-dog tug-of-war (see <see cref="CheddarAndCocoa.Minigames.TugOfWarMinigame"/>).
    ///
    /// PROTOTYPE MAP: src/systems/toys.ts (spawn + pickup + rope flag), SCORE.toy = 1.
    /// Placement constraints carry over per scene (pool: deck bands only, never water;
    /// house: room-aware). Rope toys spawn ~30% of land toys (balance.ts TUG.ropeSpawnChance).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ToyInteractable : MonoBehaviour
    {
        [SerializeField] private int scoreValue = 1;   // SCORE.toy
        [SerializeField] private bool isRopeToy = false;
        [SerializeField] private float respawnDelay = 0f; // 0 = consume permanently

        /// <summary>Raised when a dog collects this toy (objective/score systems subscribe).</summary>
        public event System.Action<DogId, int> OnCollected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var dog = other.GetComponentInParent<DogIdentity>();
            if (dog == null) return;

            if (isRopeToy)
            {
                // TODO: register this dog at the rope; when BOTH dogs are within grab range and
                // free, hand off to TugOfWarMinigame instead of an instant pickup.
                return;
            }

            OnCollected?.Invoke(dog.Id, scoreValue);
            if (respawnDelay > 0f) gameObject.SetActive(false); // TODO: schedule respawn
            else Destroy(gameObject);
        }
    }
}
