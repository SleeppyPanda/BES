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
        bool freeChatWaiting;
        Button continueButton;

        public bool IsStoryOpen => panel != null && panel.activeSelf && !freeChatMode;

        void Awake()
        {
            EnsureRuntimeBindings();
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
            {
                if (panel == null || !panel.activeSelf)
                    ShowPanel();
                RenderStoryDialogue();
            }
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
                btn.gameObject.SetActive(true);
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
            continueButton.gameObject.SetActive(true);
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
            if (freeChatWaiting)
                return;

            freeChatWaiting = true;
            if (sendButton != null)
                sendButton.interactable = false;

            if (dialogueText != null)
                dialogueText.text = "...";

            if (aiService != null)
                aiService.GenerateResponse(currentNpcId, currentNpcName, message, response => HandleFreeChatResponse(message, response));
            else
                HandleFreeChatResponse(message, $"[{currentNpcName}] ...");
        }

        void HandleFreeChatResponse(string message, string response)
        {
            if (this == null)
                return;

            freeChatWaiting = false;
            if (sendButton != null)
                sendButton.interactable = true;

            if (!freeChatMode || panel == null || !panel.activeSelf)
                return;

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
            EnsureRuntimeBindings();
            if (panel != null && !panel.activeSelf)
                panel.SetActive(true);
        }

        void ClosePanelOnly()
        {
            freeChatMode = false;
            freeChatWaiting = false;
            lastRenderedNodeId = null;
            if (sendButton != null)
                sendButton.interactable = true;

            if (panel != null && panel.activeSelf)
                panel.SetActive(false);

            ClearChoices();
        }

        void EnsureRuntimeBindings()
        {
            if (panel != null && speakerText != null && dialogueText != null &&
                choicesContainer != null && choiceButtonPrefab != null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("RuntimeDialogueCanvas", typeof(RectTransform));
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            if (panel == null)
                panel = CreatePanel(canvas.transform);

            ApplyCompactPanelLayout(panel);
            var panelTransform = panel.transform;
            speakerText ??= CreateText(panelTransform, "SpeakerText", "NPC", 22f, FontStyles.Bold,
                new Vector2(28f, -18f), new Vector2(-64f, -52f));
            dialogueText ??= CreateText(panelTransform, "DialogueText", "", 19f, FontStyles.Normal,
                new Vector2(28f, -58f), new Vector2(-28f, -126f));

            if (choicesContainer == null)
                choicesContainer = CreateChoicesContainer(panelTransform);

            if (choiceButtonPrefab == null)
                choiceButtonPrefab = CreateChoiceButtonPrefab(panelTransform);

            if (closeButton == null)
                closeButton = CreateCloseButton(panelTransform);
        }

        static GameObject CreatePanel(Transform parent)
        {
            var go = new GameObject("RuntimeDialoguePanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ApplyCompactPanelLayout(go);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.025f, 0.03f, 0.04f, 0.88f);
            return go;
        }

        static void ApplyCompactPanelLayout(GameObject target)
        {
            if (target == null)
                return;

            var rect = target.GetComponent<RectTransform>();
            if (rect == null)
                rect = target.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.18f, 0.055f);
            rect.anchorMax = new Vector2(0.82f, 0.265f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, FontStyles style, Vector2 topLeft, Vector2 bottomRight)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(topLeft.x, bottomRight.y);
            rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        static Transform CreateChoicesContainer(Transform parent)
        {
            var go = new GameObject("Choices", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(28f, 16f);
            rect.offsetMax = new Vector2(-28f, 68f);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            return go.transform;
        }

        static Button CreateChoiceButtonPrefab(Transform parent)
        {
            var go = new GameObject("ChoiceButtonPrefab", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 40f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.28f, 0.95f);
            var button = go.AddComponent<Button>();

            var label = CreateText(go.transform, "Label", "Choice", 15f, FontStyles.Normal,
                new Vector2(10f, -8f), new Vector2(-10f, -34f));
            label.alignment = TextAlignmentOptions.Center;
            go.SetActive(false);
            return button;
        }

        static Button CreateCloseButton(Transform parent)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(38f, 32f);
            rect.anchoredPosition = new Vector2(-14f, -12f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.22f, 0.28f, 0.85f);
            var button = go.AddComponent<Button>();
            var label = CreateText(go.transform, "Label", "X", 16f, FontStyles.Bold,
                new Vector2(0f, 0f), new Vector2(0f, -32f));
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }
    }
}
