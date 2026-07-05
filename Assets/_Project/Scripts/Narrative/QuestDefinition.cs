using System.Collections.Generic;
using UnityEngine;

namespace BES.Narrative
{
    public enum QuestType
    {
        Main,
        Side
    }

    public enum QuestStepType
    {
        Talk,
        Collect,
        Defeat,
        Reach,
        Choice
    }

    [System.Serializable]
    public class QuestStep
    {
        public string stepId;
        public QuestStepType stepType;
        public string targetId;
        public int requiredCount = 1;
        [TextArea] public string description;
    }

    [CreateAssetMenu(fileName = "QuestDefinition", menuName = "BES/Quest Definition")]
    public class QuestDefinition : ScriptableObject
    {
        public string questId;
        public string questTitle;
        public QuestType questType = QuestType.Main;
        [TextArea] public string summary;
        public List<QuestStep> steps = new();
        public string rewardItemId;
        public int rewardItemCount = 1;
        public string endingId;
    }
}
