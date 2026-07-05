using System.Collections.Generic;
using BES.Core;
using BES.Narrative;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text speakerText;
        [SerializeField] TMP_Text dialogueText;
        [SerializeField] TMP_InputField chatInput;
        [SerializeField] Button sendButton;
        [SerializeField] Button closeButton;
        [SerializeField] Transform choicesContainer;
        [SerializeField] Button choiceButtonPrefab;

        AIDialogueService aiService;
        string currentNpcId;
        string currentNpcName;
        string lastRenderedNodeId;
        bool freeChatMode;
        Button continueButton;

        public bool IsStoryOpen => panel != null && panel.activeSelf && !freeChatMode;

        void Awake()
        {
            aiService = FindAnyObjectByType<AIDialogueService>();
            if (sendButton != null) sendButton.onClick.AddListener(SendFreeChat);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
        }

        void OnEnable()
        {
            GameEvents.OnDialogueStarted += HandleDialogueStarted;
            GameEvents.OnDialogueEnded += HandleDialogueEnded;
        }

        void OnDisable()
        {
            GameEvents.OnDialogueStarted -= HandleDialogueStarted;
            GameEvents.OnDialogueEnded -= HandleDialogueEnded;
        }

        void Update()
        {
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive && !freeChatMode)
                RenderStoryDialogue();
        }

        void HandleDialogueStarted(string speakerId)
        {
            if (freeChatMode)
                return;

            ShowPanel();
            if (speakerText != null) speakerText.text = speakerId;
        }

        void HandleDialogueEnded() => ClosePanelOnly();

        void RenderStoryDialogue()
        {
            var node = DialogueSystem.Instance.CurrentNode;
            if (node == null)
                return;

            if (lastRenderedNodeId == node.nodeId)
                return;

            lastRenderedNodeId = node.nodeId;

            if (dialogueText != null) dialogueText.text = node.line;
            if (speakerText != null) speakerText.text = node.speakerId;
            if (chatInput != null) chatInput.gameObject.SetActive(false);

            ClearChoices();
            for (var i = 0; i < node.choices.Count; i++)
            {
                var index = i;
                var btn = Instantiate(choiceButtonPrefab, choicesContainer);
                btn.GetComponentInChildren<TMP_Text>().text = node.choices[i].choiceText;
                btn.onClick.AddListener(() => DialogueSystem.Instance.SelectChoice(index));
            }

            if (node.choices.Count == 0 && !string.IsNullOrEmpty(node.nextNodeId))
                ShowContinueButton(node.nextNodeId);
            else
                HideContinueButton();
        }

        void ShowContinueButton(string nextNodeId)
        {
            if (choicesContainer == null || choiceButtonPrefab == null)
                return;

            HideContinueButton();
            continueButton = Instantiate(choiceButtonPrefab, choicesContainer);
            continueButton.GetComponentInChildren<TMP_Text>().text = "Tiếp tục";
            continueButton.onClick.AddListener(() =>
            {
                DialogueSystem.Instance?.Advance(nextNodeId);
            });
        }

        void HideContinueButton()
        {
            if (continueButton != null)
            {
                Destroy(continueButton.gameObject);
                continueButton = null;
            }
        }

        public void OpenFreeChat(string npcId, string npcName, AIDialogueService service)
        {
            freeChatMode = true;
            lastRenderedNodeId = null;
            currentNpcId = npcId;
            currentNpcName = npcName;
            aiService = service ?? aiService;

            ShowPanel();
            if (speakerText != null) speakerText.text = npcName;
            if (dialogueText != null) dialogueText.text = "Nhập tin nhắn để trò chuyện với NPC.";
            if (chatInput != null) chatInput.gameObject.SetActive(true);
            ClearChoices();
        }

        void SendFreeChat()
        {
            if (chatInput == null || string.IsNullOrWhiteSpace(chatInput.text))
                return;

            var message = chatInput.text.Trim();
            chatInput.text = string.Empty;

            var response = aiService != null
                ? aiService.GenerateFallbackResponse(currentNpcName, message)
                : $"[{currentNpcName}] ...";

            if (dialogueText != null)
                dialogueText.text = response;

            aiService?.RememberFromExchange(currentNpcId, message);
            Core.GameManager.Instance?.Relationships.AdjustAffinity(currentNpcId, 2);
        }

        void ClearChoices()
        {
            if (choicesContainer == null)
                return;

            for (var i = choicesContainer.childCount - 1; i >= 0; i--)
                Destroy(choicesContainer.GetChild(i).gameObject);
            continueButton = null;
        }

        public void Close()
        {
            if (freeChatMode && !string.IsNullOrEmpty(currentNpcId))
                GameEvents.RaiseNpcTalked(currentNpcId);

            ClosePanelOnly();

            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive)
                DialogueSystem.Instance.EndDialogue();
        }

        void ShowPanel()
        {
            if (panel != null && !panel.activeSelf)
                panel.SetActive(true);
        }

        void ClosePanelOnly()
        {
            freeChatMode = false;
            lastRenderedNodeId = null;

            if (panel != null && panel.activeSelf)
                panel.SetActive(false);

            ClearChoices();
        }
    }
}
