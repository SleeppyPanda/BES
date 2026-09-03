using System.Collections.Generic;
using BES.Core;
using BES.Gameplay;
using BES.UI;
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
        static readonly string[] QuestPanelTestQuestIds =
        {
            "story_arc_01",
            "story_arc_02",
            "commission_delivery_01",
            "commission_hunt_01",
            "commission_scout_01",
            "world_ruins_01",
            "world_gather_01",
            "world_trial_01",
            "world_lost_relic_01"
        };

        string currentBranch = "main";
        string currentEndingId = string.Empty;
        string trackedQuestId = string.Empty;

        public string CurrentBranch => currentBranch;
        public string CurrentEndingId => currentEndingId;
        public string TrackedQuestId => trackedQuestId;
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

            EnsureQuestPanelTestQuests();
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
            trackedQuestId = string.Empty;
        }

        public bool StartQuest(string questId)
        {
            if (!lookup.ContainsKey(questId) || completedQuests.Contains(questId))
                return false;

            activeQuests.Add(questId);
            if (!questStepProgress.ContainsKey(questId))
                questStepProgress[questId] = 0;
            if (string.IsNullOrEmpty(trackedQuestId))
                trackedQuestId = questId;
            GameEvents.RaiseQuestUpdated(questId);
            GameManager.Instance?.SaveGame();
            return true;
        }

        public void StartQuestPanelTestQuests()
        {
            foreach (var questId in QuestPanelTestQuestIds)
                StartQuest(questId);
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
            GameManager.Instance?.SaveGame();
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
            if (trackedQuestId == questId)
                trackedQuestId = GetPrimaryActiveQuestId();

            if (!string.IsNullOrEmpty(quest.rewardItemId))
                RewardGrantService.Grant(quest.rewardItemId, quest.rewardItemCount, quest.questTitle);

            if (!string.IsNullOrEmpty(quest.endingId))
                currentEndingId = quest.endingId;

            GameEvents.RaiseQuestUpdated(questId);
            GameManager.Instance?.SaveGame();
        }

        public void SetBranch(string branchId)
        {
            if (!string.IsNullOrEmpty(branchId))
            {
                currentBranch = branchId;
                GameManager.Instance?.SaveGame();
            }
        }

        public QuestDefinition GetQuest(string questId) =>
            lookup.TryGetValue(questId, out var quest) ? quest : null;

        public void TrackQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId) || !activeQuests.Contains(questId))
                return;

            trackedQuestId = questId;
            GameEvents.RaiseQuestUpdated(questId);
            GameManager.Instance?.SaveGame();
        }

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
            Dictionary<string, int> stepProgress = null,
            string trackedQuest = "")
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
            trackedQuestId = !string.IsNullOrEmpty(trackedQuest) && activeQuests.Contains(trackedQuest)
                ? trackedQuest
                : GetPrimaryActiveQuestId();
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
            if (!string.IsNullOrEmpty(trackedQuestId) && activeQuests.Contains(trackedQuestId))
                return trackedQuestId;

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

        void EnsureQuestPanelTestQuests()
        {
            RegisterQuest(CreateRuntimeQuest(
                "story_arc_01",
                "Echoes of the First Gate",
                QuestType.Main,
                "Follow the signal coming from the old gate.",
                QuestStepType.Reach,
                "first_gate",
                "Reach the first gate and inspect the broken anchor.",
                "gold_coin",
                120));
            RegisterQuest(CreateRuntimeQuest(
                "story_arc_02",
                "A Name in the Ashes",
                QuestType.Main,
                "Ask the archive keeper about the burned crest.",
                QuestStepType.Talk,
                "archive_keeper",
                "Talk to the archive keeper near the central plaza.",
                "upgrade_crystal",
                2));
            RegisterQuest(CreateRuntimeQuest(
                "commission_delivery_01",
                "Commission: Field Supplies",
                QuestType.Side,
                "Deliver emergency supplies to the outer camp.",
                QuestStepType.Reach,
                "outer_camp",
                "Bring the supply crate to the outer camp marker.",
                "gold_coin",
                80));
            RegisterQuest(CreateRuntimeQuest(
                "commission_hunt_01",
                "Commission: Hostile Patrol",
                QuestType.Side,
                "Clear a small hostile patrol near the ridge.",
                QuestStepType.Defeat,
                "ridge_patrol",
                "Defeat the hostile patrol blocking the ridge road.",
                "upgrade_crystal",
                1));
            RegisterQuest(CreateRuntimeQuest(
                "commission_scout_01",
                "Commission: Scout Report",
                QuestType.Side,
                "Check the marked overlook and report the route condition.",
                QuestStepType.Reach,
                "overlook_marker",
                "Reach the overlook and verify the route condition.",
                "gold_coin",
                65));
            RegisterQuest(CreateRuntimeQuest(
                "world_ruins_01",
                "Silent Ruins",
                QuestType.Side,
                "Investigate the inactive ruins beyond the lake.",
                QuestStepType.Reach,
                "silent_ruins",
                "Enter the ruins and search for the inactive device.",
                "artifact_shard",
                1));
            RegisterQuest(CreateRuntimeQuest(
                "world_gather_01",
                "Bright Herb Cache",
                QuestType.Side,
                "Collect bright herbs for field medicine.",
                QuestStepType.Collect,
                "bright_herb",
                "Collect three bright herbs from the river path.",
                "gold_coin",
                50,
                3));
            RegisterQuest(CreateRuntimeQuest(
                "world_trial_01",
                "Trial of the Wind Step",
                QuestType.Side,
                "Reach the old platform before the marker fades.",
                QuestStepType.Reach,
                "wind_step_platform",
                "Reach the wind step platform and activate the trial sigil.",
                "upgrade_crystal",
                1));
            RegisterQuest(CreateRuntimeQuest(
                "world_lost_relic_01",
                "The Lost Relic",
                QuestType.Side,
                "Recover a relic reported near the cliff shrine.",
                QuestStepType.Collect,
                "lost_relic",
                "Find the lost relic near the cliff shrine.",
                "artifact_shard",
                1));
        }

        static QuestDefinition CreateRuntimeQuest(
            string questId,
            string title,
            QuestType type,
            string summary,
            QuestStepType stepType,
            string targetId,
            string stepDescription,
            string rewardItemId,
            int rewardCount,
            int requiredCount = 1)
        {
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.name = questId;
            quest.questId = questId;
            quest.questTitle = title;
            quest.questType = type;
            quest.summary = summary;
            quest.rewardItemId = rewardItemId;
            quest.rewardItemCount = rewardCount;
            quest.steps = new List<QuestStep>
            {
                new QuestStep
                {
                    stepId = $"{questId}_step_01",
                    stepType = stepType,
                    targetId = targetId,
                    requiredCount = requiredCount,
                    description = stepDescription
                }
            };
            return quest;
        }

        public string GetActiveQuestTitle()
        {
            var questId = GetPrimaryActiveQuestId();
            var quest = GetQuest(questId);
            return quest != null ? quest.questTitle : "—";
        }
    }
}
