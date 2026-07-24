using System.Collections.Generic;
using UnityEngine;

namespace BES.Narrative
{
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "BES/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        public List<QuestDefinition> quests = new();

        readonly Dictionary<string, QuestDefinition> lookup = new();

        public void RebuildLookup()
        {
            lookup.Clear();
            foreach (var quest in quests)
            {
                if (quest != null && !string.IsNullOrEmpty(quest.questId))
                    lookup[quest.questId] = quest;
            }
        }

        public QuestDefinition Get(string questId)
        {
            if (lookup.Count == 0)
                RebuildLookup();

            return lookup.TryGetValue(questId, out var quest) ? quest : null;
        }

        public IReadOnlyList<QuestDefinition> All => quests;
    }
}
