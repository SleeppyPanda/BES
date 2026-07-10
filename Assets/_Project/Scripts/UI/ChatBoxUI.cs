using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class ChatBoxUI : MonoBehaviour
    {
        [SerializeField] Button chatButton;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text historyText;
        [SerializeField] TMP_InputField inputField;
        [SerializeField] Button sendButton;
        [SerializeField] int maxMessages = 30;

        readonly List<string> messages = new List<string>();

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);
            if (chatButton != null)
                chatButton.onClick.AddListener(Toggle);
            if (sendButton != null)
                sendButton.onClick.AddListener(SendCurrentMessage);
            if (inputField != null)
                inputField.onSubmit.AddListener(_ => SendCurrentMessage());

            RefreshHistory();
        }

        public void Toggle()
        {
            if (panel == null)
                return;

            panel.SetActive(!panel.activeSelf);
            if (panel.activeSelf && inputField != null)
                inputField.ActivateInputField();
        }

        public void AddSystemMessage(string text)
        {
            AddMessage("System", text);
        }

        public void AddMessage(string sender, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            messages.Add($"{sender}: {text.Trim()}");
            while (messages.Count > maxMessages)
                messages.RemoveAt(0);

            RefreshHistory();
        }

        void SendCurrentMessage()
        {
            if (inputField == null)
                return;

            var text = inputField.text;
            if (string.IsNullOrWhiteSpace(text))
                return;

            AddMessage("Player", text);
            inputField.text = string.Empty;
            inputField.ActivateInputField();
        }

        void RefreshHistory()
        {
            if (historyText == null)
                return;

            var builder = new StringBuilder();
            foreach (var message in messages)
                builder.AppendLine(message);
            historyText.text = builder.ToString();
        }
    }
}
