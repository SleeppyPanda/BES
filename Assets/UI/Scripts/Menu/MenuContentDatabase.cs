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
        [Header("Battle Skill Icons")]
        [Tooltip("Icon cho nút Đánh thường trong battle. Nếu trống sẽ dùng Element Icon hoặc icon mặc định của nút.")]
        public Sprite normalAttackIcon;
        [Tooltip("Icon cho nút Kỹ năng trong battle. Nếu trống sẽ dùng Element Icon hoặc icon mặc định của nút.")]
        public Sprite skillIcon;
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
        [Header("Advanced Combat Stats")]
        [Tooltip("Số lượt cần tích để dùng kỹ năng một lần.")]
        [Min(1)] public int energyTurns = 3;
        [Tooltip("Tỷ lệ bạo kích. 0.1 = 10%.")]
        [Range(0f, 1f)] public float critRate = 0.1f;
        [Tooltip("Hệ số sát thương khi bạo kích. 1.5 = 150%.")]
        [Min(1f)] public float critDamageMultiplier = 1.5f;
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

        void OnValidate()
        {
            EnsureDefaultPlayModeStages();
            NormalizeCurrencyDefaults();
            NormalizeCharacterCombatDefaults();
            NormalizeEnemyCombatDefaults();
        }

        public void NormalizeCurrencyDefaults()
        {
            if (currencies == null) return;
            foreach (var currency in currencies)
            {
                if (currency == null || string.IsNullOrWhiteSpace(currency.id)) continue;
                if (currency.id.Equals("gems", StringComparison.OrdinalIgnoreCase) ||
                    currency.id.Equals("gem", StringComparison.OrdinalIgnoreCase))
                    currency.amount = Mathf.Max(currency.amount, 99999);
            }
        }

        public void NormalizeCharacterCombatDefaults()
        {
            NormalizeCurrencyDefaults();
            if (characters == null) return;
            foreach (var character in characters)
                NormalizeCharacterCombatDefaults(character);
            NormalizeEnemyCombatDefaults();
        }

        static void NormalizeCharacterCombatDefaults(CharacterEntry character)
        {
            if (character == null || !character.playable) return;

            var rarity = Mathf.Clamp(character.rarity, 3, 5);
            if (character.energyTurns <= 0)
                character.energyTurns = SuggestedEnergyTurns(character);
            if (character.critRate <= 0f)
                character.critRate = rarity >= 5 ? 0.12f : rarity == 4 ? 0.10f : 0.08f;
            if (character.critDamageMultiplier < 1f)
                character.critDamageMultiplier = rarity >= 5 ? 1.65f : rarity == 4 ? 1.5f : 1.35f;

            if (character.combatPower <= 0)
                character.combatPower = Mathf.RoundToInt(character.attack * 8f + character.maxHealth * .7f + character.defense * 6f + character.speed * 25f);

#if UNITY_EDITOR
            AssignDefaultAttackEffects(character);
#endif
        }

        static int SuggestedEnergyTurns(CharacterEntry character)
        {
            var text = ((character.skillType ?? string.Empty) + " " + (character.skillDescription ?? string.Empty)).ToLowerInvariant();
            if (text.Contains("hồi") || text.Contains("khiên") || text.Contains("khống"))
                return 3;
            if (text.Contains("450") || text.Contains("300"))
                return 4;
            return character.rarity >= 5 ? 3 : 2;
        }

