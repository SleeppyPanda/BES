using UnityEngine;

namespace BES.NPC
{
    public enum BESNpcRole
    {
        Social,
        Merchant,
        Fisherman,
        Photographer,
        QuestGiver,
        Animal
    }

    public class BESNpcMarker : MonoBehaviour
    {
        public BESNpcRole role;
        public string npcName;
        [TextArea(2, 4)] public string purpose;

        private void OnDrawGizmos()
        {
            Gizmos.color = role == BESNpcRole.Animal ? Color.green : new Color(1f, 0.75f, 0.2f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.6f);
        }
    }
}
