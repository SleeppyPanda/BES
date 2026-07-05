using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class UISettingsRow : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] Toggle toggle;
        [SerializeField] Slider slider;

        public void SetupToggle(string text, bool value, System.Action<bool> onChanged)
        {
            if (label != null) label.text = text;
            if (toggle != null)
            {
                toggle.gameObject.SetActive(true);
                if (slider != null) slider.gameObject.SetActive(false);
                toggle.isOn = value;
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            }
        }

        public void SetupSlider(string text, float value, System.Action<float> onChanged)
        {
            if (label != null) label.text = text;
            if (slider != null)
            {
                slider.gameObject.SetActive(true);
                if (toggle != null) toggle.gameObject.SetActive(false);
                slider.value = value;
                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            }
        }
    }
}
