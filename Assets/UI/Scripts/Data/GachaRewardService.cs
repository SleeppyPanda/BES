using BES.Core;
using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    public static class GachaRewardService
    {
        public static string ApplyReward(GachaDropEntry entry)
        {
            if (entry == null)
                return "Nothing";

            var label = string.IsNullOrEmpty(entry.displayLabel)
                ? entry.rewardId
                : entry.displayLabel;
            if (entry.rewardType == GachaRewardType.Character)
            {
                var definition = CharacterDatabaseLoader.Load()?.Get(entry.rewardId);
                if (!string.IsNullOrEmpty(definition?.displayName))
                    label = definition.displayName;
            }

            var duplicate = false;
            var duplicateCharacter = false;
            switch (entry.rewardType)
            {
                case GachaRewardType.Weapon:
                    if (EquippedWeaponState.Instance != null && EquippedWeaponState.Instance.OwnsWeapon(entry.rewardId))
                    {
                        duplicate = true;
                        if (EquippedWeaponState.Instance.Refinement < 5)
                            EquippedWeaponState.Instance.EnhanceRefinement(1);
                    }
                    else
                        EquippedWeaponState.Instance?.UnlockWeapon(entry.rewardId);
                    GameManager.Instance?.Inventory.AddItem(entry.rewardId, entry.itemAmount);
                    break;
                case GachaRewardType.Character:
                    duplicateCharacter = CharacterOwnership.Owns(entry.rewardId);
                    duplicate = duplicateCharacter;
                    CharacterOwnership.Grant(entry.rewardId, label);
                    if (duplicateCharacter)
                    {
                        var amount = CharacterDatabaseLoader.Load()?.Get(entry.rewardId)?.duplicateShardReward ?? 1;
                        CharacterProgressionState.AddDuplicateShards(entry.rewardId, amount);
                    }
                    break;
                case GachaRewardType.Item:
                    GameManager.Instance?.Inventory.AddItem(entry.rewardId, entry.itemAmount);
                    break;
            }

            if (duplicate)
            {
                if (!duplicateCharacter)
                {
                    GachaPityState.Instance?.AddStardust(GachaPityState.DuplicateShardReward);
                    label += " (duplicate → Stardust)";
                }
                else
                    label = $"{label} (duplicate character shard)";
            }

            GachaPityState.Instance?.RegisterPull(entry.rarity, entry.rewardType == GachaRewardType.Weapon);
            GameManager.Instance?.SaveGame();
            return $"{entry.rarity}★ {label}";
        }
    }
}
