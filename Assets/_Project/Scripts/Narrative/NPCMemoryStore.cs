using System.Collections.Generic;
using UnityEngine;

namespace BES.Narrative
{
    public static class NPCMemoryStore
    {
        const int MaxMemoriesPerNpc = 5;
        static readonly Dictionary<string, List<string>> Memories = new();

        public static void AddMemory(string npcId, string fact)
        {
            if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(fact))
                return;

            if (!Memories.TryGetValue(npcId, out var list))
            {
                list = new List<string>();
                Memories[npcId] = list;
            }

            if (list.Contains(fact))
                return;

            list.Add(fact);
            while (list.Count > MaxMemoriesPerNpc)
                list.RemoveAt(0);
        }

        public static IReadOnlyList<string> GetMemories(string npcId) =>
            Memories.TryGetValue(npcId, out var list) ? list : System.Array.Empty<string>();

        public static Dictionary<string, List<string>> ExportAll() => new(Memories);

        public static void ImportAll(Dictionary<string, List<string>> data)
        {
            Memories.Clear();
            if (data == null)
                return;

            foreach (var pair in data)
                Memories[pair.Key] = new List<string>(pair.Value);
        }

        public static void ClearAll() => Memories.Clear();
    }
}
