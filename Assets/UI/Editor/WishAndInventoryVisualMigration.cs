#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using BES.Gameplay;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class WishAndInventoryVisualMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string MenuDatabasePath = "Assets/Scenes/MenuContentDatabase.asset";
        const string ItemDatabasePath = "Assets/Resources/Data/ItemDatabase.asset";
        const string ItemFolder = "Assets/Resources/Data/WishItems";

        [MenuItem("BES/UI/Build Wish And Apply Inventory Rarity Art")]
        public static void Apply()
        {
            var menuDatabase =
                AssetDatabase.LoadAssetAtPath<MenuContentDatabase>(MenuDatabasePath);
            var rewards = EnsureWishInventoryDefinitions(menuDatabase);
            EnsureStartingCurrencies(menuDatabase);

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                BuildWishPanel(root.transform, menuDatabase, rewards);
                ApplyInventoryArt(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] WishPanel built; roll rewards and Inventory rarity/button art are fully wired.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
        }

        static void BuildWishPanel(
            Transform root,
            MenuContentDatabase database,
            List<MenuWishReward> rewards)
        {
            var panel = Find(root, "WishPanel");
            if (panel == null) return;
            var oldCard = FindDirect(panel, "Card");
            if (oldCard != null) Object.DestroyImmediate(oldCard.gameObject);
            var oldContent = FindDirect(panel, "WishContent");
            if (oldContent != null) Object.DestroyImmediate(oldContent.gameObject);

            var backgroundSprite = ArtSprite("Background summon.png");
            var rollOneSprite = WishSprite("Group 427323066.png");
            var rollTenSprite = WishSprite("Group 427323065.png");
            var fiveGlow = WishSprite("Union.png");
            var fourGlow = WishSprite("Union-1.png");
            var normalCard = WishSprite("Union-2.png");
            var hoverCard = WishSprite("Union-3.png");

            var content = Rect("WishContent", panel, Vector2.zero, Vector2.one);
            var background = Image("Background", content, Vector2.zero, Vector2.one);
            background.sprite = backgroundSprite;
            background.preserveAspect = false;

            var coins = CurrencyButton(
                "CoinsCurrency",
                content,
                new Vector2(.68f, .91f),
                new Vector2(.81f, .965f),
                "COIN");
            var gems = CurrencyButton(
                "GemsCurrency",
                content,
                new Vector2(.82f, .91f),
                new Vector2(.95f, .965f),
                "GEM");

            var controls = Rect(
                "RollControls",
                content,
                new Vector2(.30f, .025f),
                new Vector2(.70f, .13f));
            var rollOneImage = Image(
                "RollOneButton",
                controls,
                new Vector2(.03f, .05f),
                new Vector2(.47f, .95f));
            rollOneImage.sprite = rollOneSprite;
            rollOneImage.preserveAspect = true;
            var rollOne = rollOneImage.gameObject.AddComponent<Button>();
            rollOne.targetGraphic = rollOneImage;

            var rollTenImage = Image(
                "RollTenButton",
                controls,
                new Vector2(.53f, .05f),
                new Vector2(.97f, .95f));
            rollTenImage.sprite = rollTenSprite;
            rollTenImage.preserveAspect = true;
            var rollTen = rollTenImage.gameObject.AddComponent<Button>();
            rollTen.targetGraphic = rollTenImage;

            var cardArea = Rect(
                "ResultCards",
                content,
                new Vector2(.08f, .15f),
                new Vector2(.92f, .88f));
            var cardViews = new List<WishResultCardView>();
            var xPositions = new[] { -350f, -175f, 0f, 175f, 350f };
            for (var i = 0; i < 10; i++)
            {
                var cardRoot = Rect(
                    "ResultCard_" + i,
                    cardArea,
                    new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f));
                cardRoot.sizeDelta = new Vector2(135f, 348f);
                cardRoot.anchoredPosition = new Vector2(
                    xPositions[i % 5],
                    i < 5 ? 190f : -190f);
                var canvasGroup = cardRoot.gameObject.AddComponent<CanvasGroup>();

                var glow = Image(
                    "RarityGlow",
                    cardRoot,
                    new Vector2(-.48f, -.34f),
                    new Vector2(1.48f, 1.34f));
                glow.raycastTarget = false;
                glow.preserveAspect = false;

                var cardBackground = Image(
                    "CardBackground",
                    cardRoot,
                    Vector2.zero,
                    Vector2.one);
                cardBackground.sprite = normalCard;
                cardBackground.preserveAspect = false;
                cardBackground.raycastTarget = true;

                var icon = Image(
                    "ItemIcon",
                    cardRoot,
                    new Vector2(.13f, .28f),
                    new Vector2(.87f, .76f));
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                var itemName = Text(
                    "ItemName",
                    cardRoot,
                    new Vector2(.08f, .10f),
                    new Vector2(.92f, .28f),
                    string.Empty,
                    18f,
                    TextAlignmentOptions.Center);
                itemName.fontStyle = FontStyles.Bold;

                var rarity = Text(
                    "Rarity",
                    cardRoot,
                    new Vector2(.10f, .01f),
                    new Vector2(.90f, .12f),
                    string.Empty,
                    16f,
                    TextAlignmentOptions.Center);

                var hover = cardRoot.gameObject.AddComponent<WishResultCardHover>();
                cardViews.Add(new WishResultCardView
                {
                    root = cardRoot,
                    canvasGroup = canvasGroup,
                    rarityGlow = glow,
                    cardBackground = cardBackground,
                    itemIcon = icon,
                    itemNameText = itemName,
                    rarityText = rarity,
                    hover = hover
                });
            }

            var detailPanel = Image(
                "ResultDetailPanel",
                content,
                new Vector2(.015f, .59f),
                new Vector2(.27f, .89f));
            detailPanel.color = new Color(.12f, .16f, .27f, .88f);
            var detailCardBackground = Image(
                "CardBackground",
                detailPanel.transform,
                new Vector2(.04f, .12f),
                new Vector2(.35f, .88f));
            detailCardBackground.sprite = normalCard;
            detailCardBackground.preserveAspect = false;
            detailCardBackground.raycastTarget = false;
            var detailIcon = Image(
                "ItemIcon",
                detailPanel.transform,
                new Vector2(.08f, .31f),
                new Vector2(.31f, .72f));
            detailIcon.preserveAspect = true;
            detailIcon.raycastTarget = false;
            var detailName = Text(
                "Name",
                detailPanel.transform,
                new Vector2(.38f, .66f),
                new Vector2(.96f, .90f),
                string.Empty,
                25f,
                TextAlignmentOptions.TopLeft);
            var detailRarity = Text(
                "Rarity",
                detailPanel.transform,
                new Vector2(.38f, .50f),
                new Vector2(.96f, .66f),
                string.Empty,
                22f,
                TextAlignmentOptions.TopLeft);
            var detailDescription = Text(
                "Description",
                detailPanel.transform,
                new Vector2(.38f, .10f),
                new Vector2(.96f, .50f),
                string.Empty,
                19f,
                TextAlignmentOptions.TopLeft);
            detailDescription.textWrappingMode = TextWrappingModes.Normal;
            detailPanel.gameObject.SetActive(false);

            var claimImage = Image(
                "ClaimButton",
                content,
                new Vector2(.42f, .035f),
                new Vector2(.58f, .105f));
            claimImage.color = new Color(.43f, .18f, .14f, 1f);
            var claimButton = claimImage.gameObject.AddComponent<Button>();
            claimButton.targetGraphic = claimImage;
            var claimLabel = Text(
                "Label",
                claimImage.transform,
                Vector2.zero,
                Vector2.one,
                "✦  CLAIM  ✦",
                25f,
                TextAlignmentOptions.Center);
            claimLabel.fontStyle = FontStyles.Bold;
            claimImage.gameObject.SetActive(false);

            var feedback = Text(
                "Feedback",
                content,
                new Vector2(.34f, .12f),
                new Vector2(.66f, .17f),
                string.Empty,
                21f,
                TextAlignmentOptions.Center);

            var closeImage = Image(
                "CloseButton",
                content,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f));
            closeImage.rectTransform.pivot = new Vector2(1f, 1f);
            closeImage.rectTransform.sizeDelta = new Vector2(80f, 80f);
            closeImage.rectTransform.anchoredPosition = new Vector2(-18f, -14f);
            closeImage.color = new Color(1f, 1f, 1f, 0f);
            var closeButton = closeImage.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var closeLabel = Text(
                "Label",
                closeImage.transform,
                Vector2.zero,
                Vector2.one,
                "×",
                64f,
                TextAlignmentOptions.Center);

            var controller = panel.GetComponent<MenuWishController>() ??
                             panel.gameObject.AddComponent<MenuWishController>();
            for (var i = 0; i < cardViews.Count; i++)
                cardViews[i].hover.Configure(
                    controller,
                    i,
                    cardViews[i].cardBackground,
                    normalCard,
                    hoverCard);

            WireWishController(
                controller,
                database,
                root.GetComponentInChildren<MenuHomeController>(true),
                rewards,
                coins.button,
                gems.button,
                coins.amount,
                gems.amount,
                controls.gameObject,
                rollOne,
                rollTen,
                claimButton,
                feedback,
                cardViews,
                fourGlow,
                fiveGlow,
                detailPanel.gameObject,
                detailCardBackground,
                detailIcon,
                detailName,
                detailDescription,
                detailRarity);
            WireModal(panel.GetComponent<SimpleModalPanel>(), panel, closeButton);
        }

        static void ApplyInventoryArt(Transform root)
        {
            var panel = Find(root, "InventoryPanel");
            var controller = panel?.GetComponent<BagPanelController>();
            if (panel == null || controller == null) return;

            var five = ArtSprite("Rectangle 40187.png");
            var four = ArtSprite("Rectangle 40188.png");
            var three = ArtSprite("Rectangle 40190.png");
            var empty = ArtSprite("Rectangle 40200.png");
            var removeSprite = ArtSprite("Group 427323006.png");
            var useSprite = ArtSprite("Group 427323007.png");

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("fiveStarSlotSprite").objectReferenceValue = five;
            serialized.FindProperty("fourStarSlotSprite").objectReferenceValue = four;
            serialized.FindProperty("threeStarSlotSprite").objectReferenceValue = three;
            serialized.FindProperty("emptySlotSprite").objectReferenceValue = empty;

            var slots = serialized.FindProperty("slots");
            for (var i = 0; i < slots.arraySize; i++)
            {
                var image = slots.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("background").objectReferenceValue as Image;
                if (image == null) continue;
                image.sprite = empty;
                image.color = Color.white;
                image.preserveAspect = false;
            }

            var artwork = Find(panel, "InventoryArtwork");
            var detailIcon = Find(panel, "DetailIcon")?.GetComponent<Image>();
            var oldDetailBackground = FindDirect(artwork, "DetailCardBackground");
            if (oldDetailBackground != null) Object.DestroyImmediate(oldDetailBackground.gameObject);
            var detailBackground = Image(
                "DetailCardBackground",
                artwork,
                new Vector2(.724f, .592f),
                new Vector2(.835f, .815f));
            detailBackground.sprite = empty;
            detailBackground.color = Color.white;
            detailBackground.preserveAspect = false;
            detailBackground.raycastTarget = false;
            if (detailIcon != null)
                detailBackground.transform.SetSiblingIndex(
                    Mathf.Max(0, detailIcon.transform.GetSiblingIndex()));
            serialized.FindProperty("detailCardBackground").objectReferenceValue = detailBackground;

            ApplyActionSprite(Find(panel, "RemoveButton"), removeSprite);
            ApplyActionSprite(Find(panel, "UseButton"), useSprite);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ApplyActionSprite(Transform buttonRoot, Sprite sprite)
        {
            if (buttonRoot == null) return;
            var image = buttonRoot.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = false;
            }
            var label = FindDirect(buttonRoot, "Label");
            if (label != null) label.gameObject.SetActive(false);
        }

        static List<MenuWishReward> EnsureWishInventoryDefinitions(
            MenuContentDatabase menuDatabase)
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Data");
            EnsureFolder(ItemFolder);

            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(database, ItemDatabasePath);
            }
            database.items ??= new List<ItemDefinition>();

            var result = new List<MenuWishReward>();
            var fallbackIcon = WishSprite("ICON.png");
            var characterCount = Mathf.Min(6, menuDatabase?.characters.Count ?? 0);
            for (var i = 0; i < characterCount; i++)
            {
                var character = menuDatabase.characters[i];
                var rarity = Mathf.Clamp(character.rarity, 3, 5);
                var id = "wish_" + character.id;
                var icon = character.portrait != null ? character.portrait :
                           character.chibi != null ? character.chibi : fallbackIcon;
                EnsureItemDefinition(
                    database,
                    id,
                    character.displayName,
                    character.description,
                    icon,
                    rarity,
                    ItemType.Quest);
                result.Add(new MenuWishReward
                {
                    itemId = id,
                    displayName = character.displayName,
                    description = character.description,
                    icon = icon,
                    rarity = rarity,
                    weight = rarity >= 5 ? 4 : rarity == 4 ? 14 : 30,
                    amount = 1,
                    unlockAsCharacter = true
                });
            }

            AddGenericReward(
                result, database, "wish_relic_5", "Celestial Relic",
                "A legendary relic obtained from Wish.", fallbackIcon, 5, 2, ItemType.Material);
            AddGenericReward(
                result, database, "wish_relic_4", "Astral Relic",
                "A rare relic obtained from Wish.", fallbackIcon, 4, 12, ItemType.Material);
            AddGenericReward(
                result, database, "wish_material_3", "Wish Fragment",
                "A common material obtained from Wish.", fallbackIcon, 3, 38, ItemType.Material);

            EditorUtility.SetDirty(database);
            return result;
        }

        static void AddGenericReward(
            List<MenuWishReward> rewards,
            ItemDatabase database,
            string id,
            string displayName,
            string description,
            Sprite icon,
            int rarity,
            int weight,
            ItemType itemType)
        {
            EnsureItemDefinition(database, id, displayName, description, icon, rarity, itemType);
            rewards.Add(new MenuWishReward
            {
                itemId = id,
                displayName = displayName,
                description = description,
                icon = icon,
                rarity = rarity,
                weight = weight,
                amount = 1
            });
        }

        static void EnsureItemDefinition(
            ItemDatabase database,
            string id,
            string displayName,
            string description,
            Sprite icon,
            int rarity,
            ItemType type)
        {
            var item = database.items.Find(x => x != null && x.itemId == id);
            if (item == null)
            {
                var path = ItemFolder + "/" + Sanitize(id) + ".asset";
                item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<ItemDefinition>();
                    AssetDatabase.CreateAsset(item, path);
                }
                database.items.Add(item);
            }
            item.itemId = id;
            item.displayName = displayName;
            item.description = description;
            item.icon = icon;
            item.rarity = rarity;
            item.itemType = type;
            item.maxStack = 99;
            EditorUtility.SetDirty(item);
        }

        static void EnsureStartingCurrencies(MenuContentDatabase database)
        {
            if (database == null) return;
            var coins = database.currencies.Find(x => x.id == "coins");
            var gems = database.currencies.Find(x => x.id == "gems");
            if (coins != null && coins.amount < 16000) coins.amount = 99999;
            if (gems != null && gems.amount < 1600) gems.amount = 1600;
            EditorUtility.SetDirty(database);
        }

        static void WireWishController(
            MenuWishController controller,
            MenuContentDatabase database,
            MenuHomeController homeController,
            List<MenuWishReward> rewards,
            Button coinsButton,
            Button gemsButton,
            TMP_Text coinsText,
            TMP_Text gemsText,
            GameObject controls,
            Button rollOne,
            Button rollTen,
            Button claim,
            TMP_Text feedback,
            List<WishResultCardView> cards,
            Sprite fourGlow,
            Sprite fiveGlow,
            GameObject detailPanel,
            Image detailBackground,
            Image detailIcon,
            TMP_Text detailName,
            TMP_Text detailDescription,
            TMP_Text detailRarity)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("database").objectReferenceValue = database;
            serialized.FindProperty("homeController").objectReferenceValue = homeController;
            var rewardList = serialized.FindProperty("rewards");
            rewardList.arraySize = rewards.Count;
            for (var i = 0; i < rewards.Count; i++)
            {
                var target = rewardList.GetArrayElementAtIndex(i);
                var source = rewards[i];
                target.FindPropertyRelative("itemId").stringValue = source.itemId;
                target.FindPropertyRelative("displayName").stringValue = source.displayName;
                target.FindPropertyRelative("description").stringValue = source.description;
                target.FindPropertyRelative("icon").objectReferenceValue = source.icon;
                target.FindPropertyRelative("rarity").intValue = source.rarity;
                target.FindPropertyRelative("weight").intValue = source.weight;
                target.FindPropertyRelative("amount").intValue = source.amount;
                target.FindPropertyRelative("unlockAsCharacter").boolValue = source.unlockAsCharacter;
            }

            serialized.FindProperty("coinsButton").objectReferenceValue = coinsButton;
            serialized.FindProperty("gemsButton").objectReferenceValue = gemsButton;
            serialized.FindProperty("coinsAmountText").objectReferenceValue = coinsText;
            serialized.FindProperty("gemsAmountText").objectReferenceValue = gemsText;
            serialized.FindProperty("rollControls").objectReferenceValue = controls;
            serialized.FindProperty("rollOneButton").objectReferenceValue = rollOne;
            serialized.FindProperty("rollTenButton").objectReferenceValue = rollTen;
            serialized.FindProperty("claimButton").objectReferenceValue = claim;
            serialized.FindProperty("feedbackText").objectReferenceValue = feedback;

            var cardList = serialized.FindProperty("resultCards");
            cardList.arraySize = cards.Count;
            for (var i = 0; i < cards.Count; i++)
            {
                var target = cardList.GetArrayElementAtIndex(i);
                target.FindPropertyRelative("root").objectReferenceValue = cards[i].root;
                target.FindPropertyRelative("canvasGroup").objectReferenceValue = cards[i].canvasGroup;
                target.FindPropertyRelative("rarityGlow").objectReferenceValue = cards[i].rarityGlow;
                target.FindPropertyRelative("cardBackground").objectReferenceValue = cards[i].cardBackground;
                target.FindPropertyRelative("itemIcon").objectReferenceValue = cards[i].itemIcon;
                target.FindPropertyRelative("itemNameText").objectReferenceValue = cards[i].itemNameText;
                target.FindPropertyRelative("rarityText").objectReferenceValue = cards[i].rarityText;
                target.FindPropertyRelative("hover").objectReferenceValue = cards[i].hover;
            }
            serialized.FindProperty("fourStarGlow").objectReferenceValue = fourGlow;
            serialized.FindProperty("fiveStarGlow").objectReferenceValue = fiveGlow;
            serialized.FindProperty("detailPanel").objectReferenceValue = detailPanel;
            serialized.FindProperty("detailCardBackground").objectReferenceValue = detailBackground;
            serialized.FindProperty("detailItemIcon").objectReferenceValue = detailIcon;
            serialized.FindProperty("detailNameText").objectReferenceValue = detailName;
            serialized.FindProperty("detailDescriptionText").objectReferenceValue = detailDescription;
            serialized.FindProperty("detailRarityText").objectReferenceValue = detailRarity;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static (Button button, TMP_Text amount) CurrencyButton(
            string name,
            Transform parent,
            Vector2 min,
            Vector2 max,
            string label)
        {
            var image = Image(name, parent, min, max);
            image.color = new Color(.97f, .95f, .88f, 1f);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var title = Text(
                "Label",
                image.transform,
                new Vector2(.05f, .05f),
                new Vector2(.43f, .95f),
                label,
                20f,
                TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            var amount = Text(
                "Amount",
                image.transform,
                new Vector2(.43f, .05f),
                new Vector2(.95f, .95f),
                "0",
                20f,
                TextAlignmentOptions.Center);
            return (button, amount);
        }

        static void WireModal(SimpleModalPanel modal, Transform panel, Button closeButton)
        {
            if (modal == null) return;
            var serialized = new SerializedObject(modal);
            serialized.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
            serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite WishSprite(string fileName) =>
            FindSprite(fileName, "/Wish/");

        static Sprite ArtSprite(string fileName) =>
            FindSprite(fileName, "/Mới/");

        static Sprite FindSprite(string fileName, string pathPart)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art Ui" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (path.Contains(pathPart) && path.EndsWith("/" + fileName))
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static string Sanitize(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }

        static RectTransform Rect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        static Image Image(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax) =>
            Rect(name, parent, anchorMin, anchorMax).gameObject.AddComponent<Image>();

        static TMP_Text Text(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string value,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            var text = Rect(name, parent, anchorMin, anchorMax)
                .gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(1f, .96f, .88f, 1f);
            text.raycastTarget = false;
            return text;
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
