using System;
using System.Collections.Generic;
using UnityEngine;
using BES.UI.Menu;

namespace BES.UI
{
    public enum CharacterSkillType { Active, Passive }
    public enum PassiveSkillCondition { Always, HealthBelowPercent, HealthAbovePercent }

    [Serializable]
    public class CharacterSkillUnlock
    {
        public string skillId;
        public CharacterSkillType skillType;
        [Range(0, 1)] public int activeSlot;
        [Range(0, 6)] public int requiredConstellation;
        public PassiveSkillCondition passiveCondition;
        [Range(0f, 1f)] public float healthThreshold = .5f;
        [Min(0f)] public float attackMultiplier = 1f;
        [Min(0f)] public float defenseMultiplier = 1f;
        [Min(0f)] public float healthMultiplier = 1f;
    }

    [Serializable]
    public class CharacterBreakthroughTier
    {
        [Range(20, 60)] public int levelCap = 20;
        public string materialId;
        [Min(0)] public int materialAmount;
    }

    [Serializable]
    public class CharacterDefinition
    {
        public string characterId;
        public string displayName;
        public int rarity = 4;
        public int level = 1;
        [HideInInspector] public int maxLevel = 80;
        public float baseAttack = 15f;
        public float baseHealth = 100f;
        public float baseDefense = 5f;
        public float baseMana = 100f;
        public float critRate = 0.1f;
        public float critDamage = 1.5f;
        public Sprite portrait;
        public GameObject gameplayPrefab;
        public Color testVisualColor = Color.white;
        public Vector3 testVisualScale = Vector3.one;
        [Header("Mouse Attacks")]
        public string leftClickAttackId;
        public string rightClickAttackId;
        [Header("Skills")]
        public string skill1Id;
        public string skill2Id;
        public Sprite skill1Icon;
        public Sprite skill2Icon;
        [Header("Constellation progression")]
        [Min(1)] public int duplicateShardReward = 1;
        public List<int> constellationShardCosts = new() { 1, 1, 1, 1, 1, 1 };
        public List<CharacterSkillUnlock> skillUnlocks = new();
    }

    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "BES/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        public List<CharacterDefinition> characters = new();
        public List<string> defaultPartyIds = new() { "hero_01", "hero_02", "hero_03", "hero_04" };
        [Header("Shared character progression")]
        [Tooltip("EXP required to advance from level N to N+1. Index 0 is level 1. Empty entries use the fallback curve until final values are supplied.")]
        public List<int> sharedExperienceToNextLevel = new();
        [Tooltip("Shared breakthrough requirements at level caps 20, 40 and 60.")]
        public List<CharacterBreakthroughTier> breakthroughTiers = new();

        public IReadOnlyList<CharacterDefinition> Characters => characters;

