using System;
using System.Collections.Generic;
using System.Linq;
using BES.Core;
using BES.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public enum BagCategory
    {
        All,
        Supplies,
        Equipment,
        Materials,
        Mementos
    }

    [Serializable]
    public class BagSlotView
    {
        public Button button;
        public Image background;
        public Image icon;
        public TMP_Text quantityText;
        public GameObject selectedState;
    }

    [Serializable]
    public class BagCategoryButton
    {
        public BagCategory category;
        public Button button;
        public GameObject selectedState;
    }

    public class BagPanelController : MonoBehaviour
    {
        [SerializeField] InventorySystem inventory;
        [SerializeField] List<BagCategoryButton> categoryButtons = new();
        [SerializeField] List<BagSlotView> slots = new();
        [SerializeField] Image detailIcon;
        [SerializeField] Image detailCardBackground;
        [SerializeField] TMP_Text detailNameText;
        [SerializeField] TMP_Text detailDescriptionText;
        [SerializeField] TMP_Text detailQuantityText;
        [SerializeField] Button useButton;
        [SerializeField] Button removeButton;
        [SerializeField] GameObject emptyDetailState;
        [SerializeField] Sprite fiveStarSlotSprite;
        [SerializeField] Sprite fourStarSlotSprite;
        [SerializeField] Sprite threeStarSlotSprite;
        [SerializeField] Sprite emptySlotSprite;
        [SerializeField] Color emptySlotColor = new(.69f, .66f, .57f, 1f);
        [SerializeField] List<Color> rarityColors = new()
        {
            new Color(.67f, .78f, .78f, 1f),
            new Color(.90f, .68f, .89f, 1f),
            new Color(.95f, .78f, .32f, 1f),
            new Color(.62f, .53f, .82f, 1f),
            new Color(.90f, .55f, .33f, 1f)
        };

        readonly List<string> visibleItemIds = new();
        BagCategory currentCategory;
        string selectedItemId;

        void Awake()
        {
            ResolveInventory();
            for (var i = 0; i < categoryButtons.Count; i++)
            {
                var category = categoryButtons[i].category;
                categoryButtons[i].button?.onClick.AddListener(() => SelectCategory(category));
            }
            for (var i = 0; i < slots.Count; i++)
            {
                var index = i;
                slots[i].button?.onClick.AddListener(() => SelectSlot(index));
            }
            useButton?.onClick.AddListener(UseSelected);
            removeButton?.onClick.AddListener(RemoveSelected);
            SelectCategory(BagCategory.All);
        }

        void OnEnable()
        {
            ResolveInventory();
            Refresh();
        }

        public void SelectCategory(BagCategory category)
        {
            currentCategory = category;
            selectedItemId = null;
            Refresh();
        }

        public void Refresh()
        {
            ResolveInventory();
            visibleItemIds.Clear();
            if (inventory != null)
            {
                visibleItemIds.AddRange(
                    inventory.Items
                        .Where(x => x.Value > 0 && MatchesCategory(inventory.GetDefinition(x.Key)))
                        .OrderByDescending(x => inventory.GetDefinition(x.Key)?.rarity ?? 0)
                        .ThenBy(x => inventory.GetDefinition(x.Key)?.displayName ?? x.Key)
                        .Select(x => x.Key));
            }

            if (!string.IsNullOrEmpty(selectedItemId) &&
                !visibleItemIds.Contains(selectedItemId))
                selectedItemId = null;

            RefreshCategoryButtons();
            RefreshSlots();
            RefreshDetails();
        }

        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= visibleItemIds.Count)
                selectedItemId = null;
            else
                selectedItemId = visibleItemIds[slotIndex];
            RefreshSlots();
            RefreshDetails();
        }

        public void UseSelected()
        {
            if (inventory == null || string.IsNullOrEmpty(selectedItemId)) return;
            var definition = inventory.GetDefinition(selectedItemId);
            var success = definition != null && definition.itemType switch
            {
                ItemType.Consumable => inventory.TryUseItem(selectedItemId),
                ItemType.Weapon => inventory.TryEquipWeaponItem(selectedItemId),
                _ => false
            };
            if (success) Refresh();
        }

        public void RemoveSelected()
        {
            if (inventory == null || string.IsNullOrEmpty(selectedItemId)) return;
            if (inventory.RemoveItem(selectedItemId, 1)) Refresh();
        }

        void ResolveInventory()
        {
            inventory ??= GameManager.Instance != null
                ? GameManager.Instance.Inventory
                : FindAnyObjectByType<InventorySystem>();
        }

        bool MatchesCategory(ItemDefinition definition)
        {
            if (currentCategory == BagCategory.All) return true;
            if (definition == null) return false;
            return currentCategory switch
            {
                BagCategory.Supplies => definition.itemType == ItemType.Consumable,
                BagCategory.Equipment => definition.itemType == ItemType.Weapon,
                BagCategory.Materials => definition.itemType == ItemType.Material,
                BagCategory.Mementos => definition.itemType == ItemType.Quest,
                _ => true
            };
        }

        void RefreshCategoryButtons()
        {
            foreach (var category in categoryButtons)
            {
                var selected = category.category == currentCategory;
                if (category.selectedState != null) category.selectedState.SetActive(selected);
                if (category.button != null) category.button.interactable = !selected;
            }
        }

        void RefreshSlots()
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;
                var hasItem = i < visibleItemIds.Count;
                var itemId = hasItem ? visibleItemIds[i] : null;
                var definition = hasItem ? inventory?.GetDefinition(itemId) : null;
                var amount = hasItem && inventory != null ? inventory.GetCount(itemId) : 0;

                if (slot.button != null) slot.button.interactable = hasItem;
                if (slot.icon != null)
                {
                    slot.icon.sprite = definition?.icon;
                    slot.icon.enabled = definition?.icon != null;
                }
                if (slot.quantityText != null)
                    slot.quantityText.text = hasItem && amount > 1 ? amount.ToString() : string.Empty;
                if (slot.background != null)
                {
                    var rarityIndex = Mathf.Clamp((definition?.rarity ?? 0) - 1, 0, rarityColors.Count - 1);
                    slot.background.sprite = hasItem
                        ? SlotSprite(definition?.rarity ?? 3)
                        : emptySlotSprite;
                    slot.background.color =
                        slot.background.sprite != null
                            ? Color.white
                            : hasItem && rarityColors.Count > 0
                                ? rarityColors[rarityIndex]
                                : emptySlotColor;
                }
                if (slot.selectedState != null)
                    slot.selectedState.SetActive(hasItem && itemId == selectedItemId);
            }
        }

        void RefreshDetails()
        {
            var hasSelection =
                inventory != null &&
                !string.IsNullOrEmpty(selectedItemId) &&
                inventory.GetCount(selectedItemId) > 0;
            var definition = hasSelection ? inventory.GetDefinition(selectedItemId) : null;
            if (emptyDetailState != null) emptyDetailState.SetActive(!hasSelection);
            if (detailIcon != null)
            {
                detailIcon.sprite = definition?.icon;
                detailIcon.enabled = definition?.icon != null;
            }
            if (detailCardBackground != null)
            {
                detailCardBackground.sprite = hasSelection
                    ? SlotSprite(definition?.rarity ?? 3)
                    : emptySlotSprite;
                detailCardBackground.color = detailCardBackground.sprite != null
                    ? Color.white
                    : emptySlotColor;
            }
            if (detailNameText != null)
                detailNameText.text = definition?.displayName ?? string.Empty;
            if (detailDescriptionText != null)
                detailDescriptionText.text = definition?.description ?? string.Empty;
            if (detailQuantityText != null)
                detailQuantityText.text = hasSelection
                    ? $"OWNED: {inventory.GetCount(selectedItemId)}"
                    : string.Empty;

            var usable = definition != null &&
                         (definition.itemType == ItemType.Consumable ||
                          definition.itemType == ItemType.Weapon);
            if (useButton != null)
            {
                useButton.interactable = usable;
                var label = useButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = definition?.itemType == ItemType.Weapon ? "EQUIP" : "USE";
            }
            if (removeButton != null) removeButton.interactable = hasSelection;
        }

        Sprite SlotSprite(int rarity) =>
            rarity >= 5 ? fiveStarSlotSprite :
            rarity == 4 ? fourStarSlotSprite :
            threeStarSlotSprite;
    }
}
