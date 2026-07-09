using BES.Core;
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

        void OnEnable() => GameEvents.OnPartyChanged += Refresh;

        void OnDisable() => GameEvents.OnPartyChanged -= Refresh;

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
                var activeCharacter = partyRoster?.ActiveCharacter;
                var weaponAtk = EquippedWeaponState.Instance?.GetDisplayAtk() ?? 0;
                if (nameText != null)
                    nameText.text = !string.IsNullOrEmpty(activeCharacter?.displayName) ? activeCharacter.displayName : "Main Character";
                if (levelText != null)
                {
                    var level = activeCharacter != null ? activeCharacter.level : 99;
                    var maxLevel = activeCharacter != null ? activeCharacter.maxLevel : 100;
                    levelText.text = $"Lv. {level} / {maxLevel}";
                }
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
                    var definition = slot != null ? partyRoster.GetCharacterDefinition(slot.characterId) : null;
                    var displayName = !string.IsNullOrEmpty(definition?.displayName) ? definition.displayName : slot?.displayName;
                    partySlotTexts[i].text = slot != null ? $"{i + 1:D2} {displayName}" : $"{i + 1:D2} -";
                }
            }

            equipmentUI?.Refresh();
            previewRenderer?.RenderFrame();
        }
    }
}
