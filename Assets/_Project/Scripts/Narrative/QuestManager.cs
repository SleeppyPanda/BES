using System.Collections.Generic;
using BES.Core;
using BES.Gameplay;
using UnityEngine;

namespace BES.Narrative
{
    public class QuestManager : MonoBehaviour
    {
        [SerializeField] List<QuestDefinition> questDefinitions = new();
        [SerializeField] QuestDatabase questDatabase;

        readonly Dictionary<string, QuestDefinition> lookup = new();
        readonly Dictionary<string, int> questStepProgress = new();
        readonly HashSet<string> activeQuests = new();
        readonly HashSet<string> completedQuests = new();

        string currentBranch = "main";
        string currentEndingId = string.Empty;

        public string CurrentBranch => currentBranch;
        public string CurrentEndingId => currentEndingId;
        public IReadOnlyCollection<string> ActiveQuests => activeQuests;

        void Awake()
        {
            if (questDatabase != null)
            {
                foreach (var quest in questDatabase.All)
                    RegisterQuest(quest);
            }

            foreach (var quest in questDefinitions)
                RegisterQuest(quest);
        }

        void RegisterQuest(QuestDefinition quest)
        {
            if (quest != null && !string.IsNullOrEmpty(quest.questId))
                lookup[quest.questId] = quest;
        }

        public void ResetProgress()
        {
            activeQuests.Clear();
            completedQuests.Clear();
            questStepProgress.Clear();
            currentBranch = "main";
            currentEndingId = string.Empty;
        }

        public bool StartQuest(string questId)
        {
            if (!lookup.ContainsKey(questId) || completedQuests.Contains(questId))
                return false;

            activeQuests.Add(questId);
            if (!questStepProgress.ContainsKey(questId))
                questStepProgress[questId] = 0;
            GameEvents.RaiseQuestUpdated(questId);
            return true;
        }

        public void AdvanceQuest(string questId, int stepDelta = 1)
        {
            if (!activeQuests.Contains(questId))
                StartQuest(questId);

            if (!questStepProgress.ContainsKey(questId))
                questStepProgress[questId] = 0;

            questStepProgress[questId] += stepDelta;
            var quest = lookup[questId];
            if (quest != null && questStepProgress[questId] >= quest.steps.Count)
                CompleteQuest(questId);
            else if (quest != null)
                TryStartChoiceDialogue(questId, quest);

            GameEvents.RaiseQuestUpdated(questId);
        }

        void TryStartChoiceDialogue(string questId, QuestDefinition quest)
        {
            if (!questStepProgress.TryGetValue(questId, out var idx) || idx < 0 || idx >= quest.steps.Count)
                return;

            if (quest.steps[idx].stepType != QuestStepType.Choice)
                return;

            DialogueSystem.Instance?.StartDialogue("ending_choice");
        }

        public bool TryAdvanceCurrentStep(string questId, QuestStepType stepType, string targetId)
        {
            if (!lookup.TryGetValue(questId, out var quest))
                return false;

            if (!activeQuests.Contains(questId))
                StartQuest(questId);

            var stepIndex = GetStepIndex(questId);
            if (stepIndex < 0 || stepIndex >= quest.steps.Count)
                return false;

            var step = quest.steps[stepIndex];
            if (step.stepType != stepType)
                return false;

            if (!string.IsNullOrEmpty(step.targetId) &&
                !string.IsNullOrEmpty(targetId) &&
                step.targetId != targetId)
            {
                if (stepType != QuestStepType.Choice || step.targetId != "branch_choice")
                    return false;
            }

            if (stepType == QuestStepType.Collect)
            {
                var count = GameManager.Instance?.Inventory.GetCount(targetId) ?? 0;
                if (count < step.requiredCount)
                    return false;
            }

            AdvanceQuest(questId);
            return true;
        }

        public void CompleteQuest(string questId)
        {
            if (!lookup.TryGetValue(questId, out var quest))
                return;

            activeQuests.Remove(questId);
            completedQuests.Add(questId);

            if (!string.IsNullOrEmpty(quest.rewardItemId))
                GameManager.Instance?.Inventory.AddItem(quest.rewardItemId, quest.rewardItemCount);

            if (!string.IsNullOrEmpty(quest.endingId))
                currentEndingId = quest.endingId;

            GameEvents.RaiseQuestUpdated(questId);
        }

        public void SetBranch(string branchId)
        {
            if (!string.IsNullOrEmpty(branchId))
                currentBranch = branchId;
        }

        public QuestDefinition GetQuest(string questId) =>
            lookup.TryGetValue(questId, out var quest) ? quest : null;

        public int GetStepIndex(string questId) =>
            questStepProgress.TryGetValue(questId, out var step) ? step : -1;

        public QuestStep GetCurrentStep(string questId)
        {
            if (!lookup.TryGetValue(questId, out var quest))
                return null;

            var index = GetStepIndex(questId);
            if (index < 0 || index >= quest.steps.Count)
                return null;

            return quest.steps[index];
        }

        public List<string> ExportActiveQuests() => new(activeQuests);
        public List<string> ExportCompletedQuests() => new(completedQuests);

        public Dictionary<string, int> ExportStepProgress() => new(questStepProgress);

        public void ImportProgress(
            List<string> active,
            List<string> completed,
            string branch,
            string ending,
            Dictionary<string, int> stepProgress = null)
        {
            activeQuests.Clear();
            completedQuests.Clear();
            questStepProgress.Clear();

            if (active != null)
                foreach (var id in active)
                    activeQuests.Add(id);

            if (completed != null)
                foreach (var id in completed)
                    completedQuests.Add(id);

            if (stepProgress != null)
            {
                foreach (var pair in stepProgress)
                    questStepProgress[pair.Key] = pair.Value;
            }
            else
            {
                foreach (var id in activeQuests)
                    questStepProgress[id] = 0;
            }

            currentBranch = string.IsNullOrEmpty(branch) ? "main" : branch;
            currentEndingId = ending ?? string.Empty;
        }

        public void SetDatabase(QuestDatabase database)
        {
            questDatabase = database;
            if (questDatabase != null)
            {
                foreach (var quest in questDatabase.All)
                    RegisterQuest(quest);
            }
        }

        public string GetPrimaryActiveQuestId()
        {
            if (activeQuests.Contains("main_awakening"))
                return "main_awakening";

            foreach (var id in activeQuests)
                return id;

            return string.Empty;
        }

        public string GetActiveQuestStepDescription()
        {
            var questId = GetPrimaryActiveQuestId();
            if (string.IsNullOrEmpty(questId))
                return "Không có nhiệm vụ đang theo dõi";

            var quest = GetQuest(questId);
            var stepIndex = GetStepIndex(questId);
            if (quest == null || stepIndex < 0 || stepIndex >= quest.steps.Count)
                return quest != null ? quest.summary : string.Empty;

            return quest.steps[stepIndex].description;
        }

        public string GetActiveQuestTargetId()
        {
            var questId = GetPrimaryActiveQuestId();
            if (string.IsNullOrEmpty(questId))
                return string.Empty;

            var quest = GetQuest(questId);
            var stepIndex = GetStepIndex(questId);
            if (quest == null || stepIndex < 0 || stepIndex >= quest.steps.Count)
                return string.Empty;

            return quest.steps[stepIndex].targetId;
        }

        public string GetActiveQuestTitle()
        {
            var questId = GetPrimaryActiveQuestId();
            var quest = GetQuest(questId);
            return quest != null ? quest.questTitle : "—";
        }
    }
}
