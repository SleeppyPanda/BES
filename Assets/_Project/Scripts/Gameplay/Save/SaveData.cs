using System;
using System.Collections.Generic;

namespace BES.Gameplay
{
    [Serializable]
    public class StringIntPair
    {
        public string key;
        public int value;
    }

    [Serializable]
    public class StringListPair
    {
        public string key;
        public List<string> values = new();
    }

    [Serializable]
    public class StringStringPair
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class WeaponRandomStatSaveData
    {
        public int statType;
        public string displayName;
        public bool isPercent;
        public float value;
    }

    [Serializable]
    public class WeaponInstanceSaveData
    {
        public string instanceId;
        public string weaponId;
        public int level = 1;
        public int experience;
        public int refinement = 1;
        public List<WeaponRandomStatSaveData> randomStats = new();
    }

    [Serializable]
    public class SaveData
    {
        public string saveVersion = "1.0";
        public float playerHealth = 100f;
        public float playerMana = 100f;
        public float playerStamina = 100f;
        public string currentRegionId = "region_creation_city";
        public float playerPosX;
        public float playerPosY;
        public float playerPosZ;
        public List<string> activeQuestIds = new();
        public List<string> completedQuestIds = new();
        public string trackedQuestId = string.Empty;
        public List<StringIntPair> inventory = new();
        public List<StringIntPair> menuCurrencies = new();
        public List<StringIntPair> relationships = new();
        public List<StringListPair> npcMemories = new();
        public List<string> collectedItemIds = new();
        public List<StringIntPair> questStepProgress = new();
        public string equippedWeaponId = "weapon_iron_sword";
        public List<string> ownedWeaponIds = new();
        public string equippedWeaponInstanceId = string.Empty;
        public List<WeaponInstanceSaveData> ownedWeaponInstances = new();
        public List<StringStringPair> characterEquippedWeaponInstanceIds = new();
        public int weaponLevel = 1;
        public int weaponExperience = 0;
        public int weaponRefinement = 1;
        public int gems = 99999;
        public int coins = 99999;
        public int eventStreakDay;
        public List<int> eventClaimedDays = new();
        public List<string> claimedLetterIds = new();
        public List<string> claimedMissionIds = new();
        public List<string> purchasedShopItemIds = new();
        public List<string> partySlotIds = new();
        public List<StringIntPair> partyHealth = new();
        public List<StringIntPair> partyMaxHealth = new();
        public List<string> unlockedCharacterIds = new();
        public List<string> unlockedTeleportIds = new();
        public List<string> discoveredRegionIds = new();
        public List<string> collectedWorldObjectIds = new();
        public string equippedArtifactId = string.Empty;
        public string storyBranch = "main";
        public int storyProgressIndex;
        public int storyChapterIndex;
        public int storyStageIndex;
        public string activeStoryStageId = string.Empty;
        public List<string> storyPartyCharacterIds = new();
        public string activeBattleStageId = string.Empty;
        public bool activeBattleIsPlayMode;
        public string activePlayModeStageGroupId = string.Empty;
        public string endingId = string.Empty;
        public int gachaPullsSinceFiveStar;
        public int gachaPullsSinceFiveStarWeapon;
        public int consecutiveOffRates;
        public int stardust;
        public int activeCharacterIndex;
        public List<string> ownedArtifactIds = new();
        public List<StringIntPair> characterLevels = new();
        public List<StringIntPair> characterExperience = new();
        public List<StringIntPair> characterBreakthroughs = new();
        public List<StringIntPair> characterConstellations = new();
        public List<StringIntPair> characterConstellationShards = new();
        public List<StringIntPair> characterAffinity = new();
    }
}
