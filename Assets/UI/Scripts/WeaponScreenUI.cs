using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BES.Gameplay;

namespace BES.UI
{
    public class WeaponScreenUI : UIScreenBase
    {
        [SerializeField] WeaponDatabase database;
        [SerializeField] Transform gridContainer;
        [SerializeField] TMP_Text weaponNameText;
        [SerializeField] TMP_Text weaponDescText;
        [SerializeField] TMP_Text atkText;
        [SerializeField] TMP_Text hpText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text refineText;
        [SerializeField] Button switchButton;
        [SerializeField] Button removeButton;
        [SerializeField] Button enhanceButton;
        [SerializeField] Button closeButton;
        [SerializeField] WeaponEnhanceUI enhanceUI;
        [SerializeField] CharacterPreviewRenderer previewRenderer;

        readonly List<GameObject> slotRows = new();
        string selectedWeaponId;

        void Awake()
        {
            database ??= Resources.Load<WeaponDatabase>("Data/WeaponDatabase");
            if (root == null)
                root = gameObject;
            Hide();

            if (switchButton != null) switchButton.onClick.AddListener(OnSwitch);
            if (removeButton != null) removeButton.onClick.AddListener(OnRemove);
            if (enhanceButton != null) enhanceButton.onClick.AddListener(OnEnhance);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public override void Refresh()
        {
            selectedWeaponId ??= EquippedWeaponState.Instance?.EquippedWeaponId;
            RebuildGrid();
            RefreshDetails();
        }

        void RebuildGrid()
        {
            if (gridContainer == null || database == null)
                return;

            foreach (var row in slotRows)
                Object.Destroy(row);
            slotRows.Clear();

            foreach (var weapon in database.weapons)
            {
                if (weapon == null)
                    continue;

                var go = new GameObject(weapon.weaponId);
                go.transform.SetParent(gridContainer, false);
                var img = go.AddComponent<Image>();
                img.color = weapon.rarity switch
                {
                    ItemRarity.FiveStar => new Color(0.95f, 0.78f, 0.28f, 0.9f),
                    ItemRarity.FourStar => new Color(0.64f, 0.49f, 0.95f, 0.9f),
                    _ => new Color(0.32f, 0.68f, 0.95f, 0.9f)
                };
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(80, 80);
                var btn = go.AddComponent<Button>();
                var captured = weapon.weaponId;
                btn.onClick.AddListener(() =>
                {
                    selectedWeaponId = captured;
                    RefreshDetails();
                });
                slotRows.Add(go);
            }
        }

        public static string GetWeaponSkillDescription(string weaponId, int refinement)
        {
            refinement = Mathf.Clamp(refinement, 1, 5);
            return weaponId switch
            {
                "weapon_iron_sword" => $"Tăng {5 + refinement * 5}% sát thương đòn đánh thường. Tinh luyện 5: hồi thêm 2% HP khi hạ gục kẻ địch.",
                "weapon_void_edge" => $"Tăng {6 + refinement * 6}% Tấn công. Tinh luyện 5: bỏ qua 10% phòng thủ của mục tiêu.",
                "weapon_flame_blade" => $"Tăng {7.5f + refinement * 7.5f}% sát thương nguyên tố. Tinh luyện 5: tạo lá chắn bảo vệ tương đương 15% HP tối đa khi dùng kỹ năng.",
                _ => $"Kỹ năng vũ khí đặc biệt tăng {refinement * 10}% hiệu suất chiến đấu."
            };
        }

        void RefreshDetails()
        {
            var weapon = database?.GetById(selectedWeaponId);
            var equipped = EquippedWeaponState.Instance;
            if (weapon == null)
                return;

            if (weaponNameText != null) weaponNameText.text = weapon.displayName;

            var isEquipped = equipped != null && equipped.EquippedWeaponId == selectedWeaponId;
            var lvl = isEquipped ? equipped.Level : 1;
            var refi = isEquipped ? equipped.Refinement : 1;
            var displayAtk = weapon.baseAtk + (lvl - 1) * 8 + refi * 12;

            var currentSubStatValue = weapon.subStatValue + (lvl - 1) * (weapon.subStatValue * 0.05f);

            if (weaponDescText != null)
            {
                weaponDescText.text = $"{weapon.description}\n\n" +
                    $"<b>[Thuộc tính phụ]</b> {weapon.subStatName}: +{currentSubStatValue:F1}%\n\n" +
                    $"<b>[Kỹ năng vũ khí - Tinh luyện {refi}]</b> {GetWeaponSkillDescription(weapon.weaponId, refi)}";
            }

            if (atkText != null) atkText.text = $"ATK {displayAtk}";
            if (hpText != null) hpText.text = $"HP {weapon.baseHp}";
            if (levelText != null) levelText.text = $"Lv. {lvl} / {Mathf.Min(weapon.maxLevel, 80)}";
            if (refineText != null) refineText.text = $"Refinement Rank {refi}";
        }

        void OnSwitch()
        {
            if (EquippedWeaponState.Instance != null && !string.IsNullOrEmpty(selectedWeaponId))
            {
                EquippedWeaponState.Instance.UnlockWeapon(selectedWeaponId);
                EquippedWeaponState.Instance.Equip(selectedWeaponId);
                Core.GameManager.Instance?.SaveGame();
            }
            RefreshDetails();
        }

        void OnRemove()
        {
            EquippedWeaponState.Instance?.Unequip();
            Core.GameManager.Instance?.SaveGame();
            RefreshDetails();
        }

        void OnEnhance()
        {
            Hide();
            enhanceUI?.Show();
        }
    }
}
