using System.Collections.Generic;
using BES.UI;
using UnityEngine;

namespace BES.Gameplay
{
    public class MetaProgressState : MonoBehaviour
    {
        public static MetaProgressState Instance { get; private set; }

        readonly HashSet<string> unlockedTeleports = new();
        readonly HashSet<string> discoveredRegions = new();
        readonly HashSet<string> collectedWorldObjects = new();
        readonly HashSet<int> eventClaimedDays = new();
        readonly HashSet<string> ownedArtifactIds = new();

        int eventStreakDay;
        string equippedArtifactId = string.Empty;
        ArtifactDatabase artifactDatabase;

        public string EquippedArtifactId => equippedArtifactId;
        public int EventStreakDay => eventStreakDay;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            artifactDatabase ??= Resources.Load<ArtifactDatabase>("Data/ArtifactDatabase");
            if (ownedArtifactIds.Count == 0)
                UnlockArtifact("artifact_starter");
        }

        public void ResetAll()
        {
            unlockedTeleports.Clear();
            discoveredRegions.Clear();
            collectedWorldObjects.Clear();
            eventClaimedDays.Clear();
            eventStreakDay = 0;
            equippedArtifactId = string.Empty;
            ownedArtifactIds.Clear();
            UnlockArtifact("artifact_anthem");
        }

        public bool OwnsArtifact(string artifactId) =>
            !string.IsNullOrEmpty(artifactId) && ownedArtifactIds.Contains(artifactId);

        public void UnlockArtifact(string artifactId)
        {
            if (!string.IsNullOrEmpty(artifactId))
                ownedArtifactIds.Add(artifactId);
        }

        public ArtifactDefinition GetEquippedArtifact()
        {
            if (string.IsNullOrEmpty(equippedArtifactId) || artifactDatabase == null)
                return null;
            return artifactDatabase.GetById(equippedArtifactId);
        }

        public bool IsTeleportUnlocked(string teleportId) =>
            !string.IsNullOrEmpty(teleportId) && unlockedTeleports.Contains(teleportId);

        public void UnlockTeleport(string teleportId)
        {
            if (!string.IsNullOrEmpty(teleportId))
                unlockedTeleports.Add(teleportId);
        }

        public bool IsRegionDiscovered(string regionId) =>
            !string.IsNullOrEmpty(regionId) && discoveredRegions.Contains(regionId);

        public void DiscoverRegion(string regionId)
        {
            if (!string.IsNullOrEmpty(regionId))
                discoveredRegions.Add(regionId);
        }

        public bool IsWorldObjectCollected(string instanceId) =>
            !string.IsNullOrEmpty(instanceId) && collectedWorldObjects.Contains(instanceId);

        public void MarkWorldObjectCollected(string instanceId)
        {
            if (!string.IsNullOrEmpty(instanceId))
                collectedWorldObjects.Add(instanceId);
        }

        public bool IsEventDayClaimed(int day) => eventClaimedDays.Contains(day);

        public void MarkEventDayClaimed(int day)
        {
            if (day <= 0)
                return;

            eventClaimedDays.Add(day);
            if (day > eventStreakDay)
                eventStreakDay = day;
        }

        public void SetEquippedArtifact(string artifactId) =>
            equippedArtifactId = OwnsArtifact(artifactId) ? artifactId : equippedArtifactId;

        public void ExportToSave(SaveData data)
        {
            if (data == null)
                return;

            data.eventStreakDay = eventStreakDay;
            data.eventClaimedDays = new List<int>(eventClaimedDays);
            data.unlockedTeleportIds = new List<string>(unlockedTeleports);
            data.discoveredRegionIds = new List<string>(discoveredRegions);
            data.collectedWorldObjectIds = new List<string>(collectedWorldObjects);
            data.equippedArtifactId = equippedArtifactId;
            data.ownedArtifactIds = new List<string>(ownedArtifactIds);
        }

        public void ImportFromSave(SaveData data)
        {
            ResetAll();
            if (data == null)
                return;

            eventStreakDay = data.eventStreakDay;
            if (data.eventClaimedDays != null)
                foreach (var day in data.eventClaimedDays)
                    eventClaimedDays.Add(day);

            if (data.unlockedTeleportIds != null)
                foreach (var id in data.unlockedTeleportIds)
                    unlockedTeleports.Add(id);

            if (data.discoveredRegionIds != null)
                foreach (var id in data.discoveredRegionIds)
                    discoveredRegions.Add(id);

            if (data.collectedWorldObjectIds != null)
                foreach (var id in data.collectedWorldObjectIds)
                    collectedWorldObjects.Add(id);

            equippedArtifactId = data.equippedArtifactId ?? string.Empty;

            if (data.ownedArtifactIds != null && data.ownedArtifactIds.Count > 0)
            {
                foreach (var id in data.ownedArtifactIds)
                    ownedArtifactIds.Add(id);
            }
            else
            {
                UnlockArtifact("artifact_starter");
            }
        }
    }
}
