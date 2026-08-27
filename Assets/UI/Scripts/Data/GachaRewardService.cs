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

            var duplicateCharacter = entry.rewardType == GachaRewardType.Character && CharacterOwnership.Owns(entry.rewardId);
            RewardGrantService.Grant(entry.rewardId, Mathf.Max(1, entry.itemAmount), label);
            if (duplicateCharacter)
                label = $"{label} (duplicate character shard)";

            GachaPityState.Instance?.RegisterPull(entry.rarity, entry.rewardType == GachaRewardType.Weapon);
            GameManager.Instance?.SaveGame();
            return $"{entry.rarity}â˜… {label}";
        }
    }
}

