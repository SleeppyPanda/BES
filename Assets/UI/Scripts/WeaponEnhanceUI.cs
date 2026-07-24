using BES.Core;
using BES.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class WeaponEnhanceUI : UIScreenBase
    {
        [SerializeField] TMP_Text beforeAtkText;
        [SerializeField] TMP_Text afterAtkText;
        [SerializeField] TMP_Text materialsText;
        [SerializeField] Button confirmButton;
        [SerializeField] Button backButton;
        [SerializeField] WeaponRankUpUI rankUpUI;

        void Awake()
        {
            if (root == null)
                root = gameObject;
            Hide();
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            if (backButton != null) backButton.onClick.AddListener(Hide);
        }

        public override void Refresh()
        {
            var equipped = EquippedWeaponState.Instance;
            var atk = equipped?.GetDisplayAtk() ?? 15;
            if (beforeAtkText != null) beforeAtkText.text = $"ATK {atk}";
            if (afterAtkText != null) afterAtkText.text = $"ATK {atk + 24}";
            if (materialsText != null)
                materialsText.text =
                    $"Materials: Ore x{WeaponUpgradeCosts.EnhanceOreCost}, Crystal x{WeaponUpgradeCosts.EnhanceCrystalCost}";
        }

        void OnConfirm()
        {
            var inv = GameManager.Instance?.Inventory;
            if (inv == null)
                return;

            if (!inv.RemoveItem(WeaponUpgradeCosts.OreItemId, WeaponUpgradeCosts.EnhanceOreCost) ||
                !inv.RemoveItem(WeaponUpgradeCosts.CrystalItemId, WeaponUpgradeCosts.EnhanceCrystalCost))
            {
                if (materialsText != null)
                    materialsText.text = "Không đủ nguyên liệu!";
                return;
            }

            EquippedWeaponState.Instance?.EnhanceLevel(1);
            GameManager.Instance?.SaveGame();
            Hide();
            rankUpUI?.Show();
        }
    }
}