        public CharacterDefinition GetRaw(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) || characters == null)
                return null;

            foreach (var character in characters)
            {
                if (character != null && string.Equals(character.characterId, characterId, StringComparison.OrdinalIgnoreCase))
                    return character;
            }

            return null;
        }

        public CharacterDefinition Get(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) || characters == null)
                return null;

            var combatId = CharacterIdentity.CombatId(characterId);
            return GetRaw(combatId) ?? GetRaw(characterId) ?? GetRaw(CharacterIdentity.Canonical(characterId));
        }

        public CharacterDefinition GetByDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName) || characters == null)
                return null;

            foreach (var character in characters)
            {
                if (character != null && string.Equals(character.displayName, displayName, StringComparison.OrdinalIgnoreCase))
                    return character;
            }

            return null;
        }

        public string GetDisplayName(string characterId)
        {
            var character = Get(characterId);
            if (!string.IsNullOrEmpty(character?.displayName))
                return character.displayName;
            var menu = CharacterIdentity.FindEntry(null, characterId);
            return !string.IsNullOrEmpty(menu?.displayName) ? menu.displayName : characterId;
        }

        public IReadOnlyList<string> GetDefaultPartyIds() => defaultPartyIds;

        public int GetExperienceToNextLevel(int level)
        {
            return CharacterProgressionState.GetExperienceToNextLevelForLevel(level);
        }

        public CharacterBreakthroughTier GetBreakthroughTier(int currentCap)
        {
            if (breakthroughTiers != null)
                foreach (var tier in breakthroughTiers)
                    if (tier != null && tier.levelCap == currentCap) return tier;

            var amount = currentCap switch
            {
                20 => 5,
                40 => 15,
                60 => 25,
                _ => currentCap / 20
            };

            return new CharacterBreakthroughTier
            {
                levelCap = currentCap,
                materialId = $"character_breakthrough_{currentCap}",
                materialAmount = amount
            };
        }

        public void ResetToDefaultEntries()
        {
            characters = new List<CharacterDefinition>
            {
                new()
                {
                    characterId = "hero_01",
                    displayName = "Đau hơn NYC bạn",
                    rarity = 5,
                    baseAttack = 15f,
                    baseHealth = 100f,
                    baseDefense = 5f,
                    baseMana = 100f,
                    critRate = 0.1f,
                    critDamage = 1.5f,
                    testVisualColor = new Color(0.35f, 0.75f, 1f, 1f),
                    testVisualScale = new Vector3(1f, 1f, 1f),
                    leftClickAttackId = "attack_void_edge_left",
                    rightClickAttackId = "attack_void_burst_right",
                    skill1Id = "skill_void_slash",
                    skill2Id = "skill_guard_break"
                },
                new()
                {
                    characterId = "hero_02",
                    displayName = "Mất cô ấy rồi",
                    rarity = 4,
                    baseAttack = 18f,
                    baseHealth = 90f,
                    baseDefense = 6f,
                    baseMana = 100f,
                    critRate = 0.12f,
                    critDamage = 1.6f,
                    testVisualColor = new Color(1f, 0.45f, 0.32f, 1f),
                    testVisualScale = new Vector3(0.92f, 1.05f, 0.92f),
                    leftClickAttackId = "attack_flare_cuts_left",
                    rightClickAttackId = "attack_flare_lunge_right",
                    skill1Id = "skill_quick_cut",
                    skill2Id = "skill_flare_dash"
                },
                new()
                {
                    characterId = "hero_03",
                    displayName = "Anh là thằng tồi",
                    rarity = 4,
                    baseAttack = 14f,
                    baseHealth = 110f,
                    baseDefense = 8f,
                    baseMana = 110f,
                    critRate = 0.08f,
                    critDamage = 1.4f,
                    testVisualColor = new Color(0.45f, 1f, 0.58f, 1f),
                    testVisualScale = new Vector3(1.08f, 1.12f, 1.08f),
                    leftClickAttackId = "attack_guard_sweep_left",
                    rightClickAttackId = "attack_earth_slam_right",
                    skill1Id = "skill_shield_wave",
                    skill2Id = "skill_ground_lock"
                },
                new()
                {
                    characterId = "hero_04",
                    displayName = "Nhìn em bên ai khác",
                    rarity = 4,
                    baseAttack = 16f,
                    baseHealth = 95f,
                    baseDefense = 5f,
                    baseMana = 120f,
                    critRate = 0.15f,
                    critDamage = 1.7f,
                    testVisualColor = new Color(0.92f, 0.55f, 1f, 1f),
                    testVisualScale = new Vector3(0.86f, 1f, 0.86f),
                    leftClickAttackId = "attack_arc_shot_left",
                    rightClickAttackId = "attack_marked_burst_right",
                    skill1Id = "skill_arc_bolt",
                    skill2Id = "skill_focus_shot"
                },
                new()
                {
                    characterId = "char_limited_01",
                    displayName = "Limited Hero",
                    rarity = 5,
                    baseAttack = 22f,
                    baseHealth = 100f,
                    baseDefense = 6f,
                    baseMana = 130f,
                    critRate = 0.18f,
                    critDamage = 1.8f,
                    testVisualColor = new Color(1f, 0.9f, 0.25f, 1f),
                    testVisualScale = new Vector3(1.05f, 1.1f, 1.05f),
                    leftClickAttackId = "attack_star_edge_left",
                    rightClickAttackId = "attack_lunar_cleave_right",
                    skill1Id = "skill_starfall",
                    skill2Id = "skill_lunar_drive"
                },
                new()
                {
                    characterId = "hero_05",
                    displayName = "Starbound Rookie",
                    rarity = 3,
                    baseAttack = 12f,
                    baseHealth = 88f,
                    baseDefense = 4f,
                    baseMana = 90f,
                    critRate = 0.08f,
                    critDamage = 1.35f,
                    testVisualColor = new Color(0.52f, 0.68f, 1f, 1f),
                    testVisualScale = new Vector3(0.9f, 0.95f, 0.9f),
                    leftClickAttackId = "attack_spark_jab_left",
                    rightClickAttackId = "attack_rookie_blast_right",
                    skill1Id = "skill_spark_step",
                    skill2Id = "skill_comet_burst"
                }
            };

            defaultPartyIds = new List<string> { "hero_01", "hero_02", "hero_03", "hero_04" };
        }

        public static CharacterDatabase CreateRuntimeDefault()
        {
            var database = CreateInstance<CharacterDatabase>();
            database.ResetToDefaultEntries();
            return database;
        }
    }
}
