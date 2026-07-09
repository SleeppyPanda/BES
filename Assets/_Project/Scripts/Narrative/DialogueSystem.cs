using System.Collections.Generic;
using BES.Core;
using UnityEngine;

namespace BES.Narrative
{
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
            LoadResourceNodes();
            EnsureBuiltInFallbackNodes();
            RebuildLookup();
        }

        void LoadResourceNodes()
        {
            var loaded = Resources.LoadAll<DialogueNode>("Dialogue");
            if (loaded == null || loaded.Length == 0)
                return;

            foreach (var node in loaded)
            {
                if (node != null && !nodes.Contains(node))
                    nodes.Add(node);
            }
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
            {
                LoadResourceNodes();
                EnsureBuiltInFallbackNodes();
                RebuildLookup();
                if (!lookup.TryGetValue(nodeId, out node))
                {
                    Debug.LogWarning($"[BES Dialogue] Dialogue node not found: {nodeId}");
                    return false;
                }
            }

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

        void EnsureBuiltInFallbackNodes()
        {
            AddNodeIfMissing(CreateNode(
                "nguoi_yeu_cu_intro",
                "Người yêu cũ",
                "Mừng vì anh đã quay lại...Lốp Trưởng!",
                "nguoi_yeu_cu_remember"));

            AddNodeIfMissing(CreateNode(
                "nguoi_yeu_cu_remember",
                "Người yêu cũ",
                "Anh có nhớ em không?",
                null,
                new DialogueChoice { choiceText = "Có", nextNodeId = "nguoi_yeu_cu_yes", affinityDelta = 5 },
                new DialogueChoice { choiceText = "Không", nextNodeId = "nguoi_yeu_cu_no", affinityDelta = -2 }));

            AddNodeIfMissing(CreateNode(
                "nguoi_yeu_cu_yes",
                "Người yêu cũ",
                "Vậy hả, em cũng không nhớ anh..."));

            AddNodeIfMissing(CreateNode(
                "nguoi_yeu_cu_no",
                "Người yêu cũ",
                "Không thể ngừng nhớ em đúng không?",
                "nguoi_yeu_cu_player_admit"));

            AddNodeIfMissing(CreateNode(
                "nguoi_yeu_cu_player_admit",
                "Nhân vật",
                "Đu...Đúng........."));
        }

        void AddNodeIfMissing(DialogueNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.nodeId))
                return;

            foreach (var existing in nodes)
            {
                if (existing != null && existing.nodeId == node.nodeId)
                    return;
            }

            nodes.Add(node);
        }

        static DialogueNode CreateNode(string nodeId, string speakerId, string line, string nextNodeId = null, params DialogueChoice[] choices)
        {
            var node = ScriptableObject.CreateInstance<DialogueNode>();
            node.nodeId = nodeId;
            node.speakerId = speakerId;
            node.line = line;
            node.nextNodeId = nextNodeId;
            node.choices = new List<DialogueChoice>();
            if (choices != null)
                node.choices.AddRange(choices);
            return node;
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
