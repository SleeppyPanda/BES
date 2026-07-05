using BES.Core;
using UnityEngine;

namespace BES.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class WorldRegion : MonoBehaviour
    {
        [SerializeField] string regionId = "region_creation_city";
        [SerializeField] string regionName = "Creation City Outskirts";
        [TextArea][SerializeField] string description;
        [SerializeField] bool autoCreateTrigger = true;
        [SerializeField] Vector2 mapBoundsMin = new(-25f, -25f);
        [SerializeField] Vector2 mapBoundsMax = new(25f, 25f);

        public string RegionId => regionId;
        public string RegionName => regionName;
        public Vector2 MapBoundsMin => mapBoundsMin;
        public Vector2 MapBoundsMax => mapBoundsMax;

        void Awake()
        {
            if (!autoCreateTrigger)
                return;

            var col = GetComponent<Collider>();
            if (col == null)
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 12f;
            }
            else
            {
                col.isTrigger = true;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            MetaProgressState.Instance?.DiscoverRegion(regionId);
            GameEvents.RaiseRegionEntered(regionId);

            var hud = FindAnyObjectByType<UI.HUDController>();
            hud?.SetRegion(regionName);
        }
    }
}
