using UnityEngine;

namespace BES.Interactions
{
    public enum BESInteractionType
    {
        NPC,
        PhotoSpot,
        Sitting,
        Fishing,
        Boat,
        Emote,
        ResourceGathering,
        Spawn
    }

    [RequireComponent(typeof(Collider))]
    public class BESInteractionZone : MonoBehaviour
    {
        public BESInteractionType interactionType;
        public string displayName;
        [TextArea(2, 4)] public string notes;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnDrawGizmos()
        {
            Color color = interactionType switch
            {
                BESInteractionType.PhotoSpot => new Color(0.25f, 0.75f, 1f, 0.45f),
                BESInteractionType.Fishing => new Color(0.1f, 0.55f, 1f, 0.45f),
                BESInteractionType.Sitting => new Color(1f, 0.8f, 0.2f, 0.45f),
                BESInteractionType.Boat => new Color(0.5f, 0.3f, 0.15f, 0.45f),
                BESInteractionType.ResourceGathering => new Color(0.2f, 0.9f, 0.25f, 0.45f),
                BESInteractionType.Spawn => new Color(0.4f, 1f, 0.8f, 0.45f),
                _ => new Color(0.9f, 0.6f, 1f, 0.45f)
            };

            Gizmos.color = color;
            Gizmos.DrawCube(transform.position, transform.lossyScale);
            Gizmos.color = new Color(color.r, color.g, color.b, 1f);
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
    }
}
