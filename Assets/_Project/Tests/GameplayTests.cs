using BES.Gameplay;
using BES.Narrative;
using BES.UI;
using NUnit.Framework;

namespace BES.Tests
{
    public class DamageCalculatorTests
    {
        [Test]
        public void Calculate_ReturnsAtLeastOneDamage()
        {
            var damage = DamageCalculator.Calculate(10f, 100f, 0f, 1.5f, out _);
            Assert.GreaterOrEqual(damage, 1f);
        }

        [Test]
        public void Calculate_CritIncreasesDamage()
        {
            var normal = DamageCalculator.Calculate(20f, 0f, 0f, 2f, out _);
            var crit = DamageCalculator.Calculate(20f, 0f, 1f, 2f, out var isCrit);
            Assert.IsTrue(isCrit);
            Assert.Greater(crit, normal);
        }
    }

    public class SaveDataUtilityTests
    {
        [Test]
        public void ToPairs_AndFromPairs_PreservesData()
        {
            var dict = new System.Collections.Generic.Dictionary<string, int>
            {
                { "herb_common", 3 },
                { "coin_silver", 10 }
            };

            var pairs = SaveDataUtility.ToPairs(dict);
            var restored = SaveDataUtility.FromPairs(pairs);

            Assert.AreEqual(3, restored["herb_common"]);
            Assert.AreEqual(10, restored["coin_silver"]);
        }
    }

    public class QuestChoiceBranchTests
    {
        [Test]
        public void ChoiceStep_AcceptsBranchIds_WhenTargetIsBranchChoice()
        {
            var go = new UnityEngine.GameObject("QuestMgr");
            var manager = go.AddComponent<QuestManager>();

            var quest = UnityEngine.ScriptableObject.CreateInstance<QuestDefinition>();
            quest.questId = "test_quest";
            quest.steps = new System.Collections.Generic.List<QuestStep>
            {
                new() { stepType = QuestStepType.Choice, targetId = "branch_choice", description = "Pick" }
            };

            var field = typeof(QuestManager).GetField("questDefinitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(manager, new System.Collections.Generic.List<QuestDefinition> { quest });
            manager.GetType().GetMethod("Awake",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(manager, null);

            manager.StartQuest("test_quest");
            Assert.IsTrue(manager.TryAdvanceCurrentStep("test_quest", QuestStepType.Choice, "branch_a"));
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(quest);
        }
    }

    public class GachaPityTests
    {
        [Test]
        public void HardPity_ForcesFiveStarThreshold()
        {
            var go = new UnityEngine.GameObject("Pity");
            var pity = go.AddComponent<GachaPityState>();
            for (var i = 0; i < GachaPityState.HardPity - 1; i++)
                pity.RegisterPull(3);
            Assert.IsTrue(pity.ShouldForceFiveStar());
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    public class SaveSessionTests
    {
        [Test]
        public void NewSaveData_HasDefaultSessionFields()
        {
            var data = new SaveData();
            Assert.AreEqual(0, data.activeCharacterIndex);
            Assert.AreEqual(0, data.gachaPullsSinceFiveStar);
        }
    }
}
