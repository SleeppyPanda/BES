using UnityEngine;

namespace BES.UI
{
    public enum ItemRarity
    {
        ThreeStar = 3,
        FourStar = 4,
        FiveStar = 5
    }

    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "BES/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public string weaponId;
        public string displayName;
        [TextArea] public string description;
        public int baseAtk = 100;
        public int baseHp;
        public ItemRarity rarity = ItemRarity.FourStar;
        public int maxLevel = 100;
    }
}
