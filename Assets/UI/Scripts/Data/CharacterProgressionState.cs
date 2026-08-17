using System.Collections.Generic;
using BES.Core;
using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    public static class CharacterProgressionState
    {
        public const int AbsoluteMaxLevel = 80;
        public const int ConstellationCount = 6;

        static readonly Dictionary<string, int> levels = new();
        static readonly Dictionary<string, int> experience = new();
        static readonly Dictionary<string, int> breakthroughs = new();
        static readonly Dictionary<string, int> constellations = new();
        static readonly Dictionary<string, int> constellationShards = new();

        public static int GetLevel(string characterId) => Get(levels, characterId, DefaultLevel(characterId));
        public static int GetExperience(string characterId) => Get(experience, characterId, 0);
        public static int GetBreakthroughCount(string characterId) => Mathf.Clamp(Get(breakthroughs, characterId, 0), 0, 3);
        public static int GetLevelCap(string characterId) => Mathf.Min(AbsoluteMaxLevel, 20 + GetBreakthroughCount(characterId) * 20);
        public static int GetConstellation(string characterId) => Mathf.Clamp(Get(constellations, characterId, 0), 0, ConstellationCount);
        public static int GetConstellationShards(string characterId) => Mathf.Max(0, Get(constellationShards, characterId, 0));

        public static int GetExperienceToNextLevel(string characterId)
        {
            var level = GetLevel(characterId);
            return CharacterDatabaseLoader.Load()?.GetExperienceToNextLevel(level) ?? (level < AbsoluteMaxLevel ? 100 + (level - 1) * 25 : 0);
        }

        public static int AddExperience(string characterId, int amount)
        {
            if (string.IsNullOrEmpty(characterId) || amount <= 0) return 0;
            var level = GetLevel(characterId);
            var exp = GetExperience(characterId) + amount;
            var cap = GetLevelCap(characterId);
            while (level < cap && level < AbsoluteMaxLevel)
            {
                var required = CharacterDatabaseLoader.Load()?.GetExperienceToNextLevel(level) ?? 100 + (level - 1) * 25;
                if (exp < required) break;
                exp -= required;
                level++;
            }
            levels[characterId] = level;
            experience[characterId] = level >= cap ? Mathf.Min(exp, Mathf.Max(0, GetExperienceToNextLevel(characterId) - 1)) : exp;
            GameEvents.RaisePartyChanged();
            return level;
        }

        public static bool TryBreakthrough(string characterId)
        {
            var cap = GetLevelCap(characterId);
            if (cap >= AbsoluteMaxLevel || GetLevel(characterId) < cap) return false;
            var tier = CharacterDatabaseLoader.Load()?.GetBreakthroughTier(cap);
            if (tier == null) return false;
            var inventory = GameManager.Instance?.Inventory;
            if (tier.materialAmount > 0 && (inventory == null || inventory.GetCount(tier.materialId) < tier.materialAmount)) return false;
            if (tier.materialAmount > 0 && !inventory.RemoveItem(tier.materialId, tier.materialAmount)) return false;
            breakthroughs[characterId] = GetBreakthroughCount(characterId) + 1;
            GameEvents.RaisePartyChanged();
            GameManager.Instance?.SaveGame();
            return true;
        }

        public static int AddDuplicateShards(string characterId, int amount = 0)
        {
            if (string.IsNullOrEmpty(characterId)) return 0;
            var definition = CharacterDatabaseLoader.Load()?.Get(characterId);
            if (amount <= 0) amount = Mathf.Max(1, definition?.duplicateShardReward ?? 1);
            var total = GetConstellationShards(characterId) + amount;
            constellationShards[characterId] = total;
            GameEvents.RaisePartyChanged();
            return total;
        }

        public static bool TryUnlockNextConstellation(string characterId)
        {
            var current = GetConstellation(characterId);
            if (current >= ConstellationCount) return false;
            var definition = CharacterDatabaseLoader.Load()?.Get(characterId);
            var cost = definition?.constellationShardCosts != null && current < definition.constellationShardCosts.Count
                ? Mathf.Max(1, definition.constellationShardCosts[current]) : 1;
            var shards = GetConstellationShards(characterId);
            if (shards < cost) return false;
            constellationShards[characterId] = shards - cost;
            constellations[characterId] = current + 1;
            GameEvents.RaisePartyChanged();
            GameManager.Instance?.SaveGame();
            return true;
        }

        public static bool IsSkillUnlocked(string characterId, CharacterSkillUnlock skill) =>
            skill != null && GetConstellation(characterId) >= skill.requiredConstellation;

        public static CharacterSkillUnlock GetActiveSkill(string characterId, int slot)
        {
            var definition = CharacterDatabaseLoader.Load()?.Get(characterId);
            CharacterSkillUnlock best = null;
            var hasConfiguredSlot = false;
            if (definition?.skillUnlocks != null)
                foreach (var skill in definition.skillUnlocks)
                {
                    if (skill == null || skill.skillType != CharacterSkillType.Active || skill.activeSlot != slot) continue;
                    hasConfiguredSlot = true;
                    if (IsSkillUnlocked(characterId, skill) && (best == null || skill.requiredConstellation > best.requiredConstellation))
                        best = skill;
                }
            if (hasConfiguredSlot) return best;
            var fallbackId = slot == 0 ? definition?.skill1Id : definition?.skill2Id;
            return string.IsNullOrEmpty(fallbackId) ? null : new CharacterSkillUnlock { skillId = fallbackId, skillType = CharacterSkillType.Active, activeSlot = slot };
        }

        public static IEnumerable<CharacterSkillUnlock> GetActivePassives(string characterId, float healthRatio)
        {
            var definition = CharacterDatabaseLoader.Load()?.Get(characterId);
            if (definition?.skillUnlocks == null) yield break;
            foreach (var skill in definition.skillUnlocks)
            {
                if (skill == null || skill.skillType != CharacterSkillType.Passive || !IsSkillUnlocked(characterId, skill)) continue;
                var active = skill.passiveCondition switch
                {
                    PassiveSkillCondition.HealthBelowPercent => healthRatio <= skill.healthThreshold,
                    PassiveSkillCondition.HealthAbovePercent => healthRatio >= skill.healthThreshold,
                    _ => true
                };
                if (active) yield return skill;
            }
        }

        public static void ResetAll()
        {
            levels.Clear(); experience.Clear(); breakthroughs.Clear(); constellations.Clear(); constellationShards.Clear();
        }

        public static void ExportToSave(SaveData data)
        {
            data.characterLevels = SaveDataUtility.ToPairs(levels);
            data.characterExperience = SaveDataUtility.ToPairs(experience);
            data.characterBreakthroughs = SaveDataUtility.ToPairs(breakthroughs);
            data.characterConstellations = SaveDataUtility.ToPairs(constellations);
            data.characterConstellationShards = SaveDataUtility.ToPairs(constellationShards);
        }

        public static void ImportFromSave(SaveData data)
        {
            Replace(levels, data?.characterLevels); Replace(experience, data?.characterExperience);
            Replace(breakthroughs, data?.characterBreakthroughs); Replace(constellations, data?.characterConstellations);
            Replace(constellationShards, data?.characterConstellationShards);
        }

        static int DefaultLevel(string id) => Mathf.Clamp(CharacterDatabaseLoader.Load()?.Get(id)?.level ?? 1, 1, AbsoluteMaxLevel);
        static int Get(Dictionary<string, int> source, string key, int fallback) => !string.IsNullOrEmpty(key) && source.TryGetValue(key, out var value) ? value : fallback;
        static void Replace(Dictionary<string, int> target, List<StringIntPair> source)
        {
            target.Clear();
            foreach (var pair in SaveDataUtility.FromPairs(source)) target[pair.Key] = pair.Value;
        }
    }
}
