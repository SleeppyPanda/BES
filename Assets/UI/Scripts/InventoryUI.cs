using System;
using System.Collections.Generic;
using BES.Core;
using BES.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Transform listContainer;
        [SerializeField] TMP_Text itemRowPrefab;
        [SerializeField] GameObject itemSlotPrefab;
        [SerializeField] Button closeButton;
        [SerializeField] Button itemsTabButton;
        [SerializeField] Button materialsTabButton;
        [SerializeField] TMP_Text detailText;
        [SerializeField] Sprite artworkSprite;
        [SerializeField] Sprite closeSprite;
        [SerializeField] bool buildRuntimeFallback = true;
        [SerializeField] bool closeOnEscape = true;
        [SerializeField] bool useArtworkLayout = true;
        [SerializeField] bool hideLegacyChildren = true;

        InventorySystem inventory;
        bool showMaterials;
        bool legacyChildrenHidden;
        readonly List<GameObject> runtimeRows = new();

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake()
        {
            EnsureRuntimeBindings();
            inventory = GameManager.Instance != null
                ? GameManager.Instance.Inventory
                : FindAnyObjectByType<InventorySystem>();

            if (panel != null)
                panel.SetActive(false);
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
            if (itemsTabButton != null)
            {
                itemsTabButton.onClick.RemoveListener(ShowItemsTab);
                itemsTabButton.onClick.AddListener(ShowItemsTab);
            }
            if (materialsTabButton != null)
            {
                materialsTabButton.onClick.RemoveListener(ShowMaterialsTab);
                materialsTabButton.onClick.AddListener(ShowMaterialsTab);
            }
        }

        void Update()
        {
            if (closeOnEscape && IsOpen && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Close();
        }

        public void Open()
        {
            EnsureRuntimeBindings();
            if (panel == null)
                return;

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            Refresh();
        }

        public void Toggle()
        {
            EnsureRuntimeBindings();
            if (panel == null) return;

            panel.SetActive(!panel.activeSelf);
            if (panel.activeSelf)
                Refresh();
        }

        public void Close()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        void ShowItemsTab()
        {
            showMaterials = false;
            Refresh();
        }

        void ShowMaterialsTab()
        {
            showMaterials = true;
            Refresh();
        }

        void Refresh()
        {
            EnsureRuntimeBindings();
            inventory ??= GameManager.Instance?.Inventory;
            if (listContainer == null)
                return;

            for (var i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);
            runtimeRows.Clear();

            var hasItems = false;
            if (inventory != null)
            {
                foreach (var pair in inventory.Items)
                {
                    var def = inventory.GetDefinition(pair.Key);
                    var isMaterial = def == null || def.itemType == ItemType.Material;
                    if (showMaterials != isMaterial)
                        continue;

                    hasItems = true;
                    var itemId = pair.Key;
                    var label = FormatItemLabel(itemId, pair.Value, def);

                    if (itemSlotPrefab != null)
                    {
                        var slot = Instantiate(itemSlotPrefab, listContainer);
                        var text = slot.GetComponentInChildren<TMP_Text>();
                        if (text != null)
                            text.text = label;

                        var btn = slot.GetComponent<Button>() ?? slot.AddComponent<Button>();
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnItemClicked(itemId, def));
                    }
                    else if (itemRowPrefab != null)
                    {
                        var row = Instantiate(itemRowPrefab, listContainer);
                        row.text = label;
                    }
                    else
                    {
                        CreateRuntimeRow(label, () => OnItemClicked(itemId, def));
                    }
                }
            }

            if (!hasItems)
            {
                if (itemRowPrefab != null)
                {
                    var emptyRow = Instantiate(itemRowPrefab, listContainer);
                    emptyRow.text = "(Trống)";
                }
                else
                {
                    CreateRuntimeRow("(Trống)", null);
                }
            }

            if (detailText != null)
                detailText.text = showMaterials ? "Tab: Nguyên liệu" : "Tab: Vật phẩm — nhấn slot để dùng/trang bị";
        }

        public static string FormatItemLabel(string itemId, int amount, ItemDefinition def)
        {
            var name = def != null && !string.IsNullOrWhiteSpace(def.displayName)
                ? def.displayName
                : itemId;
            return $"{name} x{amount}";
        }

        void OnItemClicked(string itemId, ItemDefinition def)
        {
            if (inventory == null || def == null)
                return;

            switch (def.itemType)
            {
                case ItemType.Consumable:
                    if (inventory.TryUseItem(itemId))
                    {
                        if (detailText != null)
                            detailText.text = $"Đã dùng {def.displayName}";
                        Refresh();
                    }
                    break;
                case ItemType.Weapon:
                    if (inventory.TryEquipWeaponItem(itemId))
                    {
                        if (detailText != null)
                            detailText.text = $"Đã trang bị {def.displayName}";
                    }
                    break;
                case ItemType.Quest:
                    if (detailText != null)
                        detailText.text = def.description;
                    break;
                default:
                    if (detailText != null)
                        detailText.text = def != null ? def.description : itemId;
                    break;
            }
        }

        void EnsureRuntimeBindings()
        {
            if (!buildRuntimeFallback)
                return;

            if (panel == null)
                panel = gameObject;

            var root = panel.transform;
            ApplyArtworkBackground(panel);
            HideLegacyChildren(root);

            if (useArtworkLayout)
                ApplyRect(panel.GetComponent<RectTransform>(), new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.82f));

            if (!IsRuntimeObject(closeButton))
            {
                if (closeButton != null)
                    closeButton.gameObject.SetActive(false);
                closeButton = CreateCloseButton("RuntimeCloseButton", root, Close, closeSprite);
            }
            if (itemsTabButton == null || !itemsTabButton.gameObject.activeSelf)
                itemsTabButton = CreateInvisibleButton("RuntimeItemsTabHitbox", root, ShowItemsTab);
            if (materialsTabButton == null || !materialsTabButton.gameObject.activeSelf)
                materialsTabButton = CreateInvisibleButton("RuntimeMaterialsTabHitbox", root, ShowMaterialsTab);

            if (useArtworkLayout)
            {
                ApplyRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.92f, 0.81f), new Vector2(0.975f, 0.93f));
                ApplyRect(itemsTabButton.GetComponent<RectTransform>(), new Vector2(0.12f, 0.56f), new Vector2(0.24f, 0.67f));
                ApplyRect(materialsTabButton.GetComponent<RectTransform>(), new Vector2(0.12f, 0.34f), new Vector2(0.24f, 0.44f));
                closeButton.transform.SetAsLastSibling();
            }

            if (listContainer == null)
            {
                var list = CreateRect("RuntimeInventoryList", root);
                list.anchorMin = useArtworkLayout ? new Vector2(0.33f, 0.19f) : new Vector2(0.07f, 0.18f);
                list.anchorMax = useArtworkLayout ? new Vector2(0.67f, 0.75f) : new Vector2(0.55f, 0.75f);
                list.offsetMin = Vector2.zero;
                list.offsetMax = Vector2.zero;
                if (useArtworkLayout)
                {
                    var grid = list.gameObject.AddComponent<GridLayoutGroup>();
                    grid.cellSize = new Vector2(54f, 54f);
                    grid.spacing = new Vector2(12f, 12f);
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = 5;
                    grid.childAlignment = TextAnchor.UpperLeft;
                }
                else
                {
                    var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = true;
                    layout.childForceExpandHeight = false;
                    layout.spacing = 8f;
                }
                listContainer = list;
            }

            if (detailText == null)
            {
                detailText = CreateText("RuntimeInventoryDetail", root, "Inventory", 18f, TextAlignmentOptions.TopLeft);
                var rect = detailText.rectTransform;
                rect.anchorMin = useArtworkLayout ? new Vector2(0.72f, 0.28f) : new Vector2(0.6f, 0.22f);
                rect.anchorMax = useArtworkLayout ? new Vector2(0.93f, 0.68f) : new Vector2(0.94f, 0.72f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                detailText.color = useArtworkLayout ? new Color(0.24f, 0.14f, 0.09f, 1f) : Color.white;
            }
        }

        void HideLegacyChildren(Transform root)
        {
            if (!hideLegacyChildren || legacyChildrenHidden || root == null)
                return;

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (!child.name.StartsWith("Runtime", StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            }

            legacyChildrenHidden = true;
        }

        void ApplyArtworkBackground(GameObject targetPanel)
        {
            if (artworkSprite == null || targetPanel == null)
                return;

            var image = targetPanel.GetComponent<Image>() ?? targetPanel.AddComponent<Image>();
            image.sprite = artworkSprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = true;
        }

        void CreateRuntimeRow(string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("InventoryRow", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(listContainer, false);
            runtimeRows.Add(go);
            var image = go.GetComponent<Image>();
            image.color = useArtworkLayout ? new Color(1f, 1f, 1f, 0.01f) : new Color(1f, 1f, 1f, 0.08f);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = useArtworkLayout ? new Vector2(54f, 54f) : new Vector2(0f, 42f);

            var text = CreateText("Label", go.transform, label, 18f, TextAlignmentOptions.MidlineLeft);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = useArtworkLayout ? Vector2.zero : new Vector2(12f, 0f);
            text.rectTransform.offsetMax = useArtworkLayout ? Vector2.zero : new Vector2(-12f, 0f);
            if (useArtworkLayout)
            {
                text.fontSize = 12f;
                text.fontSizeMax = 12f;
                text.color = new Color(0.24f, 0.14f, 0.09f, 1f);
                text.alignment = TextAlignmentOptions.Bottom;
            }

            var button = go.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(onClick);
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static TMP_Text CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        static Button CreateInvisibleButton(string name, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            return button;
        }

        static Button CreateCloseButton(string name, Transform parent, UnityEngine.Events.UnityAction onClick, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = sprite == null ? new Color(1f, 1f, 1f, 0.01f) : Color.white;
            image.preserveAspect = true;

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
            return button;
        }

        static bool IsRuntimeObject(Component component)
        {
            return component != null && component.gameObject.name.StartsWith("Runtime", StringComparison.Ordinal);
        }

        static void ApplyRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
