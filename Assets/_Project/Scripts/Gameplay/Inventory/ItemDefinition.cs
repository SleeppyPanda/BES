using UnityEngine;

namespace BES.Gameplay
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "BES/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public int maxStack = 99;
        public ItemType itemType = ItemType.Material;
        public int rarity = 1;
        public float healAmount;
        public float manaRestore;
        public string linkedWeaponId;
        [Tooltip("If set, this item can only be used on that character (Wish/library/combat share the same id).")]
        public string linkedCharacterId;
        [Min(0)] public int characterExperience;
        public int affinityGain;
    }

    public enum ItemType
    {
        Material,
        Consumable,
        Weapon,
        Quest,
        Character
    }
}
