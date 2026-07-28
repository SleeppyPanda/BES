#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class CashShopPanelMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string ArtPath = "Assets/Art Ui/Mới/Cash SHop/";

        static readonly string[] MainTitles =
        {
            "PACK SHOP",
            "EXCHANGE SHOP",
            "DIAMOND PURCHASE",
            "LIGHT PURCHASE"
        };

        static readonly string[] MainTabSprites =
        {
            "Group 427322945.png",
            "Group 427322916.png",
            "Group 427322944 (1).png",
            "Group 427322944.png"
        };

        static readonly string[] PackSubTitles =
        {
            "SOLAR PEA EXCHANGE",
            "TIDAL PEA EXCHANGE",
            "GOLDEN EXCHANGE"
        };

        static readonly string[][] ItemSprites =
        {
            new[] { "Group 427322951.png", "Group 427322952.png" },
            new[] { "Group 427322967.png", "Group 427322968.png", "Group 427322969.png", "Group 427322970.png", "Group 427322972.png", "Group 427322973.png" },
            new[] { "Group 427322966.png" },
            new[] { "Group 4273230681.png", "Group 427322974.png" },
            new[] { "Group 427322973.png", "Group 427322974.png", "Group 427322977.png", "Group 427322978.png" },
            new[] { "Group 427322979.png", "Group 427322980.png", "Group 427322981.png", "Group 427322982.png", "Group 427322983.png", "Group 427322984.png" }
        };

        [MenuItem("BES/UI/Build Nested Cash Shop %&k")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = Find(root.transform, "CashShopPanel");
                var home = root.GetComponentInChildren<MenuHomeController>(true);
                if (panel == null || home == null) return;

                RemoveLegacyCurrencyPanels(root.transform);
                ClearChildren(panel);
                ConfigurePanel(panel);
                var controller = panel.GetComponent<CashShopPanelController>() ??
                                 panel.gameObject.AddComponent<CashShopPanelController>();

                var header = Rect("ShopHeader", panel, new Vector2(.015f, .88f), new Vector2(.985f, .985f));
                var title = Text("ShopTitle", header, MainTitles[0], new Vector2(.02f, .12f), new Vector2(.42f, .88f), 31f);
                title.alignment = TextAlignmentOptions.Left;
                var currencies = BuildCurrencies(header);
                var close = Button("CloseButton", header, "X", new Vector2(.955f, .18f), new Vector2(.995f, .82f), out _);

                var tabsRoot = Rect("ShopTabs", panel, new Vector2(.025f, .08f), new Vector2(.245f, .86f));
                var contentRoot = Rect("ShopContents", panel, new Vector2(.26f, .08f), new Vector2(.975f, .86f));
                var mainButtons = new List<Button>();
                var mainPanels = new List<GameObject>();
                var packSubButtons = new List<Button>();
                var packSubPanels = new List<GameObject>();
                var indicatorPositions = new List<RectTransform>();
                var items = new List<ItemParts>();

                for (var mainIndex = 0; mainIndex < MainTitles.Length; mainIndex++)
                {
                    var top = .98f - mainIndex * .22f;
                    var tab = Image(
                        "MainTab_" + mainIndex,
                        tabsRoot,
                        new Vector2(.04f, top - .17f),
                        new Vector2(.96f, top),
                        Color.white);
                    tab.sprite = Sprite(MainTabSprites[mainIndex]);
                    var tabButton = tab.gameObject.AddComponent<Button>();
                    tabButton.targetGraphic = tab;
                    Text("Label", tab.transform, MainTitles[mainIndex], Vector2.zero, Vector2.one, 16f);
                    mainButtons.Add(tabButton);

                    var content = Rect(
                        "MainContent_" + mainIndex + "_" + MainTitles[mainIndex].Replace(" ", "_"),
                        contentRoot,
                        Vector2.zero,
                        Vector2.one);
                    mainPanels.Add(content.gameObject);
                    content.gameObject.SetActive(mainIndex == 0);

                    if (mainIndex == 0)
                    {
                        BuildPackShop(
                            content,
                            packSubButtons,
                            packSubPanels,
                            indicatorPositions,
                            items,
                            out var indicator);
                        Set(controller, "packSubTabIndicator", indicator);
                    }
                    else
                    {
                        var sourceIndex = mainIndex == 1 ? 2 : mainIndex == 2 ? 5 : 3;
                        BuildItems(content, sourceIndex, items);
                    }
                }

                var feedback = Text(
                    "PurchaseFeedback",
                    panel,
                    string.Empty,
                    new Vector2(.30f, .015f),
                    new Vector2(.90f, .065f),
                    18f);

                WireController(
                    controller,
                    root,
                    home,
                    title,
                    mainButtons,
                    mainPanels,
                    packSubButtons,
                    packSubPanels,
                    indicatorPositions,
                    currencies,
                    items,
                    feedback);
                WireModal(panel, close);
                WireShopOpenButtons(root.transform, controller, home);
                panel.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] One-panel Cash Shop with four main tabs and three Pack subtabs built and wired.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ConfigurePanel(Transform panel)
        {
            var rect = panel as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
            image.sprite = Sprite("Group 427322940.png");
            image.color = Color.white;
            image.raycastTarget = true;
        }

        static List<CurrencyParts> BuildCurrencies(Transform header)
        {
            var result = new List<CurrencyParts>();
            var ids = new[] { "energy", "gems", "coins" };
            for (var i = 0; i < 3; i++)
            {
                var minX = .45f + i * .16f;
                var bar = Image(
                    "Currency_" + ids[i],
                    header,
                    new Vector2(minX, .25f),
                    new Vector2(minX + .145f, .75f),
                    new Color(.96f, .92f, .82f, 1f));
                var icon = Image("Icon", bar.transform, new Vector2(.02f, .08f), new Vector2(.25f, .92f), Color.white);
                icon.preserveAspect = true;
                var amount = Text("Amount", bar.transform, "0", new Vector2(.27f, 0f), new Vector2(.96f, 1f), 17f);
                result.Add(new CurrencyParts { id = ids[i], icon = icon, amount = amount });
            }
            return result;
        }

        static void BuildItems(Transform content, int tabIndex, List<ItemParts> result)
        {
            var sprites = ItemSprites[tabIndex];
            for (var i = 0; i < sprites.Length; i++)
            {
                var column = i % 4;
                var row = i / 4;
                var min = new Vector2(.015f + column * .247f, .54f - row * .46f);
                var max = new Vector2(min.x + .22f, min.y + .40f);
                var art = Image($"Item_{tabIndex}_{i}", content, min, max, Color.white);
                art.sprite = Sprite(sprites[i]);
                art.preserveAspect = true;
                var purchase = art.gameObject.AddComponent<Button>();
                purchase.targetGraphic = art;
                var name = Text("Name", art.transform, $"ITEM {i + 1}", new Vector2(.05f, .78f), new Vector2(.95f, .96f), 15f);
                var price = Text("Price", art.transform, ((i + 1) * 100).ToString(), new Vector2(.30f, .02f), new Vector2(.95f, .19f), 15f);
                var soldOut = Text("SoldOut", art.transform, "SOLD OUT", new Vector2(.05f, .35f), new Vector2(.95f, .65f), 25f);
                soldOut.gameObject.SetActive(false);
                result.Add(new ItemParts
                {
                    id = $"shop_{tabIndex}_{i}",
                    tab = tabIndex,
                    root = art.gameObject,
                    artwork = art,
                    name = name,
                    price = price,
                    button = purchase,
                    currency = tabIndex == 5 ? "gems" : "coins",
                    cost = (i + 1) * 100,
                    reward = $"reward_{tabIndex}_{i}",
                    soldOut = soldOut.gameObject
                });
            }
        }

        static void BuildPackShop(
            Transform root,
            List<Button> buttons,
            List<GameObject> panels,
            List<RectTransform> indicatorPositions,
            List<ItemParts> items,
            out RectTransform indicator)
        {
            var subTabs = Rect("PackSubTabs", root, new Vector2(.03f, .82f), new Vector2(.97f, .98f));
            var subContents = Rect("PackSubContents", root, new Vector2(.02f, .02f), new Vector2(.98f, .80f));
            for (var i = 0; i < PackSubTitles.Length; i++)
            {
                var minX = .02f + i * .33f;
                var buttonImage = Image(
                    "PackSubTab_" + i,
                    subTabs,
                    new Vector2(minX, .12f),
                    new Vector2(minX + .30f, .88f),
                    Color.white);
                var button = buttonImage.gameObject.AddComponent<Button>();
                button.targetGraphic = buttonImage;
                button.transition = Selectable.Transition.None;
                Text("Label", button.transform, PackSubTitles[i], Vector2.zero, Vector2.one, 15f);
                buttons.Add(button);

                var position = Rect(
                    "SubPosition_" + i,
                    subTabs,
                    new Vector2(minX, .02f),
                    new Vector2(minX + .30f, .10f));
                indicatorPositions.Add(position);

                var panel = Rect(
                    "PackSubContent_" + i + "_" + PackSubTitles[i].Replace(" ", "_"),
                    subContents,
                    Vector2.zero,
                    Vector2.one);
                panels.Add(panel.gameObject);
                var sourceIndex = i == 0 ? 0 : i == 1 ? 1 : 4;
                BuildItems(panel, sourceIndex, items);
                panel.gameObject.SetActive(i == 0);
            }

            var indicatorImage = Image(
                "PackSubTabIndicator",
                subTabs,
                new Vector2(.02f, .02f),
                new Vector2(.32f, .10f),
                Color.white);
            indicatorImage.sprite = Sprite("Union.png");
            indicatorImage.raycastTarget = false;
            indicator = indicatorImage.rectTransform;
            indicator.SetAsLastSibling();
        }

        static void WireController(
            CashShopPanelController controller,
            GameObject root,
            MenuHomeController home,
            TMP_Text title,
            List<Button> mainButtons,
            List<GameObject> mainPanels,
            List<Button> packSubButtons,
            List<GameObject> packSubPanels,
            List<RectTransform> indicatorPositions,
            List<CurrencyParts> currencies,
            List<ItemParts> items,
            TMP_Text feedback)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("database").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
            serialized.FindProperty("homeController").objectReferenceValue = home;
            serialized.FindProperty("tabTitle").objectReferenceValue = title;
            serialized.FindProperty("feedbackText").objectReferenceValue = feedback;
            SetObjectList(serialized.FindProperty("mainTabButtons"), mainButtons);
            SetObjectList(serialized.FindProperty("mainTabPanels"), mainPanels);
            SetObjectList(serialized.FindProperty("packSubTabButtons"), packSubButtons);
            SetObjectList(serialized.FindProperty("packSubTabPanels"), packSubPanels);
            SetObjectList(serialized.FindProperty("packSubTabIndicatorPositions"), indicatorPositions);

            var titles = serialized.FindProperty("mainTabTitles");
            titles.arraySize = MainTitles.Length;
            for (var i = 0; i < MainTitles.Length; i++)
                titles.GetArrayElementAtIndex(i).stringValue = MainTitles[i];

            var currencyList = serialized.FindProperty("currencies");
            currencyList.arraySize = currencies.Count;
            for (var i = 0; i < currencies.Count; i++)
            {
                var entry = currencyList.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("currencyId").stringValue = currencies[i].id;
                entry.FindPropertyRelative("icon").objectReferenceValue = currencies[i].icon;
                entry.FindPropertyRelative("amountText").objectReferenceValue = currencies[i].amount;
            }

            var itemList = serialized.FindProperty("items");
            itemList.arraySize = items.Count;
            for (var i = 0; i < items.Count; i++)
            {
                var source = items[i];
                var entry = itemList.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = source.id;
                entry.FindPropertyRelative("tabIndex").intValue = source.tab;
                entry.FindPropertyRelative("root").objectReferenceValue = source.root;
                entry.FindPropertyRelative("artwork").objectReferenceValue = source.artwork;
                entry.FindPropertyRelative("nameText").objectReferenceValue = source.name;
                entry.FindPropertyRelative("priceText").objectReferenceValue = source.price;
                entry.FindPropertyRelative("purchaseButton").objectReferenceValue = source.button;
                entry.FindPropertyRelative("currencyId").stringValue = source.currency;
                entry.FindPropertyRelative("price").intValue = source.cost;
                entry.FindPropertyRelative("rewardId").stringValue = source.reward;
                entry.FindPropertyRelative("rewardAmount").intValue = 1;
                entry.FindPropertyRelative("oneTimePurchase").boolValue = false;
                entry.FindPropertyRelative("soldOutState").objectReferenceValue = source.soldOut;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireModal(Transform panel, Button close)
        {
            var modal = panel.GetComponent<SimpleModalPanel>();
            if (modal == null) return;
            var serialized = new SerializedObject(modal);
            serialized.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("closeOnEscape").boolValue = true;
            serialized.FindProperty("showOnStart").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireShopOpenButtons(
            Transform root,
            CashShopPanelController controller,
            MenuHomeController home)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "CashShopButton") continue;
                ConfigureRouter(child.GetComponent<Button>(), controller, 0, 0);
            }

            WireCurrencyAdd(root, "energy", controller, 3, -1);
            WireCurrencyAdd(root, "coins", controller, 0, 2);
            WireCurrencyAdd(root, "gems", controller, 2, -1);

            var homeSerialized = new SerializedObject(home);
            var currencyViews = homeSerialized.FindProperty("currencies");
            for (var i = 0; i < currencyViews.arraySize; i++)
            {
                var view = currencyViews.GetArrayElementAtIndex(i);
                var eventProperty = view.FindPropertyRelative("onAddPressed");
                if (eventProperty != null)
                {
                    var calls = eventProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
                    if (calls != null) calls.arraySize = 0;
                }
            }
            homeSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireCurrencyAdd(
            Transform root,
            string currencyId,
            CashShopPanelController controller,
            int mainTabIndex,
            int packSubTabIndex)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "Currency_" + currencyId) continue;
                var add = Find(child, "Add");
                if (add != null)
                    ConfigureRouter(
                        add.GetComponent<Button>(),
                        controller,
                        mainTabIndex,
                        packSubTabIndex);
            }
        }

        static void ConfigureRouter(
            Button button,
            CashShopPanelController controller,
            int mainTabIndex,
            int packSubTabIndex)
        {
            if (button == null) return;
            var router = button.GetComponent<ShopTabOpenButton>() ??
                         button.gameObject.AddComponent<ShopTabOpenButton>();
            router.Configure(controller, mainTabIndex, packSubTabIndex);
        }

        static void RemoveLegacyCurrencyPanels(Transform root)
        {
            var names = new[]
            {
                "CurrencyShop_energy",
                "CurrencyShop_gems",
                "CurrencyShop_coins"
            };
            foreach (var name in names)
            {
                var legacy = Find(root, name);
                if (legacy != null) Object.DestroyImmediate(legacy.gameObject);
            }
        }

        static void SetObjectList<T>(SerializedProperty list, List<T> values) where T : Object
        {
            list.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void Set(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ClearChildren(Transform root)
        {
            var children = new List<GameObject>();
            foreach (Transform child in root) children.Add(child.gameObject);
            foreach (var child in children) Object.DestroyImmediate(child);
        }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        static Image Image(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var image = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        static TMP_Text Text(string name, Transform parent, string value, Vector2 min, Vector2 max, float size)
        {
            var text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 8f;
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.35f, .16f, .13f, 1f);
            return text;
        }

        static Button Button(string name, Transform parent, string value, Vector2 min, Vector2 max, out TMP_Text label)
        {
            var image = Image(name, parent, min, max, new Color(.96f, .92f, .82f, 1f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = Text("Label", image.transform, value, Vector2.zero, Vector2.one, 24f);
            return button;
        }

        static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + name);

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        struct CurrencyParts { public string id; public Image icon; public TMP_Text amount; }
        struct ItemParts
        {
            public string id;
            public int tab;
            public GameObject root;
            public Image artwork;
            public TMP_Text name;
            public TMP_Text price;
            public Button button;
            public string currency;
            public int cost;
            public string reward;
            public GameObject soldOut;
        }
    }
}
#endif
