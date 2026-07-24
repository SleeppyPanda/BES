using UnityEngine;

namespace BES.Gameplay
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "BES/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public int maxStack = 99;
        public ItemType itemType = ItemType.Material;
        public int rarity = 1;
        public float healAmount;
        public float manaRestore;
        public string linkedWeaponId;
    }

    public enum ItemType
    {
        Material,
        Consumable,
        Weapon,
        Quest
    }
}
