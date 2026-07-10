using System.Collections.Generic;
using BES.Core;
using BES.Gameplay;
using BES.Narrative;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class QuestLogUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Transform storyQuestContainer;
        [SerializeField] Transform commissionQuestContainer;
        [SerializeField] Transform worldQuestContainer;
        [SerializeField] QuestCardUI questCardPrefab;
        [SerializeField] Button closeButton;
        [SerializeField] Button navigateButton;
        [SerializeField] RawImage fixedArtworkA;
        [SerializeField] RawImage fixedArtworkB;
        [SerializeField] RawImage locationImage;
        [SerializeField] TMP_Text questTitleText;
        [SerializeField] TMP_Text questLocationText;
        [SerializeField] TMP_Text questDetailText;
        [SerializeField] Transform rewardContainer;
        [SerializeField] QuestRewardItemUI rewardItemPrefab;

        readonly List<QuestCardUI> cards = new List<QuestCardUI>();
        QuestDefinition selectedQuest;

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (navigateButton != null)
                navigateButton.onClick.AddListener(NavigateSelectedQuest);
        }

        void OnEnable() => GameEvents.OnQuestUpdated += OnQuestUpdated;

        void OnDisable() => GameEvents.OnQuestUpdated -= OnQuestUpdated;

        void OnQuestUpdated(string _)
        {
            if (IsOpen)
                Refresh();
        }

        public void Toggle()
        {
            if (panel == null)
                return;

            panel.SetActive(!panel.activeSelf);
            if (panel.activeSelf)
                Refresh();
        }

        public void Close()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        void Refresh()
        {
            ClearQuestCards();
            var quests = GameManager.Instance?.Quests;
            if (quests == null)
                return;

            foreach (var questId in quests.ActiveQuests)
            {
                var quest = quests.GetQuest(questId);
                if (quest == null)
                    continue;

                AddQuestCard(GetContainerForQuest(quest), quest, quests.GetCurrentStep(questId));
            }

            if (selectedQuest == null || !IsQuestActive(quests, selectedQuest.questId))
            {
                var tracked = quests.GetQuest(quests.GetPrimaryActiveQuestId());
                selectedQuest = tracked;
            }

            SelectQuest(selectedQuest);
        }

        void AddQuestCard(Transform container, QuestDefinition quest, QuestStep step)
        {
            if (container == null || questCardPrefab == null)
                return;

            var card = Instantiate(questCardPrefab, container);
            card.Setup(quest, step != null ? step.description : quest.summary, SelectQuest);
            cards.Add(card);
        }

        Transform GetContainerForQuest(QuestDefinition quest)
        {
            if (quest == null)
                return worldQuestContainer;

            if (quest.questType == QuestType.Main)
                return storyQuestContainer;
            if (!string.IsNullOrEmpty(quest.questId) && quest.questId.ToLowerInvariant().Contains("commission"))
                return commissionQuestContainer;
            return worldQuestContainer;
        }

        void SelectQuest(QuestDefinition quest)
        {
            selectedQuest = quest;
            foreach (var card in cards)
                card.SetSelected(card != null && quest != null && card.QuestId == quest.questId);

            RefreshDetail();
        }

        void RefreshDetail()
        {
            ClearRewards();
            var quests = GameManager.Instance?.Quests;
            if (selectedQuest == null || quests == null)
            {
                if (questTitleText != null) questTitleText.text = "No quest selected";
                if (questLocationText != null) questLocationText.text = "Quest location";
                if (questDetailText != null) questDetailText.text = string.Empty;
                return;
            }

            var step = quests.GetCurrentStep(selectedQuest.questId);
            if (questTitleText != null)
                questTitleText.text = selectedQuest.questTitle;
            if (questLocationText != null)
                questLocationText.text = step != null && !string.IsNullOrEmpty(step.targetId)
                    ? $"Quest location: {step.targetId}"
                    : "Quest location: not assigned";
            if (questDetailText != null)
                questDetailText.text = step != null ? step.description : selectedQuest.summary;

            AddReward(selectedQuest.rewardItemId, selectedQuest.rewardItemCount);
        }

        void NavigateSelectedQuest()
        {
            if (selectedQuest == null)
                return;

            GameManager.Instance?.Quests.TrackQuest(selectedQuest.questId);
        }

        static bool IsQuestActive(QuestManager quests, string questId)
        {
            if (quests == null || string.IsNullOrEmpty(questId))
                return false;

            foreach (var activeQuestId in quests.ActiveQuests)
            {
                if (activeQuestId == questId)
                    return true;
            }

            return false;
        }

        void ClearQuestCards()
        {
            cards.Clear();
            ClearChildren(storyQuestContainer);
            ClearChildren(commissionQuestContainer);
            ClearChildren(worldQuestContainer);
        }

        void ClearRewards() => ClearChildren(rewardContainer);

        void ClearChildren(Transform root)
        {
            if (root == null)
                return;

            for (var i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        void AddReward(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || rewardContainer == null || rewardItemPrefab == null)
                return;

            var reward = Instantiate(rewardItemPrefab, rewardContainer);
            var item = GameManager.Instance?.Inventory.GetDefinition(itemId);
            var label = item != null ? item.displayName : itemId;
            if (amount > 1)
                label += $" x{amount}";
            reward.Setup(label, item != null ? item.rarity : 3);
        }
    }
}
