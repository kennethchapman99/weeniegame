using System.Collections.Generic;
using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Input;

namespace CheddarAndCocoa.Game
{
    public sealed class GameManager : MonoBehaviour
    {
        public enum State { Playing, LevelClear, GameOver }
        public enum LevelKind { BreakfastHeist, LivingRoomChaos }
        public enum RoundModifier { ZoomiesSurge, SquirrelTrouble, PancakePanic }

        [SerializeField] private float roundDuration = 75f;
        [SerializeField] private int treatCount = 4;
        [SerializeField] private float unitedBarkWindow = 0.8f;
        [SerializeField] private float unitedBarkRange = 3f;
        [SerializeField] private float barkCooldown = 1.2f;

        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public float TimeRemaining { get; private set; }
        public float RoundDuration => roundDuration;
        public State Phase { get; private set; } = State.Playing;
        public bool IsGameOver => Phase == State.GameOver;
        public bool IsLevelClear => Phase == State.LevelClear;
        public int UnitedBarks { get; private set; }
        public float Pressure { get; private set; }
        public float ObjectiveProgress { get; private set; }
        public float BossHealth { get; private set; }
        public int Mistakes { get; private set; }
        public int Recoveries { get; private set; }
        public int TeamCombos { get; private set; }
        public string LevelName { get; private set; }
        public string ObjectiveText { get; private set; }
        public RoundModifier ActiveModifier { get; private set; }
        public string ModifierLabel => ActiveModifier.ToString().Replace("Surge", " Surge").Replace("Trouble", " Trouble").Replace("Panic", " Panic");
        public bool ModifierDisplayed => !string.IsNullOrEmpty(ModifierLabel);

        private DogController[] _dogs;
        private GamepadPlayerInput[] _inputs;
        private Vector2[] _dogStarts;
        private Sprite _sprite;
        private Rect _bounds;
        private System.Random _rng;
        private Transform _root;
        private readonly List<Treat> _treats = new();
        private float[] _lastBarks, _nextBarkAt;
        private LevelKind _level;
        private GameObject _robot, _sharedToy, _squirrel;
        private float _nextChaosAt;
        private int _teamActionsNeeded;
        private bool _robotInterrupted;

        public void Init(DogController[] dogs, GamepadPlayerInput[] inputs, Sprite treatSprite, Rect bounds, int seed, LevelKind level = LevelKind.BreakfastHeist)
        {
            _dogs = dogs; _inputs = inputs; _sprite = treatSprite; _bounds = bounds; _level = level; _rng = new System.Random(seed);
            _dogStarts = new Vector2[dogs.Length]; _lastBarks = new float[dogs.Length]; _nextBarkAt = new float[dogs.Length];
            for (int i = 0; i < dogs.Length; i++) { _dogStarts[i] = dogs[i].transform.position; dogs[i].OnBark += OnDogBarked; }
            _root = new GameObject("RoundObjects").transform;
            BeginRound();
        }

        public void OnTreatCollected(Treat treat, DogController dog)
        {
            if (Phase != State.Playing || treat == null) return;
            AddScore(1, dog); _treats.Remove(treat); Destroy(treat.gameObject); SpawnTreat();
            if (_level == LevelKind.BreakfastHeist) ObjectiveProgress = Mathf.Min(100f, ObjectiveProgress + 16f);
        }

        public void Restart() => BeginRound();
        public void SetRoundDuration(float seconds) { roundDuration = Mathf.Max(0.01f, seconds); if (Phase == State.Playing) TimeRemaining = Mathf.Min(TimeRemaining, roundDuration); }
        public void CompleteObjectiveForTest() { ObjectiveProgress = 100f; Pressure = Mathf.Min(Pressure, 85f); CompleteLevel(); }

