using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class PlayerProfileUI : UIScreenBase
    {
        [SerializeField] TMP_Text usernameText;
        [SerializeField] TMP_Text serverText;
        [SerializeField] TMP_Text uidText;
        [SerializeField] TMP_InputField usernameInput;
        [SerializeField] Button createAccountButton;
        [SerializeField] Button closeButton;

        public static bool HasAccount => AuthManager.Instance != null && AuthManager.Instance.IsAuthenticated;
        public static string AccountName => AuthManager.Instance != null ? AuthManager.Instance.CurrentUserName : "Player";

        private TMP_Text _buttonText;
        private bool _isProcessing = false;

        void Awake()
        {
            if (root == null)
                root = gameObject;

            Hide();

            // Fix the visual bug where the stretched mockup image "Username PLayer.png" is used as background on the child "Background" GameObject
            var bgTransform = transform.Find("Background");
            if (bgTransform != null)
            {
                var rawImg = bgTransform.GetComponent<RawImage>();
                if (rawImg != null)
                {
                    rawImg.texture = null; // Clear the stretched mockup texture
                    rawImg.color = new Color(0.01f, 0.03f, 0.05f, 0.92f); // Set a premium dark semi-transparent overlay color
                }
            }

            // Get the button text component for dynamic text change
            if (createAccountButton != null)
            {
                _buttonText = createAccountButton.GetComponentInChildren<TMP_Text>();
                createAccountButton.onClick.AddListener(HandleSubmit);
            }

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnAuthStatusChanged += Refresh;
            }
        }

        private void OnDisable()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnAuthStatusChanged -= Refresh;
            }
        }

        public override void Show()
        {
            base.Show();
            transform.SetAsLastSibling(); // Ensure we render on top of the logo and other UI elements
        }

        public override void Refresh()
        {
            bool loggedIn = HasAccount;

            if (usernameText != null)
                usernameText.text = loggedIn ? $"Account: {AccountName}" : "Create Account / Login";
            if (serverText != null)
                serverText.text = $"Region: {ServerPickerUI.GetSelectedServer()}";
            if (uidText != null)
                uidText.text = loggedIn ? $"UID: {AuthManager.Instance.CurrentUserId}" : "UID: not logged in";

            if (usernameInput != null)
            {
                usernameInput.gameObject.SetActive(!loggedIn);
                // Set custom placeholder for clearer instruction
                var placeholder = usernameInput.placeholder as TMP_Text;
                if (placeholder != null)
                {
                    placeholder.text = "Username / Email (Leave empty for Guest)";
                }
            }

            if (_buttonText != null)
            {
                _buttonText.text = loggedIn ? "Logout" : "Login / Register";
            }
        }

        private async void HandleSubmit()
        {
            if (_isProcessing || AuthManager.Instance == null) return;

            if (HasAccount)
            {
                // If logged in, button acts as Logout
                AuthManager.Instance.SignOut();
                return;
            }

            string inputVal = usernameInput != null ? usernameInput.text.Trim() : string.Empty;

            SetLoading(true);

            string error = null;
            if (string.IsNullOrEmpty(inputVal))
            {
                // Empty input: Login as Guest
                error = await AuthManager.Instance.SignInAnonymouslyAsync();
            }
            else
            {
                // Email format check
                string email = inputVal.Contains("@") ? inputVal : $"{inputVal}@bes.com";
                string username = inputVal.Contains("@") ? inputVal.Split('@')[0] : inputVal;

                // Try to sign up first
                error = await AuthManager.Instance.SignUpWithEmailAsync(email, "default_pass123", username);
                if (!string.IsNullOrEmpty(error))
                {
                    // If sign up fails because user exists or validation, try to sign in
                    error = await AuthManager.Instance.SignInWithEmailAsync(email, "default_pass123");
                }
            }

            SetLoading(false);

            if (!string.IsNullOrEmpty(error))
            {
                // Display error on the usernameText
                if (usernameText != null)
                {
                    usernameText.text = $"<color=red>Error: {error}</color>";
                }
            }
            else
            {
                Hide();
            }
        }

        private void SetLoading(bool loading)
        {
            _isProcessing = loading;
            if (createAccountButton != null) createAccountButton.interactable = !loading;
            if (usernameInput != null) usernameInput.interactable = !loading;
            if (closeButton != null) closeButton.interactable = !loading;

            if (usernameText != null && loading)
            {
                usernameText.text = "Processing authentication...";
            }
        }
    }
}

