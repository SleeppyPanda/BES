using BES.Core;
using BES.Gameplay;
using BES.Narrative;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] Slider healthBar;
        [SerializeField] Slider staminaBar;
        [SerializeField] Slider manaBar;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text hpValueText;
        [SerializeField] TMP_Text regionText;

        void OnEnable()
        {
            GameEvents.OnPlayerHealthChanged += UpdateHealth;
            GameEvents.OnPlayerStaminaChanged += UpdateStamina;
            GameEvents.OnPlayerManaChanged += UpdateMana;
        }

        void OnDisable()
        {
            GameEvents.OnPlayerHealthChanged -= UpdateHealth;
            GameEvents.OnPlayerStaminaChanged -= UpdateStamina;
            GameEvents.OnPlayerManaChanged -= UpdateMana;
        }

        void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (player.TryGetComponent<PlayerStats>(out var stats))
                {
                    UpdateHealth(stats.CurrentHealth, stats.MaxHealth);
                    UpdateMana(stats.CurrentMana, stats.MaxMana);
                }

                if (levelText != null)
                    levelText.text = "Level 1.";

                if (player.TryGetComponent<StaminaSystem>(out var stamina))
                    UpdateStamina(stamina.Current, stamina.Max);
            }
        }

        void UpdateHealth(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = max;
                healthBar.value = current;
            }

            if (hpValueText != null)
                hpValueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        void UpdateStamina(float current, float max)
        {
            if (staminaBar != null)
            {
                staminaBar.maxValue = max;
                staminaBar.value = current;
            }
        }

        void UpdateMana(float current, float max)
        {
            if (manaBar != null)
            {
                manaBar.maxValue = max;
                manaBar.value = current;
            }
        }

        public void SetLevel(int level)
        {
            if (levelText != null)
                levelText.text = $"Level {level}.";
        }

        public void SetRegion(string regionName)
        {
            if (regionText != null)
                regionText.text = string.IsNullOrEmpty(regionName) ? string.Empty : regionName;
        }
    }
}
