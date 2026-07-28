#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class BagPanelMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        [MenuItem("BES/UI/Build Bag Inventory Panel")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = Find(root.transform, "InventoryPanel");
                if (panel == null) return;
                var oldCard = FindDirect(panel, "Card");
                if (oldCard != null) Object.DestroyImmediate(oldCard.gameObject);
                var oldContent = FindDirect(panel, "BagContent");
                if (oldContent != null) Object.DestroyImmediate(oldContent.gameObject);

                var content = Rect("BagContent", panel, Vector2.zero, Vector2.one);
                var whiteBackground = Image("PanelBackground", content, Vector2.zero, Vector2.one);
                whiteBackground.color = Color.white;

                var artwork = Image(
                    "InventoryArtwork",
                    content,
                    new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f));
                artwork.rectTransform.sizeDelta = new Vector2(1767f, 847f);
                artwork.sprite = InventorySprite();
                artwork.preserveAspect = true;
                artwork.raycastTarget = false;

                var categoryBindings = BuildCategories(artwork.transform);
                var slotBindings = BuildGrid(artwork.transform);

                var detailIcon = Image(
                    "DetailIcon",
                    artwork.transform,
                    new Vector2(.724f, .592f),
                    new Vector2(.835f, .815f));
                detailIcon.preserveAspect = true;
                detailIcon.raycastTarget = false;
                detailIcon.enabled = false;

                var detailName = Text(
                    "DetailName",
                    artwork.transform,
                    new Vector2(.83f, .705f),
                    new Vector2(.968f, .80f),
                    string.Empty,
                    27f,
                    TextAlignmentOptions.TopLeft);
                detailName.fontStyle = FontStyles.Bold;

                var detailQuantity = Text(
                    "DetailQuantity",
                    artwork.transform,
                    new Vector2(.83f, .61f),
                    new Vector2(.968f, .70f),
                    string.Empty,
                    23f,
                    TextAlignmentOptions.TopLeft);

                var detailDescription = Text(
                    "DetailDescription",
                    artwork.transform,
                    new Vector2(.72f, .255f),
                    new Vector2(.97f, .575f),
                    string.Empty,
                    24f,
                    TextAlignmentOptions.TopLeft);
                detailDescription.textWrappingMode = TextWrappingModes.Normal;

                var emptyDetail = Text(
                    "EmptyDetailState",
                    artwork.transform,
                    new Vector2(.73f, .41f),
                    new Vector2(.965f, .55f),
                    "SELECT AN ITEM",
                    29f,
                    TextAlignmentOptions.Center);
                emptyDetail.fontStyle = FontStyles.Bold;

                var removeButton = ActionButton(
                    "RemoveButton",
                    artwork.transform,
                    new Vector2(.716f, .085f),
                    new Vector2(.839f, .145f),
                    "REMOVE");
                var useButton = ActionButton(
                    "UseButton",
                    artwork.transform,
                    new Vector2(.856f, .085f),
                    new Vector2(.974f, .145f),
                    "USE");

                var closeImage = Image(
                    "CloseButton",
                    content,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f));
                closeImage.rectTransform.pivot = new Vector2(1f, 1f);
                closeImage.rectTransform.sizeDelta = new Vector2(90f, 90f);
                closeImage.rectTransform.anchoredPosition = new Vector2(-24f, -20f);
                closeImage.color = new Color(1f, 1f, 1f, 0f);
                var closeButton = closeImage.gameObject.AddComponent<Button>();
                closeButton.targetGraphic = closeImage;
                var closeLabel = Text(
                    "Label",
                    closeImage.transform,
                    Vector2.zero,
                    Vector2.one,
                    "×",
                    72f,
                    TextAlignmentOptions.Center);
                closeLabel.color = new Color(.39f, .19f, .14f, 1f);

                var controller = panel.GetComponent<BagPanelController>() ??
                                 panel.gameObject.AddComponent<BagPanelController>();
                WireController(
                    controller,
                    categoryBindings,
                    slotBindings,
                    detailIcon,
                    detailName,
                    detailDescription,
                    detailQuantity,
                    useButton,
                    removeButton,
                    emptyDetail.gameObject);
                WireModal(panel.GetComponent<SimpleModalPanel>(), panel, closeButton);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] InventoryPanel rebuilt with five filters, 25 slots, details, Use and Remove.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static List<BagCategoryButton> BuildCategories(Transform artwork)
        {
            var categories = new[]
            {
                BagCategory.All,
                BagCategory.Supplies,
                BagCategory.Equipment,
                BagCategory.Materials,
                BagCategory.Mementos
            };
            var yCenters = new[] { .735f, .585f, .465f, .345f, .225f };
            var result = new List<BagCategoryButton>();
            for (var i = 0; i < categories.Length; i++)
            {
                var root = Image(
                    "Category_" + categories[i],
                    artwork,
                    new Vector2(.137f, yCenters[i] - .045f),
                    new Vector2(.247f, yCenters[i] + .045f));
                root.color = new Color(1f, 1f, 1f, 0f);
                var button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = root;
                button.transition = Selectable.Transition.None;

                var selected = Image("SelectedState", root.transform, Vector2.zero, Vector2.one);
                selected.color = new Color(1f, .83f, .38f, .22f);
                selected.raycastTarget = false;
                selected.gameObject.SetActive(i == 0);
                result.Add(new BagCategoryButton
                {
                    category = categories[i],
                    button = button,
                    selectedState = selected.gameObject
                });
            }
            return result;
        }

        static List<BagSlotView> BuildGrid(Transform artwork)
        {
            var grid = Rect(
                "ItemGrid",
                artwork,
                new Vector2(.31f, .16f),
                new Vector2(.70f, .86f));
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(100f, 100f);
            layout.spacing = new Vector2(18f, 18f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.padding = new RectOffset(8, 0, 8, 0);

            var result = new List<BagSlotView>();
            for (var i = 0; i < 25; i++)
            {
                var background = Image(
                    "ItemSlot_" + i,
                    grid,
                    new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f));
                background.color = new Color(.69f, .66f, .57f, 1f);
                var button = background.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                button.transition = Selectable.Transition.ColorTint;

                var icon = Image(
                    "Icon",
                    background.transform,
                    new Vector2(.10f, .10f),
                    new Vector2(.90f, .90f));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.enabled = false;

                var quantity = Text(
                    "Quantity",
                    background.transform,
                    new Vector2(.48f, 0f),
                    new Vector2(.96f, .36f),
                    string.Empty,
                    22f,
                    TextAlignmentOptions.BottomRight);
                quantity.color = Color.white;
                quantity.fontStyle = FontStyles.Bold;

                var selected = Image(
                    "SelectedState",
                    background.transform,
                    Vector2.zero,
                    Vector2.one);
                selected.color = new Color(1f, .83f, .23f, .32f);
                selected.raycastTarget = false;
                selected.gameObject.SetActive(false);

                result.Add(new BagSlotView
                {
                    button = button,
                    background = background,
                    icon = icon,
                    quantityText = quantity,
                    selectedState = selected.gameObject
                });
            }
            return result;
        }

        static Button ActionButton(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string label)
        {
            var image = Image(name, parent, anchorMin, anchorMax);
            image.color = new Color(.45f, .18f, .14f, 1f);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text(
                "Label",
                image.transform,
                Vector2.zero,
                Vector2.one,
                "✦  " + label,
                24f,
                TextAlignmentOptions.Center);
            text.color = new Color(1f, .96f, .85f, 1f);
            text.fontStyle = FontStyles.Bold;
            return button;
        }

        static void WireController(
            BagPanelController controller,
            List<BagCategoryButton> categories,
            List<BagSlotView> slots,
            Image detailIcon,
            TMP_Text detailName,
            TMP_Text detailDescription,
            TMP_Text detailQuantity,
            Button useButton,
            Button removeButton,
            GameObject emptyDetail)
        {
            var serialized = new SerializedObject(controller);
            var categoryList = serialized.FindProperty("categoryButtons");
            categoryList.arraySize = categories.Count;
            for (var i = 0; i < categories.Count; i++)
            {
                var target = categoryList.GetArrayElementAtIndex(i);
                target.FindPropertyRelative("category").enumValueIndex = (int)categories[i].category;
                target.FindPropertyRelative("button").objectReferenceValue = categories[i].button;
                target.FindPropertyRelative("selectedState").objectReferenceValue = categories[i].selectedState;
            }

            var slotList = serialized.FindProperty("slots");
            slotList.arraySize = slots.Count;
            for (var i = 0; i < slots.Count; i++)
            {
                var target = slotList.GetArrayElementAtIndex(i);
                target.FindPropertyRelative("button").objectReferenceValue = slots[i].button;
                target.FindPropertyRelative("background").objectReferenceValue = slots[i].background;
                target.FindPropertyRelative("icon").objectReferenceValue = slots[i].icon;
                target.FindPropertyRelative("quantityText").objectReferenceValue = slots[i].quantityText;
                target.FindPropertyRelative("selectedState").objectReferenceValue = slots[i].selectedState;
            }

            serialized.FindProperty("detailIcon").objectReferenceValue = detailIcon;
            serialized.FindProperty("detailNameText").objectReferenceValue = detailName;
            serialized.FindProperty("detailDescriptionText").objectReferenceValue = detailDescription;
            serialized.FindProperty("detailQuantityText").objectReferenceValue = detailQuantity;
            serialized.FindProperty("useButton").objectReferenceValue = useButton;
            serialized.FindProperty("removeButton").objectReferenceValue = removeButton;
            serialized.FindProperty("emptyDetailState").objectReferenceValue = emptyDetail;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireModal(
            SimpleModalPanel modal,
            Transform panel,
            Button closeButton)
        {
            if (modal == null) return;
            var serialized = new SerializedObject(modal);
            serialized.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
            serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite InventorySprite()
        {
            foreach (var guid in AssetDatabase.FindAssets("Iventory t:Sprite", new[] { "Assets/Art Ui" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (path.EndsWith("/Mới/Iventory.png"))
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
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
            text.color = new Color(.35f, .18f, .13f, 1f);
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
