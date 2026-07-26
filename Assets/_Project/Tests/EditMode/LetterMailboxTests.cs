using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;

namespace BES.Tests.EditMode
{
    public class LetterMailboxTests
    {
        [Test]
        public void DefaultMailboxContainsReadableLetters()
        {
            var database = CreateScriptable("BES.UI.LetterDatabase");
            Invoke(database, "ResetToDefaultEntries");
            var letters = GetReadOnlyList(database, "Letters");
            var first = letters[0];

            Assert.That(letters.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(GetField<string>(first, "senderName"), Is.Not.Empty);
            Assert.That(GetField<string>(first, "title"), Does.Contain("Welcome"));
            Assert.That(GetField<bool>(first, "isRead"), Is.False);
        }

        [Test]
        public void MarkReadUpdatesKnownLetterOnly()
        {
            var database = CreateScriptable("BES.UI.LetterDatabase");
            Invoke(database, "ResetToDefaultEntries");
            var letters = GetReadOnlyList(database, "Letters");
            var letterId = GetField<string>(letters[0], "letterId");

            Assert.That(Invoke<bool>(database, "MarkRead", letterId), Is.True);
            Assert.That(GetField<bool>(letters[0], "isRead"), Is.True);
            Assert.That(Invoke<bool>(database, "MarkRead", "missing_letter"), Is.False);
        }

        [Test]
        public void InventoryRowsUseItemDefinitionWhenAvailable()
        {
            var item = CreateScriptable("BES.Gameplay.ItemDefinition");
            SetField(item, "itemId", "potion_heal");
            SetField(item, "displayName", "Healing Potion");
            SetField(item, "rarity", 2);
            var inventoryUi = GetType("BES.UI.InventoryUI");
            var format = inventoryUi.GetMethod("FormatItemLabel", BindingFlags.Public | BindingFlags.Static);

            Assert.That(format.Invoke(null, new object[] { "potion_heal", 3, item }), Is.EqualTo("Healing Potion x3"));
            Assert.That(format.Invoke(null, new object[] { "unknown_item", 2, null }), Is.EqualTo("unknown_item x2"));
        }

        [Test]
        public void CashShopCatalogProvidesExpectedTabsAndProducts()
        {
            var catalog = GetType("BES.UI.CashShopCatalog");
            var tabType = GetType("BES.UI.CashShopTab");
            var diamond = Enum.Parse(tabType, "DiamondPurchase");
            var tidal = Enum.Parse(tabType, "TidalPeaExchange");

            var productsFor = catalog.GetMethod("ProductsFor", BindingFlags.Public | BindingFlags.Static);
            var titleFor = catalog.GetMethod("TitleFor", BindingFlags.Public | BindingFlags.Static);

            var diamondProducts = (System.Collections.IList)productsFor.Invoke(null, new[] { diamond });
            var tidalProducts = (System.Collections.IList)productsFor.Invoke(null, new[] { tidal });

            Assert.That(titleFor.Invoke(null, new[] { diamond }), Is.EqualTo("Diamond Purchase"));
            Assert.That(diamondProducts.Count, Is.EqualTo(6));
            Assert.That(GetField<string>(diamondProducts[0], "spriteName"), Is.EqualTo("Group 427322977"));
            Assert.That(tidalProducts.Count, Is.EqualTo(8));
        }

        [Test]
        public void MissionCatalogProvidesFiveDefaultMissions()
        {
            var catalog = GetType("BES.UI.MissionCatalog");
            var defaultMissions = catalog.GetMethod("DefaultMissions", BindingFlags.Public | BindingFlags.Static);
            var missions = (System.Collections.IList)defaultMissions.Invoke(null, null);

            Assert.That(missions.Count, Is.EqualTo(5));
            Assert.That(GetField<string>(missions[2], "cardSpriteName"), Is.EqualTo("Group 427323033"));
            Assert.That(GetField<bool>(missions[0], "claimable"), Is.True);
        }

        static ScriptableObject CreateScriptable(string typeName) =>
            ScriptableObject.CreateInstance(GetType(typeName));

        static Type GetType(string typeName) =>
            Type.GetType($"{typeName}, Assembly-CSharp", throwOnError: true);

        static void Invoke(object target, string methodName, params object[] args) =>
            target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance).Invoke(target, args);

        static T Invoke<T>(object target, string methodName, params object[] args) =>
            (T)target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance).Invoke(target, args);

        static System.Collections.IList GetReadOnlyList(object target, string propertyName) =>
            (System.Collections.IList)target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).GetValue(target);

        static T GetField<T>(object target, string fieldName) =>
            (T)target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance).GetValue(target);

        static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance).SetValue(target, value);
        }
    }
}
