using UnityEngine;
using System;
using System.Collections.Generic;

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
        public string subStatName = "Crit Rate";
        public float subStatValue = 5.0f;
        [Header("Random Stats")]
        [Tooltip("Always rolls this many unique random stat lines when this weapon is obtained.")]
        [Min(0)] public int randomStatLineCount = 4;
        public List<WeaponRandomStatRange> randomStatPool = new();
    }

    public enum WeaponStatType
    {
        AttackFlat,
        AttackPercent,
        HealthFlat,
        HealthPercent,
        DefenseFlat,
        DefensePercent,
        SpeedFlat,
        CritRatePercent,
        CritDamagePercent,
        ElementDamagePercent,
        HealingBonusPercent,
        ShieldBonusPercent
    }

    [Serializable]
    public class WeaponRandomStatRange
    {
        public WeaponStatType statType = WeaponStatType.AttackFlat;
        public string displayName;
        public bool isPercent;
        public float minValue = 1f;
        public float maxValue = 5f;
        [Min(1)] public int weight = 1;
    }

    [Serializable]
    public class WeaponRandomStatInstance
    {
        public WeaponStatType statType;
        public string displayName;
        public bool isPercent;
        public float value;
    }

    [Serializable]
    public class WeaponInstance
    {
        public string instanceId;
        public string weaponId;
        public int level = 1;
        public int experience;
        public int refinement = 1;
        public List<WeaponRandomStatInstance> randomStats = new();
    }
}
