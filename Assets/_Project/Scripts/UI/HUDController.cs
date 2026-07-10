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
            healthFillImage = ResolveFillImage(healthFillImage, healthBar);
            staminaFillImage = ResolveFillImage(staminaFillImage, staminaBar);
            ConfigureFillImage(healthFillImage);
            ConfigureFillImage(staminaFillImage);
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
            SetFillAmount(healthFillImage, current, max);

            if (hpValueText != null)
                hpValueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        void UpdateStamina(float current, float max)
        {
            SetFillAmount(staminaFillImage, current, max);

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

        static Image ResolveFillImage(Image assignedImage, Slider slider)
        {
            if (assignedImage != null)
                return assignedImage;

            return slider != null && slider.fillRect != null
                ? slider.fillRect.GetComponent<Image>()
                : null;
        }

        static void ConfigureFillImage(Image fillImage)
        {
            if (fillImage == null)
                return;

            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        static void SetFillAmount(Image fillImage, float current, float max)
        {
            if (fillImage == null)
                return;

            fillImage.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }
    }
}
