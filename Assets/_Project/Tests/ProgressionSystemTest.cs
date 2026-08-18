using UnityEngine;
using BES.UI;
using BES.Gameplay;

namespace BES.Tests
{
    public class ProgressionSystemTest : MonoBehaviour
    {
        [SerializeField] bool runOnStart = true;

        void Start()
        {
            if (runOnStart)
            {
                RunTestSequence();
            }
        }

        public void RunTestSequence()
        {
            Debug.Log("<color=cyan>====== STARTING PROGRESSION & UPGRADE SYSTEM TESTS =====</color>");

            // Test 1: Cumulative EXP Math Curves
            Assert(CharacterProgressionState.GetCumulativeExperience(1) == 0, "Test 1a: Level 1 should have 0 cumulative EXP");
            Assert(CharacterProgressionState.GetCumulativeExperience(20) == 1400, "Test 1b: Level 20 should have exactly 1400 cumulative EXP");
            Assert(CharacterProgressionState.GetCumulativeExperience(40) == 13500, "Test 1c: Level 40 should have exactly 13500 cumulative EXP");
            Assert(CharacterProgressionState.GetCumulativeExperience(60) == 38100, "Test 1d: Level 60 should have exactly 38100 cumulative EXP");
            Assert(CharacterProgressionState.GetCumulativeExperience(80) == 75000, "Test 1e: Level 80 should have exactly 75000 cumulative EXP");
            Debug.Log("✔ Test 1: Cumulative EXP Math Curves verified.");

            // Test 2: EXP per Level Curve Checks
            int sumExp = 0;
            for (int lvl = 1; lvl < 20; lvl++)
            {
                sumExp += CharacterProgressionState.GetExperienceToNextLevelForLevel(lvl);
            }
            Assert(sumExp == 1400, $"Test 2a: Sum of EXP levels 1-19 must be 1400. Got: {sumExp}");
            
            sumExp = 1400;
            for (int lvl = 20; lvl < 40; lvl++)
            {
                sumExp += CharacterProgressionState.GetExperienceToNextLevelForLevel(lvl);
            }
            Assert(sumExp == 13500, $"Test 2b: Sum of EXP levels 1-39 must be 13500. Got: {sumExp}");

            sumExp = 13500;
            for (int lvl = 40; lvl < 60; lvl++)
            {
                sumExp += CharacterProgressionState.GetExperienceToNextLevelForLevel(lvl);
            }
            Assert(sumExp == 38100, $"Test 2c: Sum of EXP levels 1-59 must be 38100. Got: {sumExp}");

            sumExp = 38100;
            for (int lvl = 60; lvl < 80; lvl++)
            {
                sumExp += CharacterProgressionState.GetExperienceToNextLevelForLevel(lvl);
            }
            Assert(sumExp == 75000, $"Test 2d: Sum of EXP levels 1-79 must be 75000. Got: {sumExp}");
            Debug.Log("✔ Test 2: EXP per Level curves verified.");

            // Test 3: Star-Based Level Caps
            var characterDatabase = CharacterDatabaseLoader.Load();
            var hero01 = characterDatabase?.Get("hero_01"); // 5-star character
            var hero02 = characterDatabase?.Get("hero_02"); // 4-star character

            if (hero01 != null && hero02 != null)
            {
                CharacterProgressionState.ResetAll();

                // Test 3a: 5-star caps
                Assert(CharacterProgressionState.GetLevelCap("hero_01") == 20, "Test 3a: 5-star breakthrough 0 level cap should be 20");
                CharacterProgressionState.AddExperience("hero_01", 10000); 
                Assert(CharacterProgressionState.GetLevel("hero_01") == 20, "Test 3b: 5-star should be locked at cap 20");

                if (PlayerWallet.Instance != null)
                {
                    PlayerWallet.Instance.LoadDefaults(); 
                    bool btSucceeded = CharacterProgressionState.TryBreakthrough("hero_01");
                    Assert(btSucceeded, "Test 3c: Breakthrough should succeed with default gold");
                    Assert(CharacterProgressionState.GetLevelCap("hero_01") == 40, "Test 3d: 5-star breakthrough 1 level cap should be 40");
                    Assert(PlayerWallet.Instance.Coins == 99999 - 3000, $"Test 3e: 3000 gold must be spent. Got: {PlayerWallet.Instance.Coins}");
                }

                // Test 3b: 4-star caps
                Assert(CharacterProgressionState.GetLevelCap("hero_02") == 20, "Test 3f: 4-star breakthrough 0 level cap should be 20");
            }
            else
            {
                Debug.LogWarning("Skipping Test 3: hero_01 or hero_02 not found in database.");
            }

            Debug.Log("<color=green>====== ALL PROGRESSION & UPGRADE SYSTEM TESTS PASSED SUCCESSFULLY! =====</color>");
        }

        private void Assert(bool condition, string errorMessage)
        {
            if (!condition)
            {
                Debug.LogError($"<color=red>[Assertion Failed] {errorMessage}</color>");
                throw new System.Exception(errorMessage);
            }
        }
    }
}
