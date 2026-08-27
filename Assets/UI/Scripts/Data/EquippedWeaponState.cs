using System.Collections.Generic;
using System;
using System.Linq;
using BES.Core;
using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    public class EquippedWeaponState : MonoBehaviour
    {
        public static EquippedWeaponState Instance { get; private set; }

        [SerializeField] WeaponDatabase database;
        [SerializeField] string equippedWeaponId = "weapon_iron_sword";
        [SerializeField] int level = 1;
        [SerializeField] int experience = 0;
        [SerializeField] int refinement = 1;

        readonly HashSet<string> ownedWeaponIds = new();
        readonly List<WeaponInstance> ownedWeaponInstances = new();
        readonly Dictionary<string, string> characterEquippedWeaponInstanceIds = new();

        public string EquippedWeaponId => equippedWeaponId;
        public string EquippedWeaponInstanceId { get; private set; }
        public int Level => level;
        public int Experience => experience;
        public int Refinement => refinement;
        public IReadOnlyCollection<string> OwnedWeaponIds => ownedWeaponIds;
        public IReadOnlyList<WeaponInstance> OwnedWeaponInstances => ownedWeaponInstances;

        public WeaponDefinition EquippedWeapon =>
            database != null ? database.GetById(equippedWeaponId) : null;

        public WeaponInstance EquippedWeaponInstance =>
            GetWeaponInstance(EquippedWeaponInstanceId) ?? FirstInstanceOf(equippedWeaponId);

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            database ??= Resources.Load<WeaponDatabase>("Data/WeaponDatabase");
            if (ownedWeaponIds.Count == 0)
                ownedWeaponIds.Add(equippedWeaponId);
            EnsureStarterInstance();
        }

        public bool OwnsWeapon(string weaponId) =>
            !string.IsNullOrEmpty(weaponId) && ownedWeaponIds.Contains(weaponId);

        public bool OwnsWeaponInstance(string instanceId) =>
            GetWeaponInstance(instanceId) != null;

        public WeaponInstance GetWeaponInstance(string instanceId) =>
            string.IsNullOrEmpty(instanceId)
                ? null
                : ownedWeaponInstances.Find(x => x != null && x.instanceId == instanceId);

        public WeaponInstance GetEquippedWeaponForCharacter(string characterId)
        {
            var key = CharacterIdentity.Canonical(characterId);
            if (!string.IsNullOrEmpty(key) &&
                characterEquippedWeaponInstanceIds.TryGetValue(key, out var instanceId))
                return GetWeaponInstance(instanceId);
            return EquippedWeaponInstance;
        }

        public WeaponInstance AddWeaponInstance(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
                return null;

            var definition = database != null ? database.FindExact(weaponId) : null;
            if (database != null && definition == null)
                return null;
            weaponId = definition != null ? definition.weaponId : weaponId;
            ownedWeaponIds.Add(weaponId);

            var instance = new WeaponInstance
            {
                instanceId = CreateInstanceId(weaponId),
                weaponId = weaponId,
                level = 1,
                experience = 0,
                refinement = 1,
                randomStats = RollRandomStats(definition)
            };
            ownedWeaponInstances.Add(instance);
            if (string.IsNullOrEmpty(EquippedWeaponInstanceId))
                EquipInstance(instance.instanceId);
            GameManager.Instance?.SaveGame();
            return instance;
        }

        public void UnlockWeapon(string weaponId)
        {
            if (!string.IsNullOrEmpty(weaponId))
            {
                ownedWeaponIds.Add(weaponId);
                if (FirstInstanceOf(weaponId) == null)
                    AddWeaponInstance(weaponId);
                GameManager.Instance?.SaveGame();
            }
        }

        public void Equip(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
                return;
            var instance = GetWeaponInstance(weaponId) ?? FirstInstanceOf(weaponId);
            if (instance == null && OwnsWeapon(weaponId))
                instance = AddWeaponInstance(weaponId);
            if (instance == null)
                return;
            EquipInstance(instance.instanceId);
        }

        public void EquipInstance(string instanceId, string characterId = null)
        {
            var instance = GetWeaponInstance(instanceId);
            if (instance == null)
                return;
            equippedWeaponId = instance.weaponId;
            EquippedWeaponInstanceId = instance.instanceId;
            level = Mathf.Max(1, instance.level);
            experience = Mathf.Max(0, instance.experience);
            refinement = Mathf.Max(1, instance.refinement);
            var targetCharacter = CharacterIdentity.Canonical(characterId ?? CharacterOwnership.FocusedCharacterId);
            if (!string.IsNullOrEmpty(targetCharacter))
                characterEquippedWeaponInstanceIds[targetCharacter] = instance.instanceId;
            RefreshPlayerBuild();
            GameManager.Instance?.SaveGame();
        }

        public void Unequip()
        {
            equippedWeaponId = string.Empty;
            GameManager.Instance?.SaveGame();
        }

        public void SetLevel(int newLevel) { level = Mathf.Max(1, newLevel); SyncActiveInstanceStats(); GameManager.Instance?.SaveGame(); }
        public void SetExperience(int newExp) { experience = Mathf.Max(0, newExp); SyncActiveInstanceStats(); GameManager.Instance?.SaveGame(); }
        public void SetRefinement(int newRefine) { refinement = Mathf.Max(1, newRefine); SyncActiveInstanceStats(); GameManager.Instance?.SaveGame(); }
        public void EnhanceLevel(int delta = 1)
        {
            level = Mathf.Min(80, level + delta);
            SyncActiveInstanceStats();
            RefreshPlayerBuild();
            GameManager.Instance?.SaveGame();
        }
        public void EnhanceRefinement(int delta = 1)
        {
            refinement = Mathf.Min(5, refinement + delta);
            SyncActiveInstanceStats();
            RefreshPlayerBuild();
            GameManager.Instance?.SaveGame();
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            var cap = EquippedWeapon != null ? EquippedWeapon.maxLevel : 80;
            cap = Mathf.Min(cap, 80);
            var exp = experience + amount;
            while (level < cap && level < 80)
            {
                var required = CharacterProgressionState.GetExperienceToNextLevelForLevel(level);
                if (exp < required) break;
                exp -= required;
                level++;
            }
            experience = level >= cap ? 0 : exp;
            SyncActiveInstanceStats();
            RefreshPlayerBuild();
            GameManager.Instance?.SaveGame();
        }

        public int SimulateLevelAfterExp(int addedExp)
        {
            var simulatedLevel = level;
            var cap = EquippedWeapon != null ? EquippedWeapon.maxLevel : 80;
            cap = Mathf.Min(cap, 80);
            var exp = experience + addedExp;
            while (simulatedLevel < cap && simulatedLevel < 80)
            {
                var required = CharacterProgressionState.GetExperienceToNextLevelForLevel(simulatedLevel);
                if (exp < required) break;
                exp -= required;
                simulatedLevel++;
            }
            return simulatedLevel;
        }

        public int GetSimulatedAtk(int targetLevel)
        {
            var w = EquippedWeapon;
            if (w == null)
                return 15;
            return w.baseAtk + (targetLevel - 1) * 8 + refinement * 12;
        }

        static void RefreshPlayerBuild()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent<PlayerBuildStats>(out var build))
                build.Refresh();
        }

        public int GetDisplayAtk()
        {
            var w = EquippedWeapon;
            if (w == null)
                return 15;
            return w.baseAtk + (level - 1) * 8 + refinement * 12 + Mathf.RoundToInt(GetRandomStatValue(EquippedWeaponInstance, WeaponStatType.AttackFlat));
        }

        public int GetDisplayAtk(string characterId)
        {
            var instance = GetEquippedWeaponForCharacter(characterId);
            var w = database != null ? database.GetById(instance?.weaponId) : null;
            if (w == null)
                return 15;
            var instanceLevel = Mathf.Max(1, instance?.level ?? 1);
            var instanceRefinement = Mathf.Max(1, instance?.refinement ?? 1);
            var flat = GetRandomStatValue(instance, WeaponStatType.AttackFlat);
            return w.baseAtk + (instanceLevel - 1) * 8 + instanceRefinement * 12 + Mathf.RoundToInt(flat);
        }

        public WeaponRuntimeBonus GetRuntimeBonus(string characterId)
        {
            var instance = GetEquippedWeaponForCharacter(characterId);
            var bonus = new WeaponRuntimeBonus();
            if (instance?.randomStats == null) return bonus;
            foreach (var stat in instance.randomStats)
            {
                switch (stat.statType)
                {
                    case WeaponStatType.AttackFlat: bonus.attackFlat += stat.value; break;
                    case WeaponStatType.AttackPercent: bonus.attackPercent += stat.value; break;
                    case WeaponStatType.HealthFlat: bonus.healthFlat += stat.value; break;
                    case WeaponStatType.HealthPercent: bonus.healthPercent += stat.value; break;
                    case WeaponStatType.DefenseFlat: bonus.defenseFlat += stat.value; break;
                    case WeaponStatType.DefensePercent: bonus.defensePercent += stat.value; break;
                    case WeaponStatType.SpeedFlat: bonus.speedFlat += stat.value; break;
                    case WeaponStatType.CritRatePercent: bonus.critRatePercent += stat.value; break;
                    case WeaponStatType.CritDamagePercent: bonus.critDamagePercent += stat.value; break;
                    case WeaponStatType.ElementDamagePercent: bonus.elementDamagePercent += stat.value; break;
                    case WeaponStatType.HealingBonusPercent: bonus.healingBonusPercent += stat.value; break;
                    case WeaponStatType.ShieldBonusPercent: bonus.shieldBonusPercent += stat.value; break;
                }
            }
            return bonus;
        }

        public void ResetToDefaults()
        {
            equippedWeaponId = "weapon_iron_sword";
            EquippedWeaponInstanceId = string.Empty;
            level = 1;
            experience = 0;
            refinement = 1;
            ownedWeaponIds.Clear();
            ownedWeaponIds.Add(equippedWeaponId);
            ownedWeaponInstances.Clear();
            characterEquippedWeaponInstanceIds.Clear();
            EnsureStarterInstance();
        }

        public void ExportToSave(SaveData data)
        {
            if (data == null)
                return;

            data.equippedWeaponId = equippedWeaponId;
            data.weaponLevel = level;
            data.weaponExperience = experience;
            data.weaponRefinement = refinement;
            data.ownedWeaponIds = new List<string>(ownedWeaponIds);
            data.equippedWeaponInstanceId = EquippedWeaponInstanceId;
            data.ownedWeaponInstances = ExportInstances();
            data.characterEquippedWeaponInstanceIds = characterEquippedWeaponInstanceIds
                .Select(pair => new StringStringPair { key = pair.Key, value = pair.Value })
                .ToList();
        }

        public void ImportFromSave(SaveData data)
        {
            if (data == null)
                return;

            ownedWeaponIds.Clear();
            if (data.ownedWeaponIds != null && data.ownedWeaponIds.Count > 0)
            {
                foreach (var id in data.ownedWeaponIds)
                    ownedWeaponIds.Add(id);
            }
            else
            {
                ownedWeaponIds.Add("weapon_iron_sword");
            }

            equippedWeaponId = string.IsNullOrEmpty(data.equippedWeaponId)
                ? "weapon_iron_sword"
                : data.equippedWeaponId;
            level = Mathf.Max(1, data.weaponLevel);
            experience = Mathf.Max(0, data.weaponExperience);
            refinement = Mathf.Max(1, data.weaponRefinement);
            ownedWeaponInstances.Clear();
            if (data.ownedWeaponInstances != null)
            {
                foreach (var saved in data.ownedWeaponInstances)
                    ownedWeaponInstances.Add(ImportInstance(saved));
            }
            EnsureInstancesForLegacyOwnedWeapons();
            EquippedWeaponInstanceId = data.equippedWeaponInstanceId;
            if (GetWeaponInstance(EquippedWeaponInstanceId) == null)
                EquippedWeaponInstanceId = FirstInstanceOf(equippedWeaponId)?.instanceId;

            characterEquippedWeaponInstanceIds.Clear();
            if (data.characterEquippedWeaponInstanceIds != null)
            {
                foreach (var pair in data.characterEquippedWeaponInstanceIds)
                    if (!string.IsNullOrEmpty(pair.key) && GetWeaponInstance(pair.value) != null)
                        characterEquippedWeaponInstanceIds[CharacterIdentity.Canonical(pair.key)] = pair.value;
            }
        }

        void EnsureStarterInstance()
        {
            EnsureInstancesForLegacyOwnedWeapons();
            if (string.IsNullOrEmpty(EquippedWeaponInstanceId))
                EquippedWeaponInstanceId = FirstInstanceOf(equippedWeaponId)?.instanceId;
        }

        void EnsureInstancesForLegacyOwnedWeapons()
        {
            foreach (var weaponId in ownedWeaponIds.ToList())
            {
                if (!string.IsNullOrEmpty(weaponId) && FirstInstanceOf(weaponId) == null)
                    ownedWeaponInstances.Add(new WeaponInstance
                    {
                        instanceId = CreateInstanceId(weaponId),
                        weaponId = weaponId,
                        level = weaponId == equippedWeaponId ? Mathf.Max(1, level) : 1,
                        experience = weaponId == equippedWeaponId ? Mathf.Max(0, experience) : 0,
                        refinement = weaponId == equippedWeaponId ? Mathf.Max(1, refinement) : 1,
                        randomStats = RollRandomStats(database != null ? database.FindExact(weaponId) : null)
                    });
            }
        }

        WeaponInstance FirstInstanceOf(string weaponId) =>
            string.IsNullOrEmpty(weaponId) ? null : ownedWeaponInstances.Find(x => x != null && x.weaponId == weaponId);

        void SyncActiveInstanceStats()
        {
            var instance = EquippedWeaponInstance;
            if (instance == null) return;
            instance.level = level;
            instance.experience = experience;
            instance.refinement = refinement;
        }

        static float GetRandomStatValue(WeaponInstance instance, WeaponStatType type)
        {
            if (instance?.randomStats == null) return 0f;
            var total = 0f;
            foreach (var stat in instance.randomStats)
                if (stat.statType == type)
                    total += stat.value;
            return total;
        }

        static string CreateInstanceId(string weaponId) =>
            $"{weaponId}_{DateTime.UtcNow.Ticks}_{UnityEngine.Random.Range(1000, 9999)}";

        static List<WeaponRandomStatInstance> RollRandomStats(WeaponDefinition definition)
        {
            var result = new List<WeaponRandomStatInstance>();
            if (definition?.randomStatPool == null || definition.randomStatPool.Count == 0)
                return result;

            var pool = new List<WeaponRandomStatRange>(definition.randomStatPool);
            var targetCount = Mathf.Min(Mathf.Max(0, definition.randomStatLineCount), pool.Count);
            while (result.Count < targetCount && pool.Count > 0)
            {
                var picked = PickWeighted(pool);
                pool.RemoveAll(x => x.statType == picked.statType);
                var min = Mathf.Min(picked.minValue, picked.maxValue);
                var max = Mathf.Max(picked.minValue, picked.maxValue);
                result.Add(new WeaponRandomStatInstance
                {
                    statType = picked.statType,
                    displayName = string.IsNullOrWhiteSpace(picked.displayName) ? picked.statType.ToString() : picked.displayName,
                    isPercent = picked.isPercent,
                    value = (float)Math.Round(UnityEngine.Random.Range(min, max), 2)
                });
            }
            return result;
        }

        static WeaponRandomStatRange PickWeighted(List<WeaponRandomStatRange> pool)
        {
            var total = 0;
            foreach (var entry in pool)
                total += Mathf.Max(1, entry.weight);
            var roll = UnityEngine.Random.Range(0, total);
            var cursor = 0;
            foreach (var entry in pool)
            {
                cursor += Mathf.Max(1, entry.weight);
                if (roll < cursor) return entry;
            }
            return pool[0];
        }

        List<WeaponInstanceSaveData> ExportInstances()
        {
            var result = new List<WeaponInstanceSaveData>();
            foreach (var instance in ownedWeaponInstances)
            {
                if (instance == null) continue;
                result.Add(new WeaponInstanceSaveData
                {
                    instanceId = instance.instanceId,
                    weaponId = instance.weaponId,
                    level = instance.level,
                    experience = instance.experience,
                    refinement = instance.refinement,
                    randomStats = instance.randomStats?.Select(stat => new WeaponRandomStatSaveData
                    {
                        statType = (int)stat.statType,
                        displayName = stat.displayName,
                        isPercent = stat.isPercent,
                        value = stat.value
                    }).ToList() ?? new List<WeaponRandomStatSaveData>()
                });
            }
            return result;
        }

        static WeaponInstance ImportInstance(WeaponInstanceSaveData saved)
        {
            if (saved == null) return null;
            return new WeaponInstance
            {
                instanceId = saved.instanceId,
                weaponId = saved.weaponId,
                level = Mathf.Max(1, saved.level),
                experience = Mathf.Max(0, saved.experience),
                refinement = Mathf.Max(1, saved.refinement),
                randomStats = saved.randomStats?.Select(stat => new WeaponRandomStatInstance
                {
                    statType = (WeaponStatType)stat.statType,
                    displayName = stat.displayName,
                    isPercent = stat.isPercent,
                    value = stat.value
                }).ToList() ?? new List<WeaponRandomStatInstance>()
            };
        }
    }

    public struct WeaponRuntimeBonus
    {
        public float attackFlat;
        public float attackPercent;
        public float healthFlat;
        public float healthPercent;
        public float defenseFlat;
        public float defensePercent;
        public float speedFlat;
        public float critRatePercent;
        public float critDamagePercent;
        public float elementDamagePercent;
        public float healingBonusPercent;
        public float shieldBonusPercent;
    }
}
