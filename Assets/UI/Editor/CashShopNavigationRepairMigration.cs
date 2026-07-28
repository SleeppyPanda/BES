#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class CashShopNavigationRepairMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        static readonly string[] MainNames =
        {
            "MainContent_0_EXCHANGE_SHOP",
            "MainContent_1_PACK_SHOP",
            "MainContent_2_LIGHT_PURCHASE",
            "MainContent_3_DIAMOND_PURCHASE"
        };

        static readonly string[] MainTitles =
        {
            "EXCHANGE SHOP",
            "PACK SHOP",
            "LIGHT PURCHASE",
            "DIAMOND PURCHASE"
        };

        [MenuItem("BES/UI/Repair Cash Shop Smooth Navigation")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var shop = Find(root.transform, "CashShopPanel");
                var contentsRoot = Find(shop, "ShopContents");
                var controller = shop != null ? shop.GetComponent<CashShopPanelController>() : null;
                if (shop == null || contentsRoot == null || controller == null) return;

                var mainPanels = new List<GameObject>();
                var mainButtons = new List<Button>();
                for (var i = 0; i < MainNames.Length; i++)
                {
                    var panel = FindDirect(contentsRoot, MainNames[i]);
                    if (panel != null)
                    {
                        panel.SetSiblingIndex(i);
                        mainPanels.Add(panel.gameObject);
                    }
                    var button = Find(shop, "MainTab_" + i)?.GetComponent<Button>();
                    mainButtons.Add(button);
                    var label = button != null ? Find(button.transform, "Label")?.GetComponent<TMP_Text>() : null;
                    if (label != null) label.text = MainTitles[i];
                }
                if (mainPanels.Count != 4) return;

                var mainGroup = ConfigureSmoothGroup(
                    contentsRoot.gameObject,
                    contentsRoot as RectTransform,
                    new List<Button>(),
                    mainPanels,
                    null,
                    new List<RectTransform>());

                var subGroups = new List<SmoothTabGroup>
                {
                    ConfigureExistingExchangeGroup(mainPanels[0].transform),
                    EnsureSingleSubGroup(mainPanels[1].transform, "Pack"),
                    EnsureSingleSubGroup(mainPanels[2].transform, "Light"),
                    null
                };

                WireController(controller, mainButtons, mainPanels, mainGroup, subGroups);
                RepairRouters(root.transform, controller);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Cash Shop main order repaired; smooth main/subtab swipe navigation wired.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static SmoothTabGroup ConfigureExistingExchangeGroup(Transform mainPanel)
        {
            var tabs = Find(mainPanel, "PackSubTabs");
            var contents = Find(mainPanel, "PackSubContents");
            if (tabs == null || contents == null) return null;
            OrganizeNavigation(tabs, "Pack", 3);

            var system = FindDirect(mainPanel, "ExchangeSubTabSystem");
            if (system == null)
            {
                system = CreateRect("ExchangeSubTabSystem", mainPanel, Vector2.zero, Vector2.one);
                tabs.SetParent(system, true);
                contents.SetParent(system, true);
            }

            var buttons = new List<Button>();
            var panels = new List<GameObject>();
            var positions = new List<RectTransform>();
            for (var i = 0; i < 3; i++)
            {
                buttons.Add(Find(tabs, "PackSubTab_" + i)?.GetComponent<Button>());
                var panel = Find(contents, "PackSubContent_" + i + "_");
                if (panel == null)
                {
                    foreach (Transform child in contents)
                        if (child.name.StartsWith("PackSubContent_" + i + "_")) panel = child;
                }
                if (panel != null) panels.Add(panel.gameObject);
                positions.Add(Find(tabs, "SubPosition_" + i) as RectTransform);
            }
            var indicator = Find(tabs, "PackSubTabIndicator") as RectTransform;
            return ConfigureSmoothGroup(
                system.gameObject,
                mainPanel as RectTransform,
                buttons,
                panels,
                indicator,
                positions);
        }

        static SmoothTabGroup EnsureSingleSubGroup(Transform mainPanel, string prefix)
        {
            var systemName = prefix + "SubTabSystem";
            var existing = FindDirect(mainPanel, systemName);
            if (existing != null)
            {
                var existingTabs = Find(existing, prefix + "SubTabs");
                if (existingTabs != null) OrganizeNavigation(existingTabs, prefix, 1);
                return existing.GetComponent<SmoothTabGroup>();
            }

            var oldChildren = new List<Transform>();
            foreach (Transform child in mainPanel) oldChildren.Add(child);

            var system = CreateRect(systemName, mainPanel, Vector2.zero, Vector2.one);
            var tabs = CreateRect(prefix + "SubTabs", system, new Vector2(.03f, .82f), new Vector2(.97f, .98f));
            var contents = CreateRect(prefix + "SubContents", system, new Vector2(.02f, .02f), new Vector2(.98f, .80f));
            var content = CreateRect(prefix + "SubContent_0", contents, Vector2.zero, Vector2.one);
            foreach (var child in oldChildren)
            {
                child.SetParent(content, true);
                RestoreCollapsedScale(child);
            }

            var tabImage = CreateImage(
                prefix + "SubTab_0",
                tabs,
                new Vector2(.02f, .12f),
                new Vector2(.32f, .88f),
                new Color(1f, 1f, 1f, 0f));
            var button = tabImage.gameObject.AddComponent<Button>();
            button.targetGraphic = tabImage;
            button.transition = Selectable.Transition.None;
            var label = CreateText("Label", button.transform, prefix.ToUpperInvariant(), Vector2.zero, Vector2.one);

            var position = CreateRect(
                "SubPosition_0",
                tabs,
                new Vector2(.02f, .02f),
                new Vector2(.32f, .10f));
            var indicatorImage = CreateImage(
                prefix + "SubTabIndicator",
                tabs,
                new Vector2(.02f, .02f),
                new Vector2(.32f, .10f),
                Color.white);
            indicatorImage.raycastTarget = false;
            OrganizeNavigation(tabs, prefix, 1);

            return ConfigureSmoothGroup(
                system.gameObject,
                mainPanel as RectTransform,
                new List<Button> { button },
                new List<GameObject> { content.gameObject },
                indicatorImage.rectTransform,
                new List<RectTransform> { position });
        }

        static void OrganizeNavigation(Transform tabs, string prefix, int count)
        {
            var buttonRoot = FindDirect(tabs, prefix + "SubTabButtons");
            if (buttonRoot == null)
                buttonRoot = CreateRect(prefix + "SubTabButtons", tabs, Vector2.zero, Vector2.one);
            var positionRoot = FindDirect(tabs, prefix + "SubPositions");
            if (positionRoot == null)
                positionRoot = CreateRect(prefix + "SubPositions", tabs, Vector2.zero, Vector2.one);

            for (var i = 0; i < count; i++)
            {
                var button = Find(tabs, prefix + "SubTab_" + i);
                if (button == null && prefix == "Pack") button = Find(tabs, "PackSubTab_" + i);
                if (button != null && button.parent != buttonRoot)
                {
                    button.SetParent(buttonRoot, true);
                    RestoreCollapsedScale(button);
                }

                var position = Find(tabs, "SubPosition_" + i);
                if (position != null && position.parent != positionRoot)
                {
                    position.SetParent(positionRoot, true);
                    RestoreCollapsedScale(position);
                }
            }
            buttonRoot.SetAsFirstSibling();
            positionRoot.SetSiblingIndex(Mathf.Min(1, tabs.childCount - 1));
        }

        static SmoothTabGroup ConfigureSmoothGroup(
            GameObject owner,
            RectTransform viewport,
            List<Button> buttons,
            List<GameObject> panels,
            RectTransform indicator,
            List<RectTransform> positions)
        {
            var group = owner.GetComponent<SmoothTabGroup>() ?? owner.AddComponent<SmoothTabGroup>();
            var serialized = new SerializedObject(group);
            serialized.FindProperty("viewport").objectReferenceValue = viewport;
            serialized.FindProperty("clipToViewport").boolValue = false;
            SetList(serialized.FindProperty("buttons"), buttons);
            SetList(serialized.FindProperty("panels"), panels);
            serialized.FindProperty("indicator").objectReferenceValue = indicator;
            SetList(serialized.FindProperty("indicatorPositions"), positions);
            serialized.FindProperty("transitionDuration").floatValue = .32f;
            serialized.FindProperty("swipeThreshold").floatValue = 70f;
            serialized.FindProperty("hiddenGap").floatValue = 24f;
            serialized.FindProperty("hiddenAlpha").floatValue = .15f;
            serialized.FindProperty("easing").animationCurveValue =
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            serialized.FindProperty("initialIndex").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return group;
        }

        static void WireController(
            CashShopPanelController controller,
            List<Button> buttons,
            List<GameObject> panels,
            SmoothTabGroup mainGroup,
            List<SmoothTabGroup> subGroups)
        {
            var serialized = new SerializedObject(controller);
            SetList(serialized.FindProperty("mainTabButtons"), buttons);
            SetList(serialized.FindProperty("mainTabPanels"), panels);
            serialized.FindProperty("mainTabGroup").objectReferenceValue = mainGroup;
            SetList(serialized.FindProperty("subTabGroups"), subGroups);
            var titles = serialized.FindProperty("mainTabTitles");
            titles.arraySize = MainTitles.Length;
            for (var i = 0; i < MainTitles.Length; i++)
                titles.GetArrayElementAtIndex(i).stringValue = MainTitles[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void RepairRouters(Transform root, CashShopPanelController controller)
        {
            foreach (var router in root.GetComponentsInChildren<ShopTabOpenButton>(true))
            {
                var name = router.transform.parent != null ? router.transform.parent.name : string.Empty;
                if (router.name == "CashShopButton") router.Configure(controller, 1, 0);
                else if (name == "Currency_coins") router.Configure(controller, 0, 2);
                else if (name == "Currency_energy") router.Configure(controller, 2, 0);
                else if (name == "Currency_gems") router.Configure(controller, 3, -1);
            }
        }

        static void SetList<T>(SerializedProperty property, List<T> values) where T : Object
        {
            property.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 min,
            Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        static void RestoreCollapsedScale(Transform target)
        {
            var scale = target.localScale;
            if (Mathf.Abs(scale.x) <= .0001f ||
                Mathf.Abs(scale.y) <= .0001f ||
                Mathf.Abs(scale.z) <= .0001f)
                target.localScale = Vector3.one;
        }

        static Image CreateImage(
            string name,
            Transform parent,
            Vector2 min,
            Vector2 max,
            Color color)
        {
            var image = CreateRect(name, parent, min, max).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            Vector2 min,
            Vector2 max)
        {
            var text = CreateRect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = 16f;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        static Transform FindDirect(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root)
                if (child.name == name) return child;
            return null;
        }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name || child.name.StartsWith(name)) return child;
            return null;
        }
    }
}
#endif
