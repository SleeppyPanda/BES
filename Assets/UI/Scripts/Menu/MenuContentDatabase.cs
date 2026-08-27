using System;
using System.Collections.Generic;
using UnityEngine;

namespace BES.UI.Menu
{
    public enum MenuScreenId { Home, StoryParty, ResourceStages, SanctumRelics, WeaponBreakthrough, Battle, Dialogue, Management, CashShop, BattlePass, PlayParty }

    [Serializable] public class CurrencyEntry { public string id; public Sprite icon; public int amount; }

    [Serializable]
    public class CharacterEntry
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public string faction;
        public bool playable = true;
        public string element;
        public string weaponType;
        public string skillType;
        [TextArea] public string normalAttack;
        [TextArea] public string skillDescription;
        [TextArea] public string passiveDescription;
        public Sprite portrait;
        [Tooltip("Card background shared by Gallery, Story Mode and future Play Mode rosters.")]
        public Sprite cardBackground;
        public Sprite fullBody;
        public Sprite chibi;
        [Header("Battle Attack Animation (3 FPS)")]
        [Tooltip("Gán đủ cả 5 frame để phát animation đánh. Thiếu một frame thì battle chỉ dùng ảnh Chibi đứng yên.")]
        public Sprite attackFrame1;
        public Sprite attackFrame2;
        public Sprite attackFrame3;
        public Sprite attackFrame4;
        public Sprite attackFrame5;
        [Tooltip("Attack VFX prefabs tested/assigned for this character. Copied into battle units and played when attacking.")]
        public List<GameObject> attackEffectPrefabs = new();
        public Vector3 attackEffectOffset = Vector3.zero;
        public Vector3 attackEffectScale = Vector3.one;
        public Sprite elementIcon;
        [Tooltip("Four artifact slot sprites shown in Character Information and Equipment tabs.")]
        public List<Sprite> equippedArtifacts = new();
        [Tooltip("IDs used by Story requirements, e.g. Fire, Ice, Healer or Ranged.")]
        public List<string> attributes = new();
        public UnityEngine.Video.VideoClip revealVideoClip;
        [Range(1, 6)] public int rarity = 4;
        [HideInInspector] public int starLevel;
        [HideInInspector] public int level = 1;
        [Min(0)] public int combatPower;
        [HideInInspector] public int constellation;
        [Range(1, 10)] public int quality = 1;
        [Range(0, 100)] public int affinity;
        public int maxHealth = 100;
        public int attack = 10;
        public int defense = 5;
        public int speed = 10;
    }

    [Serializable]
    public class RewardEntry
    {
        public string id;
        public Sprite icon;
        [Tooltip("Fallback amount for old data. Used when Min/Max Amount are not set.")]
        public int amount = 1;
        [Min(0)] public int minAmount = 0;
        [Min(0)] public int maxAmount = 0;
        [Range(0, 100)] public int dropChancePercent = 100;
        public bool guaranteed = true;
        public int rarity = 1;

        public int RollAmount()
        {
            if (amount <= 0 && minAmount <= 0 && maxAmount <= 0)
                return 0;
            var min = minAmount > 0 ? minAmount : Mathf.Max(1, amount);
            var max = maxAmount > 0 ? maxAmount : min;
            if (max < min) max = min;
            return UnityEngine.Random.Range(min, max + 1);
        }

        public bool ShouldDrop()
        {
            if (guaranteed) return true;
            return UnityEngine.Random.Range(0, 100) < Mathf.Clamp(dropChancePercent, 0, 100);
        }
    }

    [Serializable]
    public class PartyAttributeRequirement
    {
        public string attributeId;
        public Sprite icon;
        [Min(1)] public int minimumCount = 1;
    }

    [Serializable]
    public class StageEntry
    {
        public string id;
        public string title;
        [Tooltip("Mode/category used by Play Mode buttons, e.g. resources, sanctum, weapon, event.")]
        public string playModeType;
        [TextArea] public string description;
        public Sprite preview;
        public int energyCost = 10;
        [Header("Story Dialogue")]
        public DialogueSequence preBattleDialogue;
        public DialogueSequence victoryDialogue;
        public List<CombatDialogueTrigger> combatDialogueTriggers = new();
        public List<RewardEntry> rewards = new();
        public List<PartyAttributeRequirement> partyRequirements = new();
        [Header("Stage Combat Setup")]
        public int enemyLevel = 1;
        public List<BattleUnitDefinition> enemies = new();
        public BattleUnitDefinition boss;
        [Header("Optional Multi Phase Combat")]
        public List<BattlePhaseEntry> battlePhases = new();
    }

    [Serializable]
    public class PlayModeStageGroup
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public Sprite icon;
        public List<StageEntry> stages = new();
    }

    [Serializable]
    public class StoryChapterEntry
    {
        public string id;
        public string title;
        [TextArea] public string summary;
        public Sprite background;
        public DialogueSequence introDialogue;
        public List<StageEntry> stages = new();
    }

    public enum CombatDialogueTriggerType
    {
        BattleStart,
        RoundStart,
        BossHealthBelowPercent,
        EnemyDefeated,
        BeforeVictory,
        TotalEnemyHealthBelowPercent,
        EnemyCountAtOrBelow,
        PhaseStart,
        PhaseVictory,
        AllAlliesDefeated
    }

    public enum CombatTriggerActionType
    {
        None,
        StartNextPhase,
        ConvertUnitToAlly,
        ConvertUnitToAllyAndStartNextPhase,
        KillAllEnemiesAndPlayPhaseVictory,
        ReturnToStoryWithoutResult,
        SetElioHealthToTenPercentAndPlayPhaseVictory,
        HealElioToThirtyFivePercent,
        AddAurelianAlly
    }

    [Serializable]
    public class BattlePhaseEntry
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public int enemyLevel = 1;
        [Tooltip("Optional fixed player-side units for this phase. If empty, the selected story/play party is used.")]
        public List<BattleUnitDefinition> allies = new();
        public List<BattleUnitDefinition> enemies = new();
        public BattleUnitDefinition boss;
        public List<CombatDialogueTrigger> combatDialogueTriggers = new();
    }

    [Serializable]
    public class CombatDialogueTrigger
    {
        public string id;
        public CombatDialogueTriggerType triggerType;
        [Min(1)] public int round = 1;
        [Range(1, 100)] public int healthPercent = 50;
        [Min(0)] public int enemyCount = 0;
        public string unitId;
        public bool pauseCombat = true;
        public CombatTriggerActionType actionAfterDialogue = CombatTriggerActionType.None;
        [Tooltip("Used by ConvertUnitToAlly actions. If empty, the unit passed to the trigger is used.")]
        public string convertUnitId;
        public DialogueSequence dialogue;
        [NonSerialized] public bool played;
    }

    [CreateAssetMenu(menuName = "BES/UI/Menu Content Database", fileName = "MenuContentDatabase")]
    public class MenuContentDatabase : ScriptableObject
    {
        public List<CurrencyEntry> currencies = new();
        public List<CharacterEntry> characters = new();
        public List<StoryChapterEntry> storyChapters = new();
        public List<StageEntry> resourceStages = new();
        public List<StageEntry> sanctumStages = new();
        public List<StageEntry> weaponStages = new();
        [Tooltip("Extra Play Mode groups for future modes. Add group 4+ here and point StageSelectionController/PlayMode buttons to its id.")]
        public List<PlayModeStageGroup> playModeStageGroups = new();

        public void EnsureDefaultPlayModeStages()
        {
            if (resourceStages == null) resourceStages = new List<StageEntry>();
            if (sanctumStages == null) sanctumStages = new List<StageEntry>();
            if (weaponStages == null) weaponStages = new List<StageEntry>();

            if (resourceStages.Count == 0)
                resourceStages.Add(CreateDefaultStage("play_resource_01", "Đường Cát Tài Nguyên", "Ải luyện tài nguyên cơ bản, phù hợp để test đội hình.", 1,
                    NewReward("coins", 1200, 3),
                    NewReward("character_exp_green", 2, 3),
                    NewEnemy("sand_wisp", "Cát Xoáy Sa Mạc", 520, 90, 34, 13),
                    NewEnemy("fire_wisp", "Lửa Linh Hồn", 620, 12, 45, 9),
                    NewEnemy("sand_wisp_2", "Cát Xoáy Sa Mạc", 520, 90, 34, 13)));

            if (sanctumStages.Count == 0)
                sanctumStages.Add(CreateDefaultStage("play_sanctum_01", "Thánh Tích Vang Vọng", "Ải thánh tích với quái hỗ trợ và khống chế.", 3,
                    NewReward("artifact_shard", 3, 4),
                    NewReward("relic_exp_blue", 1, 4),
                    NewEnemy("sand_wisp_guard", "Cát Xoáy Sa Mạc", 680, 112, 44, 13),
                    NewEnemy("fire_wisp_elite", "Lửa Linh Hồn", 720, 14, 54, 9),
                    NewEnemy("flame_beast", "Thú Lửa Nhỏ", 560, 96, 34, 14)));

            if (weaponStages.Count == 0)
                weaponStages.Add(CreateDefaultStage("play_weapon_01", "Lò Rèn Ảo Ảnh", "Ải nguyên liệu vũ khí, kẻ địch thiên về sát thương nhanh.", 5,
                    NewReward("weapon_ore", 4, 4),
                    NewReward("weapon_exp_blue", 1, 4),
                    NewEnemy("flame_beast_a", "Thú Lửa Nhỏ", 620, 108, 38, 15),
                    NewEnemy("flame_beast_b", "Thú Lửa Nhỏ", 620, 108, 38, 15),
                    NewEnemy("sand_wisp_elite", "Cát Xoáy Sa Mạc", 680, 112, 44, 13),
                    NewEnemy("fire_wisp_support", "Lửa Linh Hồn", 720, 14, 54, 9)));
        }

        static StageEntry CreateDefaultStage(string id, string title, string description, int level, RewardEntry rewardA, RewardEntry rewardB, params BattleUnitDefinition[] enemies)
        {
            return new StageEntry
            {
                id = id,
                title = title,
                playModeType = id.Contains("resource", StringComparison.OrdinalIgnoreCase) ? "resources" :
                    id.Contains("sanctum", StringComparison.OrdinalIgnoreCase) ? "sanctum" :
                    id.Contains("weapon", StringComparison.OrdinalIgnoreCase) ? "weapon" : "play",
                description = description,
                energyCost = 10 + level * 2,
                enemyLevel = level,
                rewards = new List<RewardEntry> { rewardA, rewardB },
                enemies = new List<BattleUnitDefinition>(enemies)
            };
        }

        static RewardEntry NewReward(string id, int amount, int rarity) =>
            new RewardEntry
            {
                id = id,
                amount = Mathf.Max(1, amount),
                minAmount = Mathf.Max(1, amount),
                maxAmount = Mathf.Max(1, amount),
                dropChancePercent = 100,
                guaranteed = true,
                rarity = Mathf.Max(1, rarity)
            };

        public List<StageEntry> GetPlayModeStages(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return resourceStages;

            if (groupId.Equals("resources", StringComparison.OrdinalIgnoreCase) ||
                groupId.Equals("resource", StringComparison.OrdinalIgnoreCase))
                return resourceStages;
            if (groupId.Equals("sanctum", StringComparison.OrdinalIgnoreCase) ||
                groupId.Equals("relics", StringComparison.OrdinalIgnoreCase) ||
                groupId.Equals("sanctumRelics", StringComparison.OrdinalIgnoreCase))
                return sanctumStages;
            if (groupId.Equals("weapon", StringComparison.OrdinalIgnoreCase) ||
                groupId.Equals("weaponBreakthrough", StringComparison.OrdinalIgnoreCase))
                return weaponStages;

            var group = playModeStageGroups?.Find(x => x != null && groupId.Equals(x.id, StringComparison.OrdinalIgnoreCase));
            return group?.stages ?? new List<StageEntry>();
        }

        static BattleUnitDefinition NewEnemy(string id, string displayName, int hp, int atk, int def, int spd)
        {
            return new BattleUnitDefinition
            {
                id = id,
                displayName = displayName,
                element = InferEnemyElement(id),
                maxHealth = hp,
                attack = Mathf.Max(1, atk),
                defense = Mathf.Max(0, def),
                speed = Mathf.Max(1, spd),
                skills = new List<BattleSkillDefinition>
                {
                    new BattleSkillDefinition { id = "attack", displayName = "Tấn Công", powerMultiplier = 1f }
                }
            };
        }

        static string InferEnemyElement(string id)
        {
            id = id?.ToLowerInvariant() ?? string.Empty;
            if (id.Contains("flame") || id.Contains("fire")) return "Hỏa";
            if (id.Contains("wisp")) return "Thủy";
            if (id.Contains("sarcophagus") || id.Contains("coffin")) return "Thảo";
            if (id.Contains("sand")) return "Phong";
            return string.Empty;
        }

        public CharacterEntry FindCharacter(string id) => CharacterIdentity.FindEntry(this, id);
    }
}

