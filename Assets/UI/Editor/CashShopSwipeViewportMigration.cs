#if UNITY_EDITOR
using BES.UI.Menu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    /// <summary>
    /// Assigns clipping only to real content frames. Header, currency, tab
    /// buttons and indicators remain outside their sub-content masks.
    /// </summary>
    public static class CashShopSwipeViewportMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        [MenuItem("BES/UI/Configure Cash Shop Swipe Viewports")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var shop = Find(root.transform, "CashShopPanel");
                var shopContents = FindDirect(shop, "ShopContents");
                var exchange = Find(shopContents, "ExchangeSubTabSystem");
                var pack = Find(shopContents, "PackSubTabSystem");
                var light = Find(shopContents, "LightSubTabSystem");

                Configure(
                    shopContents?.GetComponent<SmoothTabGroup>(),
                    shopContents as RectTransform);
                Configure(
                    exchange?.GetComponent<SmoothTabGroup>(),
                    FindDirect(exchange, "PackSubContents") as RectTransform);
                Configure(
                    pack?.GetComponent<SmoothTabGroup>(),
                    FindDirect(pack, "PackSubContents") as RectTransform);
                Configure(
                    light?.GetComponent<SmoothTabGroup>(),
                    FindDirect(light, "LightSubContents") as RectTransform);

                RemoveWrongMask(Find(shopContents, "MainContent_0_EXCHANGE_SHOP"));
                RemoveWrongMask(Find(shopContents, "MainContent_1_PACK_SHOP"));
                RemoveWrongMask(Find(shopContents, "MainContent_2_LIGHT_PURCHASE"));
                RemoveWrongMask(Find(shopContents, "MainContent_3_DIAMOND_PURCHASE"));

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Cash Shop swipe clipping is limited to ShopContents and each SubContents frame.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void Configure(SmoothTabGroup group, RectTransform viewport)
        {
            if (group == null || viewport == null) return;

            var serialized = new SerializedObject(group);
            serialized.FindProperty("viewport").objectReferenceValue = viewport;
            serialized.FindProperty("clipToViewport").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
        }

        static void RemoveWrongMask(Transform panel)
        {
            if (panel == null) return;
            var mask = panel.GetComponent<RectMask2D>();
            if (mask != null) Object.DestroyImmediate(mask, true);
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
