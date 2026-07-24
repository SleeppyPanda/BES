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
        public List<StringIntPair> relationships = new();
        public List<StringListPair> npcMemories = new();
        public List<string> collectedItemIds = new();
        public List<StringIntPair> questStepProgress = new();
        public string equippedWeaponId = "weapon_iron_sword";
        public List<string> ownedWeaponIds = new();
        public int weaponLevel = 1;
        public int weaponRefinement = 1;
        public int gems = 1600;
        public int coins = 99999;
        public int eventStreakDay;
        public List<int> eventClaimedDays = new();
        public List<string> partySlotIds = new();
        public List<StringIntPair> partyHealth = new();
        public List<StringIntPair> partyMaxHealth = new();
        public List<string> unlockedCharacterIds = new();
        public List<string> unlockedTeleportIds = new();
        public List<string> discoveredRegionIds = new();
        public List<string> collectedWorldObjectIds = new();
        public string equippedArtifactId = string.Empty;
        public string storyBranch = "main";
        public string endingId = string.Empty;
        public int gachaPullsSinceFiveStar;
        public int stardust;
        public int activeCharacterIndex;
        public List<string> ownedArtifactIds = new();
    }
}
