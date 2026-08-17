using BES.Gameplay;
using BES.Narrative;
using BES.UI;
using UnityEngine;

namespace BES.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] SaveSystem saveSystem;
        [SerializeField] QuestManager questManager;
        [SerializeField] InventorySystem inventorySystem;
        [SerializeField] RelationshipSystem relationshipSystem;

        public SaveSystem Save => saveSystem;
        public QuestManager Quests => questManager;
        public InventorySystem Inventory => inventorySystem;
        public RelationshipSystem Relationships => relationshipSystem;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSystems();
            if (GetComponent<RuntimeResourceLoader>() == null)
                gameObject.AddComponent<RuntimeResourceLoader>();
            if (GetComponent<PerformanceSettings>() == null)
                gameObject.AddComponent<PerformanceSettings>();
            if (GetComponent<GameAutoSave>() == null)
                gameObject.AddComponent<GameAutoSave>();
            if (GetComponent<OpenWorldSliceValidator>() == null)
                gameObject.AddComponent<OpenWorldSliceValidator>();
        }

        void EnsureSystems()
        {
            saveSystem ??= GetComponentInChildren<SaveSystem>() ?? gameObject.AddComponent<SaveSystem>();
            questManager ??= GetComponentInChildren<QuestManager>() ?? gameObject.AddComponent<QuestManager>();
            inventorySystem ??= GetComponentInChildren<InventorySystem>() ?? gameObject.AddComponent<InventorySystem>();
            relationshipSystem ??= GetComponentInChildren<RelationshipSystem>() ?? gameObject.AddComponent<RelationshipSystem>();
        }

        public void NewGame()
        {
            saveSystem.DeleteSaveFile();
            questManager.ResetProgress();
            inventorySystem.Clear();
            relationshipSystem.ResetAll();
            NPCMemoryStore.ClearAll();
            PlayerWallet.Instance?.LoadDefaults();
            EquippedWeaponState.Instance?.ResetToDefaults();
            PartyRoster.Instance?.ResetToDefaults();
            MetaProgressState.Instance?.ResetAll();
            GachaPityState.Instance?.ResetAll();
            CharacterProgressionState.ResetAll();
            SceneLoader.Instance.LoadGameplay();
        }

        public void ContinueGame()
        {
            if (saveSystem.Load())
                SceneLoader.Instance.LoadGameplay();
        }

        public void SaveGame()
        {
            saveSystem.Save();
        }
    }
}
