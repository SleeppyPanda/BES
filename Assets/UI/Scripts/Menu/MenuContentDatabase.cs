using System;
using System.Collections.Generic;
using UnityEngine;

namespace BES.UI.Menu
{
    public enum MenuScreenId { Home, StoryParty, ResourceStages, SanctumRelics, WeaponBreakthrough, Battle, Dialogue, Management, CashShop, BattlePass }

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
    }

    [Serializable] public class RewardEntry { public string id; public Sprite icon; public int amount = 1; public int rarity = 1; }

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
        PhaseVictory
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
        public CharacterEntry FindCharacter(string id) => characters.Find(x => x.id == id);
    }
}
