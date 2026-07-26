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

        public void ResetToDefaultEntries()
        {
            items.Clear();
            items.Add(CreateRuntimeItem("herb_common", "Common Herb", ItemType.Material, 1, "A basic herb gathered around the island."));
            items.Add(CreateRuntimeItem("material_ore", "Ore", ItemType.Material, 2, "Ore used for weapon upgrades."));
            items.Add(CreateRuntimeItem("material_crystal", "Crystal", ItemType.Material, 2, "A small crystal with upgrade energy."));
            items.Add(CreateRuntimeItem("potion_heal", "Healing Potion", ItemType.Consumable, 2, "Restores HP.", healAmount: 30f));
            items.Add(CreateRuntimeItem("relic_shard", "Relic Shard", ItemType.Quest, 3, "A quest relic fragment.", maxStack: 1));
            items.Add(CreateRuntimeItem("weapon_iron_sword", "Iron Sword", ItemType.Weapon, 3, "A reliable starter sword.", linkedWeaponId: "weapon_iron_sword", maxStack: 1));
            items.Add(CreateRuntimeItem("weapon_void_edge", "Void Edge", ItemType.Weapon, 4, "A blade for testing stronger weapon rewards.", linkedWeaponId: "weapon_void_edge", maxStack: 1));
            RebuildLookup();
        }

        public static ItemDatabase CreateDefaultRuntime()
        {
            var database = CreateInstance<ItemDatabase>();
            database.ResetToDefaultEntries();
            return database;
        }

        static ItemDefinition CreateRuntimeItem(
            string id,
            string displayName,
            ItemType type,
            int rarity,
            string description,
            float healAmount = 0f,
            string linkedWeaponId = "",
            int maxStack = 99)
        {
            var item = CreateInstance<ItemDefinition>();
            item.itemId = id;
            item.displayName = displayName;
            item.description = description;
            item.itemType = type;
            item.rarity = rarity;
            item.healAmount = healAmount;
            item.linkedWeaponId = linkedWeaponId;
            item.maxStack = maxStack;
            return item;
        }
    }
}
