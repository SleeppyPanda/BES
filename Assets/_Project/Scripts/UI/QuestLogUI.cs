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
            ConfigureQuestListLayout();
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

            foreach (var questId in BuildOrderedQuestIds(quests))
            {
                var quest = quests.GetQuest(questId);
                if (quest == null)
                    continue;

                AddQuestCard(GetContainerForQuest(quest), quest, quests.GetCurrentStep(questId));
            }
            RebuildQuestListLayout();

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
            EnsureCardLayout(card);
            card.Setup(quest, step != null ? step.description : quest.summary, SelectQuest);
            cards.Add(card);
        }

        Transform GetContainerForQuest(QuestDefinition quest)
        {
            if (quest == null)
                return worldQuestContainer;

            if (quest.questType == QuestType.Main)
                return storyQuestContainer;
            if (IsCommissionQuest(quest))
                return commissionQuestContainer;
            return worldQuestContainer;
        }

        static List<string> BuildOrderedQuestIds(QuestManager quests)
        {
            var ordered = new List<string>();
            AddQuestIdsBySection(quests, ordered, QuestType.Main, false);
            AddQuestIdsBySection(quests, ordered, QuestType.Side, true);
            AddQuestIdsBySection(quests, ordered, QuestType.Side, false);
            return ordered;
        }

        static void AddQuestIdsBySection(QuestManager quests, List<string> ordered, QuestType type, bool commission)
        {
            if (quests == null)
                return;

            foreach (var questId in quests.ActiveQuests)
            {
                var quest = quests.GetQuest(questId);
                if (quest == null || quest.questType != type || IsCommissionQuest(quest) != commission)
                    continue;

                ordered.Add(questId);
            }
        }

        static bool IsCommissionQuest(QuestDefinition quest)
        {
            return quest != null &&
                   !string.IsNullOrEmpty(quest.questId) &&
                   quest.questId.ToLowerInvariant().Contains("commission");
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
            RebuildQuestListLayout();
        }

        void ClearRewards() => ClearChildren(rewardContainer);

        void ClearChildren(Transform root)
        {
            if (root == null)
                return;

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        void ConfigureQuestListLayout()
        {
            ConfigureSectionContainer(storyQuestContainer);
            ConfigureSectionContainer(commissionQuestContainer);
            ConfigureSectionContainer(worldQuestContainer);
            ConfigureSectionRoot(storyQuestContainer);
            ConfigureSectionRoot(commissionQuestContainer);
            ConfigureSectionRoot(worldQuestContainer);
            RebuildQuestListLayout();
        }

        static void ConfigureSectionContainer(Transform container)
        {
            if (container == null)
                return;

            var layout = container.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = container.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void ConfigureSectionRoot(Transform container)
        {
            if (container == null || container.parent == null)
                return;

            ConfigureSectionContainer(container.parent);
        }

        static void EnsureCardLayout(QuestCardUI card)
        {
            if (card == null)
                return;

            var layoutElement = card.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = card.gameObject.AddComponent<LayoutElement>();

            if (layoutElement.preferredHeight <= 0f)
                layoutElement.preferredHeight = 72f;
            layoutElement.flexibleHeight = 0f;
        }

        void RebuildQuestListLayout()
        {
            RebuildLayout(storyQuestContainer);
            RebuildLayout(commissionQuestContainer);
            RebuildLayout(worldQuestContainer);
        }

        static void RebuildLayout(Transform root)
        {
            if (root == null)
                return;

            if (root is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            if (root.parent is RectTransform parentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
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
