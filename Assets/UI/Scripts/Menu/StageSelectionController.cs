using System.Collections.Generic;
using BES.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public enum StageCollection { Resources, SanctumRelics, WeaponBreakthrough, CustomGroup }

    public class StageSelectionController : MonoBehaviour
    {
        static string pendingGroupOverride;

        [SerializeField] MenuContentDatabase database;
        [SerializeField] MenuNavigator navigator;
        [SerializeField] StageCollection collection;
        [Tooltip("Used when Collection = CustomGroup. This matches MenuContentDatabase.PlayModeStageGroup.id.")]
        [SerializeField] string customGroupId;
        [SerializeField] Transform stageRoot;
        [SerializeField] Button stageButtonPrefab;
        [SerializeField] Image previewImage;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text energyText;
        [SerializeField] Transform rewardRoot;
        [SerializeField] Image rewardIconPrefab;
        [SerializeField] Button enterButton;
        [SerializeField] Button backButton;
        [SerializeField] UnityEvent<string> onStageEntered;
        List<StageEntry> stages;
        StageEntry selected;
        string activeGroupId;

        void OnEnable() { Rebuild(); }
        void Start()
        {
            if (enterButton != null) enterButton.onClick.AddListener(EnterSelected);
            if (backButton != null) backButton.onClick.AddListener(() => navigator?.Back());
        }

        public void Rebuild()
        {
            ResolveDatabase();
            if (database == null) return;
            database.EnsureDefaultPlayModeStages();
            var groupOverride = pendingGroupOverride;
            pendingGroupOverride = null;
            activeGroupId = !string.IsNullOrWhiteSpace(groupOverride)
                ? groupOverride.Trim()
                : collection switch
                {
                    StageCollection.SanctumRelics => "sanctum",
                    StageCollection.WeaponBreakthrough => "weapon",
                    StageCollection.CustomGroup => customGroupId,
                    _ => "resources"
                };
            stages = !string.IsNullOrWhiteSpace(groupOverride)
                ? database.GetPlayModeStages(groupOverride)
                : collection switch
            {
                StageCollection.SanctumRelics => database.sanctumStages,
                StageCollection.WeaponBreakthrough => database.weaponStages,
                StageCollection.CustomGroup => database.GetPlayModeStages(customGroupId),
                _ => database.resourceStages
            };
            stages ??= new List<StageEntry>();
            if (stageRoot != null && stageButtonPrefab != null)
            {
                foreach (Transform child in stageRoot) Destroy(child.gameObject);
                foreach (var stage in stages)
                {
                    var captured = stage;
                    var button = Instantiate(stageButtonPrefab, stageRoot);
                    var label = button.GetComponentInChildren<TMP_Text>();
                    if (label != null) label.text = stage.title;
                    button.onClick.AddListener(() => Select(captured));
                }
            }
            Select(stages.Count > 0 ? stages[0] : null);
        }

        void Select(StageEntry stage)
        {
            selected = stage;
            if (previewImage != null) { previewImage.enabled = stage != null; previewImage.sprite = stage?.preview; }
            if (titleText != null) titleText.text = stage?.title ?? string.Empty;
            if (descriptionText != null) descriptionText.text = stage?.description ?? string.Empty;
            if (energyText != null) energyText.text = stage == null ? "-" : stage.energyCost.ToString();
            if (enterButton != null) enterButton.interactable = stage != null;
            if (rewardRoot == null || rewardIconPrefab == null) return;
            foreach (Transform child in rewardRoot) Destroy(child.gameObject);
            if (stage == null) return;
            foreach (var reward in stage.rewards)
            {
                if (reward == null || string.IsNullOrWhiteSpace(reward.id) ||
                    (reward.amount <= 0 && reward.minAmount <= 0 && reward.maxAmount <= 0))
                    continue;
                var icon = Instantiate(rewardIconPrefab, rewardRoot);
                icon.sprite = reward.icon;
                icon.enabled = reward.icon != null;
                var label = icon.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    var chance = reward.guaranteed ? "100%" : $"{Mathf.Clamp(reward.dropChancePercent, 0, 100)}%";
                    var min = reward.minAmount > 0 ? reward.minAmount : Mathf.Max(1, reward.amount);
                    var max = reward.maxAmount > 0 ? reward.maxAmount : min;
                    label.text = max > min ? $"{min}-{max}\n{chance}" : $"{min}\n{chance}";
                }
            }
        }

        void EnterSelected()
        {
            if (selected == null) return;
            onStageEntered?.Invoke(selected.id);
            TurnBattleUI.ActiveStageId = selected.id;
            TurnBattleUI.IsPlayModeBattle = true;
            TurnBattleUI.ActivePlayModeStageGroupId = !string.IsNullOrWhiteSpace(activeGroupId)
                ? activeGroupId
                : selected.playModeType;
            var save = GameManager.Instance?.Save?.Current;
            if (save != null)
            {
                save.activeBattleStageId = selected.id;
                save.activeBattleIsPlayMode = true;
                save.activePlayModeStageGroupId = TurnBattleUI.ActivePlayModeStageGroupId;
                GameManager.Instance.SaveGame();
            }
            navigator?.Open(MenuScreenId.PlayParty);
        }

        public static void OpenGroupOnNextEnable(string groupId)
        {
            pendingGroupOverride = string.IsNullOrWhiteSpace(groupId) ? null : groupId.Trim();
        }

        void ResolveDatabase()
        {
            if (database != null) return;

            database = Resources.Load<MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
            if (database == null)
                database = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
#endif
        }
    }
}
