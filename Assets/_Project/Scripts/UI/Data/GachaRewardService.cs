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

            var duplicate = false;
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
                    if (PartyRoster.Instance != null && PartyRoster.Instance.IsCharacterUnlocked(entry.rewardId))
                        duplicate = true;
                    else
                        PartyRoster.Instance?.UnlockCharacter(entry.rewardId, label);
                    break;
                case GachaRewardType.Item:
                    GameManager.Instance?.Inventory.AddItem(entry.rewardId, entry.itemAmount);
                    break;
            }

            if (duplicate)
            {
                GachaPityState.Instance?.AddStardust(GachaPityState.DuplicateShardReward);
                label += " (duplicate → Stardust)";
            }

            GachaPityState.Instance?.RegisterPull(entry.rarity);
            GameManager.Instance?.SaveGame();
            return $"{entry.rarity}★ {label}";
        }
    }
}
