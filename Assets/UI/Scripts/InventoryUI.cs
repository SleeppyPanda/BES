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

        InventorySystem inventory;
        bool showMaterials;

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake()
        {
            inventory = GameManager.Instance != null
                ? GameManager.Instance.Inventory
                : FindAnyObjectByType<InventorySystem>();

            if (panel != null)
                panel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (itemsTabButton != null) itemsTabButton.onClick.AddListener(() => { showMaterials = false; Refresh(); });
            if (materialsTabButton != null) materialsTabButton.onClick.AddListener(() => { showMaterials = true; Refresh(); });
        }

        public void Toggle()
        {
            if (panel == null)
                return;

            panel.SetActive(!panel.activeSelf);
            if (panel.activeSelf)
                Refresh();
        }

        public void Close()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        void Refresh()
        {
            inventory ??= GameManager.Instance?.Inventory;
            if (listContainer == null || inventory == null)
                return;

            for (var i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);

            var hasItems = false;
            foreach (var pair in inventory.Items)
            {
                var def = inventory.GetDefinition(pair.Key);
                if (def == null)
                    continue;

                var isMaterial = def.itemType == ItemType.Material;
                if (showMaterials != isMaterial)
                    continue;

                hasItems = true;
                var label = $"{def.displayName} x{pair.Value}";
                var itemId = pair.Key;

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
            }

            if (!hasItems && itemRowPrefab != null)
            {
                var emptyRow = Instantiate(itemRowPrefab, listContainer);
                emptyRow.text = "(Trống)";
            }

            if (detailText != null)
                detailText.text = showMaterials ? "Tab: Nguyên liệu" : "Tab: Vật phẩm — nhấn slot để dùng/trang bị";
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
                        detailText.text = def.description;
                    break;
            }
        }
    }
}
