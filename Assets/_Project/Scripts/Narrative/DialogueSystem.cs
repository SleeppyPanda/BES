using System.Collections.Generic;
using BES.Core;
using UnityEngine;

namespace BES.Narrative
{
    [CreateAssetMenu(fileName = "DialogueNode", menuName = "BES/Dialogue Node")]
    public class DialogueNode : ScriptableObject
    {
        public string nodeId;
        public string speakerId;
        [TextArea] public string line;
        public List<DialogueChoice> choices = new();
        public string nextNodeId;
        public string questTriggerId;
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public string nextNodeId;
        public string branchId;
        public int affinityDelta;
    }

    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        [SerializeField] List<DialogueNode> nodes = new();
        DialogueNode currentNode;
        string lastSpeakerId;
        readonly Dictionary<string, DialogueNode> lookup = new();

        public DialogueNode CurrentNode => currentNode;
        public bool IsActive => currentNode != null;
        public string LastSpeakerId => lastSpeakerId;

        void Awake()
        {
            Instance = this;
            if (nodes.Count == 0)
            {
                var loaded = Resources.LoadAll<DialogueNode>("Dialogue");
                if (loaded != null && loaded.Length > 0)
                    nodes.AddRange(loaded);
            }

            RebuildLookup();
        }

        public void RebuildLookup()
        {
            lookup.Clear();
            foreach (var node in nodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.nodeId))
                    lookup[node.nodeId] = node;
            }
        }

        public bool StartDialogue(string nodeId)
        {
            if (!lookup.TryGetValue(nodeId, out var node))
                return false;

            currentNode = node;
            lastSpeakerId = node.speakerId;
            GameEvents.RaiseDialogueStarted(node.speakerId);

            if (!string.IsNullOrEmpty(node.questTriggerId))
                GameManager.Instance?.Quests.AdvanceQuest(node.questTriggerId);

            return true;
        }

        public void SelectChoice(int index)
        {
            if (currentNode == null || index < 0 || index >= currentNode.choices.Count)
                return;

            var choice = currentNode.choices[index];
            if (choice.affinityDelta != 0)
                GameManager.Instance?.Relationships.AdjustAffinity(currentNode.speakerId, choice.affinityDelta);

            if (!string.IsNullOrEmpty(choice.branchId))
            {
                GameManager.Instance?.Quests.SetBranch(choice.branchId);
                GameManager.Instance?.Quests.TryAdvanceCurrentStep(
                    "main_awakening",
                    QuestStepType.Choice,
                    choice.branchId);
                if (choice.branchId == "branch_a")
                    GameManager.Instance?.Quests.CompleteQuest("ending_guardian_pact");
                else if (choice.branchId == "branch_b")
                    GameManager.Instance?.Quests.CompleteQuest("ending_void_whisper");
            }

            Advance(choice.nextNodeId);
        }

        public void Advance(string nextNodeId)
        {
            if (string.IsNullOrEmpty(nextNodeId))
            {
                EndDialogue();
                return;
            }

            StartDialogue(nextNodeId);
        }

        public void EndDialogue()
        {
            if (currentNode == null)
                return;

            currentNode = null;
            GameEvents.RaiseDialogueEnded();
        }
    }
}
