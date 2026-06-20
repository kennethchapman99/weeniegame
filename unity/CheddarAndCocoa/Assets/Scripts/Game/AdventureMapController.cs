using System;
using System.Collections.Generic;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// UI-agnostic adventure map state. It owns selection, locked/unlocked visibility, and mission-launch intent,
    /// while AdventureProgressService owns durable save data.
    /// </summary>
    public sealed class AdventureMapController
    {
        private readonly AdventureProgressService _progress;
        private int _selectedLocationIndex;
        private int _selectedMissionIndex;

        public AdventureMapController(AdventureProgressService progress)
        {
            _progress = progress ?? AdventureProgressService.CreateInMemoryForTests();
            _selectedLocationIndex = 0;
            _selectedMissionIndex = 0;
        }

        public AdventureProgressService Progress => _progress;
        public IReadOnlyList<AdventureLocationDefinition> Locations => _progress.Locations;
        public int SelectedLocationIndex => _selectedLocationIndex;
        public int SelectedMissionIndex => _selectedMissionIndex;
        public int TotalStars => _progress.Snapshot.TotalStars;

        public AdventureLocationDefinition SelectedLocation
        {
            get
            {
                if (_progress.Locations.Count == 0) return null;
                int index = ClampIndex(_selectedLocationIndex, _progress.Locations.Count);
                return _progress.Locations[index];
            }
        }

        public bool SelectedLocationUnlocked => SelectedLocation != null && _progress.IsLocationUnlocked(SelectedLocation.Id);

        public GameManager.MissionVariant? SelectedMission
        {
            get
            {
                var location = SelectedLocation;
                if (location == null || location.Missions == null || location.Missions.Length == 0) return null;
                int index = ClampIndex(_selectedMissionIndex, location.Missions.Length);
                return location.Missions[index];
            }
        }

        public bool CanLaunchSelectedMission => SelectedLocationUnlocked && SelectedMission.HasValue;

        public void SelectLocation(int index)
        {
            if (_progress.Locations.Count == 0)
            {
                _selectedLocationIndex = 0;
                _selectedMissionIndex = 0;
                return;
            }

            _selectedLocationIndex = ClampIndex(index, _progress.Locations.Count);
            _selectedMissionIndex = 0;
        }

        public void SelectNextLocation() => SelectLocation(_selectedLocationIndex + 1);
        public void SelectPreviousLocation() => SelectLocation(_selectedLocationIndex - 1);

        public void SelectMission(int index)
        {
            var location = SelectedLocation;
            if (location == null || location.Missions == null || location.Missions.Length == 0)
            {
                _selectedMissionIndex = 0;
                return;
            }

            _selectedMissionIndex = ClampIndex(index, location.Missions.Length);
        }

        public void SelectNextMission() => SelectMission(_selectedMissionIndex + 1);
        public void SelectPreviousMission() => SelectMission(_selectedMissionIndex - 1);

        public bool TryQueueSelectedMissionLaunch()
        {
            if (!CanLaunchSelectedMission) return false;
            AdventureMissionLaunch.QueueMission(SelectedMission.Value, SelectedLocation.Id);
            return true;
        }

        public string BuildHeaderLabel()
        {
            return $"Adventure Map • {TotalStars} stars • {CountUnlockedLocations()}/{_progress.Locations.Count} locations unlocked";
        }

        public string BuildSelectedLocationLabel()
        {
            var location = SelectedLocation;
            if (location == null) return "No locations configured";
            string state = SelectedLocationUnlocked ? "UNLOCKED" : $"LOCKED - needs {location.RequiredStars} stars";
            return $"{location.DisplayName} • {state} • {location.Description}";
        }

        public string BuildLocationRowLabel(int index)
        {
            if (index < 0 || index >= _progress.Locations.Count) return string.Empty;
            var location = _progress.Locations[index];
            string cursor = index == _selectedLocationIndex ? "> " : "  ";
            string state = _progress.IsLocationUnlocked(location.Id) ? "OPEN" : $"LOCKED {TotalStars}/{location.RequiredStars}";
            return $"{cursor}{location.DisplayName} [{state}]";
        }

        public string BuildSelectedMissionLabel()
        {
            if (!SelectedLocationUnlocked) return "Unlock this location to see its missions.";
            if (!SelectedMission.HasValue) return "No missions in this location yet.";
            return BuildMissionRowLabel(_selectedMissionIndex);
        }

        public string BuildMissionRowLabel(int index)
        {
            var location = SelectedLocation;
            if (location == null || location.Missions == null || index < 0 || index >= location.Missions.Length) return string.Empty;
            var mission = location.Missions[index];
            var record = _progress.GetMissionProgressOrEmpty(mission);
            string cursor = index == _selectedMissionIndex ? "> " : "  ";
            string status = record.Completed ? $"{record.BestStars}★ {record.BestScore} {record.BestRank}" : "NEW";
            return $"{cursor}{DisplayNameForMission(mission)} • {status}";
        }

        public List<string> BuildMissionRows()
        {
            var rows = new List<string>();
            var location = SelectedLocation;
            if (location == null || location.Missions == null) return rows;
            for (int i = 0; i < location.Missions.Length; i++) rows.Add(BuildMissionRowLabel(i));
            return rows;
        }

        public int CountUnlockedLocations()
        {
            int count = 0;
            for (int i = 0; i < _progress.Locations.Count; i++)
                if (_progress.IsLocationUnlocked(_progress.Locations[i].Id)) count++;
            return count;
        }

        public static string DisplayNameForMission(GameManager.MissionVariant mission)
        {
            switch (mission)
            {
                case GameManager.MissionVariant.BackyardRescue: return "Backyard Rescue";
                case GameManager.MissionVariant.SnackHeist: return "Snack Heist";
                case GameManager.MissionVariant.SockPanic: return "Sock Panic";
                case GameManager.MissionVariant.SquirrelConspiracy: return "Squirrel Conspiracy";
                case GameManager.MissionVariant.EagleShadowPanic: return "Eagle Shadow Panic";
                case GameManager.MissionVariant.CoyotesFence: return "Coyotes at the Fence";
                case GameManager.MissionVariant.WeenieRoundup: return "Weenie Roundup";
                case GameManager.MissionVariant.ScentSearch: return "Scent Search";
                case GameManager.MissionVariant.ThunderstormComfort: return "Thunderstorm Comfort";
                case GameManager.MissionVariant.MarkTheYard: return "Mark the Yard";
                case GameManager.MissionVariant.LeashWalk: return "Walkies on the Leash";
                case GameManager.MissionVariant.CarRide: return "Car Ride Balance";
                default: return mission.ToString();
            }
        }

        private static int ClampIndex(int index, int count)
        {
            if (count <= 0) return 0;
            int wrapped = index % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }
    }
}
