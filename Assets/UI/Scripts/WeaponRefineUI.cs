using BES.Core;
using BES.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class WeaponRefineUI : UIScreenBase
    {
        [SerializeField] TMP_Text refineLevelText;
        [SerializeField] TMP_Text effectText;
        [SerializeField] Button doneButton;

        void Awake()
        {
            if (root == null)
                root = gameObject;
            Hide();
            if (doneButton != null) doneButton.onClick.AddListener(OnDone);
        }

        public override void Refresh()
        {
            var refine = EquippedWeaponState.Instance?.Refinement ?? 1;
            if (refineLevelText != null) refineLevelText.text = $"Refinement Rank {refine}";
            if (effectText != null)
                effectText.text = $"Cần Crystal x{WeaponUpgradeCosts.RefineCrystalCost} — +{refine * 12}% ATK bonus";
        }

        void OnDone()
        {
            var inv = GameManager.Instance?.Inventory;
            if (inv != null &&
                inv.RemoveItem(WeaponUpgradeCosts.CrystalItemId, WeaponUpgradeCosts.RefineCrystalCost))
            {
                EquippedWeaponState.Instance?.EnhanceRefinement(1);
                GameManager.Instance?.SaveGame();
            }

            Hide();
        }
    }
}
