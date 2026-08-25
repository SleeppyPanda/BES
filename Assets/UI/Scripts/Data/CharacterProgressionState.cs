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
        static readonly Dictionary<string, int> affinity = new();

        public static int GetLevel(string characterId) => Get(levels, characterId, DefaultLevel(characterId));
        public static int GetExperience(string characterId) => Get(experience, characterId, 0);
        public static int GetBreakthroughCount(string characterId) => Mathf.Clamp(Get(breakthroughs, characterId, 0), 0, 4);

        public static int GetLevelCap(string characterId)
        {
            var definition = CharacterDatabaseLoader.Load()?.Get(characterId);
            var rarity = definition?.rarity ?? 4;
            var breakthroughCount = GetBreakthroughCount(characterId);

            if (rarity >= 5)
            {
                return breakthroughCount switch
                {
                    0 => 20,
                    1 => 40,
                    2 => 60,
                    _ => 80
                };
            }
            else
            {
                return breakthroughCount switch
                {
                    0 => 20,
                    1 => 40,
                    2 => 60,
                    3 => 70,
                    _ => 80
                };
            }
        }

        public static int GetAffinity(string characterId)
        {
            var key = Key(characterId);
            if (affinity.TryGetValue(key, out var value))
                return Mathf.Clamp(value, 0, 100);

            var relationships = GameManager.Instance?.Relationships;
            if (relationships != null)
            {
                foreach (var alias in CharacterIdentity.Aliases(key))
                {
                    var related = relationships.GetAffinity(alias);
                    if (related != 0)
                    {
                        affinity[key] = Mathf.Clamp(related, 0, 100);
                        return affinity[key];
                    }
                }
            }

            var entry = CharacterIdentity.FindEntry(null, key);
            return Mathf.Clamp(entry?.affinity ?? 0, 0, 100);
        }

        public static int AddAffinity(string characterId, int delta)
        {
            if (string.IsNullOrEmpty(characterId) || delta == 0) return GetAffinity(characterId);
            var key = Key(characterId);
            var next = Mathf.Clamp(GetAffinity(key) + delta, 0, 100);
            affinity[key] = next;
            GameEvents.RaisePartyChanged();
            GameManager.Instance?.SaveGame();
            return next;
        }

        public static string GetAffinityDisposition(string characterId)
        {
            var value = GetAffinity(characterId);
            if (value >= 80) return "Khăng khít";
            if (value >= 50) return "Thân thiết";
            if (value >= 20) return "Thân thiện";
            return "Xa cách";
        }

        public static int GetConstellation(string characterId) => Mathf.Clamp(Get(constellations, characterId, 0), 0, ConstellationCount);
        public static int GetConstellationShards(string characterId) => Mathf.Max(0, Get(constellationShards, characterId, 0));

        public static int GetCumulativeExperience(int level)
        {
            level = Mathf.Clamp(level, 1, 80);
            if (level <= 20)
                return Mathf.RoundToInt((level - 1) * (1400f / 19f));
            if (level <= 40)
                return 1400 + (level - 20) * 605;
            if (level <= 60)
                return 13500 + (level - 40) * 1230;
            return 38100 + (level - 60) * 1845;
        }

        public static int GetExperienceToNextLevelForLevel(int level)
        {
            if (level >= 80) return 0;
            return GetCumulativeExperience(level + 1) - GetCumulativeExperience(level);
        }

        public static int GetExperienceToNextLevel(string characterId)
        {
            var level = GetLevel(characterId);
            return GetExperienceToNextLevelForLevel(level);
        }

        public static int AddExperience(string characterId, int amount)
        {
            if (string.IsNullOrEmpty(characterId) || amount <= 0) return 0;
            var level = GetLevel(characterId);
            var exp = GetExperience(characterId) + amount;
            var cap = GetLevelCap(characterId);
            while (level < cap && level < AbsoluteMaxLevel)
            {
                var required = GetExperienceToNextLevelForLevel(level);
                if (exp < required) break;
                exp -= required;
                level++;
            }
            var key = Key(characterId);
            levels[key] = level;
            experience[key] = level >= cap ? Mathf.Min(exp, Mathf.Max(0, GetExperienceToNextLevel(key) - 1)) : exp;
            GameEvents.RaisePartyChanged();
            GameManager.Instance?.SaveGame();
            return level;
        }

        public static bool TryBreakthrough(string characterId)
        {
            var cap = GetLevelCap(characterId);
            if (cap >= AbsoluteMaxLevel || GetLevel(characterId) < cap) return false;

            var goldCost = cap switch
            {
                20 => 3000,
                40 => 8000,
                60 => 15000,
                70 => 20000,
                _ => 0
            };

            if (goldCost > 0)
            {
                if (PlayerWallet.Instance == null || PlayerWallet.Instance.Coins < goldCost)
                    return false;
            }

            var inventory = GameManager.Instance?.Inventory;

            if (cap == 70)
            {
                const string specialItemId = "material_special_breakthrough";
                if (inventory == null || inventory.GetCount(specialItemId) < 1)
                    return false;
                if (!inventory.RemoveItem(specialItemId, 1))
                    return false;
            }
            else
            {
                var tier = CharacterDatabaseLoader.Load()?.GetBreakthroughTier(cap);
                if (tier != null)
                {
                    var materialAmount = cap switch
                    {
                        20 => 5,
                        40 => 15,
                        60 => 25,
                        _ => tier.materialAmount
                    };

                    if (materialAmount > 0)
                    {
                        if (inventory == null || inventory.GetCount(tier.materialId) < materialAmount) return false;
                        if (!inventory.RemoveItem(tier.materialId, materialAmount)) return false;
                    }
                }
            }

            if (goldCost > 0)
            {
                PlayerWallet.Instance.TrySpendCoins(goldCost);
            }

            var key = Key(characterId);
            breakthroughs[key] = GetBreakthroughCount(key) + 1;
            GameEvents.RaisePartyChanged();
            GameManager.Instance?.SaveGame();
            return true;
        }

        public static int AddDuplicateShards(string characterId, int amount = 0)
        {
            if (string.IsNullOrEmpty(characterId)) return 0;
            var definition = CharacterDatabaseLoader.Load()?.Get(characterId);
            if (amount <= 0) amount = Mathf.Max(1, definition?.duplicateShardReward ?? 1);
            var key = Key(characterId);
            var total = GetConstellationShards(key) + amount;
            constellationShards[key] = total;
            GameEvents.RaisePartyChanged();
            GameManager.Instance?.SaveGame();
            return total;
        }

        public static bool TryUnlockNextConstellation(string characterId)
        {
            var key = Key(characterId);
            var current = GetConstellation(key);
            if (current >= ConstellationCount) return false;
            var definition = CharacterDatabaseLoader.Load()?.Get(key);
            var cost = definition?.constellationShardCosts != null && current < definition.constellationShardCosts.Count
                ? Mathf.Max(1, definition.constellationShardCosts[current]) : 1;
            var shards = GetConstellationShards(key);
            if (shards < cost) return false;
            constellationShards[key] = shards - cost;
            constellations[key] = current + 1;
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
            levels.Clear(); experience.Clear(); breakthroughs.Clear(); constellations.Clear(); constellationShards.Clear(); affinity.Clear();
        }

        public static void ExportToSave(SaveData data)
        {
            data.characterLevels = SaveDataUtility.ToPairs(Canonicalize(levels));
            data.characterExperience = SaveDataUtility.ToPairs(Canonicalize(experience));
            data.characterBreakthroughs = SaveDataUtility.ToPairs(Canonicalize(breakthroughs));
            data.characterConstellations = SaveDataUtility.ToPairs(Canonicalize(constellations));
            data.characterConstellationShards = SaveDataUtility.ToPairs(Canonicalize(constellationShards));
            data.characterAffinity = SaveDataUtility.ToPairs(Canonicalize(affinity));
        }

        public static void ImportFromSave(SaveData data)
        {
            Replace(levels, data?.characterLevels); Replace(experience, data?.characterExperience);
            Replace(breakthroughs, data?.characterBreakthroughs); Replace(constellations, data?.characterConstellations);
            Replace(constellationShards, data?.characterConstellationShards);
            Replace(affinity, data?.characterAffinity);
        }

        static int DefaultLevel(string id) => Mathf.Clamp(CharacterDatabaseLoader.Load()?.Get(id)?.level ?? 1, 1, AbsoluteMaxLevel);
        static string Key(string characterId) => CharacterIdentity.Canonical(characterId);
        static int Get(Dictionary<string, int> source, string key, int fallback)
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            var canonical = Key(key);
            if (source.TryGetValue(canonical, out var value)) return value;
            if (source.TryGetValue(key, out value))
            {
                source[canonical] = value;
                return value;
            }
            foreach (var alias in CharacterIdentity.Aliases(canonical))
            {
                if (!source.TryGetValue(alias, out value)) continue;
                source[canonical] = value;
                return value;
            }
            return fallback;
        }
        static Dictionary<string, int> Canonicalize(Dictionary<string, int> source)
        {
            var result = new Dictionary<string, int>();
            foreach (var pair in source)
            {
                var key = Key(pair.Key);
                if (string.IsNullOrEmpty(key)) continue;
                result[key] = result.TryGetValue(key, out var current) ? Mathf.Max(current, pair.Value) : pair.Value;
            }
            return result;
        }
        static void Replace(Dictionary<string, int> target, List<StringIntPair> source)
        {
            target.Clear();
            foreach (var pair in Canonicalize(SaveDataUtility.FromPairs(source)))
                target[pair.Key] = pair.Value;
        }
    }
}