#if UNITY_EDITOR
        static void AssignDefaultAttackEffects(CharacterEntry character)
        {
            if (character.attackEffectPrefabs != null && character.attackEffectPrefabs.Count > 0)
                return;

            var paths = DefaultAttackEffectPaths(character.element);
            if (paths == null || paths.Length == 0)
                return;

            character.attackEffectPrefabs ??= new List<GameObject>();
            foreach (var path in paths)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && !character.attackEffectPrefabs.Contains(prefab))
                    character.attackEffectPrefabs.Add(prefab);
            }
        }

        static string[] DefaultAttackEffectPaths(string element)
        {
            element = element?.ToLowerInvariant() ?? string.Empty;
            if (element.Contains("hỏa") || element.Contains("hoa") || element.Contains("fire"))
                return new[]
                {
                    "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR3 Hit Fire B (Air).prefab",
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_2_Bomb_Red.prefab"
                };
            if (element.Contains("thủy") || element.Contains("thuy") || element.Contains("water") || element.Contains("ice"))
                return new[]
                {
                    "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Ice/CFXR3 Hit Ice B (Air).prefab",
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_2_Bomb_Blue.prefab"
                };
            if (element.Contains("lôi") || element.Contains("loi") || element.Contains("lightning") || element.Contains("electric"))
                return new[]
                {
                    "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Electric/CFXR3 Hit Electric C (Air).prefab",
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_2_Zap.prefab"
                };
            if (element.Contains("thảo") || element.Contains("thao") || element.Contains("grass"))
                return new[]
                {
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Battle_Effect_Green.prefab",
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_2_Bomb_Green.prefab"
                };
            if (element.Contains("phong") || element.Contains("wind"))
                return new[]
                {
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Battle_Effect_White.prefab",
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_1_Woa_Yellow.prefab"
                };
            return new[] { "Assets/CartoonVFX9x/Comic_FX/Prefabs/Battle_Effect_Yellow.prefab" };
        }
#endif

        public void NormalizeEnemyCombatDefaults()
        {
#if UNITY_EDITOR
            NormalizeEnemyCombatDefaults(storyChapters);
            NormalizeEnemyCombatDefaults(resourceStages);
            NormalizeEnemyCombatDefaults(sanctumStages);
            NormalizeEnemyCombatDefaults(weaponStages);
            if (playModeStageGroups != null)
            {
                foreach (var group in playModeStageGroups)
                    NormalizeEnemyCombatDefaults(group?.stages);
            }
#endif
        }

