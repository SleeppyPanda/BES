using BES.Core;
using BES.UI;
using UnityEngine;
using UnityEngine.InputSystem;

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
        float nextInteractTime;

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

            if (inRange && Time.time >= nextInteractTime && WasInteractPressed())
                StartInteraction();
        }

        void StartInteraction()
        {
            nextInteractTime = Time.time + 0.35f;

            if (DialogueSystem.Instance != null && DialogueSystem.Instance.StartDialogue(startDialogueNodeId))
            {
                GameEvents.RaiseNpcOutOfRange();
                return;
            }

            if (!string.IsNullOrEmpty(startDialogueNodeId))
            {
                Debug.LogWarning($"[BES NPC] Không mở được dialogue node '{startDialogueNodeId}' cho NPC '{npcDisplayName}'.");
                return;
            }

            var ai = FindAnyObjectByType<AIDialogueService>();
            var ui = FindAnyObjectByType<DialogueUI>();
            if (ui != null)
            {
                GameEvents.RaiseNpcOutOfRange();
                ui.OpenFreeChat(npcId, npcDisplayName, ai);
            }
        }

        bool WasInteractPressed()
        {
            if (player != null &&
                player.TryGetComponent<Gameplay.PlayerInputReader>(out var input) &&
                input.InteractPressed)
                return true;

            var keyboard = Keyboard.current;
            return keyboard != null &&
                (keyboard.fKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
