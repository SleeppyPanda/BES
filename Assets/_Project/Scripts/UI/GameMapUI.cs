using BES.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class GameMapUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text regionCreationText;
        [SerializeField] TMP_Text regionRuinsText;
        [SerializeField] TMP_Text regionForestText;
        [SerializeField] Transform markersContainer;
        [SerializeField] GameObject mapMarkerPrefab;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text statusText;

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake()
        {
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

        void Refresh()
        {
            if (markersContainer != null && mapMarkerPrefab != null)
            {
                for (var i = markersContainer.childCount - 1; i >= 0; i--)
                    Destroy(markersContainer.GetChild(i).gameObject);

                CreateMarker("region_creation_city", "Creation City Outskirts");
                CreateMarker("region_ruins", "Ancient Ruins");
                CreateMarker("region_forest", "Whispering Forest");
            }
            else
            {
                SetLegacyText(regionCreationText, "region_creation_city", "Creation City Outskirts");
                SetLegacyText(regionRuinsText, "region_ruins", "Ancient Ruins");
                SetLegacyText(regionForestText, "region_forest", "Whispering Forest");
            }
        }

        void SetLegacyText(TMP_Text text, string regionId, string displayName)
        {
            if (text == null)
                return;

            var explored = MetaProgressState.Instance == null ||
                           MetaProgressState.Instance.IsRegionDiscovered(regionId);
            text.text = explored ? $"{displayName} — Đã khám phá" : displayName;
        }

        void CreateMarker(string regionId, string displayName)
        {
            var explored = MetaProgressState.Instance == null ||
                           MetaProgressState.Instance.IsRegionDiscovered(regionId);
            var canTravel = explored &&
                            (MetaProgressState.Instance == null ||
                             MetaProgressState.Instance.IsTeleportUnlocked(regionId) ||
                             MetaProgressState.Instance.IsRegionDiscovered(regionId));

            var go = Instantiate(mapMarkerPrefab, markersContainer);
            var marker = go.GetComponent<UIMapMarker>();
            marker?.Setup(regionId, displayName, explored, id =>
            {
                if (TeleportService.TryFastTravelToRegion(id))
                {
                    if (statusText != null)
                        statusText.text = $"Dịch chuyển tới {displayName}";
                    Close();
                }
                else if (statusText != null)
                {
                    statusText.text = canTravel
                        ? "Không tìm thấy điểm dịch chuyển."
                        : "Khu vực chưa mở khóa.";
                }
            });
        }
    }
}
