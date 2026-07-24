using BES.Core;
using UnityEngine;

namespace BES.Gameplay
{
    public class Collectible : MonoBehaviour
    {
        [SerializeField] string instanceId;
        [SerializeField] string itemId = "herb_common";
        [SerializeField] int amount = 1;

        void Awake()
        {
            if (string.IsNullOrEmpty(instanceId))
                instanceId = $"collectible_{gameObject.name}_{transform.position.GetHashCode()}";

            if (MetaProgressState.Instance != null &&
                MetaProgressState.Instance.IsWorldObjectCollected(instanceId))
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            if (MetaProgressState.Instance != null &&
                MetaProgressState.Instance.IsWorldObjectCollected(instanceId))
                return;

            if (GameManager.Instance?.Inventory.AddItem(itemId, amount) != true)
                return;

            MetaProgressState.Instance?.MarkWorldObjectCollected(instanceId);
            GameEvents.RaiseCollectiblePickedUp(itemId);
            Destroy(gameObject);
        }
    }
}
