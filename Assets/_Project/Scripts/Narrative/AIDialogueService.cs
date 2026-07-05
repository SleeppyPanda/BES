using System.Text;
using UnityEngine;

namespace BES.Narrative
{
    public class AIDialogueService : MonoBehaviour
    {
        [SerializeField] string apiKey = "";
        [SerializeField] string apiUrl = "https://api.openai.com/v1/chat/completions";
        [SerializeField] string model = "gpt-4o-mini";
        [TextArea][SerializeField] string systemPromptTemplate =
            "You are {npcName}, an NPC in the world of Guarem (BES). Disposition: {disposition}. Known memories: {memories}. Reply in Vietnamese, stay in character, max 3 sentences.";

        public bool HasApiKey => !string.IsNullOrEmpty(apiKey);

        public string BuildPrompt(string npcName, string playerMessage)
        {
            var memories = NPCMemoryStore.GetMemories(npcName);
            var memoryText = memories.Count == 0 ? "none" : string.Join("; ", memories);
            var disposition = GameManagerRelationships.GetDisposition(npcName);

            return systemPromptTemplate
                .Replace("{npcName}", npcName)
                .Replace("{disposition}", disposition)
                .Replace("{memories}", memoryText)
                + $"\nPlayer says: {playerMessage}";
        }

        public string GenerateFallbackResponse(string npcName, string playerMessage)
        {
            var npcId = npcName;
            var disposition = GameManagerRelationships.GetDisposition(npcId);
            var memories = NPCMemoryStore.GetMemories(npcId);
            var questHint = GetQuestContextHint();
            var regionHint = GetRegionHint();

            var sb = new StringBuilder();
            switch (disposition)
            {
                case "Trusted":
                    sb.Append($"[{npcName}] Ta tin tưởng ngươi. ");
                    break;
                case "Friendly":
                    sb.Append($"[{npcName}] Được thôi. ");
                    break;
                case "Hostile":
                    return $"[{npcName}] Ta không muốn nói chuyện với ngươi.";
                default:
                    sb.Append($"[{npcName}] ");
                    break;
            }

            sb.Append($"Về \"{playerMessage}\" — ");
            if (!string.IsNullOrEmpty(questHint))
                sb.Append(questHint).Append(' ');
            if (!string.IsNullOrEmpty(regionHint))
                sb.Append(regionHint).Append(' ');
            if (memories.Count > 0)
                sb.Append($"Ta nhớ: {memories[memories.Count - 1]}. ");
            else
                sb.Append("hãy cẩn thận trên con đường phía trước.");

            return sb.ToString();
        }

        static string GetQuestContextHint()
        {
            var quests = Core.GameManager.Instance?.Quests;
            if (quests == null)
                return string.Empty;

            var desc = quests.GetActiveQuestStepDescription();
            if (string.IsNullOrEmpty(desc) || desc.Contains("Không có"))
                return string.Empty;

            return $"Nhiệm vụ hiện tại: {desc}.";
        }

        static string GetRegionHint()
        {
            var save = Core.GameManager.Instance?.Save?.Current;
            if (save == null || string.IsNullOrEmpty(save.currentRegionId))
                return string.Empty;

            return save.currentRegionId switch
            {
                "region_ruins" => "Khu tàn tích đầy hiểm nguy.",
                "region_forest" => "Rừng xì xào như đang thì thầm.",
                _ => "Thành phố vẫn đang được Bảo Hộ gìn giữ."
            };
        }

        public void RememberFromExchange(string npcId, string playerMessage)
        {
            if (playerMessage.Length > 8)
                NPCMemoryStore.AddMemory(npcId, $"Player once said: {playerMessage.Substring(0, Mathf.Min(80, playerMessage.Length))}");
        }
    }

    static class GameManagerRelationships
    {
        public static string GetDisposition(string npcId)
        {
            if (Core.GameManager.Instance?.Relationships != null)
                return Core.GameManager.Instance.Relationships.GetDisposition(npcId);
            return "Neutral";
        }
    }
}
