namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Tiny handoff object between the AdventureMap and ArenaScene. It lets the map queue a mission
    /// without forcing GameManager to know about map UI or save persistence.
    /// </summary>
    public static class AdventureMissionLaunch
    {
        private static bool _hasPendingMission;
        private static GameManager.MissionVariant _pendingMission;
        private static string _pendingLocationId;

        public static bool HasPendingMission => _hasPendingMission;

        public static void QueueMission(GameManager.MissionVariant mission, string locationId)
        {
            _hasPendingMission = true;
            _pendingMission = mission;
            _pendingLocationId = locationId ?? string.Empty;
        }

        public static bool TryPeek(out GameManager.MissionVariant mission, out string locationId)
        {
            mission = _pendingMission;
            locationId = _pendingLocationId;
            return _hasPendingMission;
        }

        public static bool TryConsume(out GameManager.MissionVariant mission, out string locationId)
        {
            bool hadPending = TryPeek(out mission, out locationId);
            Clear();
            return hadPending;
        }

        public static void Clear()
        {
            _hasPendingMission = false;
            _pendingMission = default;
            _pendingLocationId = string.Empty;
        }
    }
}
