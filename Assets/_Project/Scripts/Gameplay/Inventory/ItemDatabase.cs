using System.Collections.Generic;
using UnityEngine;

namespace BES.Gameplay
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "BES/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemDefinition> items = new();

        readonly Dictionary<string, ItemDefinition> lookup = new();

        void OnEnable() => RebuildLookup();

        public void RebuildLookup()
        {
            lookup.Clear();
            foreach (var item in items)
            {
                if (item != null && !string.IsNullOrEmpty(item.itemId))
                    lookup[item.itemId] = item;
            }
        }

        public ItemDefinition Get(string itemId)
        {
            if (lookup.Count == 0)
                RebuildLookup();

            return lookup.TryGetValue(itemId, out var item) ? item : null;
        }
    }
}
