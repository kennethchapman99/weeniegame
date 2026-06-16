using System.Collections.Generic;
using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Input;

namespace CheddarAndCocoa.Game
{
    public sealed class GameManager : MonoBehaviour
    {
        public enum State { Intro, Playing, PredatorWarning, PredatorAttack, LevelClear, GameOver }
        public enum RoundModifier { SquirrelTrouble, ZoomiesSurge, PancakePanic }

        [SerializeField] private float roundDuration = 60f;
        [SerializeField] private int treatCount = 5;
        [SerializeField] private int recoveryGoal = 6;
        [SerializeField] private float unitedBarkWindow = 0.8f;
        [SerializeField] private float unitedBarkRange = 3f;
        [SerializeField] private float unitedBarkCooldown = 1.2f;

        public int Score { get; private set; }
        public float TimeRemaining { get; private set; }
        public float RoundDuration => roundDuration;
        public State Phase { get; private set; } = State.Intro;
        public bool IsGameOver => Phase == State.GameOver;
        public bool IsLevelClear => Phase == State.LevelClear;
        public int UnitedBarks { get; private set; }
        public int BreakfastRecovered { get; private set; }
        public int BreakfastGoal => recoveryGoal;
        public int StolenFood { get; private set; }
        public int MaxStolenFood => 3;
        public bool PredatorResolved { get; private set; }
        public bool PredatorFailed { get; private set; }
        public bool AnyDogGrabbed => _grabbedDog >= 0;
        public float TugProgress { get; private set; }
        public bool TugComplete { get; private set; }
        public int StarRating { get; private set; }
        public RoundModifier ActiveModifier { get; private set; }
        public string ActiveModifierLabel => ActiveModifier switch { RoundModifier.SquirrelTrouble => "Squirrel Trouble", RoundModifier.ZoomiesSurge => "Zoomies Surge", _ => "Pancake Panic" };
        public GameObject SquirrelObject { get; private set; }
        public GameObject PredatorObject { get; private set; }
        public GameObject RopeObject { get; private set; }

        private DogController[] _dogs;
        private GamepadPlayerInput[] _inputs;
        private Vector2[] _dogStarts;
        private Sprite _sprite;
        private Rect _bounds;
        private System.Random _rng;
        private Transform _treatRoot;
        private readonly List<Treat> _treats = new();
        private float[] _lastBarks;
        private float _nextUnitedBarkAt;
        private float _squirrelTimer;
        private float _squirrelScaredUntil;
        private Treat _squirrelTarget;
        private float _predatorTimer;
        private int _predatorTarget = -1;
        private int _grabbedDog = -1;
        private float _grabbedUntil;

        public void Init(DogController[] dogs, GamepadPlayerInput[] inputs, Sprite treatSprite, Rect bounds, int seed)
        {
            _dogs = dogs; _inputs = inputs; _sprite = treatSprite; _bounds = bounds; _rng = new System.Random(seed);
            _dogStarts = new Vector2[dogs.Length]; _lastBarks = new float[dogs.Length];
            for (int i = 0; i < dogs.Length; i++) { _dogStarts[i] = dogs[i].transform.position; dogs[i].OnBark += OnDogBarked; dogs[i].OnInteract += OnDogInteracted; }
            _treatRoot = new GameObject("Breakfast/Weenies").transform;
            BuildMissionObjects();
            BeginRound();
        }

        public void OnTreatCollected(Treat treat, DogController dog)
        {
            if (!MissionActive() || treat == null) return;
            Score += 10; BreakfastRecovered++;
            _treats.Remove(treat); Destroy(treat.gameObject); SpawnTreat(); CheckClear();
        }

        public void Restart() => BeginRound();
        public void SetRoundDuration(float seconds) { roundDuration = Mathf.Max(0.01f, seconds); if (MissionActive()) TimeRemaining = Mathf.Min(TimeRemaining, roundDuration); }
        public void ForcePredatorWarning() { if (MissionActive()) StartPredatorWarning(); }
        public void ForcePredatorAttack() { if (MissionActive() || Phase == State.PredatorWarning) StartPredatorAttack(); }
        public void ForceGameOver() => EndRound(false);

        private void BeginRound()
        {
            Score = 0; UnitedBarks = 0; BreakfastRecovered = 0; StolenFood = 0; PredatorResolved = false; PredatorFailed = false; TugProgress = 0f; TugComplete = false; StarRating = 0;
            TimeRemaining = roundDuration; Phase = State.Playing; _nextUnitedBarkAt = 0f; _squirrelScaredUntil = 0f; _squirrelTarget = null; _grabbedDog = -1;
            ActiveModifier = (RoundModifier)_rng.Next(0, 3);
            _squirrelTimer = ActiveModifier == RoundModifier.SquirrelTrouble ? 1.2f : 2.2f; _predatorTimer = 8f;
            for (int i = 0; i < _dogs.Length; i++) { _lastBarks[i] = float.NegativeInfinity; _dogs[i].SetMode(MovementMode.Free); _dogs[i].transform.position = _dogStarts[i]; if (_dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero; if (i < _inputs.Length && _inputs[i] != null) _inputs[i].enabled = true; }
            ClearTreats(); for (int i = 0; i < treatCount; i++) SpawnTreat();
            PlaceObject(SquirrelObject, new Vector2(_bounds.xMax - 2f, _bounds.yMax - 2f)); PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f)); PlaceObject(RopeObject, Vector2.zero);
        }

