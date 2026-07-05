using BES.Core;
using BES.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class WeaponRankUpUI : UIScreenBase
    {
        [SerializeField] TMP_Text rankText;
        [SerializeField] TMP_Text resultText;
        [SerializeField] Button confirmButton;
        [SerializeField] Button backButton;
        [SerializeField] WeaponRefineUI refineUI;

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
            var refine = EquippedWeaponState.Instance?.Refinement ?? 1;
            if (rankText != null) rankText.text = $"Rank Up → {refine + 1}";
            if (resultText != null)
                resultText.text = $"Cần Ore x{WeaponUpgradeCosts.RankOreCost}";
        }

        void OnConfirm()
        {
            var inv = GameManager.Instance?.Inventory;
            if (inv == null || !inv.RemoveItem(WeaponUpgradeCosts.OreItemId, WeaponUpgradeCosts.RankOreCost))
            {
                if (resultText != null)
                    resultText.text = "Không đủ Ore!";
                return;
            }

            EquippedWeaponState.Instance?.EnhanceRefinement(1);
            GameManager.Instance?.SaveGame();
            Hide();
            refineUI?.Show();
        }
    }
}