        private void BeginRound()
        {
            Score = UnitedBarks = Mistakes = Recoveries = TeamCombos = 0; TimeRemaining = roundDuration; Phase = State.Playing;
            Pressure = _level == LevelKind.BreakfastHeist ? 24f : 8f; BossHealth = 100f; ObjectiveProgress = 0f;
            _teamActionsNeeded = _level == LevelKind.LivingRoomChaos ? 3 : 0; _robotInterrupted = false;
            ActiveModifier = (RoundModifier)_rng.Next(0, 3);
            LevelName = _level == LevelKind.BreakfastHeist ? "Level 1: Breakfast Heist" : "Level 2: Living Room Chaos";
            ObjectiveText = _level == LevelKind.BreakfastHeist ? "Serve breakfast while barking Pancake Robot off the table" : "Tug the giant rope toy onto the couch sunbeam together";
            for (int i = 0; i < _dogs.Length; i++) { _lastBarks[i] = -999f; _nextBarkAt[i] = 0f; _dogs[i].transform.position = _dogStarts[i]; if (_dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero; if (i < _inputs.Length && _inputs[i]) _inputs[i].enabled = true; }
            ClearRoundObjects();
            for (int i = 0; i < treatCount; i++) SpawnTreat();
            if (_level == LevelKind.BreakfastHeist) BuildRobot(); else BuildLivingRoomToy();
            _nextChaosAt = Time.time + 4f + (float)_rng.NextDouble() * 2f;
        }

        private void Update()
        {
            if (Phase != State.Playing) return;
            TimeRemaining -= Time.deltaTime;
            float pressureRate = _level == LevelKind.BreakfastHeist ? 5f : 2f;
            if (ActiveModifier == RoundModifier.PancakePanic) pressureRate *= 1.55f;
            Pressure += pressureRate * Time.deltaTime;
            if (_level == LevelKind.BreakfastHeist && _robot != null) _robot.transform.position = Vector3.Lerp(new Vector3(7, 0, 0), new Vector3(-1, 0, 0), Pressure / 100f);
            if (ActiveModifier == RoundModifier.ZoomiesSurge && Mathf.FloorToInt(Time.time) % 9 == 0) foreach (var d in _dogs) d.TriggerZoomies();
            if (Time.time >= _nextChaosAt) SpawnChaosEvent();
            if (Pressure >= 100f) { Mistakes++; AddScore(-3, null); Pressure = 72f; ObjectiveProgress = Mathf.Max(0, ObjectiveProgress - 12f); }
            if (ObjectiveProgress >= 100f && Pressure < 95f) CompleteLevel();
            if (TimeRemaining <= 0f) EndRound();
        }

        private void OnDogBarked(DogId dogId)
        {
            if (Phase != State.Playing) return;
            int idx = IndexOfDog(dogId); if (idx < 0 || Time.time < _nextBarkAt[idx]) return;
            _nextBarkAt[idx] = Time.time + barkCooldown; _lastBarks[idx] = Time.time;
            float barkPower = dogId == DogId.Cheddar ? 18f : 12f; // Cheddar louder; Cocoa steadier/faster via tuning.
            if (_level == LevelKind.BreakfastHeist && Vector2.Distance(_dogs[idx].transform.position, _robot.transform.position) < 5f)
            { Pressure = Mathf.Max(0, Pressure - barkPower); BossHealth = Mathf.Max(0, BossHealth - barkPower); _robotInterrupted = true; Recoveries++; }
            if (_squirrel != null && Vector2.Distance(_dogs[idx].transform.position, _squirrel.transform.position) < 5f)
            { Destroy(_squirrel); _squirrel = null; Pressure = Mathf.Max(0, Pressure - 10f); AddScore(2, _dogs[idx]); Recoveries++; }
            if (AllDogsBarkedRecently() && DogsAreHuddled()) { UnitedBarks++; TeamCombos++; AddScore(3, _dogs[idx]); Pressure = Mathf.Max(0, Pressure - 14f); if (_level == LevelKind.BreakfastHeist && _robotInterrupted) ObjectiveProgress += 18f; else if (_level == LevelKind.LivingRoomChaos) TeamTug(); }
        }

        private void TeamTug()
        {
            _teamActionsNeeded = Mathf.Max(0, _teamActionsNeeded - 1); ObjectiveProgress = Mathf.Min(100f, ObjectiveProgress + 34f); Pressure = Mathf.Max(0, Pressure - 8f);
            if (_sharedToy != null) _sharedToy.transform.position += Vector3.right * 2.2f;
        }

        private void CompleteLevel() { Score += 25 + Mathf.CeilToInt(TimeRemaining) + TeamCombos * 4 - Mistakes * 3; BestScore = Mathf.Max(BestScore, Score); Phase = State.LevelClear; FreezeDogs(); }
        private void EndRound() { Phase = State.GameOver; TimeRemaining = 0f; FreezeDogs(); }
        private void FreezeDogs() { for (int i = 0; i < _dogs.Length; i++) { if (i < _inputs.Length && _inputs[i]) _inputs[i].enabled = false; if (_dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero; } }
        private void AddScore(int n, DogController dog) { Score = Mathf.Max(0, Score + n); dog?.TriggerZoomies(); }
        private int IndexOfDog(DogId dogId) { for (int i = 0; i < _dogs.Length; i++) if (_dogs[i].GetComponent<DogIdentity>().Id == dogId) return i; return -1; }
        private bool AllDogsBarkedRecently() { for (int i = 0; i < _lastBarks.Length; i++) if (Time.time - _lastBarks[i] > unitedBarkWindow) return false; return true; }
        private bool DogsAreHuddled() => Vector2.Distance(_dogs[0].transform.position, _dogs[1].transform.position) <= unitedBarkRange;

        private void BuildRobot() => _robot = MakeBlock("Pancake Robot Pressure", new Vector2(7, 0), new Vector2(1.2f, 2f), new Color(0.8f, 0.25f, 0.2f));
        private void BuildLivingRoomToy()
        { _sharedToy = MakeBlock("Shared Rope Toy - Tug Together", new Vector2(-4, 0), new Vector2(2.8f, .55f), new Color(0.9f, 0.3f, 0.9f)); MakeBlock("Couch Goal Sunbeam", new Vector2(4, 0), new Vector2(3f, 2f), new Color(1f, .85f, .35f, .55f)); }
        private void SpawnChaosEvent()
        { _nextChaosAt = Time.time + 5f + (float)_rng.NextDouble() * 4f; Pressure += 8f; if (ActiveModifier == RoundModifier.SquirrelTrouble || _level == LevelKind.LivingRoomChaos) _squirrel = MakeBlock("Squirrel Trouble - bark to scare", new Vector2(RandomX(), RandomY()), new Vector2(.8f, .8f), new Color(.45f, .25f, .08f)); }
        private GameObject MakeBlock(string name, Vector2 pos, Vector2 scale, Color color) { var go = new GameObject(name); go.transform.SetParent(_root); go.transform.position = pos; go.transform.localScale = scale; var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _sprite; sr.color = color; sr.sortingOrder = 4; return go; }
        private void SpawnTreat() { var go = MakeBlock(_level == LevelKind.BreakfastHeist ? "Breakfast Plate" : "Treat Drop", new Vector2(RandomX(), RandomY()), Vector2.one * .5f, new Color(.92f,.4f,.32f)); var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = .6f; var t = go.AddComponent<Treat>(); t.Bind(this); _treats.Add(t); }
        private float RandomX() => Mathf.Lerp(_bounds.xMin + 1.2f, _bounds.xMax - 1.2f, (float)_rng.NextDouble());
        private float RandomY() => Mathf.Lerp(_bounds.yMin + 1.2f, _bounds.yMax - 1.2f, (float)_rng.NextDouble());
        private void ClearRoundObjects() { foreach (var t in _treats) if (t) Destroy(t.gameObject); _treats.Clear(); if (_root) foreach (Transform child in _root) Destroy(child.gameObject); }
        private void OnDestroy() { if (_dogs == null) return; foreach (var d in _dogs) if (d) d.OnBark -= OnDogBarked; }
    }
}