        private void BuildMissionObjects()
        {
            SquirrelObject = MakeMarker("Squirrel", new Color(0.55f, 0.32f, 0.12f), 0.7f);
            PredatorObject = MakeMarker("Predator Warning", new Color(0.7f, 0.05f, 0.08f), 1.1f);
            RopeObject = MakeMarker("Rope/Tug", new Color(0.95f, 0.7f, 0.15f), 0.9f);
        }
        private GameObject MakeMarker(string name, Color color, float scale) { var go = new GameObject(name); go.transform.localScale = Vector3.one * scale; var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _sprite; sr.color = color; sr.sortingOrder = 6; return go; }
        private void PlaceObject(GameObject go, Vector2 p) { if (go != null) go.transform.position = p; }

        private void Update()
        {
            if (!MissionActive()) return;
            TimeRemaining -= Time.deltaTime; if (TimeRemaining <= 0f) { EndRound(false); return; }
            if (ActiveModifier == RoundModifier.ZoomiesSurge && Mathf.FloorToInt(Time.time) % 10 == 0) foreach (var d in _dogs) d.TriggerZoomies();
            TickSquirrel(); TickPredator(); TickTugProximity(); CheckClear();
        }

        private bool MissionActive() => Phase == State.Playing || Phase == State.PredatorWarning || Phase == State.PredatorAttack;
        private void TickSquirrel()
        {
            if (Time.time < _squirrelScaredUntil) return;
            var nearbySnack = FindTreatNear(SquirrelObject.transform.position, 0.3f);
            if (nearbySnack != null) { _squirrelTarget = nearbySnack; SquirrelStealsTarget(); return; }
            if (_squirrelTarget == null) { _squirrelTimer -= Time.deltaTime; if (_squirrelTimer <= 0f && _treats.Count > 0) _squirrelTarget = _treats[_rng.Next(_treats.Count)]; return; }
            if (_squirrelTarget == null) return;
            SquirrelObject.transform.position = Vector3.MoveTowards(SquirrelObject.transform.position, _squirrelTarget.transform.position, Time.deltaTime * 2.2f);
            if (Vector2.Distance(SquirrelObject.transform.position, _squirrelTarget.transform.position) < 0.25f) SquirrelStealsTarget();
        }
        private void SquirrelStealsTarget() { if (_squirrelTarget != null) { _treats.Remove(_squirrelTarget); Destroy(_squirrelTarget.gameObject); SpawnTreat(); } StolenFood++; Score -= ActiveModifier == RoundModifier.PancakePanic ? 8 : 5; _squirrelTarget = null; _squirrelTimer = ActiveModifier == RoundModifier.SquirrelTrouble ? 1.1f : 2f; if (StolenFood >= MaxStolenFood) EndRound(false); }

