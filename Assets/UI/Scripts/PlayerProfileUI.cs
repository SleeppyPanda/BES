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

        public enum AuthMode
        {
            SignIn,
            SignUp,
            ForgotPassword,
            VerifyResetCode
        }

        public static bool HasAccount => AuthManager.Instance != null && AuthManager.Instance.IsAuthenticated;
        public static string AccountName => AuthManager.Instance != null ? AuthManager.Instance.CurrentUserName : "Player";

        private TMP_Text _buttonText;
        private bool _isProcessing = false;

        private AuthMode _currentMode = AuthMode.SignIn;
        private string _lastErrorMessage = "";
        private string _lastStatusMessage = "";
        private string _lastEmailEntered = "";

        // Programmatically cloned fields
        private TMP_InputField displayNameInput;
        private TMP_InputField resetCodeInput;
        private TMP_InputField passwordInput;
        private TMP_InputField confirmPasswordInput;
        private Button forgotPasswordButton;
        private Button toggleModeButton;
        private TMP_Text errorText;

        void Awake()
        {
            if (root == null)
                root = gameObject;

            Hide();

            // 1. Add background dimmer overlay on the root PlayerProfileUI GameObject
            var rootImg = GetComponent<Image>();
            if (rootImg == null) rootImg = gameObject.AddComponent<Image>();
            rootImg.color = new Color(0.01f, 0.02f, 0.04f, 0.85f);
            rootImg.raycastTarget = true; // Block clicks to background elements when login panel is active

            // 2. Destroy UIScreenBackground to prevent it from loading/overwriting the mockup image
            var scrBg = GetComponent<UIScreenBackground>();
            if (scrBg != null)
            {
                Destroy(scrBg);
            }

            // 3. Format the child "Background" panel as a centered, sleek card container
            var bgTransform = transform.Find("Background");
            if (bgTransform != null)
            {
                var rect = bgTransform.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(500f, 580f);
                }

                var rawImg = bgTransform.GetComponent<RawImage>();
                if (rawImg != null)
                {
                    rawImg.texture = null;
                    rawImg.color = new Color(0.06f, 0.08f, 0.14f, 0.96f);
                    rawImg.raycastTarget = true; // Block clicks inside the card container
                }
            }

            // Get the button text component for dynamic text change
            if (createAccountButton != null)
            {
                _buttonText = createAccountButton.GetComponentInChildren<TMP_Text>();
                createAccountButton.onClick.AddListener(HandleSubmit);
            }

            CreateProgrammaticUI();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
                var closeRect = closeButton.GetComponent<RectTransform>();
                if (closeRect != null)
                {
                    closeRect.anchoredPosition = new Vector2(220f, 260f);
                    closeRect.sizeDelta = new Vector2(36f, 36f);
                }
            }
        }

        private void CreateProgrammaticUI()
        {
            if (usernameInput == null) return;

            // Clone usernameText to create errorText right below the title area
            if (usernameText != null)
            {
                var errorGo = Instantiate(usernameText.gameObject, usernameText.transform.parent);
                errorGo.name = "ErrorText";
                errorText = errorGo.GetComponent<TMP_Text>();
                errorText.fontSize = 15f;
                errorText.fontStyle = FontStyles.Normal;
                errorText.alignment = TextAlignmentOptions.Center;
                errorText.text = "";
                
                var rect = errorText.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(460f, 35f);
                }
            }

            // Style headers
            if (usernameText != null)
            {
                usernameText.fontSize = 28f;
                usernameText.fontStyle = FontStyles.Bold;
                var rect = usernameText.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(460f, 50f);
            }

            if (serverText != null)
            {
                serverText.fontSize = 16f;
                serverText.color = new Color(0.7f, 0.8f, 0.9f, 0.8f);
                var rect = serverText.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(460f, 30f);
            }

            // Duplicate and scale input fields
            displayNameInput = CloneInputField(usernameInput, "DisplayNameInput", "Display Name (Username)");
            resetCodeInput = CloneInputField(usernameInput, "ResetCodeInput", "Verification Code");
            passwordInput = CloneInputField(usernameInput, "PasswordInput", "Password (Min 6 chars)");
            confirmPasswordInput = CloneInputField(usernameInput, "ConfirmPasswordInput", "Confirm Password");

            StyleInputField(usernameInput, "Email / Username (Blank for Guest)");
            StyleInputField(displayNameInput, "Display Name (Username)");
            StyleInputField(resetCodeInput, "Verification Code");
            StyleInputField(passwordInput, "Password (Min 6 chars)");
            StyleInputField(confirmPasswordInput, "Confirm Password");

            if (passwordInput != null)
            {
                passwordInput.contentType = TMP_InputField.ContentType.Password;
            }
            if (confirmPasswordInput != null)
            {
                confirmPasswordInput.contentType = TMP_InputField.ContentType.Password;
            }
            if (resetCodeInput != null)
            {
                resetCodeInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                resetCodeInput.characterLimit = 6;
            }

            // Style submit button
            StyleSubmitButton();

            // Duplicate buttons for link toggles
            forgotPasswordButton = CloneButton(createAccountButton, "ForgotPasswordBtn", "Forgot Password?", OnForgotPasswordClicked);
            if (forgotPasswordButton != null)
            {
                var img = forgotPasswordButton.GetComponent<Image>();
                if (img != null) img.color = Color.clear;
                var txt = forgotPasswordButton.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                {
                    txt.color = new Color(0.4f, 0.7f, 1.0f, 0.8f); // link blue
                    txt.fontSize = 14f;
                    txt.fontStyle = FontStyles.Underline;
                }
                var rect = forgotPasswordButton.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(200f, 30f);
            }

            toggleModeButton = CloneButton(createAccountButton, "ToggleModeBtn", "Don't have an account? Sign Up", OnToggleModeClicked);
            if (toggleModeButton != null)
            {
                var img = toggleModeButton.GetComponent<Image>();
                if (img != null) img.color = Color.clear;
                var txt = toggleModeButton.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                {
                    txt.color = new Color(0.8f, 0.8f, 0.8f, 0.9f); // light grey
                    txt.fontSize = 14f;
                }
                var rect = toggleModeButton.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(320f, 35f);
            }
        }

        private TMP_InputField CloneInputField(TMP_InputField source, string name, string placeholder)
        {
            if (source == null) return null;
            var go = Instantiate(source.gameObject, source.transform.parent);
            go.name = name;
            var input = go.GetComponent<TMP_InputField>();
            input.text = "";
            return input;
        }

        private void StyleInputField(TMP_InputField input, string placeholder)
        {
            if (input == null) return;

            var rect = input.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(420f, 54f);
            }

            var img = input.GetComponent<Image>();
            if (img == null) img = input.gameObject.AddComponent<Image>();
            img.color = new Color(0.03f, 0.04f, 0.08f, 0.8f);
            img.sprite = null;

            if (input.textComponent != null)
            {
                input.textComponent.color = Color.white;
                input.textComponent.fontSize = 16f;
                input.textComponent.alignment = TextAlignmentOptions.Left;
            }

            var placeholderText = input.placeholder as TMP_Text;
            if (placeholderText != null)
            {
                placeholderText.text = placeholder;
                placeholderText.color = new Color(0.6f, 0.6f, 0.7f, 0.6f);
                placeholderText.fontSize = 16f;
                placeholderText.alignment = TextAlignmentOptions.Left;
            }
        }

        private void StyleSubmitButton()
        {
            if (createAccountButton == null) return;

            var rect = createAccountButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(420f, 54f);
            }

            var img = createAccountButton.GetComponent<Image>();
            if (img == null) img = createAccountButton.gameObject.AddComponent<Image>();
            img.color = new Color(0.18f, 0.5f, 0.9f, 1f);
            img.sprite = null;

            if (_buttonText != null)
            {
                _buttonText.color = Color.white;
                _buttonText.fontSize = 18f;
                _buttonText.fontStyle = FontStyles.Bold;
            }
        }

        private Button CloneButton(Button source, string name, string text, UnityEngine.Events.UnityAction action)
        {
            if (source == null) return null;
            var go = Instantiate(source.gameObject, source.transform.parent);
            go.name = name;
            var btn = go.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
            var btnText = btn.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = text;
            }
            return btn;
        }

        private void OnForgotPasswordClicked()
        {
            if (_isProcessing) return;
            _currentMode = AuthMode.ForgotPassword;
            _lastErrorMessage = "";
            _lastStatusMessage = "";
            Refresh();
        }

        private void OnToggleModeClicked()
        {
            if (_isProcessing) return;

            switch (_currentMode)
            {
                case AuthMode.SignIn:
                    _currentMode = AuthMode.SignUp;
                    break;
                case AuthMode.SignUp:
                case AuthMode.ForgotPassword:
                case AuthMode.VerifyResetCode:
                    _currentMode = AuthMode.SignIn;
                    break;
            }

            _lastErrorMessage = "";
            _lastStatusMessage = "";
            Refresh();
        }

        private void SetPosition(RectTransform rect, float y)
        {
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(0f, y);
            }
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

            // 1. Dynamic Card Sizing & Close Button Position based on logged in state
            var bgTransform = transform.Find("Background");
            if (bgTransform != null)
            {
                var rect = bgTransform.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = loggedIn ? new Vector2(500f, 350f) : new Vector2(500f, 580f);
                }
            }

            if (closeButton != null)
            {
                var closeRect = closeButton.GetComponent<RectTransform>();
                if (closeRect != null)
                {
                    closeRect.anchoredPosition = loggedIn ? new Vector2(220f, 150f) : new Vector2(220f, 260f);
                }
            }

            // 2. Text header and user details
            if (loggedIn)
            {
                if (usernameText != null) usernameText.text = $"Account: {AccountName}";
                if (serverText != null) serverText.text = $"Region: {ServerPickerUI.GetSelectedServer()}";
                if (uidText != null) uidText.text = $"UID: {AuthManager.Instance.CurrentUserId}";
                if (errorText != null) errorText.text = "";
            }
            else
            {
                if (usernameText != null)
                {
                    switch (_currentMode)
                    {
                        case AuthMode.SignIn:
                            usernameText.text = "Sign In";
                            break;
                        case AuthMode.SignUp:
                            usernameText.text = "Create Account";
                            break;
                        case AuthMode.ForgotPassword:
                            usernameText.text = "Reset Password";
                            break;
                        case AuthMode.VerifyResetCode:
                            usernameText.text = "Verify Code";
                            break;
                    }
                }

                if (serverText != null) serverText.text = $"Region: {ServerPickerUI.GetSelectedServer()}";
                if (uidText != null) uidText.text = ""; // Hide UID

                // Display status or error message inside the dedicated errorText component
                if (errorText != null)
                {
                    if (!string.IsNullOrEmpty(_lastErrorMessage))
                    {
                        errorText.text = $"<color=red>{_lastErrorMessage}</color>";
                    }
                    else if (!string.IsNullOrEmpty(_lastStatusMessage))
                    {
                        errorText.text = $"<color=green>{_lastStatusMessage}</color>";
                    }
                    else
                    {
                        errorText.text = "";
                    }
                }
            }

            // 3. Configure active inputs
            if (usernameInput != null)
            {
                usernameInput.gameObject.SetActive(!loggedIn);
                var placeholder = usernameInput.placeholder as TMP_Text;
                if (placeholder != null)
                {
                    placeholder.text = _currentMode == AuthMode.VerifyResetCode 
                        ? "Email: " + _lastEmailEntered 
                        : "Email / Username (Blank for Guest)";
                }
                usernameInput.interactable = !loggedIn && (_currentMode != AuthMode.VerifyResetCode);
            }

            if (displayNameInput != null)
            {
                displayNameInput.gameObject.SetActive(!loggedIn && _currentMode == AuthMode.SignUp);
            }

            if (resetCodeInput != null)
            {
                resetCodeInput.gameObject.SetActive(!loggedIn && _currentMode == AuthMode.VerifyResetCode);
            }

            if (passwordInput != null)
            {
                passwordInput.gameObject.SetActive(!loggedIn && 
                    (_currentMode == AuthMode.SignIn || _currentMode == AuthMode.SignUp || _currentMode == AuthMode.VerifyResetCode));
                
                var placeholder = passwordInput.placeholder as TMP_Text;
                if (placeholder != null)
                {
                    placeholder.text = _currentMode == AuthMode.VerifyResetCode 
                        ? "New Password (Min 6 chars)" 
                        : "Password (Min 6 chars)";
                }
            }

            if (confirmPasswordInput != null)
            {
                confirmPasswordInput.gameObject.SetActive(!loggedIn && _currentMode == AuthMode.SignUp);
            }

            // 4. Configure active buttons
            if (createAccountButton != null)
            {
                createAccountButton.gameObject.SetActive(true); // always active to submit or logout
                if (_buttonText != null)
                {
                    if (loggedIn)
                    {
                        _buttonText.text = "Logout";
                    }
                    else
                    {
                        switch (_currentMode)
                        {
                            case AuthMode.SignIn:
                                _buttonText.text = "Sign In";
                                break;
                            case AuthMode.SignUp:
                                _buttonText.text = "Sign Up";
                                break;
                            case AuthMode.ForgotPassword:
                                _buttonText.text = "Send Reset Code";
                                break;
                            case AuthMode.VerifyResetCode:
                                _buttonText.text = "Reset Password";
                                break;
                        }
                    }
                }
            }

            if (forgotPasswordButton != null)
            {
                forgotPasswordButton.gameObject.SetActive(!loggedIn && _currentMode == AuthMode.SignIn);
            }

            if (toggleModeButton != null)
            {
                toggleModeButton.gameObject.SetActive(!loggedIn);
                var toggleText = toggleModeButton.GetComponentInChildren<TMP_Text>();
                if (toggleText != null)
                {
                    switch (_currentMode)
                    {
                        case AuthMode.SignIn:
                            toggleText.text = "Don't have an account? Sign Up";
                            break;
                        case AuthMode.SignUp:
                        case AuthMode.ForgotPassword:
                        case AuthMode.VerifyResetCode:
                            toggleText.text = "Already have an account? Sign In";
                            break;
                    }
                }
            }

            // 5. Update vertical positions dynamically
            if (!loggedIn)
            {
                // Align headers relative to card (dimensions: 500x580)
                if (usernameText != null) SetPosition(usernameText.GetComponent<RectTransform>(), 210f);
                if (serverText != null) SetPosition(serverText.GetComponent<RectTransform>(), 170f);
                if (errorText != null) SetPosition(errorText.GetComponent<RectTransform>(), 135f);

                float currentY = 70f;
                float spacingInput = 62f;

                if (usernameInput != null && usernameInput.gameObject.activeSelf)
                {
                    SetPosition(usernameInput.GetComponent<RectTransform>(), currentY);
                    currentY -= spacingInput;
                }
                if (displayNameInput != null && displayNameInput.gameObject.activeSelf)
                {
                    SetPosition(displayNameInput.GetComponent<RectTransform>(), currentY);
                    currentY -= spacingInput;
                }
                if (resetCodeInput != null && resetCodeInput.gameObject.activeSelf)
                {
                    SetPosition(resetCodeInput.GetComponent<RectTransform>(), currentY);
                    currentY -= spacingInput;
                }
                if (passwordInput != null && passwordInput.gameObject.activeSelf)
                {
                    SetPosition(passwordInput.GetComponent<RectTransform>(), currentY);
                    currentY -= spacingInput;
                }
                if (confirmPasswordInput != null && confirmPasswordInput.gameObject.activeSelf)
                {
                    SetPosition(confirmPasswordInput.GetComponent<RectTransform>(), currentY);
                    currentY -= spacingInput;
                }

                currentY -= 5f;

                if (forgotPasswordButton != null && forgotPasswordButton.gameObject.activeSelf)
                {
                    SetPosition(forgotPasswordButton.GetComponent<RectTransform>(), currentY + 12f);
                    currentY -= 30f;
                }

                if (createAccountButton != null && createAccountButton.gameObject.activeSelf)
                {
                    SetPosition(createAccountButton.GetComponent<RectTransform>(), currentY - 10f);
                    currentY -= 64f;
                }

                if (toggleModeButton != null && toggleModeButton.gameObject.activeSelf)
                {
                    SetPosition(toggleModeButton.GetComponent<RectTransform>(), currentY - 10f);
                }
            }
            else
            {
                // Reset positions for logged in screen relative to compact card (dimensions: 500x350)
                if (usernameText != null) SetPosition(usernameText.GetComponent<RectTransform>(), 90f);
                if (serverText != null) SetPosition(serverText.GetComponent<RectTransform>(), 40f);
                if (uidText != null) SetPosition(uidText.GetComponent<RectTransform>(), -10f);
                if (createAccountButton != null) SetPosition(createAccountButton.GetComponent<RectTransform>(), -90f);
            }
        }

        private async void HandleSubmit()
        {
            if (_isProcessing || AuthManager.Instance == null) return;

            if (HasAccount)
            {
                AuthManager.Instance.SignOut();
                _currentMode = AuthMode.SignIn;
                _lastErrorMessage = "";
                _lastStatusMessage = "";
                Refresh();
                return;
            }

            _lastErrorMessage = "";
            _lastStatusMessage = "";

            string email = usernameInput != null ? usernameInput.text.Trim() : string.Empty;

            // Handle Guest login first (if email is empty and we are in SignIn mode)
            if (string.IsNullOrEmpty(email) && _currentMode == AuthMode.SignIn)
            {
                SetLoading(true);
                string err = await AuthManager.Instance.SignInAnonymouslyAsync();
                SetLoading(false);
                if (!string.IsNullOrEmpty(err))
                {
                    _lastErrorMessage = err;
                    Refresh();
                }
                else
                {
                    Hide();
                }
                return;
            }

            // --- 1. Email field validation ---
            if (string.IsNullOrEmpty(email))
            {
                _lastErrorMessage = "Email address cannot be empty.";
                Refresh();
                return;
            }

            // For Forgot Password and Sign Up, email format is strictly required
            if (_currentMode == AuthMode.ForgotPassword || _currentMode == AuthMode.SignUp)
            {
                if (!IsValidEmail(email))
                {
                    _lastErrorMessage = "Please enter a valid email address (e.g. name@domain.com).";
                    Refresh();
                    return;
                }
            }
            else if (_currentMode == AuthMode.SignIn)
            {
                // For Sign In, if they enter a plain username (no @), we format it to user@bes.com
                if (!email.Contains("@"))
                {
                    email = $"{email}@bes.com";
                }
                else if (!IsValidEmail(email))
                {
                    _lastErrorMessage = "Please enter a valid email address.";
                    Refresh();
                    return;
                }
            }

            string password = passwordInput != null ? passwordInput.text : string.Empty;
            string confirmPassword = confirmPasswordInput != null ? confirmPasswordInput.text : string.Empty;
            string displayName = displayNameInput != null ? displayNameInput.text.Trim() : string.Empty;
            string resetCode = resetCodeInput != null ? resetCodeInput.text.Trim() : string.Empty;

            // --- 2. Password field validation ---
            if (_currentMode == AuthMode.SignIn || _currentMode == AuthMode.SignUp || _currentMode == AuthMode.VerifyResetCode)
            {
                if (string.IsNullOrEmpty(password))
                {
                    _lastErrorMessage = "Password cannot be empty.";
                    Refresh();
                    return;
                }
                if (password.Length < 6)
                {
                    _lastErrorMessage = "Password must be at least 6 characters long.";
                    Refresh();
                    return;
                }
            }

            // --- 3. Mode-specific fields validation ---
            if (_currentMode == AuthMode.SignUp)
            {
                if (string.IsNullOrEmpty(displayName))
                {
                    _lastErrorMessage = "Display Name cannot be empty.";
                    Refresh();
                    return;
                }
                if (!IsValidDisplayName(displayName))
                {
                    _lastErrorMessage = "Display Name must be 3-20 characters and contain only letters, numbers, or underscores.";
                    Refresh();
                    return;
                }
                if (password != confirmPassword)
                {
                    _lastErrorMessage = "Passwords do not match.";
                    Refresh();
                    return;
                }
            }
            else if (_currentMode == AuthMode.VerifyResetCode)
            {
                if (string.IsNullOrEmpty(resetCode))
                {
                    _lastErrorMessage = "Verification code cannot be empty.";
                    Refresh();
                    return;
                }
                if (resetCode.Length != 6 || !System.Text.RegularExpressions.Regex.IsMatch(resetCode, @"^\d{6}$"))
                {
                    _lastErrorMessage = "Verification code must be exactly 6 numeric digits.";
                    Refresh();
                    return;
                }
            }

            string error = null;
            SetLoading(true);

            switch (_currentMode)
            {
                case AuthMode.SignIn:
                    error = await AuthManager.Instance.SignInWithEmailAsync(email, password);
                    break;

                case AuthMode.SignUp:
                    error = await AuthManager.Instance.SignUpWithEmailAsync(email, password, displayName);
                    break;

                case AuthMode.ForgotPassword:
                    error = await AuthManager.Instance.SendResetCodeAsync(email);
                    if (error == null)
                    {
                        _lastEmailEntered = email;
                        _currentMode = AuthMode.VerifyResetCode;
                        _lastStatusMessage = "A verification code has been sent.";
                    }
                    break;

                case AuthMode.VerifyResetCode:
                    error = await AuthManager.Instance.ResetPasswordWithCodeAsync(_lastEmailEntered, resetCode, password);
                    if (error == null)
                    {
                        _currentMode = AuthMode.SignIn;
                        _lastStatusMessage = "Password reset successfully! Please sign in.";
                        if (resetCodeInput != null) resetCodeInput.text = "";
                        if (passwordInput != null) passwordInput.text = "";
                    }
                    break;
            }

            SetLoading(false);

            if (!string.IsNullOrEmpty(error))
            {
                _lastErrorMessage = error;
            }
            else if (_currentMode == AuthMode.SignIn && HasAccount)
            {
                Hide();
            }
            else
            {
                Refresh();
            }
        }

        private void SetLoading(bool loading)
        {
            _isProcessing = loading;
            if (createAccountButton != null) createAccountButton.interactable = !loading;
            if (usernameInput != null) usernameInput.interactable = !loading && (_currentMode != AuthMode.VerifyResetCode);
            if (displayNameInput != null) displayNameInput.interactable = !loading;
            if (passwordInput != null) passwordInput.interactable = !loading;
            if (confirmPasswordInput != null) confirmPasswordInput.interactable = !loading;
            if (resetCodeInput != null) resetCodeInput.interactable = !loading;
            if (forgotPasswordButton != null) forgotPasswordButton.interactable = !loading;
            if (toggleModeButton != null) toggleModeButton.interactable = !loading;
            if (closeButton != null) closeButton.interactable = !loading;

            if (errorText != null && loading)
            {
                errorText.text = "<color=yellow>Processing authentication...</color>";
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                return System.Text.RegularExpressions.Regex.IsMatch(email, 
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidDisplayName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9_]{3,20}$");
        }
    }
}
