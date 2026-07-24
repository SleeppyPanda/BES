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
    }
}
