using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class PlayerProfileUI : UIScreenBase
    {
        const string AccountCreatedKey = "BES_AccountCreated";
        const string AccountNameKey = "BES_AccountName";

        [SerializeField] TMP_Text usernameText;
        [SerializeField] TMP_Text serverText;
        [SerializeField] TMP_Text uidText;
        [SerializeField] TMP_InputField usernameInput;
        [SerializeField] Button createAccountButton;
        [SerializeField] Button closeButton;

        public static bool HasAccount => PlayerPrefs.GetInt(AccountCreatedKey, 0) == 1;
        public static string AccountName => PlayerPrefs.GetString(AccountNameKey, "Player");

        void Awake()
        {
            if (root == null)
                root = gameObject;

            EnsureAccountControls();
            Hide();

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
            if (createAccountButton != null)
                createAccountButton.onClick.AddListener(CreateAccount);
        }

        public override void Refresh()
        {
            if (usernameText != null)
                usernameText.text = HasAccount ? $"Account: {AccountName}" : "Create account";
            if (serverText != null)
                serverText.text = $"Region: {ServerPickerUI.GetSelectedServer()}";
            if (uidText != null)
                uidText.text = HasAccount ? "UID: 100000001" : "UID: not created";
            if (usernameInput != null)
                usernameInput.text = HasAccount ? AccountName : string.Empty;
        }

        void CreateAccount()
        {
            var accountName = usernameInput != null ? usernameInput.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(accountName))
                accountName = "Player";

            PlayerPrefs.SetInt(AccountCreatedKey, 1);
            PlayerPrefs.SetString(AccountNameKey, accountName);
            PlayerPrefs.Save();
            Refresh();
        }

        void EnsureAccountControls()
        {
            var parent = root != null ? root.transform : transform;

            if (usernameInput == null)
                usernameInput = CreateInput(parent, "AccountNameInput", new Vector2(0f, -92f));

            if (createAccountButton == null)
                createAccountButton = CreateButton(parent, "CreateAccountButton", "Create account", new Vector2(0f, -152f), new Vector2(220f, 44f));
        }

        static TMP_InputField CreateInput(Transform parent, string name, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(320f, 44f);
            rect.anchoredPosition = position;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.05f, 0.07f, 0.11f, 0.72f);

            var text = CreateText(go.transform, "Text", string.Empty, 18f, TextAlignmentOptions.MidlineLeft);
            text.rectTransform.offsetMin = new Vector2(14f, 0f);
            text.rectTransform.offsetMax = new Vector2(-14f, 0f);

            var placeholder = CreateText(go.transform, "Placeholder", "Account name", 18f, TextAlignmentOptions.MidlineLeft);
            placeholder.rectTransform.offsetMin = new Vector2(14f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-14f, 0f);
            placeholder.color = new Color(1f, 1f, 1f, 0.42f);

            var input = go.GetComponent<TMP_InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.95f, 0.72f, 0.18f, 0.86f);

            var text = CreateText(go.transform, "Label", label, 18f, TextAlignmentOptions.Center);
            text.color = new Color(0.04f, 0.04f, 0.05f, 1f);
            return go.GetComponent<Button>();
        }

        static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