        private void TickPredator() { if (PredatorResolved || PredatorFailed) return; _predatorTimer -= Time.deltaTime; if (_predatorTimer <= 2f && Phase == State.Playing) StartPredatorWarning(); if (_predatorTimer <= 0f && Phase == State.PredatorWarning) StartPredatorAttack(); }
        private void StartPredatorWarning() { _nextUnitedBarkAt = 0f; Phase = State.PredatorWarning; _predatorTarget = _rng.Next(_dogs.Length); PredatorObject.name = "Predator Warning"; PlaceObject(PredatorObject, (Vector2)_dogs[_predatorTarget].transform.position + Vector2.up * 2f); }
        private void StartPredatorAttack() { _nextUnitedBarkAt = 0f; Phase = State.PredatorAttack; PredatorObject.name = "Predator Attack"; if (_predatorTarget < 0) _predatorTarget = 0; PlaceObject(PredatorObject, _dogs[_predatorTarget].transform.position); _grabbedDog = _predatorTarget; _grabbedUntil = Time.time + 3f; _dogs[_grabbedDog].SetMode(MovementMode.Stunned); PredatorFailed = true; Score -= 10; }
        private void ResolvePredator() { PredatorResolved = true; PredatorFailed = false; Phase = State.Playing; Score += 30; PredatorObject.name = "Predator Driven Away"; PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f)); CheckClear(); }

        private void TickTugProximity() { if (TugComplete) return; bool both = _dogs.Length >= 2 && Vector2.Distance(_dogs[0].transform.position, RopeObject.transform.position) < 1.6f && Vector2.Distance(_dogs[1].transform.position, RopeObject.transform.position) < 1.6f; if (both) { TugProgress = Mathf.Min(1f, TugProgress + Time.deltaTime * 0.45f); if (TugProgress >= 1f) CompleteTug(); } }
        private void OnDogInteracted(DogId dogId) { if (!MissionActive() || TugComplete) return; int idx = IndexOfDog(dogId); if (idx >= 0 && Vector2.Distance(_dogs[idx].transform.position, RopeObject.transform.position) < 1.8f) TugProgress = Mathf.Min(1f, TugProgress + 0.2f); if (TugProgress >= 1f) CompleteTug(); }
        private void CompleteTug() { TugComplete = true; Score += 25; RopeObject.name = "Rope/Tug Complete"; CheckClear(); }

        private void OnDogBarked(DogId dogId)
        {
            if (!MissionActive()) return; int idx = IndexOfDog(dogId); if (idx < 0) return; _lastBarks[idx] = Time.time;
            if (Vector2.Distance(_dogs[idx].transform.position, SquirrelObject.transform.position) < 4f) { _squirrelTarget = null; _squirrelScaredUntil = Time.time + 1.5f; _squirrelTimer = 2f; Score += 3; }
            if (_grabbedDog >= 0 && idx != _grabbedDog && Vector2.Distance(_dogs[idx].transform.position, _dogs[_grabbedDog].transform.position) < 2f) RescueGrabbedDog();
            if (Time.time < _nextUnitedBarkAt || !AllDogsBarkedRecently() || !DogsAreHuddled()) return;
            UnitedBarks++; Score += 5; _nextUnitedBarkAt = Time.time + unitedBarkCooldown; _squirrelScaredUntil = Mathf.Max(_squirrelScaredUntil, Time.time + 3.5f); _squirrelTarget = null;
            if (Phase == State.PredatorWarning || Phase == State.PredatorAttack) ResolvePredator();
        }
        private void RescueGrabbedDog() { if (_grabbedDog < 0) return; _dogs[_grabbedDog].SetMode(MovementMode.Free); _grabbedDog = -1; Phase = State.Playing; Score += 8; }
        private Treat FindTreatNear(Vector2 position, float range) { foreach (var t in _treats) if (t != null && Vector2.Distance(position, t.transform.position) <= range) return t; return null; }
        private void CheckClear() { if (Phase == State.LevelClear || Phase == State.GameOver) return; if (BreakfastRecovered >= recoveryGoal && PredatorResolved && TugComplete) EndRound(true); }
        private void EndRound(bool clear) { Phase = clear ? State.LevelClear : State.GameOver; if (clear) { Score += Mathf.CeilToInt(TimeRemaining); StarRating = Score >= 115 ? 3 : Score >= 75 ? 2 : 1; } else StarRating = 0; foreach (var d in _dogs) { d.SetMode(MovementMode.Free); if (d.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero; } for (int i = 0; i < _inputs.Length; i++) if (_inputs[i] != null) _inputs[i].enabled = false; }
        private int IndexOfDog(DogId dogId) { for (int i = 0; i < _dogs.Length; i++) if (_dogs[i] != null && _dogs[i].GetComponent<DogIdentity>().Id == dogId) return i; return -1; }
        private bool AllDogsBarkedRecently() { for (int i = 0; i < _lastBarks.Length; i++) if (Time.time - _lastBarks[i] > unitedBarkWindow) return false; return true; }
        private bool DogsAreHuddled() { Vector2 first = _dogs[0].transform.position; for (int i = 1; i < _dogs.Length; i++) if (Vector2.Distance(first, _dogs[i].transform.position) > unitedBarkRange) return false; return true; }
        private void SpawnTreat() { const float margin = 1.2f; float x = Mathf.Lerp(_bounds.xMin + margin, _bounds.xMax - margin, (float)_rng.NextDouble()); float y = Mathf.Lerp(_bounds.yMin + margin, _bounds.yMax - margin, (float)_rng.NextDouble()); var go = new GameObject("Breakfast/Weenie"); go.transform.SetParent(_treatRoot); go.transform.position = new Vector3(x, y, 0f); go.transform.localScale = new Vector3(0.5f, 0.5f, 1f); var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _sprite; sr.color = new Color(0.92f, 0.4f, 0.32f); sr.sortingOrder = 5; var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.6f; var treat = go.AddComponent<Treat>(); treat.Bind(this); _treats.Add(treat); }
        private void ClearTreats() { foreach (var t in _treats) if (t != null) Destroy(t.gameObject); _treats.Clear(); }
        private void OnDestroy() { if (_dogs == null) return; foreach (var d in _dogs) { if (d != null) { d.OnBark -= OnDogBarked; d.OnInteract -= OnDogInteracted; } } }
    }
}
