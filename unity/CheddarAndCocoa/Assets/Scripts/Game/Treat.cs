using UnityEngine;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// A collectible "weenie" treat. A trigger collider sits on the treat; when a dog (which carries
    /// a Rigidbody2D) overlaps it, the treat reports itself collected to the <see cref="GameManager"/>,
    /// which scores it and respawns a replacement. Deliberately tiny — this is prototype scoring, not
    /// the real pickup/interaction system in Assets/Scripts/Interactions.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class Treat : MonoBehaviour
    {
        private GameManager _game;

        /// <summary>Wire this treat to the manager that owns scoring/respawn (called on spawn).</summary>
        public void Bind(GameManager game) => _game = game;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var dog = other.GetComponentInParent<DogController>();
            if (dog != null) CollectBy(dog);
        }

        /// <summary>The collection path: score it + respawn. Public so the headless PlayMode test can
        /// drive a deterministic collect without relying on physics-trigger timing.</summary>
        public void CollectBy(DogController dog)
        {
            if (_game == null) return;
            _game.OnTreatCollected(this, dog);
        }
    }
}
