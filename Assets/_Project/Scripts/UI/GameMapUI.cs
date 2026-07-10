using BES.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class GameMapUI : MonoBehaviour
    {
        [System.Serializable]
        public class TeleportMarkerConfig
        {
            public string regionId = "region_creation_city";
            public string displayName = "Creation City Outskirts";
            public Vector2 anchoredPosition;
        }

        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text regionCreationText;
        [SerializeField] TMP_Text regionRuinsText;
        [SerializeField] TMP_Text regionForestText;
        [SerializeField] Transform markersContainer;
        [SerializeField] GameObject mapMarkerPrefab;
        [SerializeField] UIMapMarker[] markerSlots;
        [SerializeField] TeleportMarkerConfig[] teleportMarkers =
        {
            new TeleportMarkerConfig { regionId = "region_creation_city", displayName = "Creation City Outskirts", anchoredPosition = new Vector2(-180f, -40f) },
            new TeleportMarkerConfig { regionId = "region_ruins", displayName = "Ancient Ruins", anchoredPosition = new Vector2(150f, 20f) },
            new TeleportMarkerConfig { regionId = "region_forest", displayName = "Whispering Forest", anchoredPosition = new Vector2(-40f, 130f) }
        };
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
            if (teleportMarkers != null && teleportMarkers.Length > 0 && markerSlots != null && markerSlots.Length > 0)
            {
                RefreshFixedMarkers();
            }
            else if (markersContainer != null && mapMarkerPrefab != null)
            {
                RefreshRuntimeMarkers();
            }
            else
            {
                SetLegacyText(regionCreationText, "region_creation_city", "Creation City Outskirts");
                SetLegacyText(regionRuinsText, "region_ruins", "Ancient Ruins");
                SetLegacyText(regionForestText, "region_forest", "Whispering Forest");
            }
        }

        void RefreshFixedMarkers()
        {
            for (var i = 0; i < markerSlots.Length; i++)
            {
                var marker = markerSlots[i];
                if (marker == null)
                    continue;

                var hasConfig = i < teleportMarkers.Length && teleportMarkers[i] != null;
                marker.gameObject.SetActive(hasConfig);
                if (!hasConfig)
                    continue;

                var config = teleportMarkers[i];
                var rect = marker.GetComponent<RectTransform>();
                if (rect != null)
                    rect.anchoredPosition = config.anchoredPosition;
                SetupMarker(marker, config.regionId, config.displayName);
            }
        }

        void RefreshRuntimeMarkers()
        {
            for (var i = markersContainer.childCount - 1; i >= 0; i--)
                Destroy(markersContainer.GetChild(i).gameObject);

            foreach (var config in teleportMarkers)
            {
                if (config == null)
                    continue;

                var go = Instantiate(mapMarkerPrefab, markersContainer);
                var rect = go.GetComponent<RectTransform>();
                if (rect != null)
                    rect.anchoredPosition = config.anchoredPosition;
                SetupMarker(go.GetComponent<UIMapMarker>(), config.regionId, config.displayName);
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

        void SetupMarker(UIMapMarker marker, string regionId, string displayName)
        {
            var explored = MetaProgressState.Instance == null ||
                           MetaProgressState.Instance.IsRegionDiscovered(regionId);
            var canTravel = explored &&
                            (MetaProgressState.Instance == null ||
                             MetaProgressState.Instance.IsTeleportUnlocked(regionId) ||
                             MetaProgressState.Instance.IsRegionDiscovered(regionId));

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
