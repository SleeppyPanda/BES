using System.Collections.Generic;
using BES.Core;
using BES.Narrative;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class QuestLogUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Transform listContainer;
        [SerializeField] TMP_Text rowPrefab;
        [SerializeField] Button closeButton;

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
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
            if (listContainer == null || rowPrefab == null)
                return;

            for (var i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);

            var quests = GameManager.Instance?.Quests;
            if (quests == null)
                return;

            foreach (var questId in quests.ActiveQuests)
                AddRow($"[Active] {GetQuestLabel(questId, quests)}");

            AddRow("— Completed —");
            foreach (var questId in quests.ExportCompletedQuests())
                AddRow($"[Done] {GetQuestLabel(questId, quests)}");
        }

        void AddRow(string text)
        {
            var row = Instantiate(rowPrefab, listContainer);
            row.text = text;
        }

        static string GetQuestLabel(string questId, QuestManager quests)
        {
            var quest = quests.GetQuest(questId);
            return quest != null ? quest.questTitle : questId;
        }
    }
}
