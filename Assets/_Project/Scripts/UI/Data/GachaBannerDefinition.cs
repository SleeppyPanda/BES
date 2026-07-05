using System;
using System.Collections.Generic;
using UnityEngine;

namespace BES.UI
{
    public enum GachaRewardType
    {
        Weapon,
        Character,
        Item
    }

    [Serializable]
    public class GachaDropEntry
    {
        public string entryId;
        public GachaRewardType rewardType = GachaRewardType.Weapon;
        public string rewardId;
        public int itemAmount = 1;
        public int rarity = 3;
        public int weight = 100;
        public string displayLabel;
    }

    [CreateAssetMenu(fileName = "GachaBannerDefinition", menuName = "BES/Gacha Banner")]
    public class GachaBannerDefinition : ScriptableObject
    {
        public string bannerId;
        public string displayName;
        [TextArea] public string description;
        public int singleCostGems = 160;
        public int tenPullCostGems = 1600;
        public List<GachaDropEntry> drops = new();

        public GachaDropEntry Roll(System.Random rng)
        {
            if (drops == null || drops.Count == 0)
                return null;

            var total = 0;
            foreach (var drop in drops)
                total += Mathf.Max(1, drop.weight);

            var roll = rng.Next(0, total);
            var acc = 0;
            foreach (var drop in drops)
            {
                acc += Mathf.Max(1, drop.weight);
                if (roll < acc)
                    return drop;
            }

            return drops[drops.Count - 1];
        }
    }
}
