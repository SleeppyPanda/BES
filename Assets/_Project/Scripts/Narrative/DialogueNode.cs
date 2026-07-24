using System.Collections.Generic;
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
}
