using System.Collections.Generic;
using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class ScentSearchMissionController : IMissionController, IMissionInteractionController,
        IMissionSuccessPresentationController
    {
        private const int RequiredFinds = 3;
        private const int MaxWastedDigs = 4;
        private const float DigRange = 2f;
        private const float HotCallRange = 5f;
        private const float WarmCallRange = 11f;
        private const float SuccessHoldSeconds = 1.15f;

        private readonly ScentSearchMissionState _state = new();
        private MissionContext _context;
        private Vector2[] _digSpots;
        private GameObject[] _digMarkers;
        private int _buriedSpot = -1;
        private int _calledSpot = -1;
        private float _successHoldRemaining;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.ScentSearch;
        public bool IsComplete => _state.ReadyToClear(RequiredFinds) && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _state.ReadyToClear(RequiredFinds) && _successHoldRemaining > 0f;
        public bool IsFailed => _state.TooManyWastedDigs(MaxWastedDigs);
        public string FailReason => IsFailed ? "The dogs dug up half the yard chasing cold scents and ran out of patience." : null;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildScentSummary(_state, RequiredFinds);
        public Vector2 EntryTarget => _digSpots != null && _digSpots.Length > 0 ? _digSpots[0] : Vector2.zero;
        public ScentSearchMissionState State => _state;
        public Vector2[] DigSpots => _digSpots != null ? (Vector2[])_digSpots.Clone() : new Vector2[0];
        public string ObjectiveLabel => IsPresentingSuccessfulOutcome
            ? "Bone cache found! Cocoa tracked every scent and Cheddar dug the last prize."
            : _calledSpot >= 0
                ? $"COCOA CALLED IT: Cheddar Interact at the glowing mound! Bones {_state.Found}/{RequiredFinds}, cold digs {_state.WastedDigs}/{MaxWastedDigs}"
                : $"Cocoa: bark beside DIG? patches for HOT/COLD. Cheddar: follow her call and dig. Bones {_state.Found}/{RequiredFinds}, cold digs {_state.WastedDigs}/{MaxWastedDigs}";

        public static Vector2[] ComputeDigSpots(Rect bounds)
        {
            Vector2 P(float x, float y) => new(
                bounds.center.x + x * bounds.width * 0.5f,
                bounds.center.y + y * bounds.height * 0.5f);
            // The old (-0.18, 0.36) mound sat inside BackyardPoolZone.WaterRect; digging happens on
            // dry land, so that mound moved just right of the pool deck.
            return new[] { P(-0.78f, 0.58f), P(0.68f, 0.64f), P(-0.74f, -0.58f), P(0.32f, -0.68f), P(0.82f, 0.08f), P(0.08f, 0.42f) };
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
            _calledSpot = -1;
            _successHoldRemaining = 0f;
            for (int i = 0; i < _digMarkers.Length; i++)
            {
                _digMarkers[i].transform.position = _digSpots[i];
                SetDigArt(i, FinalGameplayArt.ScentSearchDigUnknown);
                _digMarkers[i].SetActive(true);
            }
            ChooseBuriedSpot();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_state.ReadyToClear(RequiredFinds))
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
        }

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
            bool isCocoa = _context.IndexOfDog(DogId.Cocoa) == dogIndex;
            if (!isCocoa && _calledSpot < 0)
            {
                int cocoa = _context.IndexOfDog(DogId.Cocoa);
                target = cocoa >= 0 ? _context.Dogs[cocoa].transform : null;
                copy = "FOLLOW COCOA'S CALL";
                hideDistance = 2f;
                return target != null;
            }

            int nearest = _calledSpot >= 0
                ? _calledSpot
                : dogIndex >= 0 && dogIndex < _context.Dogs.Length
                    ? NearestActiveDigSpot(_context.Dogs[dogIndex].transform.position)
                    : -1;
            target = nearest >= 0 ? _digMarkers[nearest].transform : null;
            copy = _calledSpot >= 0 ? "DIG COCOA'S CALL" : "BARK TO TRACK";
            hideDistance = _calledSpot >= 0 ? DigRange : 1.4f;
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

        /// <summary>Test hook: which spot currently hides the bone.</summary>
        public int BuriedSpotIndex => _buriedSpot;
        public int CalledSpotIndex => _calledSpot;

        /// <summary>Test hook: the resource path currently attached to dig marker <paramref name="index"/>.</summary>
        public string DigResourcePathAt(int index)
        {
            if (_digMarkers == null || index < 0 || index >= _digMarkers.Length || _digMarkers[index] == null) return string.Empty;
            var attachment = _digMarkers[index].GetComponent<MissionPropArtAttachment>();
            return attachment != null ? attachment.ResourcePath : string.Empty;
        }

        /// <summary>Test hook: re-roll which active spot hides the bone (used to reproduce a re-pick landing on a previously-dug spot).</summary>
        public void ForceReselectBuriedSpot() => ChooseBuriedSpot();
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        private void Sniff(int dogIndex)
        {
            if (dogIndex < 0 || dogIndex >= _context.Dogs.Length || _buriedSpot < 0) return;
            _state.AddSniff();
            DogId dogId = DogIdAt(dogIndex);
            Vector2 dogPosition = _context.Dogs[dogIndex].transform.position;
            Vector2 toBone = _digSpots[_buriedSpot] - dogPosition;

            if (dogId == DogId.Cheddar)
            {
                string direction = CardinalDirection(toBone);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue($"Cheddar catches a huge bone smell somewhere {direction} - Cocoa, track the exact patch!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, $"WILD SNIFF: {direction}");
                _context.SpawnWorldPop(dogPosition + Vector2.up, direction, new Color(1f, 0.76f, 0.3f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
                _context.LogEvent("ScentDirection", direction);
                _context.LogObjectiveChanged();
                return;
            }

            float distance = toBone.magnitude;
            string heat = distance < HotCallRange ? "RED HOT" : distance < WarmCallRange ? "WARM" : "COLD";
            bool newCall = distance < HotCallRange && _calledSpot != _buriedSpot;
            if (newCall)
            {
                _calledSpot = _buriedSpot;
                SetDigArt(_calledSpot, FinalGameplayArt.ScentSearchScentHot);
                _context.SetActorState(_digMarkers[_calledSpot], "COCOA CALLED IT - CHEDDAR DIG!", new Color(1f, 0.48f, 0.2f), 0.3f);
                _context.CreditDog(dogIndex);
                if (dogIndex < _context.DogFeedback.Length && _context.DogFeedback[dogIndex] != null)
                    _context.DogFeedback[dogIndex].ShowProudBrief();
                _context.AddScore(ScoreEventCatalog.ScentSniff.Points, ScoreEventCatalog.ScentSniff.Label);
            }
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
            _context.SetCue(newCall
                ? "Cocoa found the RED HOT patch and barked the call - Cheddar, dig the glowing mound!"
                : $"Cocoa tracks carefully... this patch is {heat}.");
            _context.SetJuice(distance < HotCallRange ? GameManager.JuiceFeedbackKind.SuccessPop : GameManager.JuiceFeedbackKind.BarkBurst, $"SCENT: {heat}");
            _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, heat,
                distance < HotCallRange ? new Color(1f, 0.45f, 0.2f) : new Color(0.6f, 0.75f, 1f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
            _context.LogEvent(newCall ? "ScentCalled" : "ScentSniff", heat);
            _context.LogObjectiveChanged();
        }

        private void DigAtSpot(int dogIndex, int spotIndex, bool force)
        {
            if (dogIndex < 0 || dogIndex >= _context.Dogs.Length) return;
            DogId dogId = DogIdAt(dogIndex);
            if (!force && dogId != DogId.Cheddar)
            {
                _context.MarkFailedInteraction(dogId, "Cocoa tracks and calls; Cheddar does the digging");
                _context.SetCue("Cocoa found the trail, but Cheddar is the dirt-flinging digger!");
                return;
            }
            if (!force && _calledSpot < 0)
            {
                _context.MarkFailedInteraction(dogId, "wait for Cocoa to bark a RED HOT mound call");
                _context.SetCue("Cheddar is ready to excavate the whole yard - Cocoa must call the RED HOT mound first!");
                return;
            }
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
                SetDigArt(spotIndex, FinalGameplayArt.ScentSearchBoneFound);
                bool searchComplete = _state.ReadyToClear(RequiredFinds);
                if (!searchComplete) _digMarkers[spotIndex].SetActive(false);
                _context.CreditDog(dogIndex);
                if (dogIndex < _context.DogFeedback.Length && _context.DogFeedback[dogIndex] != null)
                    _context.DogFeedback[dogIndex].ShowDig();
                _context.AddScore(ScoreEventCatalog.BoneFound.Points, ScoreEventCatalog.BoneFound.Label);
                _context.SetFeedback(GameManager.FeedbackKind.PartnerRescue);
                _context.SetCue(searchComplete
                    ? "Cocoa called it and Cheddar dug it - the whole BONE CACHE is uncovered!"
                    : $"Cocoa called it, Cheddar dug up a buried bone! ({_state.Found}/{RequiredFinds})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.BoneFound.Label);
                _context.SpawnWorldPop(_digSpots[spotIndex], searchComplete ? "BONE CACHE!" : "BONE!", new Color(0.95f, 0.9f, 0.7f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                _context.RequestRumble("bone_found", 0.26f, 0.5f, 0.16f);
                _context.LogEvent("BoneFound", $"{_state.Found}/{RequiredFinds}");
                if (searchComplete)
                {
                    _successHoldRemaining = SuccessHoldSeconds;
                    _context.AddScore(ScoreEventCatalog.ScentSearchComplete.Points, ScoreEventCatalog.ScentSearchComplete.Label);
                    _context.SetActorState(_digMarkers[spotIndex], "BONE CACHE FOUND - TEAM TRACKERS!", new Color(1f, 0.9f, 0.45f), 0.35f);
                    _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "SEARCH COMPLETE!");
                    _context.RequestRumble("scent_search_complete", 0.38f, 0.62f, 0.24f);
                }
                else
                    ChooseBuriedSpot();
            }
            else
            {
                _state.AddWastedDig();
                SetDigArt(spotIndex, FinalGameplayArt.ScentSearchScentCold);
                _context.AddScore(ScoreEventCatalog.ColdDig.Points, ScoreEventCatalog.ColdDig.Label);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                _context.SetCue($"{DogName(dogIndex)} dug a cold hole - nothing here ({_state.WastedDigs}/{MaxWastedDigs}).");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, ScoreEventCatalog.ColdDig.Label);
                if (dogIndex < _context.DogFeedback.Length && _context.DogFeedback[dogIndex] != null)
                    _context.DogFeedback[dogIndex].ShowDig();
                _context.SpawnWorldPop(_digSpots[spotIndex], "COLD!", new Color(0.6f, 0.75f, 1f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
                _context.RequestRumble("cold_dig", 0.14f, 0.28f, 0.12f);
                _context.LogEvent("ColdDig", $"{_state.WastedDigs}/{MaxWastedDigs}");
            }
            if (!IsComplete && !IsFailed) _context.LogObjectiveChanged();
        }

        private void ChooseBuriedSpot()
        {
            _calledSpot = -1;
            var active = new List<int>();
            for (int i = 0; i < _digMarkers.Length; i++)
                if (_digMarkers[i] != null && _digMarkers[i].activeSelf) active.Add(i);
            _buriedSpot = active.Count == 0 ? -1 : active[_context.Random().Next(active.Count)];
            // A spot dug wrong earlier keeps showing its cold-scent art (helpful "already checked
            // here" feedback) - but if that same spot gets re-picked as the new hiding place, it
            // would keep reading as cold/already-checked even though the bone is now right there.
            SetDigArt(_buriedSpot, FinalGameplayArt.ScentSearchDigUnknown);
            for (int i = 0; i < _digMarkers.Length; i++)
                if (_digMarkers[i] != null && _digMarkers[i].activeSelf)
                    _context.SetActorState(_digMarkers[i], "DIG? - COCOA SNIFF", new Color(0.42f, 0.3f, 0.16f), 0.08f);
        }

        private static string CardinalDirection(Vector2 offset)
        {
            if (Mathf.Abs(offset.x) >= Mathf.Abs(offset.y)) return offset.x >= 0f ? "EAST" : "WEST";
            return offset.y >= 0f ? "NORTH" : "SOUTH";
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
                MissionPropArt.AttachObject(marker, FinalGameplayArt.ScentSearchDigUnknown, 0.012f, 18, true);
                marker.SetActive(false);
                _digMarkers[i] = marker;
            }
        }

        private void SetDigArt(int index, string resourcePath)
        {
            if (_digMarkers == null || index < 0 || index >= _digMarkers.Length || _digMarkers[index] == null) return;
            MissionPropArt.SetSprite(_digMarkers[index].GetComponent<MissionPropArtAttachment>(), resourcePath);
        }

        private DogId DogIdAt(int dogIndex) => _context.Dogs != null && dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
            _context.Dogs[dogIndex] != null && _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                ? identity.Id : DogId.Cheddar;

        private string DogName(int dogIndex) => _context.Dogs[dogIndex] != null ? _context.Dogs[dogIndex].name : "Dog";
    }
}
