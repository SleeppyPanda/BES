#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class CurrencyLayoutMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        [MenuItem("BES/UI/Configure Six Currency Buttons")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var shop = Find(root.transform, "CashShopPanel");
                var header = FindDirect(shop, "ShopHeader");
                var shopController = shop != null
                    ? shop.GetComponent<CashShopPanelController>()
                    : null;
                var homeController =
                    root.GetComponentInChildren<MenuHomeController>(true);
                if (header == null || shopController == null || homeController == null)
                    return;

                var coins = FindDirect(root.transform, "Currency_coins");
                var gems = FindDirect(root.transform, "Currency_gems");
                var energy = FindDirect(root.transform, "Currency_energy");
                var apple = EnsureClone(energy, root.transform, "Currency_crystal_apple");

                var shopCoins = KeepSingle(header, "Currency_coins");
                var shopGems = KeepSingle(header, "Currency_gems");
                var shopEnergy = KeepSingle(header, "Currency_energy");
                var bean = FindDirect(header, "Currency_bean") ?? shopEnergy;
                if (bean != null) bean.name = "Currency_bean";
                var goldenBean = EnsureClone(bean, header, "Currency_golden_bean");
                OffsetNewCurrency(bean, goldenBean);

                RemoveExtraNamed(header, "Currency_energy");
                RemoveDuplicates(header, "Currency_coins");
                RemoveDuplicates(header, "Currency_gems");
                RemoveDuplicates(header, "Currency_bean");
                RemoveDuplicates(header, "Currency_golden_bean");

                ConfigureRouter(FindDirect(coins, "Add"), shopController, 0, 2);
                ConfigureRouter(FindDirect(gems, "Add"), shopController, 3, -1);
                ConfigureRouter(FindDirect(energy, "Add"), shopController, 2, 0);
                ConfigureRouter(FindDirect(apple, "Add"), shopController, 1, 0);
                ConfigureRouter(shopCoins, shopController, 0, 2);
                ConfigureRouter(shopGems, shopController, 3, -1);
                ConfigureRouter(bean, shopController, 0, 0);
                ConfigureRouter(goldenBean, shopController, 0, 2);

                WireHomeCurrencies(homeController, energy, gems, coins, apple);
                WireShopCurrencies(
                    shopController,
                    shopCoins,
                    shopGems,
                    bean,
                    goldenBean);
                WireVisibility(
                    root,
                    coins,
                    gems,
                    apple,
                    energy,
                    shop,
                    Find(root.transform, "HomePanel"),
                    Find(root.transform, "PlayModePanel"),
                    Find(root.transform, "StoryModePanel"),
                    Find(root.transform, "BattlePanel"));

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Six currency displays configured and routed to Cash Shop destinations.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void WireHomeCurrencies(
            MenuHomeController controller,
            Transform energy,
            Transform gems,
            Transform coins,
            Transform apple)
        {
            var serialized = new SerializedObject(controller);
            var list = serialized.FindProperty("currencies");
            list.arraySize = 4;
            SetHomeView(list.GetArrayElementAtIndex(0), "energy", energy);
            SetHomeView(list.GetArrayElementAtIndex(1), "gems", gems);
            SetHomeView(list.GetArrayElementAtIndex(2), "coins", coins);
            SetHomeView(list.GetArrayElementAtIndex(3), "crystal_apple", apple);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetHomeView(
            SerializedProperty entry,
            string id,
            Transform currency)
        {
            entry.FindPropertyRelative("currencyId").stringValue = id;
            entry.FindPropertyRelative("background").objectReferenceValue =
                currency != null ? currency.GetComponent<Image>() : null;
            entry.FindPropertyRelative("icon").objectReferenceValue =
                FindDirect(currency, "Icon")?.GetComponent<Image>();
            entry.FindPropertyRelative("amountText").objectReferenceValue =
                FindDirect(currency, "Amount")?.GetComponent<TMP_Text>();
            entry.FindPropertyRelative("addButton").objectReferenceValue =
                FindDirect(currency, "Add")?.GetComponent<Button>();
        }

        static void WireShopCurrencies(
            CashShopPanelController controller,
            Transform coins,
            Transform gems,
            Transform bean,
            Transform goldenBean)
        {
            var serialized = new SerializedObject(controller);
            var list = serialized.FindProperty("currencies");
            list.arraySize = 4;
            SetShopView(list.GetArrayElementAtIndex(0), "coins", coins);
            SetShopView(list.GetArrayElementAtIndex(1), "gems", gems);
            SetShopView(list.GetArrayElementAtIndex(2), "bean", bean);
            SetShopView(list.GetArrayElementAtIndex(3), "golden_bean", goldenBean);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetShopView(
            SerializedProperty entry,
            string id,
            Transform currency)
        {
            entry.FindPropertyRelative("currencyId").stringValue = id;
            entry.FindPropertyRelative("icon").objectReferenceValue =
                FindDirect(currency, "Icon")?.GetComponent<Image>();
            entry.FindPropertyRelative("amountText").objectReferenceValue =
                FindDirect(currency, "Amount")?.GetComponent<TMP_Text>();
        }

        static void WireVisibility(
            GameObject root,
            Transform coins,
            Transform gems,
            Transform apple,
            Transform energy,
            Transform shop,
            Transform home,
            Transform play,
            Transform story,
            Transform battle)
        {
            var controller =
                root.GetComponent<CurrencyVisibilityController>() ??
                root.AddComponent<CurrencyVisibilityController>();
            var serialized = new SerializedObject(controller);
            Set(serialized, "coins", coins);
            Set(serialized, "gems", gems);
            Set(serialized, "crystalApple", apple);
            Set(serialized, "energy", energy);
            Set(serialized, "homePanel", home);
            Set(serialized, "playModePanel", play);
            Set(serialized, "storyModePanel", story);
            Set(serialized, "cashShopPanel", shop);
            Set(serialized, "battlePanel", battle);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(
            SerializedObject serialized,
            string property,
            Transform value) =>
            serialized.FindProperty(property).objectReferenceValue =
                value != null ? value.gameObject : null;

        static Transform EnsureClone(
            Transform source,
            Transform parent,
            string name)
        {
            var existing = FindDirect(parent, name);
            if (existing != null) return existing;
            if (source == null) return null;
            var clone = Object.Instantiate(source.gameObject, parent, false);
            clone.name = name;
            clone.transform.SetSiblingIndex(source.GetSiblingIndex() + 1);
            clone.transform.localScale = Vector3.one;
            return clone.transform;
        }

        static void OffsetNewCurrency(Transform source, Transform clone)
        {
            if (source == null || clone == null || source == clone) return;
            var sourceRect = source as RectTransform;
            var cloneRect = clone as RectTransform;
            if (sourceRect == null || cloneRect == null) return;
            if ((cloneRect.anchoredPosition - sourceRect.anchoredPosition).sqrMagnitude > .01f)
                return;
            cloneRect.anchoredPosition +=
                Vector2.right * (Mathf.Abs(sourceRect.rect.width) + 16f);
        }

        static void ConfigureRouter(
            Transform target,
            CashShopPanelController shop,
            int main,
            int sub)
        {
            if (target == null) return;
            var button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
            if (button.targetGraphic == null)
                button.targetGraphic = target.GetComponent<Graphic>();
            var router =
                target.GetComponent<ShopTabOpenButton>() ??
                target.gameObject.AddComponent<ShopTabOpenButton>();
            router.Configure(shop, main, sub);
        }

        static Transform KeepSingle(Transform parent, string name)
        {
            Transform result = null;
            foreach (Transform child in parent)
            {
                if (child.name != name) continue;
                if (result == null) result = child;
            }
            return result;
        }

        static void RemoveDuplicates(Transform parent, string name)
        {
            var found = false;
            var remove = new List<GameObject>();
            foreach (Transform child in parent)
            {
                if (child.name != name) continue;
                if (!found) found = true;
                else remove.Add(child.gameObject);
            }
            foreach (var target in remove) Object.DestroyImmediate(target);
        }

        static void RemoveExtraNamed(Transform parent, string name)
        {
            var remove = new List<GameObject>();
            foreach (Transform child in parent)
                if (child.name == name) remove.Add(child.gameObject);
            foreach (var target in remove) Object.DestroyImmediate(target);
        }

        static Transform FindDirect(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
                if (child.name == name) return child;
            return null;
        }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
#endif
