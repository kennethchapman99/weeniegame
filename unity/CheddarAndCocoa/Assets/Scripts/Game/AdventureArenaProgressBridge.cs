using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheddarAndCocoa.Game
{
    public sealed class AdventureArenaProgressBridge : MonoBehaviour
    {
        private AdventureProgressService _progress;
        private GameManager.MissionVariant _mission;
        private string _locationId;
        private GameManager _game;
        private bool _recorded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "ArenaScene") return;
            if (!AdventureMissionLaunch.TryConsume(out var mission, out var locationId)) return;

            var go = new GameObject("AdventureArenaProgressBridge");
            var bridge = go.AddComponent<AdventureArenaProgressBridge>();
            bridge.Init(mission, locationId, AdventureProgressService.LoadDefault());
        }

        public void Init(GameManager.MissionVariant mission, string locationId, AdventureProgressService progress)
        {
            _mission = mission;
            _locationId = locationId ?? string.Empty;
            _progress = progress ?? AdventureProgressService.LoadDefault();
            _recorded = false;
            StartCoroutine(StartWhenArenaReady());
        }

        private IEnumerator StartWhenArenaReady()
        {
            for (int i = 0; i < 30 && _game == null; i++)
            {
                _game = FindFirstObjectByType<GameManager>();
                if (_game == null) yield return null;
            }

            if (_game == null)
            {
                Debug.LogWarning("AdventureArenaProgressBridge could not find GameManager in ArenaScene.");
                yield break;
            }

            _game.StartMission(_mission);
            StartCoroutine(WatchForMissionEnd());
        }

        private IEnumerator WatchForMissionEnd()
        {
            while (_game != null && !_recorded)
            {
                if (_game.EndScreenVisible && _game.Outcome != GameManager.MissionOutcome.InProgress)
                {
                    RecordResult();
                    yield break;
                }
                yield return null;
            }
        }

        private void RecordResult()
        {
            if (_recorded || _game == null || _progress == null) return;
            _recorded = true;

            bool cleared = _game.Outcome == GameManager.MissionOutcome.Clear;
            _progress.RecordMissionResult(_game.ActiveMissionVariant, _game.Score, _game.StarRating, _game.EndRank, cleared);
            bool saved = _progress.Save();
            Debug.Log($"Adventure progress recorded: {_locationId}/{_game.ActiveMissionVariant} score {_game.Score}, stars {_game.StarRating}, saved={saved}");
        }
    }
}
