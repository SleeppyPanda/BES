using BES.Core;
using BES.UI;
using UnityEngine;

namespace BES.Narrative
{
    [RequireComponent(typeof(QuestMarker))]
    public class NPCInteractable : MonoBehaviour
    {
        [SerializeField] string npcId = "npc_guard";
        [SerializeField] string npcDisplayName = "City Guard";
        [SerializeField] string startDialogueNodeId = "intro_guard";
        [SerializeField] float interactRange = 2.5f;

        Transform player;
        bool wasInRange;

        void Start()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;

            var marker = GetComponent<QuestMarker>() ?? gameObject.AddComponent<QuestMarker>();
            marker.SetMarkerId(npcId);
        }

        void Update()
        {
            if (player == null)
                return;

            var inRange = Vector3.Distance(transform.position, player.position) <= interactRange;
            if (inRange && !wasInRange)
                GameEvents.RaiseNpcInRange(npcDisplayName);
            else if (!inRange && wasInRange)
                GameEvents.RaiseNpcOutOfRange();

            wasInRange = inRange;

            if (inRange && player.TryGetComponent<Gameplay.PlayerInputReader>(out var input) && input.InteractPressed)
                StartInteraction();
        }

        void StartInteraction()
        {
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.StartDialogue(startDialogueNodeId))
                return;

            var ai = FindAnyObjectByType<AIDialogueService>();
            var ui = FindAnyObjectByType<DialogueUI>();
            if (ui != null)
                ui.OpenFreeChat(npcId, npcDisplayName, ai);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