#if UNITY_EDITOR
        static void NormalizeEnemyCombatDefaults(List<StoryChapterEntry> chapters)
        {
            if (chapters == null) return;
            foreach (var chapter in chapters)
                NormalizeEnemyCombatDefaults(chapter?.stages);
        }

        static void NormalizeEnemyCombatDefaults(List<StageEntry> stages)
        {
            if (stages == null) return;
            foreach (var stage in stages)
            {
                if (stage == null) continue;
                NormalizeEnemyCombatDefaults(stage.enemies);
                NormalizeEnemyCombatDefaults(stage.boss);
                if (stage.battlePhases == null) continue;
                foreach (var phase in stage.battlePhases)
                {
                    if (phase == null) continue;
                    NormalizeEnemyCombatDefaults(phase.enemies);
                    NormalizeEnemyCombatDefaults(phase.boss);
                }
            }
        }

        static void NormalizeEnemyCombatDefaults(List<BattleUnitDefinition> enemies)
        {
            if (enemies == null) return;
            foreach (var enemy in enemies)
                NormalizeEnemyCombatDefaults(enemy);
        }

        static void NormalizeEnemyCombatDefaults(BattleUnitDefinition enemy)
        {
            if (enemy == null) return;

            var sprites = DefaultEnemySprites(enemy.id, enemy.displayName);
            if (sprites.Count > 0)
            {
                if (enemy.battlefieldSprite == null)
                    enemy.battlefieldSprite = sprites[0];
                if (enemy.portrait == null)
                    enemy.portrait = sprites[0];
                if (enemy.attackFrame1 == null)
                    enemy.attackFrame1 = sprites[0];
                if (enemy.attackFrame2 == null && sprites.Count > 1)
                    enemy.attackFrame2 = sprites[1];
            }

            if (enemy.attackEffectPrefabs == null || enemy.attackEffectPrefabs.Count == 0)
            {
                enemy.attackEffectPrefabs = new List<GameObject>();
                foreach (var path in DefaultEnemyAttackEffectPaths(enemy.id, enemy.displayName, enemy.element))
                {
                    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null && !enemy.attackEffectPrefabs.Contains(prefab))
                        enemy.attackEffectPrefabs.Add(prefab);
                }
            }

            if (enemy.attackEffectScale == Vector3.zero)
                enemy.attackEffectScale = Vector3.one;
        }

        static List<Sprite> DefaultEnemySprites(string id, string displayName)
        {
            var key = ((id ?? string.Empty) + " " + (displayName ?? string.Empty)).ToLowerInvariant();
            var path = "Assets/Art Ui/Game Việt hóa mới/enemy/sprite-sheet-2frames (2).png";
            if (key.Contains("fire") || key.Contains("flame") || key.Contains("lửa") || key.Contains("lua") || key.Contains("thú lửa"))
                path = "Assets/Art Ui/Game Việt hóa mới/enemy/sprite-sheet-2frames (5).png";
            if (key.Contains("blue") || key.Contains("wisp") || key.Contains("linh hồn") || key.Contains("linh hon"))
                path = "Assets/Art Ui/Game Việt hóa mới/enemy/sprite-sheet-2frames (3).png";
            if (key.Contains("coffin") || key.Contains("sarcophagus") || key.Contains("quan tài") || key.Contains("quan tai"))
                path = "Assets/Art Ui/Game Việt hóa mới/enemy/sprite-sheet-2frames (4).png";
            if (key.Contains("sand") || key.Contains("cát") || key.Contains("cat") || key.Contains("xoáy") || key.Contains("xoay"))
                path = "Assets/Art Ui/Game Việt hóa mới/enemy/sprite-sheet-2frames (2).png";

            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new List<Sprite>();
            foreach (var asset in assets)
                if (asset is Sprite sprite)
                    sprites.Add(sprite);
            sprites.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            if (sprites.Count > 2)
                sprites.RemoveRange(2, sprites.Count - 2);
            return sprites;
        }

        static string[] DefaultEnemyAttackEffectPaths(string id, string displayName, string element)
        {
            var key = ((id ?? string.Empty) + " " + (displayName ?? string.Empty) + " " + (element ?? string.Empty)).ToLowerInvariant();
            if (key.Contains("sand") || key.Contains("cát") || key.Contains("cat") || key.Contains("xoáy") || key.Contains("xoay"))
                return new[]
                {
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Battle_Effect_Yellow.prefab",
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Battle_Effect_White.prefab"
                };
            if (key.Contains("coffin") || key.Contains("sarcophagus") || key.Contains("quan tài") || key.Contains("quan tai"))
                return new[]
                {
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Battle_Effect_Yellow.prefab",
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_1_Woa_Yellow.prefab"
                };
            if (key.Contains("blue") || key.Contains("wisp") || key.Contains("linh hồn") || key.Contains("linh hon") || key.Contains("thủy") || key.Contains("thuy"))
                return new[]
                {
                    "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Ice/CFXR3 Hit Ice B (Air).prefab",
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_2_Bomb_Blue.prefab"
                };
            if (key.Contains("fire") || key.Contains("flame") || key.Contains("lửa") || key.Contains("lua") || key.Contains("hỏa") || key.Contains("hoa"))
                return new[]
                {
                    "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR3 Hit Fire B (Air).prefab",
                    "Assets/CartoonVFX9x/Comic_FX/Prefabs/Explosion_2_Bomb_Red.prefab"
                };
            return new[]
            {
                "Assets/CartoonVFX9x/Comic_FX/Prefabs/Battle_Effect_Yellow.prefab",
                "Assets/CartoonVFX9x/Comic_FX/Prefabs/Battle_Effect_White.prefab"
            };
        }
