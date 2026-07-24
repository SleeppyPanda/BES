using BES.Core;
using UnityEngine;

namespace BES.Gameplay
{
    public class TeleportPoint : MonoBehaviour
    {
        [SerializeField] string pointId = "tp_city_gate";
        [SerializeField] Transform destination;
        [SerializeField] string regionId = "region_creation_city";

        public string PointId => pointId;
        public string RegionId => regionId;
        public Transform Destination => destination;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || destination == null)
                return;

            TeleportService.TeleportPlayer(other.transform, destination.position, destination.rotation, pointId, regionId);
        }
    }
}
