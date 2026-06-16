using UnityEngine;
using CheddarAndCocoa.Data;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>Which dog this is. Mirrors the prototype's <c>DogId</c> ('cheddar' | 'cocoa').</summary>
    public enum DogId
    {
        Cheddar, // golden, ~1.5yr, chaos puppy — fast, zoomies-prone, chair-leap, can barf
        Cocoa    // chocolate, ~4yr, reigning spot queen — stronger, calmer, better wrestle odds
    }

    /// <summary>
    /// Marks an entity as one of the two dogs and carries its tuning + visual identity.
    /// Systems (wrestle, objectives, camera, hazards) read <see cref="Id"/> to branch on
    /// asymmetric behavior. Attach to the dog prefab root.
    ///
    /// PROTOTYPE MAP: src/config/dogs.ts (palettes) + the per-dog branches scattered through
    /// systems/*.ts (wrestle odds, stair speed, kitchen abilities) collapse into the
    /// <see cref="DogTuning"/> asset referenced here.
    /// </summary>
    public sealed class DogIdentity : MonoBehaviour
    {
        [SerializeField] private DogId id = DogId.Cheddar;
        [SerializeField] private DogTuning tuning;

        public DogId Id => id;
        public DogTuning Tuning => tuning;

        /// <summary>The other dog's id — handy for "needs both dogs" gates and the shared camera.</summary>
        public DogId Sibling => id == DogId.Cheddar ? DogId.Cocoa : DogId.Cheddar;

        /// <summary>Runtime setup (used by GameBootstrap when spawning placeholder dogs from code).</summary>
        public void Configure(DogId newId, DogTuning newTuning)
        {
            id = newId;
            tuning = newTuning;
        }
    }
}
