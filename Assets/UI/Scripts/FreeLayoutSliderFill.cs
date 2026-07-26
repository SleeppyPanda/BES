using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Slider))]
    public class FreeLayoutSliderFill : MonoBehaviour
    {
        [SerializeField] Slider slider;
        [SerializeField] Image fillImage;

        public void Configure(Slider source, Image image)
        {
            slider = source;
            fillImage = image;
            Refresh();
        }

        void Awake()
        {
            if (slider == null) slider = GetComponent<Slider>();
        }

        void OnEnable() => Refresh();
        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (slider == null || fillImage == null) return;
            fillImage.fillAmount = slider.normalizedValue;
        }
    }
}
