using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BES.Core;
using BES.Narrative;
using BES.UI;
using UnityEngine;

namespace BES.Gameplay
{
    public static class SaveDataUtility
    {
        public static List<StringIntPair> ToPairs(Dictionary<string, int> dict)
        {
            var list = new List<StringIntPair>();
            if (dict == null)
                return list;

            foreach (var pair in dict)
                list.Add(new StringIntPair { key = pair.Key, value = pair.Value });
            return list;
        }

        public static Dictionary<string, int> FromPairs(List<StringIntPair> list)
        {
            var dict = new Dictionary<string, int>();
            if (list == null)
                return dict;

            foreach (var pair in list)
            {
                if (!string.IsNullOrEmpty(pair.key))
                    dict[pair.key] = pair.value;
            }
            return dict;
        }

        public static List<StringListPair> ToMemoryPairs(Dictionary<string, List<string>> dict)
        {
            var list = new List<StringListPair>();
            if (dict == null)
                return list;

            foreach (var pair in dict)
                list.Add(new StringListPair { key = pair.Key, values = new List<string>(pair.Value) });
            return list;
        }

        public static Dictionary<string, List<string>> FromMemoryPairs(List<StringListPair> list)
        {
            var dict = new Dictionary<string, List<string>>();
            if (list == null)
                return dict;

            foreach (var pair in list)
            {
                if (!string.IsNullOrEmpty(pair.key))
                    dict[pair.key] = pair.values ?? new List<string>();
            }
            return dict;
        }
    }

    public class SaveSystem : MonoBehaviour
    {
        const string SaveFileName = "bes_save.json";

        SaveData currentSave;
        string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public SaveData Current => currentSave ??= new SaveData();
        public bool HasSave => File.Exists(SavePath);
        public bool LoadedFromContinue { get; private set; }
        public bool IsNewSession { get; private set; }

        public void CreateNewSave()
        {
            currentSave = new SaveData();
            LoadedFromContinue = false;
            IsNewSession = true;
            Save();
        }

        public void DeleteSaveFile()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            currentSave = new SaveData();
            LoadedFromContinue = false;
            IsNewSession = true;
            File.WriteAllText(SavePath, JsonUtility.ToJson(currentSave, true));
        }

        public void Save()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var pos = player.transform.position;
                Current.playerPosX = pos.x;
                Current.playerPosY = pos.y;
                Current.playerPosZ = pos.z;

                if (player.TryGetComponent<PlayerStats>(out var stats))
                {
                    Current.playerHealth = stats.CurrentHealth;
                    Current.playerMana = stats.CurrentMana;
                }

