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

            var nextLevel = equipped != null ? equipped.SimulateLevelAfterExp(6500) : 1;
            var nextAtk = equipped != null ? equipped.GetSimulatedAtk(nextLevel) : atk;
            if (afterAtkText != null) afterAtkText.text = $"ATK {nextAtk} (Lv.{nextLevel})";

            if (materialsText != null)
                materialsText.text =
                    $"Cần: Ore x{WeaponUpgradeCosts.EnhanceOreCost}, Crystal x{WeaponUpgradeCosts.EnhanceCrystalCost}\nVàng: 4,550";
        }

        void OnConfirm()
        {
            var inv = GameManager.Instance?.Inventory;
            if (inv == null)
                return;

            var equipped = EquippedWeaponState.Instance;
            if (equipped == null || equipped.Level >= 80)
            {
                if (materialsText != null)
                    materialsText.text = "Vũ khí đã đạt cấp tối đa!";
                return;
            }

            int goldCost = 4550;
            if (PlayerWallet.Instance == null || PlayerWallet.Instance.Coins < goldCost)
            {
                if (materialsText != null)
                    materialsText.text = "Không đủ vàng!";
                return;
            }

            if (!inv.RemoveItem(WeaponUpgradeCosts.OreItemId, WeaponUpgradeCosts.EnhanceOreCost) ||
                !inv.RemoveItem(WeaponUpgradeCosts.CrystalItemId, WeaponUpgradeCosts.EnhanceCrystalCost))
            {
                if (materialsText != null)
                    materialsText.text = "Không đủ nguyên liệu!";
                return;
            }

            // Deduct gold
            PlayerWallet.Instance.TrySpendCoins(goldCost);

            // Add EXP (5 Ores * 500 = 2500, 2 Crystals * 2000 = 4000, Total = 6500)
            equipped.AddExperience(6500);

            GameManager.Instance?.SaveGame();
            Hide();
            rankUpUI?.Show();
        }
    }
}
