using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace BES.Narrative
{
    public class AIDialogueService : MonoBehaviour
    {
        [SerializeField] string apiKey = "";
        [SerializeField] string apiUrl = "https://api.openai.com/v1/chat/completions";
        [SerializeField] string model = "gpt-4o-mini";
        [SerializeField] int requestTimeoutSeconds = 20;
        [TextArea][SerializeField] string systemPromptTemplate =
            "You are {npcName}, an NPC in the world of Guarem (BES). Disposition: {disposition}. Known memories: {memories}. Reply in Vietnamese, stay in character, max 3 sentences.";

        public bool HasApiKey => !string.IsNullOrEmpty(EffectiveApiKey);

        string EffectiveApiKey
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(apiKey))
                    return apiKey.Trim();
#if !UNITY_WEBGL
                var environmentKey = global::System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (!string.IsNullOrWhiteSpace(environmentKey))
                    return environmentKey.Trim();
#endif
                return string.Empty;
            }
        }

        public string BuildPrompt(string npcName, string playerMessage)
        {
            return BuildPrompt(npcName, npcName, playerMessage);
        }

        public string BuildPrompt(string npcId, string npcName, string playerMessage)
        {
            return BuildSystemPrompt(npcId, npcName) + $"\nPlayer says: {playerMessage}";
        }

        string BuildSystemPrompt(string npcId, string npcName)
        {
            var memories = NPCMemoryStore.GetMemories(npcId);
            var memoryText = memories.Count == 0 ? "none" : string.Join("; ", memories);
            var disposition = GameManagerRelationships.GetDisposition(npcId);

            return systemPromptTemplate
                .Replace("{npcName}", npcName)
                .Replace("{disposition}", disposition)
                .Replace("{memories}", memoryText);
        }

        public void GenerateResponse(string npcId, string npcName, string playerMessage, Action<string> onComplete)
        {
            if (!HasApiKey || string.IsNullOrWhiteSpace(apiUrl))
            {
                onComplete?.Invoke(GenerateFallbackResponse(npcId, npcName, playerMessage));
                return;
            }

            StartCoroutine(GenerateResponseRoutine(npcId, npcName, playerMessage, onComplete));
        }

        IEnumerator GenerateResponseRoutine(string npcId, string npcName, string playerMessage, Action<string> onComplete)
        {
            var request = new ChatCompletionRequest
            {
                model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model,
                temperature = 0.7f,
                max_tokens = 180,
                messages = new List<ChatMessage>
                {
                    new() { role = "system", content = BuildSystemPrompt(npcId, npcName) },
                    new() { role = "user", content = playerMessage }
                }
            };

            var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
            using var webRequest = new UnityWebRequest(apiUrl, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(body);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = Mathf.Max(1, requestTimeoutSeconds);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {EffectiveApiKey}");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[BES AI] HTTP dialogue failed: {webRequest.error}");
                onComplete?.Invoke(GenerateFallbackResponse(npcId, npcName, playerMessage));
                yield break;
            }

            var responseText = webRequest.downloadHandler.text;
            var response = JsonUtility.FromJson<ChatCompletionResponse>(responseText);
            var content = response?.choices != null && response.choices.Count > 0
                ? response.choices[0]?.message?.content
                : null;

            if (string.IsNullOrWhiteSpace(content))
            {
                var error = response?.error?.message;
                if (!string.IsNullOrWhiteSpace(error))
                    Debug.LogWarning($"[BES AI] HTTP dialogue returned an error: {error}");
                onComplete?.Invoke(GenerateFallbackResponse(npcId, npcName, playerMessage));
                yield break;
            }

            onComplete?.Invoke(content.Trim());
        }

        public string GenerateFallbackResponse(string npcName, string playerMessage)
        {
            return GenerateFallbackResponse(npcName, npcName, playerMessage);
        }

        public string GenerateFallbackResponse(string npcId, string npcName, string playerMessage)
        {
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

        [Serializable]
        class ChatCompletionRequest
        {
            public string model;
            public float temperature;
            public int max_tokens;
            public List<ChatMessage> messages;
        }

        [Serializable]
        class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        class ChatCompletionResponse
        {
            public List<ChatChoice> choices = new();
            public ChatError error = new();
        }

        [Serializable]
        class ChatChoice
        {
            public ChatMessage message = new();
        }

        [Serializable]
        class ChatError
        {
            public string message = string.Empty;
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
