using System.Collections.Generic;
using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Input;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Mission controller for the ArenaScene Backyard Mission vertical slice. It intentionally keeps
    /// the prototype self-contained: generated placeholder actors, deterministic pacing hooks for
    /// tests, and simple co-op rules that make bark/tug/rescue gameplay-relevant.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public enum State { Intro, Playing, PredatorWarning, PredatorAttack, LevelClear, GameOver }
        public enum RoundModifier { SquirrelTrouble, ZoomiesSurge, PancakePanic }

        private const int ItemScore = 10;
        private const int SquirrelScareScore = 3;
        private const int UnitedBarkScore = 5;
        private const int PredatorDefendedScore = 30;
        private const int RescueScore = 8;
        private const int TugScore = 25;
        private const int PredatorFailurePenalty = 10;
        private const int SquirrelPenalty = 5;
        private const int PancakePenalty = 8;

        [Header("Mission pacing")]
        [SerializeField] private float roundDuration = 75f;
        [SerializeField] private int treatCount = 5;
        [SerializeField] private int recoveryGoal = 6;
        [SerializeField] private int maxStolenFood = 3;

        [Header("Bark tuning")]
        [SerializeField] private float unitedBarkWindow = 0.8f;
        [SerializeField] private float unitedBarkRange = 3f;
        [SerializeField] private float unitedBarkCooldown = 1.2f;
        [SerializeField] private float singleBarkSquirrelRange = 4f;
        [SerializeField] private float singleBarkScareSeconds = 1.5f;
        [SerializeField] private float unitedBarkScareSeconds = 3.5f;

        [Header("Hazard pacing")]
        [SerializeField] private float squirrelBaseDelay = 2.6f;
        [SerializeField] private float squirrelTroubleDelay = 1.4f;
        [SerializeField] private float squirrelMoveSpeed = 2.2f;
        [SerializeField] private float predatorWarningAt = 10f;
        [SerializeField] private float predatorWarningSeconds = 3f;
        [SerializeField] private float tugChargePerSecond = 0.38f;

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
        public int MaxStolenFood => maxStolenFood;
        public bool PredatorResolved { get; private set; }
        public bool PredatorFailed { get; private set; }
        public bool AnyDogGrabbed => _grabbedDog >= 0;
        public float TugProgress { get; private set; }
        public bool TugComplete { get; private set; }
        public int StarRating { get; private set; }
        public RoundModifier ActiveModifier { get; private set; }
        public string ActiveModifierLabel => ActiveModifier switch
        {
            RoundModifier.SquirrelTrouble => "Squirrel Trouble",
            RoundModifier.ZoomiesSurge => "Zoomies Surge",
            _ => "Pancake Panic"
        };
        public string LastCue { get; private set; } = "Ready";
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
        private AudioSource _audio;
        private AudioClip _barkCue;
        private AudioClip _dangerCue;
        private AudioClip _successCue;

        private readonly List<Treat> _treats = new();
        private float[] _lastBarks;
        private float _nextUnitedBarkAt;
        private float _squirrelTimer;
        private float _squirrelScaredUntil;
        private float _nextSquirrelScareScoreAt;
        private Treat _squirrelTarget;
        private float _predatorTimer;
        private int _predatorTarget = -1;
        private int _grabbedDog = -1;
        private float _nextZoomiesPulseAt;

        public void Init(DogController[] dogs, GamepadPlayerInput[] inputs, Sprite treatSprite, Rect bounds, int seed)
        {
            _dogs = dogs;
            _inputs = inputs;
            _sprite = treatSprite;
            _bounds = bounds;
            _rng = new System.Random(seed);
            _dogStarts = new Vector2[dogs.Length];
            _lastBarks = new float[dogs.Length];

            for (int i = 0; i < dogs.Length; i++)
            {
                _dogStarts[i] = dogs[i].transform.position;
                dogs[i].OnBark += OnDogBarked;
                dogs[i].OnInteract += OnDogInteracted;
            }

            _treatRoot = new GameObject("Breakfast/Weenies").transform;
            BuildAudio();
            BuildMissionObjects();
            BeginRound();
        }

        public void OnTreatCollected(Treat treat, DogController dog)
        {
            if (!MissionActive() || treat == null) return;

            Score += ItemScore;
            BreakfastRecovered++;
            LastCue = $"{DogName(dog)} recovered breakfast!";
            Pulse(dog != null ? dog.gameObject : null, 1.2f);
            PlayCue(_successCue);

            _treats.Remove(treat);
            Destroy(treat.gameObject);
            SpawnTreat();
            CheckClear();
        }

        public void Restart() => BeginRound();

        public void SetRoundDuration(float seconds)
        {
            roundDuration = Mathf.Max(0.01f, seconds);
            if (MissionActive()) TimeRemaining = Mathf.Min(TimeRemaining, roundDuration);
        }

        public void ForcePredatorWarning()
        {
            if (MissionActive()) StartPredatorWarning();
        }

        public void ForcePredatorAttack()
        {
            if (MissionActive() || Phase == State.PredatorWarning) StartPredatorAttack();
        }

        public void ForceGameOver() => EndRound(false);

        private void BeginRound()
        {
            Score = 0;
            UnitedBarks = 0;
            BreakfastRecovered = 0;
            StolenFood = 0;
            PredatorResolved = false;
            PredatorFailed = false;
            TugProgress = 0f;
            TugComplete = false;
            StarRating = 0;
            TimeRemaining = roundDuration;
            Phase = State.Playing;
            LastCue = "Save the breakfast!";

            ActiveModifier = (RoundModifier)_rng.Next(0, 3);
            _nextUnitedBarkAt = 0f;
            _squirrelScaredUntil = 0f;
            _nextSquirrelScareScoreAt = 0f;
            _squirrelTarget = null;
            _squirrelTimer = SquirrelDelay();
            _predatorTimer = predatorWarningAt;
            _predatorTarget = -1;
            _grabbedDog = -1;
            _nextZoomiesPulseAt = Time.time + 6f;

            for (int i = 0; i < _dogs.Length; i++)
            {
                _lastBarks[i] = float.NegativeInfinity;
                _dogs[i].SetMode(MovementMode.Free);
                _dogs[i].transform.position = _dogStarts[i];
                if (_dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
                if (i < _inputs.Length && _inputs[i] != null) _inputs[i].enabled = true;
            }

            ClearTreats();
            for (int i = 0; i < treatCount; i++) SpawnTreat();

            PlaceObject(SquirrelObject, new Vector2(_bounds.xMax - 2f, _bounds.yMax - 2f));
            PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
            PlaceObject(RopeObject, Vector2.zero);
            SetActorState(PredatorObject, "Predator Warning", Color.red, 0.15f);
            SetActorState(RopeObject, "Rope/Tug", new Color(0.95f, 0.7f, 0.15f), 0.08f);
        }

        private void Update()
        {
            if (!MissionActive()) return;

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                EndRound(false);
                return;
            }

            TickModifier();
            TickSquirrel();
            TickPredator();
            TickTugProximity();
            CheckClear();
        }

        private void TickModifier()
        {
            if (ActiveModifier != RoundModifier.ZoomiesSurge || Time.time < _nextZoomiesPulseAt) return;

            foreach (var dog in _dogs) dog.TriggerZoomies();
            LastCue = "Zoomies surge! Hold the line!";
            _nextZoomiesPulseAt = Time.time + 10f;
            PlayCue(_barkCue);
        }

        private void TickSquirrel()
        {
            if (Time.time < _squirrelScaredUntil) return;

            var nearbySnack = FindTreatNear(SquirrelObject.transform.position, 0.3f);
            if (nearbySnack != null)
            {
                _squirrelTarget = nearbySnack;
                SquirrelStealsTarget();
                return;
            }

            if (_squirrelTarget == null)
            {
                _squirrelTimer -= Time.deltaTime;
                if (_squirrelTimer <= 0f && _treats.Count > 0)
                {
                    _squirrelTarget = _treats[_rng.Next(_treats.Count)];
                    LastCue = "Squirrel is stealing breakfast!";
                    SetActorState(SquirrelObject, "Squirrel: STEALING!", new Color(0.7f, 0.35f, 0.08f), 0.25f);
                    PlayCue(_dangerCue);
                }
                return;
            }

            SquirrelObject.transform.position = Vector3.MoveTowards(
                SquirrelObject.transform.position,
                _squirrelTarget.transform.position,
                Time.deltaTime * squirrelMoveSpeed);

            if (Vector2.Distance(SquirrelObject.transform.position, _squirrelTarget.transform.position) < 0.25f)
                SquirrelStealsTarget();
        }

        private void SquirrelStealsTarget()
        {
            if (_squirrelTarget != null)
            {
                _treats.Remove(_squirrelTarget);
                Destroy(_squirrelTarget.gameObject);
                SpawnTreat();
            }

            StolenFood++;
            Score -= ActiveModifier == RoundModifier.PancakePanic ? PancakePenalty : SquirrelPenalty;
            _squirrelTarget = null;
            _squirrelTimer = SquirrelDelay();
            LastCue = "Squirrel escaped with food!";
            SetActorState(SquirrelObject, "Squirrel: GOT ONE", Color.gray, 0.18f);
            PlayCue(_dangerCue);

            if (StolenFood >= maxStolenFood) EndRound(false);
        }

        private void TickPredator()
        {
            if (PredatorResolved || PredatorFailed) return;

            _predatorTimer -= Time.deltaTime;
            if (_predatorTimer <= predatorWarningSeconds && Phase == State.Playing) StartPredatorWarning();
            if (_predatorTimer <= 0f && Phase == State.PredatorWarning) StartPredatorAttack();
        }

        private void StartPredatorWarning()
        {
            _nextUnitedBarkAt = 0f;
            Phase = State.PredatorWarning;
            _predatorTarget = _rng.Next(_dogs.Length);
            LastCue = $"Predator warning near {DogName(_dogs[_predatorTarget])}! Huddle and bark!";
            PredatorObject.name = "Predator Warning";
            PlaceObject(PredatorObject, (Vector2)_dogs[_predatorTarget].transform.position + Vector2.up * 2f);
            SetActorState(PredatorObject, "PREDATOR WARNING", new Color(1f, 0.08f, 0.08f), 0.35f);
            PlayCue(_dangerCue);
        }

        private void StartPredatorAttack()
        {
            _nextUnitedBarkAt = 0f;
            Phase = State.PredatorAttack;
            if (_predatorTarget < 0) _predatorTarget = 0;

            PredatorObject.name = "Predator Attack";
            PlaceObject(PredatorObject, _dogs[_predatorTarget].transform.position);
            SetActorState(PredatorObject, "PREDATOR ATTACK!", new Color(0.8f, 0f, 0f), 0.45f);

            _grabbedDog = _predatorTarget;
            _dogs[_grabbedDog].SetMode(MovementMode.Stunned);
            PredatorFailed = true;
            Score -= PredatorFailurePenalty;
            LastCue = $"{DogName(_dogs[_grabbedDog])} is grabbed! Partner bark rescue!";
            PlayCue(_dangerCue);
        }

        private void ResolvePredator()
        {
            PredatorResolved = true;
            PredatorFailed = false;
            Phase = State.Playing;
            Score += PredatorDefendedScore;
            LastCue = "United bark drove the predator away!";
            PredatorObject.name = "Predator Driven Away";
            PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
            SetActorState(PredatorObject, "Predator: DRIVEN AWAY", Color.gray, 0.08f);
            PlayCue(_successCue);
            CheckClear();
        }

        private void TickTugProximity()
        {
            if (TugComplete || _dogs.Length < 2) return;

            bool cheddarNear = Vector2.Distance(_dogs[0].transform.position, RopeObject.transform.position) < 1.6f;
            bool cocoaNear = Vector2.Distance(_dogs[1].transform.position, RopeObject.transform.position) < 1.6f;
            if (!cheddarNear || !cocoaNear) return;

            TugProgress = Mathf.Min(1f, TugProgress + Time.deltaTime * tugChargePerSecond);
            SetActorState(RopeObject, $"Rope/Tug {Mathf.RoundToInt(TugProgress * 100f)}%", new Color(1f, 0.78f, 0.22f), 0.18f);
            if (TugProgress >= 1f) CompleteTug();
        }

        private void OnDogInteracted(DogId dogId)
        {
            if (!MissionActive() || TugComplete) return;

            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;
            if (Vector2.Distance(_dogs[dogIndex].transform.position, RopeObject.transform.position) > 1.8f) return;

            TugProgress = Mathf.Min(1f, TugProgress + 0.2f);
            LastCue = $"{DogName(_dogs[dogIndex])} grabbed the rope!";
            SetActorState(RopeObject, $"Rope/Tug {Mathf.RoundToInt(TugProgress * 100f)}%", new Color(1f, 0.78f, 0.22f), 0.2f);
            PlayCue(_barkCue);
            if (TugProgress >= 1f) CompleteTug();
        }

        private void CompleteTug()
        {
            TugComplete = true;
            Score += TugScore;
            LastCue = "Rope tug complete!";
            RopeObject.name = "Rope/Tug Complete";
            SetActorState(RopeObject, "ROPE COMPLETE!", new Color(0.3f, 1f, 0.3f), 0.08f);
            PlayCue(_successCue);
            CheckClear();
        }

        private void OnDogBarked(DogId dogId)
        {
            if (!MissionActive()) return;

            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;

            _lastBarks[dogIndex] = Time.time;
            var dog = _dogs[dogIndex];

            if (Vector2.Distance(dog.transform.position, SquirrelObject.transform.position) < singleBarkSquirrelRange)
                ScareSquirrel(singleBarkScareSeconds, $"{DogName(dog)} scared the squirrel!", true);

            if (_grabbedDog >= 0 && dogIndex != _grabbedDog &&
                Vector2.Distance(dog.transform.position, _dogs[_grabbedDog].transform.position) < 2f)
            {
                RescueGrabbedDog(dog);
            }

            if (Time.time < _nextUnitedBarkAt || !AllDogsBarkedRecently() || !DogsAreHuddled()) return;

            UnitedBarks++;
            Score += UnitedBarkScore;
            _nextUnitedBarkAt = Time.time + unitedBarkCooldown;
            ScareSquirrel(unitedBarkScareSeconds, "United bark shook the whole yard!", false);

            if (Phase == State.PredatorWarning || Phase == State.PredatorAttack) ResolvePredator();
        }

        private void ScareSquirrel(float seconds, string cue, bool awardScore)
        {
            _squirrelTarget = null;
            _squirrelScaredUntil = Mathf.Max(_squirrelScaredUntil, Time.time + seconds);
            _squirrelTimer = SquirrelDelay();
            if (awardScore && Time.time >= _nextSquirrelScareScoreAt)
            {
                Score += SquirrelScareScore;
                _nextSquirrelScareScoreAt = Time.time + 1f;
            }
            LastCue = cue;
            SetActorState(SquirrelObject, "Squirrel: SCARED", new Color(0.85f, 0.85f, 0.85f), 0.08f);
            PlayCue(_barkCue);
        }

        private void RescueGrabbedDog(DogController rescuer)
        {
            if (_grabbedDog < 0) return;

            _dogs[_grabbedDog].SetMode(MovementMode.Free);
            _grabbedDog = -1;
            Phase = State.Playing;
            Score += RescueScore;
            LastCue = $"{DogName(rescuer)} rescued their sibling!";
            PlayCue(_successCue);
        }

        private void CheckClear()
        {
            if (Phase == State.LevelClear || Phase == State.GameOver) return;
            if (BreakfastRecovered >= recoveryGoal && PredatorResolved && TugComplete) EndRound(true);
        }

        private void EndRound(bool clear)
        {
            Phase = clear ? State.LevelClear : State.GameOver;
            if (clear)
            {
                Score += Mathf.CeilToInt(TimeRemaining);
                StarRating = Score >= 125 ? 3 : Score >= 85 ? 2 : 1;
                LastCue = $"Backyard saved! {StarRating}/3 stars";
                PlayCue(_successCue);
            }
            else
            {
                StarRating = 0;
                LastCue = "Mission failed — restart and coordinate!";
                PlayCue(_dangerCue);
            }

            foreach (var dog in _dogs)
            {
                dog.SetMode(MovementMode.Free);
                if (dog.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
            }

            for (int i = 0; i < _inputs.Length; i++)
            {
                if (_inputs[i] != null) _inputs[i].enabled = false;
            }
        }

        private bool MissionActive() => Phase == State.Playing || Phase == State.PredatorWarning || Phase == State.PredatorAttack;

        private float SquirrelDelay() => ActiveModifier == RoundModifier.SquirrelTrouble ? squirrelTroubleDelay : squirrelBaseDelay;

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

        private Treat FindTreatNear(Vector2 position, float range)
        {
            foreach (var treat in _treats)
            {
                if (treat != null && Vector2.Distance(position, treat.transform.position) <= range) return treat;
            }
            return null;
        }

        private void SpawnTreat()
        {
            const float margin = 1.2f;
            float x = Mathf.Lerp(_bounds.xMin + margin, _bounds.xMax - margin, (float)_rng.NextDouble());
            float y = Mathf.Lerp(_bounds.yMin + margin, _bounds.yMax - margin, (float)_rng.NextDouble());

            var go = new GameObject("Breakfast/Weenie");
            go.transform.SetParent(_treatRoot);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = new Color(0.92f, 0.4f, 0.32f);
            sr.sortingOrder = 5;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var treat = go.AddComponent<Treat>();
            treat.Bind(this);
            _treats.Add(treat);

            AddWorldLabel(go, "Weenie", Vector3.up * 0.7f, 16, Color.white);
        }

        private void ClearTreats()
        {
            foreach (var treat in _treats)
            {
                if (treat != null) Destroy(treat.gameObject);
            }
            _treats.Clear();
        }

        private void BuildMissionObjects()
        {
            SquirrelObject = MakeActor("Squirrel", new Color(0.55f, 0.32f, 0.12f), 0.7f, "Squirrel", 0.18f);
            PredatorObject = MakeActor("Predator Warning", new Color(0.7f, 0.05f, 0.08f), 1.1f, "Predator Warning", 0.25f);
            RopeObject = MakeActor("Rope/Tug", new Color(0.95f, 0.7f, 0.15f), 0.9f, "Rope/Tug", 0.1f);
        }

        private GameObject MakeActor(string name, Color color, float scale, string label, float pulse)
        {
            var go = new GameObject(name);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = color;
            sr.sortingOrder = 6;

            AddWorldLabel(go, label, Vector3.up * 0.85f, 24, Color.white);
            go.AddComponent<MissionActorFeedback>().Init(sr, label, pulse);
            return go;
        }

        private static TextMesh AddWorldLabel(GameObject parent, string text, Vector3 offset, int size, Color color)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(parent.transform);
            labelGo.transform.localPosition = offset;
            labelGo.transform.localRotation = Quaternion.identity;
            labelGo.transform.localScale = Vector3.one * 0.08f;

            var label = labelGo.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            return label;
        }

        private static void SetActorState(GameObject go, string label, Color color, float pulse)
        {
            if (go == null) return;
            if (go.TryGetComponent<MissionActorFeedback>(out var feedback)) feedback.SetState(label, color, pulse);
        }

        private static void Pulse(GameObject go, float amount)
        {
            if (go != null && go.TryGetComponent<MissionActorFeedback>(out var feedback)) feedback.Pulse(amount);
        }

        private void PlaceObject(GameObject go, Vector2 position)
        {
            if (go != null) go.transform.position = position;
        }

        private string DogName(DogController dog)
        {
            if (dog == null) return "A dog";
            var id = dog.GetComponent<DogIdentity>();
            return id != null ? id.Id.ToString() : dog.name;
        }

        private void BuildAudio()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.volume = 0.18f;
            _barkCue = MakeTone("MissionBarkCue", 520f, 0.08f);
            _dangerCue = MakeTone("MissionDangerCue", 180f, 0.16f);
            _successCue = MakeTone("MissionSuccessCue", 740f, 0.12f);
        }

        private AudioClip MakeTone(string name, float frequency, float seconds)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * seconds);
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (i / (float)sampleCount);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.25f;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void PlayCue(AudioClip clip)
        {
            if (_audio != null && clip != null) _audio.PlayOneShot(clip);
        }

        private void OnDestroy()
        {
            if (_dogs == null) return;
            foreach (var dog in _dogs)
            {
                if (dog == null) continue;
                dog.OnBark -= OnDogBarked;
                dog.OnInteract -= OnDogInteracted;
            }
        }
    }

    /// <summary>Lightweight placeholder polish: readable world label + idle pulse/rotation.</summary>
    public sealed class MissionActorFeedback : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private TextMesh _label;
        private Vector3 _baseScale;
        private float _pulseAmount;

        public string Label => _label != null ? _label.text : string.Empty;

        public void Init(SpriteRenderer renderer, string label, float pulseAmount)
        {
            _renderer = renderer;
            _label = GetComponentInChildren<TextMesh>();
            _baseScale = transform.localScale;
            _pulseAmount = pulseAmount;
            SetState(label, renderer != null ? renderer.color : Color.white, pulseAmount);
        }

        public void SetState(string label, Color color, float pulseAmount)
        {
            if (_label != null) _label.text = label;
            if (_renderer != null) _renderer.color = color;
            _pulseAmount = pulseAmount;
        }

        public void Pulse(float amount)
        {
            _pulseAmount = Mathf.Max(_pulseAmount, amount);
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * _pulseAmount;
            transform.localScale = _baseScale * pulse;
            if (name.Contains("Squirrel")) transform.Rotate(0f, 0f, 80f * Time.deltaTime);
            if (name.Contains("Rope")) transform.Rotate(0f, 0f, 45f * Time.deltaTime);
            if (_label != null) _label.transform.rotation = Quaternion.identity;
        }
    }
}
