using System.Collections.Generic;
using BES.Core;
using BES.Gameplay;
using UnityEngine;

namespace BES.Narrative
{
    public class QuestObjectiveTracker : MonoBehaviour
    {
        void OnEnable()
        {
            GameEvents.OnNpcTalked += HandleNpcTalked;
            GameEvents.OnRegionEntered += HandleRegionEntered;
            GameEvents.OnEnemyDefeated += HandleEnemyDefeated;
            GameEvents.OnCollectiblePickedUp += HandleCollectiblePickedUp;
            GameEvents.OnDialogueEnded += HandleDialogueEnded;
        }

        void OnDisable()
        {
            GameEvents.OnNpcTalked -= HandleNpcTalked;
            GameEvents.OnRegionEntered -= HandleRegionEntered;
            GameEvents.OnEnemyDefeated -= HandleEnemyDefeated;
            GameEvents.OnCollectiblePickedUp -= HandleCollectiblePickedUp;
            GameEvents.OnDialogueEnded -= HandleDialogueEnded;
        }

        QuestManager Quests => GameManager.Instance?.Quests;

        void HandleNpcTalked(string npcId) =>
            TryAdvanceAllActive(QuestStepType.Talk, npcId);

        void HandleRegionEntered(string regionId) =>
            TryAdvanceAllActive(QuestStepType.Reach, regionId);

        void HandleEnemyDefeated(string enemyId) =>
            TryAdvanceAllActive(QuestStepType.Defeat, enemyId);

        void HandleCollectiblePickedUp(string itemId) =>
            TryAdvanceAllActive(QuestStepType.Collect, itemId);

        void HandleDialogueEnded()
        {
            if (DialogueSystem.Instance == null)
                return;

            var lastSpeaker = DialogueSystem.Instance.LastSpeakerId;
            if (!string.IsNullOrEmpty(lastSpeaker))
                HandleNpcTalked(lastSpeaker);
        }

        void TryAdvanceAllActive(QuestStepType stepType, string targetId)
        {
            var quests = Quests;
            if (quests == null)
                return;

            // Snapshot — CompleteQuest có thể Remove khỏi activeQuests trong lúc duyệt.
            var activeIds = new List<string>(quests.ActiveQuests);
            foreach (var questId in activeIds)
                quests.TryAdvanceCurrentStep(questId, stepType, targetId);
        }
    }
}
