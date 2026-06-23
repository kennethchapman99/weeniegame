using UnityEngine;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Game
{
    // Runtime logic for the doctrine's co-op puzzle missions (Gate Crash, Table Stealth,
    // Squirrel Switcheroo, Walk Campaign, Bone Relay, Great Escape, Chaos Machine, Blanket Catch),
    // split out of GameManager.cs as a partial class. Behavior is unchanged; these methods share all
    // GameManager state and helpers.
    public sealed partial class GameManager : MonoBehaviour
    {
        // --- Gate Crash (Hold-and-Release co-op puzzle) ---

        private void TickGateCrash()
        {
            if (_gatePuzzle.Solved) return;

            int anchor = IndexOfDog(DogId.Cocoa);
            int crosser = IndexOfDog(DogId.Cheddar);
            if (anchor < 0 || crosser < 0) return;

            bool held = Vector2.Distance(_dogs[anchor].transform.position, _gateHoldZone) <= GateHoldRange;
            _gatePuzzle.SetHeld(held);
            if (Vector2.Distance(_dogs[crosser].transform.position, _gateCrossZone) <= GateCrossRange)
                _gatePuzzle.Advance(Time.deltaTime);

            HandleGateSnaps();
            if (Phase == State.GameOver) return;

            if (PredatorObject != null)
                SetActorState(PredatorObject, _gatePuzzle.Held ? "GATE HELD - SQUEEZE THROUGH!" : "COCOA: HOLD THE GATE!",
                    _gatePuzzle.Held ? new Color(0.4f, 0.8f, 0.5f) : new Color(0.7f, 0.5f, 0.2f), 0.12f);
            if (SquirrelObject != null)
                SetActorState(SquirrelObject, $"TOY - SQUEEZE {Mathf.RoundToInt(_gatePuzzle.CrossRatio * 100f)}%", new Color(0.6f, 0.8f, 1f), 0.1f);
        }

        private void HandleGateSnaps()
        {
            if (_gatePuzzle.Snaps <= _gateSnapsSeen) return;

            _gateSnapsSeen = _gatePuzzle.Snaps;
            AddScore(ScoreEventCatalog.FakeOut.Points, "GATE SNAP");
            LastFeedback = FeedbackKind.SquirrelStoleFood;
            LastCue = $"The gate snapped shut! ({_gatePuzzle.Snaps}/{GateMaxSnaps}) Cocoa has to brace it.";
            SetJuice(JuiceFeedbackKind.WarningMiss, "GATE SNAP!");
            if (SquirrelObject != null) SpawnWorldPop(_gateCrossZone, "SNAP!", new Color(1f, 0.35f, 0.2f));
            LogPlaytestEvent("GateSnap", $"{_gatePuzzle.Snaps}/{GateMaxSnaps}");
            if (_gatePuzzle.Snaps >= GateMaxSnaps) EndRound(false);
        }

        public void ForceGateHold(bool held = true)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.GateCrash) return;
            _gatePuzzle.SetHeld(held);
            HandleGateSnaps();
        }

        public void ForceGateCross(float seconds)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.GateCrash) return;
            _gatePuzzle.Advance(seconds);
            HandleGateSnaps();
            if (Phase != State.GameOver) CheckClear();
        }

        private void TickTableStealth()
        {
            if (_tablePuzzle.Solved) return;

            int distractor = IndexOfDog(DogId.Cocoa);
            int sneaker = IndexOfDog(DogId.Cheddar);
            if (distractor < 0 || sneaker < 0) return;

            // Cocoa flops belly-up (sustained distraction) while she stands by the human; Cheddar sneaks
            // the steak only while the human is held looking the other way.
            bool flopping = Vector2.Distance(_dogs[distractor].transform.position, _tableHumanZone) <= TableDistractRange;
            _tablePuzzle.SetBellyFlop(flopping);
            bool sneaking = Vector2.Distance(_dogs[sneaker].transform.position, _tableStealZone) <= TableSneakRange;
            _tablePuzzle.Advance(Time.deltaTime, sneaking);

            HandleTableExposures();
            if (Phase == State.GameOver) return;

            if (PredatorObject != null)
                SetActorState(PredatorObject, _tablePuzzle.HumanDistracted ? "HUMAN DISTRACTED - SNEAK IT!" : "COCOA: FLOP TO DISTRACT!",
                    _tablePuzzle.HumanDistracted ? new Color(0.4f, 0.8f, 0.5f) : new Color(0.7f, 0.5f, 0.2f), 0.12f);
            if (SquirrelObject != null)
                SetActorState(SquirrelObject, $"STEAK - SNEAK {Mathf.RoundToInt(_tablePuzzle.SneakRatio * 100f)}%", new Color(0.6f, 0.8f, 1f), 0.1f);
        }

        private void HandleTableExposures()
        {
            if (_tablePuzzle.Exposures <= _tableExposuresSeen) return;

            _tableExposuresSeen = _tablePuzzle.Exposures;
            AddScore(ScoreEventCatalog.FakeOut.Points, "SPOTTED");
            LastFeedback = FeedbackKind.SquirrelStoleFood;
            LastCue = $"The human glanced over! ({_tablePuzzle.Exposures}/{TableMaxExposures}) Keep them distracted before sneaking.";
            SetJuice(JuiceFeedbackKind.WarningMiss, "SPOTTED!");
            if (SquirrelObject != null) SpawnWorldPop(_tableStealZone, "SPOTTED!", new Color(1f, 0.35f, 0.2f));
            LogPlaytestEvent("TableSpotted", $"{_tablePuzzle.Exposures}/{TableMaxExposures}");
            if (_tablePuzzle.Exposures >= TableMaxExposures) EndRound(false);
        }

        /// <summary>Test hook: Cocoa commits to / releases the belly-flop distraction (the sustain hold).</summary>
        public void ForceTableFlop(bool flopped = true)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.TableStealth) return;
            _tablePuzzle.SetBellyFlop(flopped);
        }

        /// <summary>Test hook: Cheddar fires a burp-cloud distraction (the burst spike).</summary>
        public void ForceTableBurp()
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.TableStealth) return;
            _tablePuzzle.Burp();
        }

        /// <summary>Test hook: advance the sneak by <paramref name="seconds"/> with the partner in the steak lane.</summary>
        public void ForceTableSneak(float seconds)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.TableStealth) return;
            _tablePuzzle.Advance(seconds, true);
            _tablePuzzle.Advance(0.0001f, false); // reset the exposure edge so repeated forced sneaks each register
            HandleTableExposures();
            if (Phase != State.GameOver) CheckClear();
        }

        private void TickSwitcheroo()
        {
            if (_switcherooPuzzle.Solved) return;

            int baiter = IndexOfDog(DogId.Cheddar);
            int striker = IndexOfDog(DogId.Cocoa);
            if (baiter < 0 || striker < 0) return;

            // Cheddar feints at the decoy to commit the squirrel; commitment decays when he eases off.
            bool baiting = Vector2.Distance(_dogs[baiter].transform.position, _switcherooDecoyZone) <= SwitcherooBaitRange;
            _switcherooPuzzle.Advance(Time.deltaTime, baiting);

            // Cocoa raids the stash: exactly one grab per committed window (she must re-bait for the next).
            if (!_switcherooPuzzle.Committed) _switcherooStruckThisWindow = false;
            bool atStash = Vector2.Distance(_dogs[striker].transform.position, _switcherooStashZone) <= SwitcherooStashRange;
            if (atStash && _switcherooPuzzle.Committed && !_switcherooStruckThisWindow)
            {
                _switcherooPuzzle.Strike();
                _switcherooStruckThisWindow = true;
            }

            HandleSwitcherooProgress();
            if (Phase == State.GameOver) return;

            if (PredatorObject != null)
                SetActorState(PredatorObject, _switcherooPuzzle.Committed ? "SQUIRREL CHASING THE DECOY - RAID NOW!" : "SQUIRREL GUARDING THE STASH - FEINT IT!",
                    _switcherooPuzzle.Committed ? new Color(0.4f, 0.8f, 0.5f) : new Color(0.7f, 0.5f, 0.2f), 0.12f);
            if (SquirrelObject != null)
                SetActorState(SquirrelObject, $"STASH - RAIDS {_switcherooPuzzle.Hits}/{SwitcherooHitsNeeded}", new Color(0.6f, 0.8f, 1f), 0.1f);
        }

        private void HandleSwitcherooProgress()
        {
            if (_switcherooPuzzle.Hits > _switcherooHitsSeen)
            {
                _switcherooHitsSeen = _switcherooPuzzle.Hits;
                AddScore(ScoreEventCatalog.StashFound.Points, "STASH RAIDED");
                LastFeedback = FeedbackKind.SquirrelScared;
                LastCue = $"Cocoa snatched from the stash while the squirrel chased the decoy! ({_switcherooPuzzle.Hits}/{SwitcherooHitsNeeded})";
                SetJuice(JuiceFeedbackKind.SuccessPop, "SWITCHEROO!");
                if (SquirrelObject != null) SpawnWorldPop(_switcherooStashZone, "SWITCHEROO!", new Color(0.45f, 0.9f, 0.55f));
                LogPlaytestEvent("SwitcherooRaid", $"{_switcherooPuzzle.Hits}/{SwitcherooHitsNeeded}");
            }

            if (_switcherooPuzzle.Backfires > _switcherooBackfiresSeen)
            {
                _switcherooBackfiresSeen = _switcherooPuzzle.Backfires;
                AddScore(ScoreEventCatalog.FakeOut.Points, "BAIT BACKFIRE");
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                LastCue = $"Over-baited! The squirrel wised up and bolted back to the stash. ({_switcherooPuzzle.Backfires}/{SwitcherooMaxBackfires})";
                SetJuice(JuiceFeedbackKind.WarningMiss, "BACKFIRE!");
                if (PredatorObject != null) SpawnWorldPop(_switcherooDecoyZone, "WISED UP!", new Color(1f, 0.35f, 0.2f));
                LogPlaytestEvent("SwitcherooBackfire", $"{_switcherooPuzzle.Backfires}/{SwitcherooMaxBackfires}");
                if (_switcherooPuzzle.Backfires >= SwitcherooMaxBackfires) EndRound(false);
            }
        }

        /// <summary>Test hook: Cheddar feints at the decoy for <paramref name="seconds"/> (or eases off when baiting=false).</summary>
        public void ForceSwitcherooBait(float seconds, bool baiting = true)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.SquirrelSwitcheroo) return;
            _switcherooPuzzle.Advance(seconds, baiting);
            HandleSwitcherooProgress();
        }

        /// <summary>Test hook: Cocoa raids the stash; lands only while the squirrel is committed to the decoy.</summary>
        public void ForceSwitcherooStrike()
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.SquirrelSwitcheroo) return;
            _switcherooPuzzle.Strike();
            HandleSwitcherooProgress();
            if (Phase != State.GameOver) CheckClear();
        }

        private void TickWalkCampaign()
        {
            if (_walkPuzzle.Solved) return;

            int cheddar = IndexOfDog(DogId.Cheddar);
            int cocoa = IndexOfDog(DogId.Cocoa);
            if (cheddar < 0 || cocoa < 0) return;

            // The message is built from positions: Cocoa stares down the door, Cheddar presents the leash.
            // Neither stimulus alone reads, so both dogs must hold their stations at the same time.
            SocialStimulus active = SocialStimulus.None;
            if (Vector2.Distance(_dogs[cocoa].transform.position, _walkDoorZone) <= WalkStationRange)
                active |= SocialStimulus.DoorStare;
            if (Vector2.Distance(_dogs[cheddar].transform.position, _walkLeashZone) <= WalkStationRange)
                active |= SocialStimulus.PresentLeash;
            _walkPuzzle.SetActiveSet(active);
            _walkPuzzle.Advance(Time.deltaTime);

            HandleWalkCampaignProgress();
            if (Phase == State.GameOver) return;

            if (PredatorObject != null)
            {
                string human = _walkPuzzle.ExactMatch
                    ? $"HUMAN GETTING IT - HOLD IT! ({Mathf.RoundToInt(_walkPuzzle.Comprehension / WalkComprehendNeeded * 100f)}%)"
                    : $"HUMAN CONFUSED - SEND ONE MESSAGE! (misreads {_walkPuzzle.Misreads}/{WalkMaxMisreads})";
                SetActorState(PredatorObject, human, _walkPuzzle.ExactMatch ? new Color(0.5f, 0.85f, 0.55f) : new Color(0.95f, 0.7f, 0.35f), 0.13f);
            }
            if (SquirrelObject != null)
                SetActorState(SquirrelObject, (active & SocialStimulus.PresentLeash) != 0 ? "LEASH PRESENTED!" : "LEASH - CHEDDAR PRESENT IT!", new Color(0.6f, 0.8f, 1f), 0.1f);
        }

        private void HandleWalkCampaignProgress()
        {
            // First moment the combo clicks: reward reading the room together.
            if (_walkPuzzle.ExactMatch && !_walkGettingItScored && !_walkPuzzle.Solved)
            {
                _walkGettingItScored = true;
                AddScore(ScoreEventCatalog.HumanGettingIt.Points, ScoreEventCatalog.HumanGettingIt.Label);
                LastFeedback = FeedbackKind.Intro;
                LastCue = "The human's getting it - hold the door-stare and the leash together!";
                SetJuice(JuiceFeedbackKind.SuccessPop, "GETTING IT!");
                LogPlaytestEvent("WalkGettingIt", "combo");
            }

            if (_walkPuzzle.Misreads > _walkMisreadsSeen)
            {
                _walkMisreadsSeen = _walkPuzzle.Misreads;
                _walkGettingItScored = false; // earn the "getting it" pop again on the next clean combo
                AddScore(ScoreEventCatalog.HumanMisread.Points, ScoreEventCatalog.HumanMisread.Label);
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                LastCue = $"Mixed signals! The human brought the wrong thing. ({_walkPuzzle.Misreads}/{WalkMaxMisreads})";
                SetJuice(JuiceFeedbackKind.WarningMiss, "CONFUSED!");
                if (PredatorObject != null) SpawnWorldPop(PredatorObject.transform.position, "WRONG THING!", new Color(0.95f, 0.6f, 0.25f));
                LogPlaytestEvent("WalkMisread", $"{_walkPuzzle.Misreads}/{WalkMaxMisreads}");
                if (_walkPuzzle.Misreads >= WalkMaxMisreads) EndRound(false);
            }

            if (_walkPuzzle.Solved)
            {
                AddScore(ScoreEventCatalog.WalkConned.Points, ScoreEventCatalog.WalkConned.Label);
                SetJuice(JuiceFeedbackKind.SuccessPop, "WALKIES!");
                if (PredatorObject != null) SpawnWorldPop(PredatorObject.transform.position, "WALKIES!", new Color(0.5f, 0.9f, 0.55f));
                LogPlaytestEvent("WalkConned", "solved");
            }
        }

        /// <summary>Test hook: hold a stimulus combo (door-stare / present-leash) for <paramref name="seconds"/>.</summary>
        public void ForceWalkCampaign(float seconds, bool doorStare, bool presentLeash)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.WalkCampaign) return;
            SocialStimulus active = SocialStimulus.None;
            if (doorStare) active |= SocialStimulus.DoorStare;
            if (presentLeash) active |= SocialStimulus.PresentLeash;
            _walkPuzzle.SetActiveSet(active);
            _walkPuzzle.Advance(seconds);
            HandleWalkCampaignProgress();
            if (Phase != State.GameOver) CheckClear();
        }

        private void TickBoneRelay()
        {
            if (_boneRelay.Solved || _boneMounds == null) return;

            int reader = IndexOfDog(DogId.Cocoa);
            int digger = IndexOfDog(DogId.Cheddar);
            if (reader < 0 || digger < 0) return;

            // Cocoa reads the scent post: while she's nosing it she can call which mound holds the bone.
            if (Vector2.Distance(_dogs[reader].transform.position, _boneScentZone) <= BoneScentRange)
                _boneRelay.Reveal();

            // Cheddar digs a mound on entry (one dig per approach, so he must leave and re-enter to dig again).
            int inside = -1;
            for (int i = 0; i < _boneMounds.Length; i++)
            {
                if (_boneMounds[i] == null || !_boneMounds[i].activeSelf) continue;
                if (Vector2.Distance(_dogs[digger].transform.position, _boneMounds[i].transform.position) <= BoneDigRange)
                { inside = i; break; }
            }
            if (inside >= 0 && inside != _boneDiggerInside) _boneRelay.ActOn(inside);
            _boneDiggerInside = inside;

            HandleBoneRelayProgress();
            if (Phase == State.GameOver) return;
            UpdateBoneMoundVisuals();
        }

        private void HandleBoneRelayProgress()
        {
            int digger = IndexOfDog(DogId.Cheddar);
            Vector2 digPos = digger >= 0 ? (Vector2)_dogs[digger].transform.position : _boneScentZone;

            if (_boneRelay.Finds > _boneFindsSeen)
            {
                _boneFindsSeen = _boneRelay.Finds;
                AddScore(ScoreEventCatalog.BoneFound.Points, ScoreEventCatalog.BoneFound.Label);
                LastFeedback = FeedbackKind.SquirrelScared;
                LastCue = $"Cocoa called it, Cheddar dug it up! ({_boneRelay.Finds}/{BoneFindsNeeded})";
                SetJuice(JuiceFeedbackKind.SuccessPop, "BONE!");
                SpawnWorldPop(digPos, "BONE!", new Color(0.5f, 0.9f, 0.55f));
                LogPlaytestEvent("BoneFound", $"{_boneRelay.Finds}/{BoneFindsNeeded}");
            }

            bool wasted = false;
            if (_boneRelay.BlindActs > _boneBlindSeen)
            {
                _boneBlindSeen = _boneRelay.BlindActs;
                wasted = true;
                LastCue = "Cheddar dug blind - wait for Cocoa's call!";
            }
            if (_boneRelay.WrongDigs > _boneWrongSeen)
            {
                _boneWrongSeen = _boneRelay.WrongDigs;
                wasted = true;
                LastCue = "Wrong mound - that one's a decoy.";
            }
            if (wasted)
            {
                AddScore(ScoreEventCatalog.ColdDig.Points, ScoreEventCatalog.ColdDig.Label);
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                SetJuice(JuiceFeedbackKind.WarningMiss, "NOPE!");
                SpawnWorldPop(digPos, "NOPE!", new Color(0.85f, 0.5f, 0.3f));
                int totalWasted = _boneRelay.BlindActs + _boneRelay.WrongDigs;
                LogPlaytestEvent("BoneWaste", $"{totalWasted}/{BoneMaxWasted}");
                if (totalWasted >= BoneMaxWasted) EndRound(false);
            }
        }

        private void UpdateBoneMoundVisuals()
        {
            if (_boneMounds == null) return;
            int call = _boneRelay.RevealedTarget;
            for (int i = 0; i < _boneMounds.Length; i++)
            {
                if (_boneMounds[i] == null) continue;
                bool isCall = i == call;
                if (_boneMounds[i].TryGetComponent<SpriteRenderer>(out var sr))
                    sr.color = isCall ? BoneMoundCallColor : BoneMoundIdleColor;
                if (_boneMoundLabels != null && _boneMoundLabels[i] != null)
                    _boneMoundLabels[i].text = isCall ? "DIG HERE!" : "DIG?";
            }
            if (SquirrelObject != null)
                SetActorState(SquirrelObject, _boneRelay.Known ? "SCENT POST - SHE'S CALLING IT!" : "SCENT POST - COCOA SNIFF HERE",
                    _boneRelay.Known ? BoneMoundCallColor : new Color(0.7f, 0.6f, 0.95f), 0.12f);
        }

        /// <summary>Test hook: Cocoa noses the scent post and calls the real mound.</summary>
        public void ForceBoneReveal()
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.BoneRelay) return;
            _boneRelay.Reveal();
            UpdateBoneMoundVisuals();
        }

        /// <summary>Test hook: Cheddar digs mound <paramref name="target"/>; finds only the called mound.</summary>
        public void ForceBoneDig(int target)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.BoneRelay) return;
            _boneRelay.ActOn(target);
            HandleBoneRelayProgress();
            if (Phase != State.GameOver) { UpdateBoneMoundVisuals(); CheckClear(); }
        }

        private void TickGreatEscape()
        {
            if (_escape.Solved || _escapeStations == null) return;

            int cheddar = IndexOfDog(DogId.Cheddar);
            int cocoa = IndexOfDog(DogId.Cocoa);
            if (cheddar < 0 || cocoa < 0) return;

            int active = Mathf.Clamp(_escape.Step, 0, _escapeStationSpots.Length - 1);
            Vector2 station = _escapeStationSpots[active];
            ChainActor owner = _escape.NextOwner;

            bool cheddarThere = Vector2.Distance(_dogs[cheddar].transform.position, station) <= EscapeStationRange;
            bool cocoaThere = Vector2.Distance(_dogs[cocoa].transform.position, station) <= EscapeStationRange;

            // Prefer the owner if present at the active station; a wrong-dog visit registers as a fumble.
            int insideDog = -1;
            ChainActor insideActor = ChainActor.Either;
            if (owner == ChainActor.Cheddar && cheddarThere) { insideDog = cheddar; insideActor = ChainActor.Cheddar; }
            else if (owner == ChainActor.Cocoa && cocoaThere) { insideDog = cocoa; insideActor = ChainActor.Cocoa; }
            else if (cheddarThere) { insideDog = cheddar; insideActor = ChainActor.Cheddar; }
            else if (cocoaThere) { insideDog = cocoa; insideActor = ChainActor.Cocoa; }

            if (insideDog >= 0 && insideDog != _escapeDogInside) _escape.TryStep(insideActor);
            _escapeDogInside = insideDog;

            _escape.Advance(Time.deltaTime);

            HandleGreatEscapeProgress();
            if (Phase == State.GameOver) return;
            UpdateEscapeStationVisuals();
        }

        private void HandleGreatEscapeProgress()
        {
            if (_escape.Step > _escapeStepSeen)
            {
                _escapeStepSeen = _escape.Step;
                AddScore(ScoreEventCatalog.ContraptionStep.Points, ScoreEventCatalog.ContraptionStep.Label);
                LastFeedback = FeedbackKind.SquirrelScared;
                LastCue = $"Clunk! The contraption advanced. ({_escape.Step}/{_escape.StepCount})";
                SetJuice(JuiceFeedbackKind.SuccessPop, "CLUNK!");
                int doneStep = Mathf.Clamp(_escape.Step - 1, 0, _escapeStationSpots.Length - 1);
                SpawnWorldPop(_escapeStationSpots[doneStep], "CLUNK!", new Color(0.6f, 0.85f, 0.95f));
                LogPlaytestEvent("EscapeStep", $"{_escape.Step}/{_escape.StepCount}");
            }

            bool wasted = false;
            if (_escape.Fumbles > _escapeFumblesSeen)
            {
                _escapeFumblesSeen = _escape.Fumbles;
                wasted = true;
                LastCue = "Wrong dog or wrong order - nothing budged.";
            }
            if (_escape.Settles > _escapeSettlesSeen)
            {
                _escapeSettlesSeen = _escape.Settles;
                wasted = true;
                LastCue = "Too slow - the contraption eased back a step. Keep pace!";
            }
            if (wasted)
            {
                AddScore(ScoreEventCatalog.ContraptionFumble.Points, ScoreEventCatalog.ContraptionFumble.Label);
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                SetJuice(JuiceFeedbackKind.WarningMiss, "CLANK!");
                int totalWasted = _escape.Fumbles + _escape.Settles;
                LogPlaytestEvent("EscapeWaste", $"{totalWasted}/{EscapeMaxWasted}");
                if (totalWasted >= EscapeMaxWasted) EndRound(false);
            }
        }

        private void UpdateEscapeStationVisuals()
        {
            if (_escapeStations == null) return;
            int active = Mathf.Clamp(_escape.Step, 0, _escapeStations.Length - 1);
            for (int i = 0; i < _escapeStations.Length; i++)
            {
                if (_escapeStations[i] == null) continue;
                bool isActive = i == active && !_escape.Solved;
                bool done = i < _escape.Step;
                ChainActor owner = _escapeOwners[i];
                Color ownerTint = owner == ChainActor.Cheddar ? new Color(0.95f, 0.72f, 0.3f) : new Color(0.55f, 0.78f, 1f);
                Color shown = done ? new Color(0.3f, 0.55f, 0.32f) : (isActive ? ownerTint : new Color(0.3f, 0.3f, 0.34f));
                if (_escapeStations[i].TryGetComponent<SpriteRenderer>(out var sr)) sr.color = shown;
                if (_escapeStationLabels != null && _escapeStationLabels[i] != null)
                {
                    string who = owner == ChainActor.Cheddar ? "CHEDDAR" : "COCOA";
                    _escapeStationLabels[i].text = done ? "DONE" : $"{i + 1}. {who}: {_escapeStepActions[i]}";
                }
            }
        }

        /// <summary>Test hook: a dog attempts the next contraption step.</summary>
        public void ForceEscapeStep(ChainActor actor)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.GreatEscape) return;
            _escape.TryStep(actor);
            HandleGreatEscapeProgress();
            if (Phase != State.GameOver) { UpdateEscapeStationVisuals(); CheckClear(); }
        }

        /// <summary>Test hook: let the contraption sit idle for <paramref name="seconds"/> (dawdle regression).</summary>
        public void ForceEscapeIdle(float seconds)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.GreatEscape) return;
            _escape.Advance(seconds);
            HandleGreatEscapeProgress();
            if (Phase != State.GameOver) UpdateEscapeStationVisuals();
        }

        private void TickChaosMachine()
        {
            if (_chaos.Solved || _chaosJunctions == null) return;

            int cheddar = IndexOfDog(DogId.Cheddar);
            int cocoa = IndexOfDog(DogId.Cocoa);
            if (cheddar < 0 || cocoa < 0) return;

            if (!_chaos.Running)
            {
                // Either dog at the lever (re)pulls it - starting fresh or resuming from a stall.
                bool atLever = Vector2.Distance(_dogs[cheddar].transform.position, _chaosLeverZone) <= ChaosLeverRange
                    || Vector2.Distance(_dogs[cocoa].transform.position, _chaosLeverZone) <= ChaosLeverRange;
                if (atLever) _chaos.Trigger();
            }

            if (_chaos.Running)
            {
                int stage = Mathf.Clamp(_chaos.Stage, 0, _chaosJunctionSpots.Length - 1);
                ChainActor owner = _chaosOwners[stage];
                int ownerIdx = owner == ChainActor.Cheddar ? cheddar : cocoa;
                bool assisting = Vector2.Distance(_dogs[ownerIdx].transform.position, _chaosJunctionSpots[stage]) <= ChaosJunctionRange;
                _chaos.Advance(Time.deltaTime, assisting);
            }

            HandleChaosProgress();
            if (Phase == State.GameOver) return;
            UpdateChaosVisuals();
        }

        private void HandleChaosProgress()
        {
            if (_chaos.Stage > _chaosStageSeen)
            {
                _chaosStageSeen = _chaos.Stage;
                AddScore(ScoreEventCatalog.ContraptionStep.Points, "CASCADE ROLLED");
                LastFeedback = FeedbackKind.SquirrelScared;
                LastCue = $"Whirr-clunk! The cascade rolled through a junction. ({_chaos.Stage}/{_chaos.StageCount})";
                SetJuice(JuiceFeedbackKind.SuccessPop, "WHIRR!");
                int doneStage = Mathf.Clamp(_chaos.Stage - 1, 0, _chaosJunctionSpots.Length - 1);
                SpawnWorldPop(_chaosJunctionSpots[doneStage], "WHIRR!", new Color(0.6f, 0.85f, 0.95f));
                LogPlaytestEvent("ChaosStage", $"{_chaos.Stage}/{_chaos.StageCount}");
            }

            if (_chaos.Stalls > _chaosStallsSeen)
            {
                _chaosStallsSeen = _chaos.Stalls;
                AddScore(ScoreEventCatalog.ContraptionFumble.Points, "MISFIRE");
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                int jam = Mathf.Clamp(_chaos.StalledStage, 0, _chaosJunctionSpots.Length - 1);
                LastCue = $"Misfire! The machine jammed at the {_chaosJunctionActions[jam].ToLowerInvariant()} - re-pull the lever. ({_chaos.Stalls}/{ChaosMaxStalls})";
                SetJuice(JuiceFeedbackKind.WarningMiss, "MISFIRE!");
                SpawnWorldPop(_chaosJunctionSpots[jam], "STUCK!", new Color(1f, 0.4f, 0.25f));
                LogPlaytestEvent("ChaosStall", $"{_chaos.Stalls}/{ChaosMaxStalls}");
                if (_chaos.Stalls >= ChaosMaxStalls) EndRound(false);
            }
        }

        private void UpdateChaosVisuals()
        {
            if (_chaosJunctions == null) return;
            int stage = Mathf.Clamp(_chaos.Stage, 0, _chaosJunctions.Length - 1);
            for (int i = 0; i < _chaosJunctions.Length; i++)
            {
                if (_chaosJunctions[i] == null) continue;
                bool fired = i < _chaos.Stage;
                bool active = i == stage && !_chaos.Solved;
                bool stalledHere = _chaos.StalledStage == i;
                ChainActor owner = _chaosOwners[i];
                Color ownerTint = owner == ChainActor.Cheddar ? new Color(0.95f, 0.72f, 0.3f) : new Color(0.55f, 0.78f, 1f);
                Color shown = fired ? new Color(0.3f, 0.55f, 0.32f)
                    : stalledHere ? new Color(1f, 0.4f, 0.25f)
                    : active && _chaos.Running ? ownerTint
                    : new Color(0.3f, 0.3f, 0.34f);
                if (_chaosJunctions[i].TryGetComponent<SpriteRenderer>(out var sr)) sr.color = shown;
                if (_chaosJunctionLabels != null && _chaosJunctionLabels[i] != null)
                {
                    string who = owner == ChainActor.Cheddar ? "CHEDDAR" : "COCOA";
                    _chaosJunctionLabels[i].text = fired ? "FIRED" : $"{who}: {_chaosJunctionActions[i]}";
                }
            }
            if (PredatorObject != null)
                SetActorState(PredatorObject, _chaos.Running ? "CASCADE RUNNING - COVER YOUR JUNCTIONS!" : "LEVER - PULL TO START THE CASCADE",
                    _chaos.Running ? new Color(0.5f, 0.85f, 0.55f) : new Color(0.85f, 0.55f, 0.3f), 0.14f);
        }

        /// <summary>Test hook: a dog pulls the lever to start (or resume) the cascade.</summary>
        public void ForceChaosTrigger()
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.ChaosMachine) return;
            _chaos.Trigger();
            UpdateChaosVisuals();
        }

        /// <summary>Test hook: advance the live cascade, with the current junction's helper in position or not.</summary>
        public void ForceChaosAdvance(float seconds, bool assisting)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.ChaosMachine) return;
            _chaos.Advance(seconds, assisting);
            HandleChaosProgress();
            if (Phase != State.GameOver) { UpdateChaosVisuals(); CheckClear(); }
        }

        private void TickBlanketCatch()
        {
            if (_blanket.Solved) return;

            int a = IndexOfDog(DogId.Cheddar);
            int b = IndexOfDog(DogId.Cocoa);
            if (a < 0 || b < 0) return;

            // The blanket is defined by BOTH dogs: separation (taut band) and midpoint (catch alignment).
            Vector2 pa = _dogs[a].transform.position;
            Vector2 pb = _dogs[b].transform.position;
            _blanket.UpdateSpan(Vector2.Distance(pa, pb), (pa.x + pb.x) * 0.5f);
            HandleBlanketRips();

            // The current snack falls; when it reaches the blanket line, the span tries to catch it.
            _blanketItemY -= BlanketFallSpeed * Time.deltaTime;
            if (_blanketItemY <= BlanketCatchLineY)
            {
                _blanket.TryCatch(_blanketItemX);
                HandleBlanketProgress();
                if (Phase == State.GameOver) return;
                if (!_blanket.Solved) SpawnBlanketItem();
            }
            UpdateBlanketVisuals();
        }

        private void SpawnBlanketItem()
        {
            _blanketItemX = Mathf.Lerp(_bounds.xMin + 3f, _bounds.xMax - 3f, (float)_rng.NextDouble());
            _blanketItemY = BlanketSpawnY;
            if (_blanketFallingItem != null)
            {
                _blanketFallingItem.SetActive(true);
                _blanketFallingItem.transform.position = new Vector3(_blanketItemX, _blanketItemY, 0f);
            }
        }

        private void HandleBlanketRips()
        {
            if (_blanket.Rips > _blanketRipsSeen)
            {
                _blanketRipsSeen = _blanket.Rips;
                AddScore(ScoreEventCatalog.WeenieDropped.Points, "BLANKET RIP");
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                LastCue = $"Over-stretched! The blanket ripped. ({_blanket.Rips}/{BlanketMaxRips})";
                SetJuice(JuiceFeedbackKind.WarningMiss, "RIP!");
                LogPlaytestEvent("BlanketRip", $"{_blanket.Rips}/{BlanketMaxRips}");
                if (_blanket.TooManyRips) EndRound(false);
            }
        }

        private void HandleBlanketProgress()
        {
            if (_blanket.Caught > _blanketCaughtSeen)
            {
                _blanketCaughtSeen = _blanket.Caught;
                AddScore(ScoreEventCatalog.WeenieDelivered.Points, "SNACK CAUGHT");
                LastFeedback = FeedbackKind.SquirrelScared;
                LastCue = $"Nice catch! The blanket snagged the snack. ({_blanket.Caught}/{BlanketCatchesNeeded})";
                SetJuice(JuiceFeedbackKind.SuccessPop, "CAUGHT!");
                SpawnWorldPop(new Vector2(_blanketItemX, BlanketCatchLineY), "CAUGHT!", new Color(0.5f, 0.95f, 0.55f));
                LogPlaytestEvent("BlanketCatch", $"{_blanket.Caught}/{BlanketCatchesNeeded}");
            }

            if (_blanket.Missed > _blanketMissedSeen)
            {
                _blanketMissedSeen = _blanket.Missed;
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                LastCue = _blanket.Slack ? "Too close - the blanket sagged and the snack bounced off."
                    : _blanket.Overstretched ? "Blanket's torn - close the gap to fix the span."
                    : "Missed - get the middle of the blanket under the snack.";
                SetJuice(JuiceFeedbackKind.WarningMiss, "MISSED!");
                SpawnWorldPop(new Vector2(_blanketItemX, BlanketCatchLineY), "SPLAT!", new Color(0.85f, 0.5f, 0.3f));
                LogPlaytestEvent("BlanketMiss", $"{_blanket.Missed}");
            }
        }

        private void UpdateBlanketVisuals()
        {
            if (_blanketFallingItem != null)
                _blanketFallingItem.transform.position = new Vector3(_blanketItemX, _blanketItemY, 0f);
            if (_blanketObject == null) return;
            Color tint = _blanket.Taut ? new Color(0.45f, 0.85f, 0.55f)
                : _blanket.Overstretched ? new Color(0.9f, 0.35f, 0.25f)
                : new Color(0.85f, 0.8f, 0.35f); // slack
            _blanketObject.transform.position = new Vector3(_blanket.MidpointX, BlanketCatchLineY, 0f);
            float width = Mathf.Clamp(_blanket.Separation, 1f, BlanketMaxSeparation + 2f);
            _blanketObject.transform.localScale = new Vector3(width, 0.5f, 1f);
            if (_blanketObject.TryGetComponent<SpriteRenderer>(out var sr)) sr.color = tint;
            if (_blanketLabel != null)
            {
                _blanketLabel.text = _blanket.Taut ? $"BLANKET TAUT - CATCH! ({_blanket.Caught}/{BlanketCatchesNeeded})"
                    : _blanket.Overstretched ? "TOO FAR - RIPPING!" : "TOO CLOSE - SAGGING";
                // Keep the label upright and a readable size despite the blanket's horizontal stretch.
                _blanketLabel.transform.localScale = new Vector3(0.08f / Mathf.Max(width, 0.01f), 0.16f, 1f);
            }
        }

        /// <summary>Test hook: set the blanket span directly (separation + midpoint), as if from dog positions.</summary>
        public void ForceBlanketSpan(float separation, float midpointX)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.BlanketCatch) return;
            _blanket.UpdateSpan(separation, midpointX);
            HandleBlanketRips();
            if (Phase != State.GameOver) UpdateBlanketVisuals();
        }

        /// <summary>Test hook: a snack reaches the blanket line at <paramref name="itemX"/>; the span tries to catch it.</summary>
        public void ForceBlanketCatch(float itemX)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.BlanketCatch) return;
            _blanketItemX = itemX;
            _blanket.TryCatch(itemX);
            HandleBlanketProgress();
            if (Phase != State.GameOver) { UpdateBlanketVisuals(); CheckClear(); }
        }
    }
}
