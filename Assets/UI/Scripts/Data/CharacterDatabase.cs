using System;
using System.Collections.Generic;
using UnityEngine;

namespace BES.UI
{
    [Serializable]
    public class CharacterDefinition
    {
        public string characterId;
        public string displayName;
        public int rarity = 4;
        public int level = 1;
        public int maxLevel = 100;
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
    }

    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "BES/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        public List<CharacterDefinition> characters = new();
        public List<string> defaultPartyIds = new() { "hero_01", "hero_02", "hero_03", "hero_04" };

        public IReadOnlyList<CharacterDefinition> Characters => characters;

        public CharacterDefinition Get(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) || characters == null)
                return null;

            foreach (var character in characters)
            {
                if (character != null && character.characterId == characterId)
                    return character;
            }

            return null;
        }

        public string GetDisplayName(string characterId)
        {
            var character = Get(characterId);
            return !string.IsNullOrEmpty(character?.displayName) ? character.displayName : characterId;
        }

        public IReadOnlyList<string> GetDefaultPartyIds() => defaultPartyIds;

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
