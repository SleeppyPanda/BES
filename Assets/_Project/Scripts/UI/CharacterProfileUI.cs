using BES.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class CharacterProfileUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text atkText;
        [SerializeField] TMP_Text hpText;
        [SerializeField] TMP_Text defText;
        [SerializeField] TMP_Text critRateText;
        [SerializeField] TMP_Text critDmgText;
        [SerializeField] EquipmentUI equipmentUI;
        [SerializeField] PartyRoster partyRoster;
        [SerializeField] TMP_Text[] partySlotTexts = new TMP_Text[4];
        [SerializeField] WeaponScreenUI weaponScreenUI;
        [SerializeField] CharacterPreviewRenderer previewRenderer;
        [SerializeField] Button closeButton;

        PlayerStats stats;

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake()
        {
            stats = FindAnyObjectByType<PlayerStats>();
            equipmentUI ??= GetComponentInChildren<EquipmentUI>(true);
            partyRoster ??= FindAnyObjectByType<PartyRoster>();
            previewRenderer ??= GetComponentInChildren<CharacterPreviewRenderer>(true);
            if (panel != null)
                panel.SetActive(false);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
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

        public void OpenWeaponScreen()
        {
            Close();
            weaponScreenUI?.Show();
        }

        public void Refresh()
        {
            stats ??= FindAnyObjectByType<PlayerStats>();
            partyRoster ??= FindAnyObjectByType<PartyRoster>();

            if (stats != null)
            {
                var weaponAtk = EquippedWeaponState.Instance?.GetDisplayAtk() ?? 0;
                if (nameText != null) nameText.text = "Main Character";
                if (levelText != null) levelText.text = "Lv. 99 / 100";
                if (atkText != null) atkText.text = $"ATK: {stats.AttackPower + weaponAtk:0} (base {stats.AttackPower:0} + weapon {weaponAtk})";
                if (hpText != null) hpText.text = $"HP: {stats.MaxHealth:0}";
                if (defText != null) defText.text = $"DEF: {stats.Defense:0}";
                if (critRateText != null) critRateText.text = $"Crit Rate: {stats.CritRate * 100f:0}%";
                if (critDmgText != null) critDmgText.text = $"Crit DMG: {stats.CritDamage * 100f:0}%";
            }

            if (partyRoster != null && partySlotTexts != null)
            {
                for (var i = 0; i < partySlotTexts.Length; i++)
                {
                    if (partySlotTexts[i] == null)
                        continue;
                    var slot = partyRoster.GetSlot(i);
                    partySlotTexts[i].text = slot != null ? $"{i + 1:D2} {slot.displayName}" : $"{i + 1:D2} —";
                }
            }

            equipmentUI?.Refresh();
            previewRenderer?.RenderFrame();
        }
    }
}
