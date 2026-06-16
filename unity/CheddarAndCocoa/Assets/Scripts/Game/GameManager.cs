using System.Collections.Generic;
using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Input;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// The 60-second couch-co-op round: a SHARED score that both dogs feed, a countdown timer, treats
    /// that spawn around the arena, and a game-over -> restart flow. Deliberately simple and
    /// prototype-friendly (no scene-flow framework). Owns the round STATE; rendering of it lives in
    /// <see cref="ArenaHud"/> (logic/render split). The <see cref="ArenaBootstrap"/> builds and
    /// <see cref="Init"/>ialises it.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public enum State { Playing, GameOver }

        [SerializeField] private float roundDuration = 60f;
        [SerializeField] private int treatCount = 5;
        [SerializeField] private float unitedBarkWindow = 0.8f;
        [SerializeField] private float unitedBarkRange = 3f;
        [SerializeField] private float unitedBarkCooldown = 2f;
        [SerializeField] private int unitedBarkScore = 1;

        // --- Round state (read by the HUD + tests) ---
        public int Score { get; private set; }
        public float TimeRemaining { get; private set; }
        public float RoundDuration => roundDuration;
        public State Phase { get; private set; } = State.Playing;
        public bool IsGameOver => Phase == State.GameOver;
        public int UnitedBarks { get; private set; }

        private DogController[] _dogs;
        private GamepadPlayerInput[] _inputs;
        private Vector2[] _dogStarts;
        private Sprite _treatSprite;
        private Rect _bounds;        // arena play area (treats spawn inside, with margin)
        private System.Random _rng;  // seeded -> deterministic treat layout for the headless sim
        private Transform _treatRoot;
        private readonly List<Treat> _treats = new();
        private float[] _lastBarks;
        private float _nextUnitedBarkAt;

        /// <summary>Build the round. Called once by the bootstrap after the dogs/arena exist.</summary>
        public void Init(DogController[] dogs, GamepadPlayerInput[] inputs, Sprite treatSprite,
            Rect bounds, int seed)
        {
            _dogs = dogs;
            _inputs = inputs;
            _treatSprite = treatSprite;
            _bounds = bounds;
            _rng = new System.Random(seed);

            _dogStarts = new Vector2[dogs.Length];
            _lastBarks = new float[dogs.Length];
            for (int i = 0; i < dogs.Length; i++)
            {
                _dogStarts[i] = dogs[i].transform.position;
                _lastBarks[i] = float.NegativeInfinity;
                dogs[i].OnBark += OnDogBarked;
            }

            _treatRoot = new GameObject("Treats").transform;
            BeginRound();
        }

        /// <summary>Increment the shared score + respawn the eaten treat. Called by <see cref="Treat"/>.</summary>
        public void OnTreatCollected(Treat treat, DogController dog)
        {
            if (IsGameOver || treat == null) return;
            Score++;
            _treats.Remove(treat);
            Destroy(treat.gameObject);
            SpawnTreat();
        }

        /// <summary>Reset score + timer, re-home the dogs and treats, and play again.</summary>
        public void Restart() => BeginRound();

        /// <summary>Set the round length. While playing, clamps the remaining time down to it. Used by
        /// the headless test to drive the countdown to game-over quickly (and to restore 60s after).</summary>
        public void SetRoundDuration(float seconds)
        {
            roundDuration = Mathf.Max(0.01f, seconds);
            if (Phase == State.Playing) TimeRemaining = Mathf.Min(TimeRemaining, roundDuration);
        }

        private void BeginRound()
        {
            Score = 0;
            UnitedBarks = 0;
            TimeRemaining = roundDuration;
            Phase = State.Playing;
            _nextUnitedBarkAt = 0f;

            // Re-home dogs + hand control back to the players.
            for (int i = 0; i < _dogs.Length; i++)
            {
                _lastBarks[i] = float.NegativeInfinity;
                _dogs[i].transform.position = _dogStarts[i];
                if (_dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
                if (i < _inputs.Length && _inputs[i] != null) _inputs[i].enabled = true;
            }

            ClearTreats();
            for (int i = 0; i < treatCount; i++) SpawnTreat();
        }

        private void OnDogBarked(DogId dogId)
        {
            if (Phase != State.Playing || _dogs == null || _dogs.Length < 2) return;

            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;

            _lastBarks[dogIndex] = Time.time;
            if (Time.time < _nextUnitedBarkAt) return;

            // First arena use for bark: if both siblings huddle close and bark within a short
            // window, award a tiny teamwork point. This is the slice-friendly stand-in for the
            // upcoming united-front predator scare described in docs/COOP-VISION.md.
            if (!AllDogsBarkedRecently() || !DogsAreHuddled()) return;

            UnitedBarks++;
            Score += unitedBarkScore;
            _nextUnitedBarkAt = Time.time + unitedBarkCooldown;
        }

        private int IndexOfDog(DogId dogId)
        {
            for (int i = 0; i < _dogs.Length; i++)
            {
                if (_dogs[i] != null && _dogs[i].GetComponent<DogIdentity>().Id == dogId) return i;
            }
            return -1;
        }

        private bool AllDogsBarkedRecently()
        {
            for (int i = 0; i < _lastBarks.Length; i++)
            {
                if (Time.time - _lastBarks[i] > unitedBarkWindow) return false;
            }
            return true;
        }

        private bool DogsAreHuddled()
        {
            Vector2 first = _dogs[0].transform.position;
            for (int i = 1; i < _dogs.Length; i++)
            {
                if (Vector2.Distance(first, _dogs[i].transform.position) > unitedBarkRange) return false;
            }
            return true;
        }

        private void EndRound()
        {
            Phase = State.GameOver;
            TimeRemaining = 0f;

            // Freeze the dogs: cut input + stop motion so the field settles on the final score.
            for (int i = 0; i < _dogs.Length; i++)
            {
                if (i < _inputs.Length && _inputs[i] != null) _inputs[i].enabled = false;
                if (_dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
            }
        }

        private void Update()
        {
            if (Phase != State.Playing) return;
            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f) EndRound();
        }

        private void SpawnTreat()
        {
            const float margin = 1.2f; // keep treats off the walls
            float x = Mathf.Lerp(_bounds.xMin + margin, _bounds.xMax - margin, (float)_rng.NextDouble());
            float y = Mathf.Lerp(_bounds.yMin + margin, _bounds.yMax - margin, (float)_rng.NextDouble());

            var go = new GameObject("Treat");
            go.transform.SetParent(_treatRoot);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _treatSprite;
            sr.color = new Color(0.92f, 0.4f, 0.32f); // weenie red
            sr.sortingOrder = 5;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var treat = go.AddComponent<Treat>();
            treat.Bind(this);
            _treats.Add(treat);
        }

        private void ClearTreats()
        {
            foreach (var t in _treats) if (t != null) Destroy(t.gameObject);
            _treats.Clear();
        }

        private void OnDestroy()
        {
            if (_dogs == null) return;
            for (int i = 0; i < _dogs.Length; i++)
            {
                if (_dogs[i] != null) _dogs[i].OnBark -= OnDogBarked;
            }
        }
    }
}
