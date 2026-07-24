using System.Collections.Generic;
using BES.Core;
using UnityEngine;

namespace BES.Narrative
{
    public class RelationshipSystem : MonoBehaviour
    {
        readonly Dictionary<string, int> affinity = new();

        public int GetAffinity(string npcId) =>
            affinity.TryGetValue(npcId, out var value) ? value : 0;

        public void AdjustAffinity(string npcId, int delta)
        {
            var current = GetAffinity(npcId);
            var next = Mathf.Clamp(current + delta, -100, 100);
            affinity[npcId] = next;
            GameEvents.RaiseRelationshipChanged(npcId, next);
        }

        public string GetDisposition(string npcId)
        {
            var value = GetAffinity(npcId);
            if (value >= 50) return "Trusted";
            if (value >= 20) return "Friendly";
            if (value <= -50) return "Hostile";
            if (value <= -20) return "Cold";
            return "Neutral";
        }

        public void ResetAll() => affinity.Clear();

        public Dictionary<string, int> ExportState() => new(affinity);

        public void ImportState(Dictionary<string, int> state)
        {
            affinity.Clear();
            if (state == null)
                return;

            foreach (var pair in state)
                affinity[pair.Key] = pair.Value;
        }
    }
}
