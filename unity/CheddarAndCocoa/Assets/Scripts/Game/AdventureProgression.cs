using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public enum AdventureLocationStatus
    {
        Locked,
        Unlocked
    }

    [Serializable]
    public sealed class AdventureLocationDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public int RequiredStars;
        public string ThumbnailKey;
        public Color MapColor = Color.white;
        public GameManager.MissionVariant[] Missions = Array.Empty<GameManager.MissionVariant>();

        public bool IsUnlockedBy(int totalStars) => totalStars >= RequiredStars;
    }

    [Serializable]
    public sealed class MissionProgressRecord
    {
        public string MissionId;
        public int Attempts;
        public int Clears;
        public int BestScore;
        public int BestStars;
        public string BestRank;

        public bool Completed => Clears > 0;
    }

    [Serializable]
    public sealed class AdventureProgressSnapshot
    {
        public int Version = 1;
        public int TotalStars;
        public List<string> UnlockedLocationIds = new List<string>();
        public List<MissionProgressRecord> Missions = new List<MissionProgressRecord>();
    }

    public static class AdventureLocationCatalog
    {
        public const string BackyardId = "backyard";
        public const string FrontYardId = "front_yard";
        public const string HouseInteriorId = "house_interior";
        public const string NeighborhoodParkId = "neighborhood_park";

        public static List<AdventureLocationDefinition> CreateDefault()
        {
            return new List<AdventureLocationDefinition>
            {
                new AdventureLocationDefinition
                {
                    Id = BackyardId,
                    DisplayName = "Backyard",
                    Description = "Home base for weenie rescues, squirrel chaos, digging, marking, and comfort missions.",
                    RequiredStars = 0,
                    ThumbnailKey = "yard",
                    MapColor = new Color(0.45f, 0.78f, 0.36f),
                    Missions = new[]
                    {
                        GameManager.MissionVariant.BackyardRescue,
                        GameManager.MissionVariant.SquirrelConspiracy,
                        GameManager.MissionVariant.WeenieRoundup,
                        GameManager.MissionVariant.ScentSearch,
                        GameManager.MissionVariant.ThunderstormComfort,
                        GameManager.MissionVariant.MarkTheYard
                    }
                },
                new AdventureLocationDefinition
                {
                    Id = FrontYardId,
                    DisplayName = "Front Yard",
                    Description = "Boundary-control missions around gates, sidewalks, walkies, cars, and suspicious visitors.",
                    RequiredStars = 6,
                    ThumbnailKey = "front-yard",
                    MapColor = new Color(0.55f, 0.72f, 0.42f),
                    Missions = new[]
                    {
                        GameManager.MissionVariant.CoyotesFence,
                        GameManager.MissionVariant.LeashWalk,
                        GameManager.MissionVariant.CarRide
                    }
                },
                new AdventureLocationDefinition
                {
                    Id = HouseInteriorId,
                    DisplayName = "House Interior",
                    Description = "Indoor chaos missions for snack crimes, laundry emergencies, and household comedy.",
                    RequiredStars = 9,
                    ThumbnailKey = "house",
                    MapColor = new Color(0.82f, 0.65f, 0.45f),
                    Missions = new[]
                    {
                        GameManager.MissionVariant.SnackHeist,
                        GameManager.MissionVariant.SockPanic
                    }
                },
                new AdventureLocationDefinition
                {
                    Id = NeighborhoodParkId,
                    DisplayName = "Neighborhood Park",
                    Description = "A locked expansion node for bigger outside threats and future park/social missions.",
                    RequiredStars = 18,
                    ThumbnailKey = "park",
                    MapColor = new Color(0.38f, 0.68f, 0.52f),
                    Missions = new[]
                    {
                        GameManager.MissionVariant.EagleShadowPanic
                    }
                }
            };
        }
    }

    public sealed class AdventureProgressService
    {
        public const int CurrentVersion = 1;
        public const string SaveFileName = "adventure-progress.json";

        private readonly List<AdventureLocationDefinition> _locations;
        private readonly string _savePath;
        private AdventureProgressSnapshot _snapshot;

        public AdventureProgressSnapshot Snapshot => _snapshot;
        public IReadOnlyList<AdventureLocationDefinition> Locations => _locations;
        public string SavePath => _savePath;

        private AdventureProgressService(string savePath, List<AdventureLocationDefinition> locations, AdventureProgressSnapshot snapshot)
        {
            _savePath = savePath;
            _locations = locations ?? AdventureLocationCatalog.CreateDefault();
            _snapshot = snapshot ?? CreateNewSnapshot();
            NormalizeSnapshot();
        }

        public static string DefaultSavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static AdventureProgressService LoadDefault()
        {
            return Load(DefaultSavePath, AdventureLocationCatalog.CreateDefault());
        }

        public static AdventureProgressService Load(string savePath, List<AdventureLocationDefinition> locations = null)
        {
            AdventureProgressSnapshot snapshot = null;
            if (!string.IsNullOrEmpty(savePath) && File.Exists(savePath))
            {
                try
                {
                    string json = File.ReadAllText(savePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        snapshot = JsonUtility.FromJson<AdventureProgressSnapshot>(json);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Adventure progress save could not be read. Starting fresh. {ex.Message}");
                    snapshot = null;
                }
            }

            return new AdventureProgressService(savePath, locations ?? AdventureLocationCatalog.CreateDefault(), snapshot ?? CreateNewSnapshot());
        }

        public static AdventureProgressService CreateInMemoryForTests(List<AdventureLocationDefinition> locations = null)
        {
            return new AdventureProgressService(string.Empty, locations ?? AdventureLocationCatalog.CreateDefault(), CreateNewSnapshot());
        }

        public bool Save()
        {
            if (string.IsNullOrEmpty(_savePath)) return false;

            try
            {
                string directory = Path.GetDirectoryName(_savePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(_savePath, JsonUtility.ToJson(_snapshot, true));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Adventure progress save failed. {ex.Message}");
                return false;
            }
        }

        public void ClearSave()
        {
            _snapshot = CreateNewSnapshot();
            NormalizeSnapshot();
            if (!string.IsNullOrEmpty(_savePath) && File.Exists(_savePath)) File.Delete(_savePath);
        }

        public MissionProgressRecord RecordMissionResult(GameManager.MissionVariant mission, int score, int stars, string rank, bool cleared)
        {
            string missionId = mission.ToString();
            MissionProgressRecord record = GetOrCreateMissionRecord(missionId);
            record.Attempts++;
            if (cleared) record.Clears++;

            int clampedStars = Mathf.Clamp(stars, 0, 3);
            bool betterStars = clampedStars > record.BestStars;
            bool betterScore = score > record.BestScore;
            if (betterStars || betterScore)
            {
                record.BestScore = Mathf.Max(record.BestScore, score);
                record.BestStars = Mathf.Max(record.BestStars, clampedStars);
                record.BestRank = string.IsNullOrEmpty(rank) ? record.BestRank : rank;
            }

            RecalculateTotalsAndUnlocks();
            return record;
        }

        public bool TryGetMissionProgress(GameManager.MissionVariant mission, out MissionProgressRecord record)
        {
            record = FindMissionRecord(mission.ToString());
            return record != null;
        }

        public MissionProgressRecord GetMissionProgressOrEmpty(GameManager.MissionVariant mission)
        {
            return FindMissionRecord(mission.ToString()) ?? new MissionProgressRecord
            {
                MissionId = mission.ToString(),
                BestRank = "NEW"
            };
        }

        public bool IsLocationUnlocked(string locationId)
        {
            if (string.IsNullOrEmpty(locationId)) return false;
            return _snapshot.UnlockedLocationIds.Contains(locationId);
        }

        public AdventureLocationStatus GetLocationStatus(AdventureLocationDefinition location)
        {
            if (location == null) return AdventureLocationStatus.Locked;
            return IsLocationUnlocked(location.Id) ? AdventureLocationStatus.Unlocked : AdventureLocationStatus.Locked;
        }

        public AdventureLocationDefinition FindLocation(string locationId)
        {
            for (int i = 0; i < _locations.Count; i++)
            {
                if (_locations[i].Id == locationId) return _locations[i];
            }
            return null;
        }

        public AdventureLocationDefinition FindLocationForMission(GameManager.MissionVariant mission)
        {
            for (int i = 0; i < _locations.Count; i++)
            {
                var missions = _locations[i].Missions;
                if (missions == null) continue;
                for (int m = 0; m < missions.Length; m++)
                {
                    if (missions[m] == mission) return _locations[i];
                }
            }
            return null;
        }

        public List<GameManager.MissionVariant> GetUnlockedMissions()
        {
            var result = new List<GameManager.MissionVariant>();
            for (int i = 0; i < _locations.Count; i++)
            {
                if (!IsLocationUnlocked(_locations[i].Id) || _locations[i].Missions == null) continue;
                result.AddRange(_locations[i].Missions);
            }
            return result;
        }

        private static AdventureProgressSnapshot CreateNewSnapshot()
        {
            return new AdventureProgressSnapshot
            {
                Version = CurrentVersion,
                TotalStars = 0,
                UnlockedLocationIds = new List<string> { AdventureLocationCatalog.BackyardId },
                Missions = new List<MissionProgressRecord>()
            };
        }

        private MissionProgressRecord GetOrCreateMissionRecord(string missionId)
        {
            MissionProgressRecord record = FindMissionRecord(missionId);
            if (record != null) return record;

            record = new MissionProgressRecord
            {
                MissionId = missionId,
                BestRank = "NEW"
            };
            _snapshot.Missions.Add(record);
            return record;
        }

        private MissionProgressRecord FindMissionRecord(string missionId)
        {
            if (_snapshot.Missions == null) return null;
            for (int i = 0; i < _snapshot.Missions.Count; i++)
            {
                if (_snapshot.Missions[i].MissionId == missionId) return _snapshot.Missions[i];
            }
            return null;
        }

        private void NormalizeSnapshot()
        {
            if (_snapshot == null) _snapshot = CreateNewSnapshot();
            _snapshot.Version = CurrentVersion;
            if (_snapshot.UnlockedLocationIds == null) _snapshot.UnlockedLocationIds = new List<string>();
            if (_snapshot.Missions == null) _snapshot.Missions = new List<MissionProgressRecord>();
            if (!_snapshot.UnlockedLocationIds.Contains(AdventureLocationCatalog.BackyardId))
            {
                _snapshot.UnlockedLocationIds.Add(AdventureLocationCatalog.BackyardId);
            }
            RecalculateTotalsAndUnlocks();
        }

        private void RecalculateTotalsAndUnlocks()
        {
            int total = 0;
            if (_snapshot.Missions != null)
            {
                for (int i = 0; i < _snapshot.Missions.Count; i++)
                {
                    total += Mathf.Clamp(_snapshot.Missions[i].BestStars, 0, 3);
                }
            }
            _snapshot.TotalStars = total;

            for (int i = 0; i < _locations.Count; i++)
            {
                var location = _locations[i];
                if (location != null && location.IsUnlockedBy(total) && !_snapshot.UnlockedLocationIds.Contains(location.Id))
                {
                    _snapshot.UnlockedLocationIds.Add(location.Id);
                }
            }
        }
    }
}