                if (player.TryGetComponent<StaminaSystem>(out var stamina))
                    Current.playerStamina = stamina.Current;
            }

            if (GameManager.Instance != null)
            {
                Current.inventory = SaveDataUtility.ToPairs(GameManager.Instance.Inventory.ExportState());
                Current.relationships = SaveDataUtility.ToPairs(GameManager.Instance.Relationships.ExportState());
                Current.activeQuestIds = GameManager.Instance.Quests.ExportActiveQuests();
                Current.completedQuestIds = GameManager.Instance.Quests.ExportCompletedQuests();
                Current.trackedQuestId = GameManager.Instance.Quests.TrackedQuestId;
                Current.questStepProgress = SaveDataUtility.ToPairs(GameManager.Instance.Quests.ExportStepProgress());
                Current.storyBranch = GameManager.Instance.Quests.CurrentBranch;
                Current.endingId = GameManager.Instance.Quests.CurrentEndingId;
                Current.npcMemories = SaveDataUtility.ToMemoryPairs(NPCMemoryStore.ExportAll());
            }

            if (PlayerWallet.Instance != null)
                PlayerWallet.Instance.ExportToSave(Current);

            if (EquippedWeaponState.Instance != null)
                EquippedWeaponState.Instance.ExportToSave(Current);

            if (PartyRoster.Instance != null)
                PartyRoster.Instance.ExportToSave(Current);

            if (MetaProgressState.Instance != null)
                MetaProgressState.Instance.ExportToSave(Current);

            if (GachaPityState.Instance != null)
                GachaPityState.Instance.ExportToSave(Current);

            CharacterProgressionState.ExportToSave(Current);

            var json = JsonUtility.ToJson(Current, true);
            File.WriteAllText(SavePath, json);
            GameEvents.RaiseGameSaved();
        }

        public bool Load()
        {
            if (!HasSave)
                return false;

            var json = File.ReadAllText(SavePath);
            currentSave = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            LoadedFromContinue = true;
            IsNewSession = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.Inventory.ImportState(SaveDataUtility.FromPairs(Current.inventory));
                GameManager.Instance.Relationships.ImportState(SaveDataUtility.FromPairs(Current.relationships));
                GameManager.Instance.Quests.ImportProgress(
                    Current.activeQuestIds,
                    Current.completedQuestIds,
                    Current.storyBranch,
                    Current.endingId,
                    SaveDataUtility.FromPairs(Current.questStepProgress),
                    Current.trackedQuestId);
                NPCMemoryStore.ImportAll(SaveDataUtility.FromMemoryPairs(Current.npcMemories));
            }

            PlayerWallet.Instance?.ImportFromSave(Current);
            EquippedWeaponState.Instance?.ImportFromSave(Current);
            PartyRoster.Instance?.ImportFromSave(Current);
            MetaProgressState.Instance?.ImportFromSave(Current);
            GachaPityState.Instance?.ImportFromSave(Current);
            CharacterProgressionState.ImportFromSave(Current);

            GameEvents.RaiseGameLoaded();
            return true;
        }

        public void ApplyPlayerState(GameObject player)
        {
            if (player == null || currentSave == null)
                return;

            player.transform.position = new Vector3(
                currentSave.playerPosX,
                currentSave.playerPosY,
                currentSave.playerPosZ);

            if (player.TryGetComponent<PlayerStats>(out var stats))
                stats.LoadState(currentSave.playerHealth, currentSave.playerMana);

            if (player.TryGetComponent<StaminaSystem>(out var stamina))
                stamina.LoadState(currentSave.playerStamina);
        }

        // ==================== CLOUD SYNC SIMULATION ====================

        public async Task SyncSaveToCloudAsync()
        {
            if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated)
            {
                Debug.LogWarning("[CloudSave] Player is not authenticated. Skipping cloud upload.");
                return;
            }

            var userId = AuthManager.Instance.CurrentUserId;
            Debug.Log($"[CloudSave] Starting upload for user: {userId}...");
            
            // Auto save locally first
            Save();
            
            await Task.Delay(500); // Simulate network latency

            if (File.Exists(SavePath))
            {
                var json = File.ReadAllText(SavePath);
                // Simulate cloud database save
                PlayerPrefs.SetString($"BES_CloudSave_{userId}", json);
                PlayerPrefs.Save();
                Debug.Log($"[CloudSave] Save data successfully synced to cloud for user: {userId}. Data size: {json.Length} bytes.");
            }
        }

        public async Task<bool> SyncSaveFromCloudAsync()
        {
            if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated)
            {
                Debug.LogWarning("[CloudSave] Player is not authenticated. Skipping cloud download.");
                return false;
            }

            var userId = AuthManager.Instance.CurrentUserId;
            var cloudKey = $"BES_CloudSave_{userId}";

            if (!PlayerPrefs.HasKey(cloudKey))
            {
                Debug.Log($"[CloudSave] No cloud save found for user: {userId}. Using local save.");
                return false;
            }

            Debug.Log($"[CloudSave] Downloading save file for user: {userId}...");
            await Task.Delay(600); // Simulate network latency

            var json = PlayerPrefs.GetString(cloudKey);
            if (!string.IsNullOrEmpty(json))
            {
                File.WriteAllText(SavePath, json);
                currentSave = JsonUtility.FromJson<SaveData>(json);
                LoadedFromContinue = true;
                IsNewSession = false;
                Debug.Log($"[CloudSave] Cloud save downloaded and loaded successfully for user: {userId}.");
                return true;
            }

            return false;
        }
    }
}
