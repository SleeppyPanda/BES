using System.Collections.Generic;
using BES.Core;
using BES.UI;
using UnityEngine;

namespace BES.Gameplay
{
    public class InventorySystem : MonoBehaviour
    {
        [SerializeField] ItemDatabase itemDatabase;
        readonly Dictionary<string, int> items = new();

        public IReadOnlyDictionary<string, int> Items => items;

        public bool AddItem(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
                return false;

            var def = itemDatabase != null ? itemDatabase.Get(itemId) : null;
            var maxStack = def != null ? def.maxStack : 99;

            items.TryGetValue(itemId, out var current);
            var newAmount = Mathf.Min(maxStack, current + amount);
            if (newAmount == current)
                return false;

            items[itemId] = newAmount;
            GameManager.Instance?.SaveGame();
            return true;
        }

        public bool RemoveItem(string itemId, int amount = 1)
        {
            if (!items.TryGetValue(itemId, out var current) || current < amount)
                return false;

            current -= amount;
            if (current <= 0)
                items.Remove(itemId);
            else
                items[itemId] = current;

            GameManager.Instance?.SaveGame();
            return true;
        }

        public int GetCount(string itemId) =>
            items.TryGetValue(itemId, out var count) ? count : 0;

        public void Clear() => items.Clear();

        public Dictionary<string, int> ExportState() => new(items);

        public void ImportState(Dictionary<string, int> state)
        {
            items.Clear();
            if (state == null)
                return;

            foreach (var pair in state)
                items[pair.Key] = pair.Value;
        }

        public ItemDefinition GetDefinition(string itemId) =>
            itemDatabase != null ? itemDatabase.Get(itemId) : null;

        public bool TryUseItem(string itemId)
        {
            var def = GetDefinition(itemId);
            if (def == null || (def.itemType != ItemType.Consumable && def.itemType != ItemType.Quest))
                return false;

            var focused = BES.UI.CharacterOwnership.FocusedCharacterId;
            if (!string.IsNullOrEmpty(focused) &&
                (def.characterExperience > 0 || def.affinityGain != 0 || !string.IsNullOrEmpty(def.linkedCharacterId) ||
                 itemId.Contains("exp")))
                return BES.UI.CharacterOwnership.TryUseInventoryOnCharacter(itemId, focused);

            if (def.itemType != ItemType.Consumable)
                return false;

            if (!RemoveItem(itemId, 1))
                return false;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return true;

            if (player.TryGetComponent<PlayerStats>(out var stats))
            {
                if (def.healAmount > 0f)
                    stats.Heal(def.healAmount);
                if (def.manaRestore > 0f)
                    stats.RestoreMana(def.manaRestore);
            }

            return true;
        }

        public bool TryEquipWeaponItem(string itemId)
        {
            var def = GetDefinition(itemId);
            if (def == null || def.itemType != ItemType.Weapon)
                return false;

            var weaponId = string.IsNullOrEmpty(def.linkedWeaponId) ? itemId : def.linkedWeaponId;
            if (EquippedWeaponState.Instance == null)
                return false;

            EquippedWeaponState.Instance.UnlockWeapon(weaponId);
            EquippedWeaponState.Instance.Equip(weaponId);
            GameManager.Instance?.SaveGame();
            return true;
        }

        public void SetDatabase(ItemDatabase database) => itemDatabase = database;
    }
}
