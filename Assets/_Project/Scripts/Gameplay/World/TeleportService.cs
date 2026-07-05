using BES.Core;
using UnityEngine;

namespace BES.Gameplay
{
    public static class TeleportService
    {
        public static bool TryFastTravelToRegion(string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
                return false;

            if (MetaProgressState.Instance != null &&
                !MetaProgressState.Instance.IsTeleportUnlocked(regionId) &&
                !MetaProgressState.Instance.IsRegionDiscovered(regionId))
                return false;

            var point = FindTeleportForRegion(regionId);
            if (point == null || point.Destination == null)
                return false;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return false;

            TeleportPlayer(
                player.transform,
                point.Destination.position,
                point.Destination.rotation,
                point.PointId,
                point.RegionId);
            return true;
        }

        public static void TeleportPlayer(
            Transform player,
            Vector3 position,
            Quaternion rotation,
            string teleportId,
            string regionId)
        {
            if (player == null)
                return;

            player.position = position;
            player.rotation = rotation;

            if (!string.IsNullOrEmpty(teleportId))
                MetaProgressState.Instance?.UnlockTeleport(teleportId);

            if (!string.IsNullOrEmpty(regionId))
            {
                MetaProgressState.Instance?.DiscoverRegion(regionId);
                GameEvents.RaiseRegionEntered(regionId);
            }

            if (GameManager.Instance?.Save != null)
                GameManager.Instance.Save.Current.currentRegionId = regionId;
        }

        static TeleportPoint FindTeleportForRegion(string regionId)
        {
            var points = Object.FindObjectsByType<TeleportPoint>(FindObjectsSortMode.None);
            foreach (var point in points)
            {
                if (point.RegionId == regionId)
                    return point;
            }

            return null;
        }
    }
}
