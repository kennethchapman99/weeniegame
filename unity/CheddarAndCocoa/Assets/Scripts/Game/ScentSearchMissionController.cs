using System.Collections.Generic;
using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class ScentSearchMissionController : IMissionController, IMissionInteractionController
    {
        private const int RequiredFinds = 3;
        private const int MaxWastedDigs = 4;
        private const float DigRange = 2f;

        private readonly ScentSearchMissionState _state = new();
        private MissionContext _context;
        private Vector2[] _digSpots;
        private GameObject[] _digMarkers;
        private int _buriedSpot = -1;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.ScentSearch;
        public bool IsComplete => _state.ReadyToClear(RequiredFinds);
        public bool IsFailed => _state.TooManyWastedDigs(MaxWastedDigs);
        public string FailReason => IsFailed ? "The dogs dug up half the yard chasing cold scents and ran out of patience." : null;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildScentSummary(_state, RequiredFinds);
        public Vector2 EntryTarget => _digSpots != null && _digSpots.Length > 0 ? _digSpots[0] : Vector2.zero;
        public ScentSearchMissionState State => _state;
        public Vector2[] DigSpots => _digSpots != null ? (Vector2[])_digSpots.Clone() : new Vector2[0];
        public string ObjectiveLabel =>
            $"Sniff (bark) for HOT/COLD, then dig (interact): bones {_state.Found}/{RequiredFinds}, cold digs {_state.WastedDigs}/{MaxWastedDigs}";

        public static Vector2[] ComputeDigSpots(Rect bounds)
        {
            Vector2 P(float x, float y) => new(
                bounds.center.x + x * bounds.width * 0.5f,
                bounds.center.y + y * bounds.height * 0.5f);
            return new[] { P(-0.78f, 0.58f), P(0.68f, 0.64f), P(-0.74f, -0.58f), P(0.32f, -0.68f), P(0.82f, 0.08f), P(-0.18f, 0.36f) };
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _digSpots = ComputeDigSpots(_context.Bounds);
            BuildMarkers();
        }

        public void StartMission()
        {
            _state.Reset();
            for (int i = 0; i < _digMarkers.Length; i++)
            {
                _digMarkers[i].transform.position = _digSpots[i];
                _digMarkers[i].SetActive(true);
            }
            ChooseBuriedSpot();
        }

        public void Tick(float deltaTime, float now) { }

        public bool HandleBark(int dogIndex)
        {
            Sniff(dogIndex);
            return true;
        }

        public bool HandleInteract(int dogIndex)
        {
            if (dogIndex >= 0 && dogIndex < _context.Dogs.Length)
                DigAtSpot(dogIndex, NearestActiveDigSpot(_context.Dogs[dogIndex].transform.position), false);
            return true;
        }

        public void Cleanup()
        {
            if (_digMarkers == null) return;
            foreach (var marker in _digMarkers)
                if (marker != null) marker.SetActive(false);
        }

        public void StageDogsForEntry()
        {
            if (_context.Dogs == null || _context.Dogs.Length < 2) return;
            Vector2 inward = _context.Bounds.center - EntryTarget;
            inward = inward.sqrMagnitude < 0.01f ? Vector2.down : inward.normalized;
            Vector2 center = EntryTarget + inward * 7f;
            Vector2 side = new Vector2(-inward.y, inward.x) * 1.5f;
            _context.Dogs[0].transform.position = center - side;
            _context.Dogs[1].transform.position = center + side;
            foreach (var dog in _context.Dogs)
                if (dog != null && dog.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            int nearest = dogIndex >= 0 && dogIndex < _context.Dogs.Length
                ? NearestActiveDigSpot(_context.Dogs[dogIndex].transform.position)
                : -1;
            target = nearest >= 0 ? _digMarkers[nearest].transform : null;
            copy = "SNIFF PATCH";
            hideDistance = 1.4f;
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("scent_search", score, timeRemaining, _state.Found, RequiredFinds, _state.WastedDigs,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceSniff(DogId dogId) => Sniff(_context.IndexOfDog(dogId));

        public void ForceDigCorrect(DogId dogId) => DigAtSpot(_context.IndexOfDog(dogId), _buriedSpot, true);

        public void ForceDigWrong(DogId dogId)
        {
            for (int i = 0; i < _digMarkers.Length; i++)
            {
                if (_digMarkers[i] == null || !_digMarkers[i].activeSelf || i == _buriedSpot) continue;
                DigAtSpot(_context.IndexOfDog(dogId), i, true);
                return;
            }
        }

        private void Sniff(int dogIndex)
        {
            if (dogIndex < 0 || dogIndex >= _context.Dogs.Length || _buriedSpot < 0) return;
            _state.AddSniff();
            float distance = Vector2.Distance(_context.Dogs[dogIndex].transform.position, _digSpots[_buriedSpot]);
            string heat = distance < 5f ? "RED HOT" : distance < 11f ? "WARM" : "COLD";
            if (distance < 5f) _context.AddScore(ScoreEventCatalog.ScentSniff.Points, ScoreEventCatalog.ScentSniff.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
            _context.SetCue($"{DogName(dogIndex)} sniffs... the bone scent is {heat}!");
            _context.SetJuice(distance < 5f ? GameManager.JuiceFeedbackKind.SuccessPop : GameManager.JuiceFeedbackKind.BarkBurst, $"SCENT: {heat}");
            _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, heat,
                distance < 5f ? new Color(1f, 0.45f, 0.2f) : new Color(0.6f, 0.75f, 1f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
            _context.LogEvent("ScentSniff", heat);
            _context.LogObjectiveChanged();
        }

        private void DigAtSpot(int dogIndex, int spotIndex, bool force)
        {
            if (dogIndex < 0 || dogIndex >= _context.Dogs.Length) return;
            if (spotIndex < 0 || spotIndex >= _digMarkers.Length || !_digMarkers[spotIndex].activeSelf)
            {
                _context.MarkFailedInteraction(DogIdAt(dogIndex), "nothing to dig here");
                return;
            }
            if (!force && Vector2.Distance(_context.Dogs[dogIndex].transform.position, _digSpots[spotIndex]) > DigRange)
            {
                _context.MarkFailedInteraction(DogIdAt(dogIndex), "too far from the dig spot");
                return;
            }

            if (spotIndex == _buriedSpot)
            {
                _state.AddFind();
                _digMarkers[spotIndex].SetActive(false);
                _context.CreditDog(dogIndex);
                if (dogIndex < _context.DogFeedback.Length && _context.DogFeedback[dogIndex] != null)
                    _context.DogFeedback[dogIndex].ShowProudBrief();
                _context.AddScore(ScoreEventCatalog.BoneFound.Points, ScoreEventCatalog.BoneFound.Label);
                _context.SetFeedback(GameManager.FeedbackKind.PartnerRescue);
                _context.SetCue($"{DogName(dogIndex)} dug up a buried bone! ({_state.Found}/{RequiredFinds})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.BoneFound.Label);
                _context.SpawnWorldPop(_digSpots[spotIndex], "BONE!", new Color(0.95f, 0.9f, 0.7f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                _context.RequestRumble("bone_found", 0.26f, 0.5f, 0.16f);
                _context.LogEvent("BoneFound", $"{_state.Found}/{RequiredFinds}");
                if (IsComplete)
                    _context.AddScore(ScoreEventCatalog.ScentSearchComplete.Points, ScoreEventCatalog.ScentSearchComplete.Label);
                else
                    ChooseBuriedSpot();
            }
            else
            {
                _state.AddWastedDig();
                _context.AddScore(ScoreEventCatalog.ColdDig.Points, ScoreEventCatalog.ColdDig.Label);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                _context.SetCue($"{DogName(dogIndex)} dug a cold hole - nothing here ({_state.WastedDigs}/{MaxWastedDigs}).");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, ScoreEventCatalog.ColdDig.Label);
                if (dogIndex < _context.DogFeedback.Length && _context.DogFeedback[dogIndex] != null)
                    _context.DogFeedback[dogIndex].ShowPanic();
                _context.SpawnWorldPop(_digSpots[spotIndex], "COLD!", new Color(0.6f, 0.75f, 1f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
                _context.RequestRumble("cold_dig", 0.14f, 0.28f, 0.12f);
                _context.LogEvent("ColdDig", $"{_state.WastedDigs}/{MaxWastedDigs}");
            }
            if (!IsComplete && !IsFailed) _context.LogObjectiveChanged();
        }

        private void ChooseBuriedSpot()
        {
            var active = new List<int>();
            for (int i = 0; i < _digMarkers.Length; i++)
                if (_digMarkers[i] != null && _digMarkers[i].activeSelf) active.Add(i);
            _buriedSpot = active.Count == 0 ? -1 : active[_context.Random().Next(active.Count)];
        }

        private int NearestActiveDigSpot(Vector2 position)
        {
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < _digMarkers.Length; i++)
            {
                if (_digMarkers[i] == null || !_digMarkers[i].activeSelf) continue;
                float distance = Vector2.Distance(position, _digMarkers[i].transform.position);
                if (distance >= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            return best;
        }

        private void BuildMarkers()
        {
            _digMarkers = new GameObject[_digSpots.Length];
            for (int i = 0; i < _digSpots.Length; i++)
            {
                var marker = new GameObject($"DigSpot_{i}");
                marker.transform.position = _digSpots[i];
                marker.transform.localScale = new Vector3(1.6f, 1f, 1f);
                var renderer = marker.AddComponent<SpriteRenderer>();
                renderer.sprite = _context.ActorSprite;
                renderer.color = new Color(0.42f, 0.3f, 0.16f);
                renderer.sortingOrder = 3;
                _context.AddWorldLabel(marker, "DIG?", Vector3.up * 1.1f, 13, Color.white);
                marker.SetActive(false);
                _digMarkers[i] = marker;
            }
        }

        private DogId DogIdAt(int dogIndex) => _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
            ? identity.Id : DogId.Cheddar;

        private string DogName(int dogIndex) => _context.Dogs[dogIndex] != null ? _context.Dogs[dogIndex].name : "Dog";
    }
}
