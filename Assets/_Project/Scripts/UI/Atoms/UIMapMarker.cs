using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class UIMapMarker : MonoBehaviour
    {
        [SerializeField] TMP_Text regionLabel;
        [SerializeField] Image markerIcon;
        [SerializeField] Button button;

        public string RegionId { get; private set; }

        public void Setup(string regionId, string displayName, bool explored, System.Action<string> onClick)
        {
            RegionId = regionId;
            if (regionLabel != null)
                regionLabel.text = explored ? $"{displayName} — Đã khám phá" : displayName;
            if (markerIcon != null)
                markerIcon.color = explored ? Color.white : new Color(0.6f, 0.6f, 0.65f, 0.9f);
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(regionId));
            }
        }
    }
}
