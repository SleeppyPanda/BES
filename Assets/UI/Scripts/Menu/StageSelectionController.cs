using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public enum StageCollection { Resources, SanctumRelics, WeaponBreakthrough }

    public class StageSelectionController : MonoBehaviour
    {
        [SerializeField] MenuContentDatabase database;
        [SerializeField] MenuNavigator navigator;
        [SerializeField] StageCollection collection;
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

        void OnEnable() { Rebuild(); }
        void Start()
        {
            if (enterButton != null) enterButton.onClick.AddListener(EnterSelected);
            if (backButton != null) backButton.onClick.AddListener(() => navigator?.Back());
        }

        public void Rebuild()
        {
            if (database == null) return;
            stages = collection switch
            {
                StageCollection.SanctumRelics => database.sanctumStages,
                StageCollection.WeaponBreakthrough => database.weaponStages,
                _ => database.resourceStages
            };
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
                var icon = Instantiate(rewardIconPrefab, rewardRoot);
                icon.sprite = reward.icon;
            }
        }

        void EnterSelected()
        {
            if (selected == null) return;
            onStageEntered?.Invoke(selected.id);
            TurnBattleUI.ActiveStageId = selected.id;
            navigator?.Open(MenuScreenId.Battle);
        }
    }
}
