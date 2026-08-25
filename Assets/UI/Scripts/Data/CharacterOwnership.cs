using System.Collections.Generic;
using BES.Core;
using BES.Gameplay;
using BES.UI.Menu;
using UnityEngine;

namespace BES.UI
{
    public static class CharacterOwnership
    {
        public static string FocusedCharacterId { get; private set; }

        public static void Focus(string characterId)
        {
            var canonical = CharacterIdentity.Canonical(characterId);
            if (string.IsNullOrEmpty(canonical) || !Owns(canonical))
                return;
            FocusedCharacterId = canonical;
        }

        public static bool Owns(string characterId)
        {
            var roster = ResolveRoster();
            return roster != null && roster.IsCharacterUnlocked(characterId);
        }

        public static void Grant(string characterId, string displayName = null)
        {
            var canonical = CharacterIdentity.Canonical(characterId);
            if (string.IsNullOrEmpty(canonical))
                return;

            var roster = ResolveRoster();
            var alreadyOwned = roster != null && roster.IsCharacterUnlocked(canonical);
            roster?.UnlockCharacter(canonical, displayName);
            SyncInventoryToken(canonical, true);
            if (string.IsNullOrEmpty(FocusedCharacterId) || !alreadyOwned)
                Focus(canonical);
        }

        public static IReadOnlyList<CharacterEntry> GetOwnedEntries(MenuContentDatabase menuDatabase)
        {
            var owned = new List<CharacterEntry>();
            var seen = new HashSet<string>();
            var roster = ResolveRoster();
            if (roster == null)
                return owned;

            foreach (var member in roster.GetUnlockedRosterMembers())
            {
                var entry = CharacterIdentity.FindEntry(menuDatabase, member?.characterId);
                if (entry == null || !seen.Add(entry.id))
                    continue;
                owned.Add(entry);
            }

            return owned;
        }

        public static string ResolveOwnedId(string requested, MenuContentDatabase menuDatabase)
        {
            var canonical = CharacterIdentity.Canonical(requested, menuDatabase);
            if (Owns(canonical) && CharacterIdentity.FindEntry(menuDatabase, canonical) != null)
                return CharacterIdentity.FindEntry(menuDatabase, canonical).id;

            var owned = GetOwnedEntries(menuDatabase);
            if (owned.Count > 0)
                return owned[0].id;

            return canonical;
        }

        public static string InventoryTokenId(string characterId)
        {
            var canonical = CharacterIdentity.Canonical(characterId);
            return string.IsNullOrEmpty(canonical) ? string.Empty : $"character_{canonical}";
        }

        public static void SyncInventoryToken(string characterId, bool owned)
        {
            var inventory = GameManager.Instance?.Inventory;
            var tokenId = InventoryTokenId(characterId);
            if (inventory == null || string.IsNullOrEmpty(tokenId) || inventory.GetDefinition(tokenId) == null)
                return;

            var count = inventory.GetCount(tokenId);
            if (owned && count <= 0)
                inventory.AddItem(tokenId, 1);
            else if (!owned && count > 0)
                inventory.RemoveItem(tokenId, count);
        }

        public static bool TryUseInventoryOnCharacter(string itemId, string characterId)
        {
            var inventory = GameManager.Instance?.Inventory;
            var canonical = CharacterIdentity.Canonical(characterId);
            if (inventory == null || string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(canonical) || !Owns(canonical))
                return false;

            var definition = inventory.GetDefinition(itemId);
            if (definition != null && definition.itemType == ItemType.Weapon)
                return inventory.TryEquipWeaponItem(itemId);

            var exp = definition != null && definition.characterExperience > 0
                ? definition.characterExperience
                : ExperienceForItem(itemId);
            var affinity = definition != null && definition.affinityGain != 0
                ? definition.affinityGain
                : definition != null && definition.itemType == ItemType.Quest ? 8 : 0;
            var linked = definition != null && !string.IsNullOrEmpty(definition.linkedCharacterId);
            if (exp <= 0 && affinity == 0 && !linked)
                return false;
            if (linked && !CharacterIdentity.Same(definition.linkedCharacterId, canonical))
                return false;

            if (!inventory.RemoveItem(itemId, 1))
                return false;

            if (exp > 0)
                CharacterProgressionState.AddExperience(canonical, exp);
            if (affinity != 0)
                CharacterProgressionState.AddAffinity(canonical, affinity);

            Focus(canonical);
            GameEvents.RaisePartyChanged();
            GameManager.Instance?.SaveGame();
            return true;
        }

        static int ExperienceForItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;
            if (itemId.Contains("exp_gold", System.StringComparison.OrdinalIgnoreCase)) return 5000;
            if (itemId.Contains("exp_blue", System.StringComparison.OrdinalIgnoreCase)) return 2000;
            if (itemId.Contains("exp_green", System.StringComparison.OrdinalIgnoreCase) ||
                itemId.Contains("character_exp", System.StringComparison.OrdinalIgnoreCase))
                return 500;
            return 0;
        }

        static PartyRoster ResolveRoster()
        {
            if (PartyRoster.Instance != null)
                return PartyRoster.Instance;

            var existing = Object.FindAnyObjectByType<PartyRoster>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var host = GameManager.Instance != null ? GameManager.Instance.gameObject : new GameObject("PartyRoster");
            return host.GetComponent<PartyRoster>() ?? host.AddComponent<PartyRoster>();
        }
    }
}
