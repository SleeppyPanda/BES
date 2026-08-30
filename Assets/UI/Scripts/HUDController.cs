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
        [SerializeField] Image healthFillImage;
        [SerializeField] Image staminaFillImage;
        [SerializeField] TMP_Text hpValueText;
        [SerializeField] TMP_Text staminaValueText;
        [SerializeField] TMP_Text regionText;

        void Awake()
        {
            HideLegacyChild("ManaBar");
            HideLegacyChild("LevelText");
        }

        void OnEnable()
        {
            GameEvents.OnPlayerHealthChanged += UpdateHealth;
            GameEvents.OnPlayerStaminaChanged += UpdateStamina;
        }

        void OnDisable()
        {
            GameEvents.OnPlayerHealthChanged -= UpdateHealth;
            GameEvents.OnPlayerStaminaChanged -= UpdateStamina;
        }

        void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                var motor = FindAnyObjectByType<PlayerMotor>();
                if (motor != null) player = motor.gameObject;
            }

            if (player != null)
            {
                if (player.TryGetComponent<PlayerStats>(out var stats))
                    UpdateHealth(stats.CurrentHealth, stats.MaxHealth);

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

            if (staminaValueText != null)
                staminaValueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        public void SetRegion(string regionName)
        {
            if (regionText != null)
                regionText.text = string.IsNullOrEmpty(regionName) ? string.Empty : regionName;
        }

        void HideLegacyChild(string childName)
        {
            var child = transform.Find(childName);
            if (child != null)
                child.gameObject.SetActive(false);
        }
    }
}