#endif

        public void EnsureDefaultPlayModeStages()
        {
            if (resourceStages == null) resourceStages = new List<StageEntry>();
            if (sanctumStages == null) sanctumStages = new List<StageEntry>();
            if (weaponStages == null) weaponStages = new List<StageEntry>();
            if (playModeStageGroups == null) playModeStageGroups = new List<PlayModeStageGroup>();

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

            EnsureResonanceSanctumPlayModeStages();
            EnsureDivineRemnantPlayModeStages();
            EnsureFuturePlayModeGroups();
            NormalizeEnemyCombatDefaults();
        }

        void EnsureFuturePlayModeGroups()
        {
            EnsurePlayModeStageGroup(
                "arena_of_lost_echoes",
                "Arena of Lost Echoes",
                "Nhóm Play Mode Content_1_Arena of Lost Echoes. Thêm các tab phụ/map sau trong Unity.");
            EnsurePlayModeStageGroup(
                "rift_of_the_hunt",
                "Rift of the Hunt",
                "Nhóm Play Mode Content_2_Rift of the Hunt. Thêm các tab phụ/map sau trong Unity.");
        }

        void EnsureResonanceSanctumPlayModeStages()
        {
            var lostEchoes = EnsurePlayModeStageGroup(
                "resonance_sanctum_lost_echoes",
                "Sanctum of Lost Echoes",
                "Nhóm ải thánh tích. UI map nằm dưới Content_0_Resonance Sanctum/TabList_0_Sanctum of Lost Echoes.");
            EnsurePlayModeStage(lostEchoes, CreatePlayModeStage(
                "LostEchoAchievement_1", "Tuyệt Vực Tàn Chiếu", "Ải Lost Echoes 1. Reward UI lấy từ relicslot_00-03, nút vào trận là EnergyCostBg.", "resonance_sanctum_lost_echoes", 3, 30,
                NewRewards("lost_echo_relic_1", "lost_echo_relic_2", "lost_echo_relic_3", "lost_echo_relic_4"),
                NewEnemy("sand_wisp_lost_echo_1a", "Cát Xoáy Sa Mạc", 760, 118, 52, 14),
                NewEnemy("fire_wisp_lost_echo_1b", "Lửa Linh Hồn", 700, 24, 58, 10),
                NewEnemy("flame_beast_lost_echo_1c", "Thú Lửa Nhỏ", 660, 110, 42, 15)));
            EnsurePlayModeStage(lostEchoes, CreatePlayModeStage(
                "LostEchoAchievement_2", "Dư Âm Tàn Khắc", "Ải Lost Echoes 2. Reward UI lấy từ relicslot_00-03, nút vào trận là EnergyCostBg.", "resonance_sanctum_lost_echoes", 5, 30,
                NewRewards("lost_echo_relic_5", "lost_echo_relic_6", "lost_echo_relic_7", "lost_echo_relic_8"),
                NewEnemy("sand_wisp_lost_echo_2a", "Cát Xoáy Sa Mạc", 900, 140, 66, 15),
                NewEnemy("flame_beast_lost_echo_2b", "Thú Lửa Nhỏ", 760, 132, 50, 16),
                NewEnemy("fire_wisp_lost_echo_2c", "Lửa Linh Hồn", 820, 30, 70, 10)));
            EnsurePlayModeStage(lostEchoes, CreatePlayModeStage(
                "LostEchoAchievement_3", "Xích Minh Đế Chủ", "Ải Lost Echoes 3. Reward UI lấy từ relicslot_00-03, nút vào trận là EnergyCostBg.", "resonance_sanctum_lost_echoes", 8, 30,
                NewRewards("lost_echo_relic_9", "lost_echo_relic_10", "lost_echo_relic_11", "lost_echo_relic_12"),
                NewEnemy("sand_wisp_lost_echo_3a", "Cát Xoáy Sa Mạc", 1080, 166, 82, 15),
                NewEnemy("sand_wisp_lost_echo_3b", "Cát Xoáy Sa Mạc", 1080, 166, 82, 15),
                NewEnemy("flame_beast_lost_echo_3c", "Thú Lửa Nhỏ", 920, 158, 64, 17)));
            EnsurePlayModeStage(lostEchoes, CreatePlayModeStage(
                "LostEchoAchievement_4", "Thánh Trì Vĩnh Mộc", "Ải Lost Echoes 4. Reward UI lấy từ relicslot_00-03, nút vào trận là EnergyCostBg.", "resonance_sanctum_lost_echoes", 10, 30,
                NewRewards("lost_echo_relic_13", "lost_echo_relic_14", "lost_echo_relic_15", "lost_echo_relic_16"),
                NewEnemy("flame_beast_lost_echo_4a", "Thú Lửa Nhỏ", 1060, 184, 74, 18),
                NewEnemy("sand_wisp_lost_echo_4b", "Cát Xoáy Sa Mạc", 1240, 186, 96, 16),
                NewEnemy("fire_wisp_lost_echo_4c", "Lửa Linh Hồn", 1120, 40, 98, 11)));

            var ascension = EnsurePlayModeStageGroup(
                "resonance_sanctum_ascension",
                "Sanctum of Ascension",
                "Nhóm ải đột phá nhân vật. UI map nằm dưới TabList_1_Sanctum of Ascension.");
            EnsureNumberedDomainStages(ascension, "ascensionDomain_", 4, "Đột Phá Nhân Vật", "ascension_material", 4, 30, "resonance_sanctum_ascension");

            var insight = EnsurePlayModeStageGroup(
                "resonance_sanctum_insight",
                "Sanctum of Insight",
                "Nhóm ải nâng cấp kỹ năng. UI map nằm dưới TabList_2_Sanctum of Insight.");
            EnsureNumberedDomainStages(insight, "insightDomain_", 3, "Minh Triết Lý Luận", "insight_scroll", 4, 30, "resonance_sanctum_insight");

            var forging = EnsurePlayModeStageGroup(
                "resonance_sanctum_forging",
                "Sanctum of Forging",
                "Nhóm ải đột phá vũ khí. UI map nằm dưới TabList_3_Sanctum of Forging.");
            EnsureNumberedDomainStages(forging, "AssignableListEntry_", 4, "Đột Phá Vũ Khí", "forging_material", 4, 30, "resonance_sanctum_forging", zeroBasedIds: true);
        }

        void EnsureDivineRemnantPlayModeStages()
        {
            var divineRemnant = EnsurePlayModeStageGroup(
                "divine_remnant",
                "Divine Remnant",
                "Nhóm ải Divine Remnant. UI map nằm dưới Content_3_Divine Remnant.");
            EnsurePlayModeStage(divineRemnant, CreatePlayModeStage(
                "EnemySection_1", "Di Tích Thần Vị", "Ải Divine Remnant 1. Reward UI lấy từ DropSlot_00-02, nút vào trận là PlayButton.", "divine_remnant", 12, 20,
                NewRewards("divine_remnant_core", "divine_remnant_fragment", "coins"),
                NewEnemy("sand_wisp_divine_1a", "Cát Xoáy Sa Mạc", 1320, 190, 106, 16),
                NewEnemy("flame_beast_divine_1b", "Thú Lửa Nhỏ", 1180, 196, 84, 18),
                NewEnemy("fire_wisp_divine_1c", "Lửa Linh Hồn", 1240, 48, 112, 11)));
        }

        void EnsureNumberedDomainStages(PlayModeStageGroup group, string idPrefix, int count, string titlePrefix, string rewardPrefix, int rewardCount, int energyCost, string playModeType, bool zeroBasedIds = false)
        {
            for (var i = 0; i < count; i++)
            {
                var displayIndex = i + 1;
                var idIndex = zeroBasedIds ? i : displayIndex;
                EnsurePlayModeStage(group, CreatePlayModeStage(
                    $"{idPrefix}{idIndex}",
                    $"{titlePrefix} {displayIndex}",
                    $"{titlePrefix} {displayIndex}. Reward UI lấy từ bg/RewardSlot_00-03, nút vào trận là EnterButton.",
                    playModeType,
                    2 + displayIndex * 2,
                    energyCost,
                    NewRewards(rewardPrefix, displayIndex, rewardCount),
                    NewEnemy($"sand_wisp_{rewardPrefix}_{displayIndex}_a", "Cát Xoáy Sa Mạc", 640 + displayIndex * 180, 96 + displayIndex * 24, 40 + displayIndex * 14, 13 + displayIndex / 2),
                    NewEnemy($"flame_beast_{rewardPrefix}_{displayIndex}_b", "Thú Lửa Nhỏ", 560 + displayIndex * 160, 92 + displayIndex * 25, 34 + displayIndex * 12, 14 + displayIndex),
                    NewEnemy($"fire_wisp_{rewardPrefix}_{displayIndex}_c", "Lửa Linh Hồn", 620 + displayIndex * 170, 18 + displayIndex * 5, 46 + displayIndex * 15, 9 + displayIndex / 2)));
            }
        }

        PlayModeStageGroup EnsurePlayModeStageGroup(string id, string title, string description)
        {
            var group = playModeStageGroups.Find(x => x != null && id.Equals(x.id, StringComparison.OrdinalIgnoreCase));
            if (group != null) return group;
            group = new PlayModeStageGroup
            {
                id = id,
                title = title,
                description = description,
                stages = new List<StageEntry>()
            };
            playModeStageGroups.Add(group);
            return group;
        }

        static void EnsurePlayModeStage(PlayModeStageGroup group, StageEntry stage)
        {
            if (group == null || stage == null || string.IsNullOrWhiteSpace(stage.id)) return;
            group.stages ??= new List<StageEntry>();
            if (group.stages.Exists(x => x != null && stage.id.Equals(x.id, StringComparison.OrdinalIgnoreCase)))
                return;
            group.stages.Add(stage);
        }

        static StageEntry CreatePlayModeStage(string id, string title, string description, string playModeType, int level, int energyCost, List<RewardEntry> rewards, params BattleUnitDefinition[] enemies)
        {
            return new StageEntry
            {
                id = id,
                title = title,
                playModeType = playModeType,
                description = description,
                energyCost = Mathf.Max(0, energyCost),
                enemyLevel = Mathf.Max(1, level),
                rewards = rewards ?? new List<RewardEntry>(),
                enemies = new List<BattleUnitDefinition>(enemies ?? Array.Empty<BattleUnitDefinition>())
            };
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

        static List<RewardEntry> NewRewards(params string[] ids)
        {
            var rewards = new List<RewardEntry>();
            if (ids == null) return rewards;
            for (var i = 0; i < ids.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(ids[i])) continue;
                rewards.Add(NewReward(ids[i], ids[i].Equals("coins", StringComparison.OrdinalIgnoreCase) ? 1000 : 1, Mathf.Clamp(i + 2, 2, 5)));
            }
            return rewards;
        }

        static List<RewardEntry> NewRewards(string rewardPrefix, int stageIndex, int count)
        {
            var rewards = new List<RewardEntry>();
            for (var i = 0; i < count; i++)
            {
                var rewardId = $"{rewardPrefix}_{stageIndex}_{i + 1}";
                rewards.Add(new RewardEntry
                {
                    id = rewardId,
                    amount = 1,
                    minAmount = 1,
                    maxAmount = stageIndex >= 3 ? 2 : 1,
                    dropChancePercent = i == 0 ? 100 : Mathf.Clamp(80 - i * 10, 35, 100),
                    guaranteed = i == 0,
                    rarity = Mathf.Clamp(stageIndex + i, 2, 5)
                });
            }
            return rewards;
        }

        public List<StageEntry> GetPlayModeStages(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return resourceStages;
            groupId = NormalizePlayModeStageGroupId(groupId);

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

        static string NormalizePlayModeStageGroupId(string groupId)
        {
            var value = groupId?.Trim() ?? string.Empty;
            if (value.Equals("TabList_0_Sanctum of Lost Echoes", StringComparison.OrdinalIgnoreCase))
                return "resonance_sanctum_lost_echoes";
            if (value.Equals("TabList_1_Sanctum of Ascension", StringComparison.OrdinalIgnoreCase))
                return "resonance_sanctum_ascension";
            if (value.Equals("TabList_2_Sanctum of Insight", StringComparison.OrdinalIgnoreCase))
                return "resonance_sanctum_insight";
            if (value.Equals("TabList_3_Sanctum of Forging", StringComparison.OrdinalIgnoreCase))
                return "resonance_sanctum_forging";
            if (value.Equals("Content_1_Arena of Lost Echoes", StringComparison.OrdinalIgnoreCase))
                return "arena_of_lost_echoes";
            if (value.Equals("Content_2_Rift of the Hunt", StringComparison.OrdinalIgnoreCase))
                return "rift_of_the_hunt";
            if (value.Equals("Content_3_Divine Remnant", StringComparison.OrdinalIgnoreCase))
                return "divine_remnant";
            return value;
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

