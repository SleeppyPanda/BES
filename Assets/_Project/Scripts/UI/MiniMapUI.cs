using BES.Core;
using BES.Gameplay;
using BES.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class MiniMapUI : MonoBehaviour
    {
        [SerializeField] RectTransform mapRect;
        [SerializeField] RawImage mapImage;
        [SerializeField] RectTransform playerIcon;
        [SerializeField] RectTransform objectiveIcon;
        [SerializeField] Button openMapButton;
        [SerializeField] UINavigationController navigation;
        [SerializeField] Texture mapTexture;
        [SerializeField] float mapZoom = 0.28f;
        [SerializeField] Vector2 worldMin = new(-25f, -25f);
        [SerializeField] Vector2 worldMax = new(25f, 25f);

        Transform player;
        Transform objective;

        void Start()
        {
            if (navigation == null)
                navigation = GetComponentInParent<UINavigationController>();
            if (openMapButton != null)
                openMapButton.onClick.AddListener(OpenWorldMap);

            if (mapImage != null && mapTexture != null)
                mapImage.texture = mapTexture;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;

            RefreshWorldBounds();
            RefreshObjective();
            GameEvents.OnQuestUpdated += OnQuestUpdated;
            GameEvents.OnRegionEntered += OnRegionEntered;
        }

        void OnDestroy()
        {
            GameEvents.OnQuestUpdated -= OnQuestUpdated;
            GameEvents.OnRegionEntered -= OnRegionEntered;
        }

        void OnQuestUpdated(string _) => RefreshObjective();

        void OnRegionEntered(string regionId)
        {
            var regions = FindObjectsByType<WorldRegion>(FindObjectsSortMode.None);
            foreach (var region in regions)
            {
                if (region.RegionId != regionId)
                    continue;

                worldMin = region.MapBoundsMin;
                worldMax = region.MapBoundsMax;
                return;
            }
        }

        void Update()
        {
            if (player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            if (player == null || mapRect == null || playerIcon == null)
                return;

            playerIcon.anchoredPosition = WorldToMap(player.position);
            UpdateMapWindow(player.position);

            if (objective != null && objectiveIcon != null)
            {
                objectiveIcon.gameObject.SetActive(true);
                objectiveIcon.anchoredPosition = WorldToMap(objective.position);
            }
            else if (objectiveIcon != null)
            {
                objectiveIcon.gameObject.SetActive(false);
            }
        }

        void RefreshWorldBounds()
        {
            var regions = FindObjectsByType<WorldRegion>(FindObjectsSortMode.None);
            if (regions.Length == 0)
                return;

            var min = regions[0].MapBoundsMin;
            var max = regions[0].MapBoundsMax;
            foreach (var region in regions)
            {
                min = Vector2.Min(min, region.MapBoundsMin);
                max = Vector2.Max(max, region.MapBoundsMax);
            }

            worldMin = min;
            worldMax = max;
        }

        void RefreshObjective()
        {
            var targetId = GameManager.Instance?.Quests.GetActiveQuestTargetId();
            objective = QuestMarker.GetMarker(targetId);
        }

        Vector2 WorldToMap(Vector3 worldPos)
        {
            var nx = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x);
            var ny = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.z);
            var size = mapRect.rect.size;
            return new Vector2((nx - 0.5f) * size.x, (ny - 0.5f) * size.y);
        }

        void UpdateMapWindow(Vector3 worldPos)
        {
            if (mapImage == null || mapImage.texture == null)
                return;

            var nx = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x);
            var ny = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.z);
            var zoom = Mathf.Clamp01(mapZoom);
            var minX = Mathf.Clamp01(nx - zoom * 0.5f);
            var minY = Mathf.Clamp01(ny - zoom * 0.5f);
            if (minX + zoom > 1f) minX = 1f - zoom;
            if (minY + zoom > 1f) minY = 1f - zoom;
            mapImage.uvRect = new Rect(minX, minY, zoom, zoom);
        }

        void OpenWorldMap()
        {
            if (navigation == null)
                navigation = GetComponentInParent<UINavigationController>();
            navigation?.ToggleWorldMap();
        }
    }
}
