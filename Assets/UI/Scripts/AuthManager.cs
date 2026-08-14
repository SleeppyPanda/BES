using System;
using System.Threading.Tasks;
using UnityEngine;

namespace BES.UI
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance
        {
            get
            {
                if (_isQuitting)
                {
                    return null;
                }
                if (_instance == null)
                {
                    var go = GameObject.Find("AuthManager");
                    if (go == null)
                    {
                        go = new GameObject("AuthManager");
                    }
                    _instance = go.GetComponent<AuthManager>();
                    if (_instance == null)
                    {
                        _instance = go.AddComponent<AuthManager>();
                    }
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        private static AuthManager _instance;
        private static bool _isQuitting = false;

        public static bool IsTesting = false;

        private const string FirebaseApiKey = "AIzaSyCpKVcYV3GM9-pwhATjmDOrC7FiN1bpSTM";

        private const string SessionTokenKey = "BES_SessionToken";
        private const string SessionUserIdKey = "BES_SessionUserId";
        private const string SessionUserEmailKey = "BES_SessionUserEmail";
        private const string SessionUserNameKey = "BES_SessionUserName";
        private const string SessionIsGuestKey = "BES_SessionIsGuest";

        private const string UserPasswordPrefix = "BES_User_Password_";
        private const string UserNamePrefix = "BES_User_Name_";

        private string _mockResetCode;
        private string _mockResetEmail;

        public event Action OnAuthStatusChanged;

        public bool IsAuthenticated { get; private set; }
        public bool IsGuest { get; private set; }
        public string CurrentUserId { get; private set; }
        public string CurrentUserEmail { get; private set; }
        public string CurrentUserName { get; private set; }

        private void Awake()
        {
            _isQuitting = false;
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                TryAutoLogin();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void TryAutoLogin()
        {
            if (PlayerPrefs.HasKey(SessionTokenKey))
            {
                CurrentUserId = Decrypt(PlayerPrefs.GetString(SessionUserIdKey, ""));
                CurrentUserEmail = Decrypt(PlayerPrefs.GetString(SessionUserEmailKey, ""));
                CurrentUserName = Decrypt(PlayerPrefs.GetString(SessionUserNameKey, "Player"));
                IsGuest = PlayerPrefs.GetInt(SessionIsGuestKey, 0) == 1;
                IsAuthenticated = !string.IsNullOrEmpty(CurrentUserId);
            }
        }

        public async Task<string> SignInWithEmailAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return "Invalid email address format.";
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return "Password must be at least 6 characters long.";
            }

            if (IsTesting)
            {
                await Task.Delay(500);
                var emailKey = email.ToLower();
                string expectedPassword = emailKey == "admin@bes.com" ? "admin123" : PlayerPrefs.GetString(UserPasswordPrefix + emailKey, "");
                if (string.IsNullOrEmpty(expectedPassword) && emailKey != "admin@bes.com")
                {
                    return "Email is not registered.";
                }
                if (password != expectedPassword)
                {
                    return "Incorrect password.";
                }

                IsAuthenticated = true;
                IsGuest = false;
                CurrentUserId = "UID_" + Mathf.Abs(email.GetHashCode());
                CurrentUserEmail = email;
                CurrentUserName = emailKey.Split('@')[0];
                SaveSession("mock_token_email_" + CurrentUserId);
                OnAuthStatusChanged?.Invoke();
                return null;
            }

            // Check local PlayerPrefs first to support OTP password reset flow
            var lowerEmail = email.ToLower();
            string savedPassword = PlayerPrefs.GetString(UserPasswordPrefix + lowerEmail, "");
            if (!string.IsNullOrEmpty(savedPassword) && savedPassword == password)
            {
                IsAuthenticated = true;
                IsGuest = false;
                CurrentUserId = "UID_" + Mathf.Abs(email.GetHashCode());
                CurrentUserEmail = email;
                CurrentUserName = PlayerPrefs.GetString(UserNamePrefix + lowerEmail, lowerEmail.Split('@')[0]);
                SaveSession("local_token_email_" + CurrentUserId);
                OnAuthStatusChanged?.Invoke();
                return null;
            }

            // Real Firebase REST API Sign In
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FirebaseApiKey}";
            var req = new FirebaseSignInRequest { email = email, password = password, returnSecureToken = true };
            string payload = JsonUtility.ToJson(req);

            string error = await PostToFirebaseAsync(url, payload, (res) =>
            {
                IsAuthenticated = true;
                IsGuest = false;
                CurrentUserId = res.localId;
                CurrentUserEmail = res.email;
                CurrentUserName = string.IsNullOrEmpty(res.displayName) ? email.Split('@')[0] : res.displayName;
                SaveSession(res.idToken);
            });

            if (error == null)
            {
                OnAuthStatusChanged?.Invoke();
            }
            return error;
        }

        public async Task<string> SignUpWithEmailAsync(string email, string password, string username)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return "Invalid email address format.";
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return "Password must be at least 6 characters long.";
            }

            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            {
                return "Username must be at least 3 characters long.";
            }

            if (IsTesting)
            {
                await Task.Delay(500);
                var emailKey = email.ToLower();
                if (emailKey == "admin@bes.com" || PlayerPrefs.HasKey(UserPasswordPrefix + emailKey))
                {
                    return "Email is already registered.";
                }
                PlayerPrefs.SetString(UserPasswordPrefix + emailKey, password);
                PlayerPrefs.SetString(UserNamePrefix + emailKey, username);
                PlayerPrefs.Save();

                IsAuthenticated = true;
                IsGuest = false;
                CurrentUserId = "UID_" + Mathf.Abs(email.GetHashCode());
                CurrentUserEmail = email;
                CurrentUserName = username;
                SaveSession("mock_token_email_" + CurrentUserId);
                OnAuthStatusChanged?.Invoke();
                return null;
            }

            // Real Firebase REST API Sign Up
            string signUpUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={FirebaseApiKey}";
            var signUpReq = new FirebaseSignUpRequest { email = email, password = password, returnSecureToken = true };
            string signUpPayload = JsonUtility.ToJson(signUpReq);

            string tokenToUse = "";
            string localIdToUse = "";
            string error = await PostToFirebaseAsync(signUpUrl, signUpPayload, (res) =>
            {
                tokenToUse = res.idToken;
                localIdToUse = res.localId;
            });

            if (error != null) return error;

            // Set the Display Name via profile update API
            string updateUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={FirebaseApiKey}";
            var updateReq = new FirebaseUpdateProfileRequest { idToken = tokenToUse, displayName = username, returnSecureToken = true };
            string updatePayload = JsonUtility.ToJson(updateReq);

            await PostToFirebaseAsync(updateUrl, updatePayload, (res) =>
            {
                IsAuthenticated = true;
                IsGuest = false;
                CurrentUserId = localIdToUse;
                CurrentUserEmail = email;
                CurrentUserName = username;
                SaveSession(tokenToUse);

                // Save locally to support OTP verification flow
                var emailKey = email.ToLower();
                PlayerPrefs.SetString(UserPasswordPrefix + emailKey, password);
                PlayerPrefs.SetString(UserNamePrefix + emailKey, username);
                PlayerPrefs.Save();
            });

            OnAuthStatusChanged?.Invoke();
            return null;
        }

        public async Task<string> SendResetCodeAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return "Invalid email address format.";
            }

            if (IsTesting)
            {
                await Task.Delay(500);
                _mockResetCode = "123456";
                _mockResetEmail = email.ToLower();
                return null;
            }

            // Generate a real random 6-digit OTP code
            int generatedOtp = UnityEngine.Random.Range(100000, 999999);
            _mockResetCode = generatedOtp.ToString();
            _mockResetEmail = email.ToLower();

            // Send real email via SMTP
            return await SendOTPEmailAsync(email, _mockResetCode);
        }

        public async Task<string> ResetPasswordWithCodeAsync(string email, string code, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return "Invalid email address format.";
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return "Password must be at least 6 characters long.";
            }

            var emailKey = email.ToLower();
            if (emailKey == _mockResetEmail && code == _mockResetCode)
            {
                // Save the new password in local PlayerPrefs so they can log in immediately
                PlayerPrefs.SetString(UserPasswordPrefix + emailKey, newPassword);
                PlayerPrefs.Save();

                _mockResetCode = null;
                _mockResetEmail = null;
                return null; // Return null to indicate reset success
            }

            return "Invalid verification code. Please check your email.";
        }

        private async Task<string> SendOTPEmailAsync(string recipientEmail, string otpCode)
        {
            string errorResult = null;
            await Task.Run(() =>
            {
                try
                {
                    var mail = new System.Net.Mail.MailMessage();
                    mail.From = new System.Net.Mail.MailAddress("noreply@test-65qngkdqqndlwr12.mlsender.net", "BES-GAME");
                    mail.To.Add(recipientEmail);
                    mail.Subject = "BES-GAME: Password Reset Verification Code";
                    mail.Body = $"Hello,\n\nYour password reset verification code is: {otpCode}\n\nPlease enter this code in the game client to reset your password.\n\nThank you,\nBES-GAME Team";
                    
                    var smtpServer = new System.Net.Mail.SmtpClient("smtp.mailersend.net");
                    smtpServer.Port = 587;
                    smtpServer.Credentials = new System.Net.NetworkCredential(
                        "MS_F4opPv@test-65qngkdqqndlwr12.mlsender.net", 
                        "mssp.3dt3y2M.z3m5jgrz0zoldpyo.MYxiSF8"
                    );
                    smtpServer.EnableSsl = true;
                    
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = 
                        delegate (object s, System.Security.Cryptography.X509Certificates.X509Certificate certificate, 
                                  System.Security.Cryptography.X509Certificates.X509Chain chain, 
                                  System.Net.Security.SslPolicyErrors sslPolicyErrors) 
                        { return true; };

                    smtpServer.Send(mail);
                    Debug.Log($"[SMTP] Real OTP email sent successfully to {recipientEmail} with code {otpCode}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SMTP] Failed to send real OTP email: {ex.Message}");
                    errorResult = $"Failed to send email: {ex.Message}";
                }
            });
            return errorResult;
        }

        public async Task<string> SignInAnonymouslyAsync()
        {
            if (IsTesting)
            {
                await Task.Delay(500);
                IsAuthenticated = true;
                IsGuest = true;
                CurrentUserId = "GUEST_" + UnityEngine.Random.Range(100000, 999999);
                CurrentUserEmail = "";
                CurrentUserName = "Guest " + CurrentUserId.Split('_')[1];
                SaveSession("mock_token_guest_" + CurrentUserId);
                OnAuthStatusChanged?.Invoke();
                return null;
            }

            // Real Firebase REST API Anonymous Sign In
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={FirebaseApiKey}";
            var req = new FirebaseAnonymousRequest { returnSecureToken = true };
            string payload = JsonUtility.ToJson(req);

            string error = await PostToFirebaseAsync(url, payload, (res) =>
            {
                IsAuthenticated = true;
                IsGuest = true;
                CurrentUserId = res.localId;
                CurrentUserEmail = "";
                CurrentUserName = "Guest " + res.localId.Substring(Math.Max(0, res.localId.Length - 6));
                SaveSession(res.idToken);
            });

            if (error == null)
            {
                OnAuthStatusChanged?.Invoke();
            }
            return error;
        }

        public void SignOut()
        {
            IsAuthenticated = false;
            IsGuest = false;
            CurrentUserId = null;
            CurrentUserEmail = null;
            CurrentUserName = null;

            PlayerPrefs.DeleteKey(SessionTokenKey);
            PlayerPrefs.DeleteKey(SessionUserIdKey);
            PlayerPrefs.DeleteKey(SessionUserEmailKey);
            PlayerPrefs.DeleteKey(SessionUserNameKey);
            PlayerPrefs.DeleteKey(SessionIsGuestKey);
            PlayerPrefs.Save();

            OnAuthStatusChanged?.Invoke();
        }

        internal string Encrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ 0x5A); // Simple XOR encryption with key 0x5A
            }
            return Convert.ToBase64String(bytes);
        }

        internal string Decrypt(string encryptedValue)
        {
            if (string.IsNullOrEmpty(encryptedValue)) return "";
            try
            {
                byte[] bytes = Convert.FromBase64String(encryptedValue);
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(bytes[i] ^ 0x5A);
                }
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "";
            }
        }

        private void SaveSession(string token)
        {
            PlayerPrefs.SetString(SessionTokenKey, Encrypt(token));
            PlayerPrefs.SetString(SessionUserIdKey, Encrypt(CurrentUserId));
            PlayerPrefs.SetString(SessionUserEmailKey, Encrypt(CurrentUserEmail));
            PlayerPrefs.SetString(SessionUserNameKey, Encrypt(CurrentUserName));
            PlayerPrefs.SetInt(SessionIsGuestKey, IsGuest ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ==================== FIREBASE HTTP HELPER ====================

        private async Task<string> PostToFirebaseAsync(string endpoint, string jsonPayload, Action<FirebaseResponse> onSuccess)
        {
            using (var webRequest = new UnityEngine.Networking.UnityWebRequest(endpoint, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                webRequest.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Delay(50);
                }

                if (webRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    var res = JsonUtility.FromJson<FirebaseResponse>(jsonResponse);
                    onSuccess?.Invoke(res);
                    return null; // Null means success
                }
                else
                {
                    string errorText = webRequest.downloadHandler.text;
                    try
                    {
                        var err = JsonUtility.FromJson<FirebaseErrorContainer>(errorText);
                        string msg = err?.error?.message ?? "UNKNOWN_ERROR";
                        return FormatErrorMessage(msg);
                    }
                    catch
                    {
                        return "Network or server connection failed.";
                    }
                }
            }
        }

        private string FormatErrorMessage(string firebaseMsg)
        {
            if (firebaseMsg.Contains("EMAIL_EXISTS")) return "Email is already registered.";
            if (firebaseMsg.Contains("INVALID_LOGIN_CREDENTIALS") || firebaseMsg.Contains("INVALID_PASSWORD") || firebaseMsg.Contains("EMAIL_NOT_FOUND"))
                return "Incorrect email or password.";
            if (firebaseMsg.Contains("WEAK_PASSWORD")) return "Password must be at least 6 characters long.";
            if (firebaseMsg.Contains("INVALID_EMAIL")) return "Invalid email address format.";
            if (firebaseMsg.Contains("USER_DISABLED")) return "This account has been disabled.";
            if (firebaseMsg.Contains("TOO_MANY_ATTEMPTS_TRY_LATER")) return "Too many failed attempts. Please try again later.";
            return firebaseMsg;
        }

        // ==================== FIREBASE DATA CLASSES ====================

        [Serializable]
        private class FirebaseSignUpRequest
        {
            public string email;
            public string password;
            public bool returnSecureToken;
        }

        [Serializable]
        private class FirebaseSignInRequest
        {
            public string email;
            public string password;
            public bool returnSecureToken;
        }

        [Serializable]
        private class FirebaseAnonymousRequest
        {
            public bool returnSecureToken;
        }

        [Serializable]
        private class FirebaseUpdateProfileRequest
        {
            public string idToken;
            public string displayName;
            public bool returnSecureToken;
        }

#pragma warning disable 0649
        [Serializable]
        private class FirebaseResetRequest
        {
            public string requestType;
            public string email;
        }
#pragma warning restore 0649

#pragma warning disable 0649
        [Serializable]
        private class FirebaseErrorContainer
        {
            public FirebaseError error;
        }

        [Serializable]
        private class FirebaseError
        {
            public int code;
            public string message;
        }
#pragma warning restore 0649

        [Serializable]
        public class FirebaseResponse
        {
            public string idToken;
            public string localId;
            public string email;
            public string displayName;
        }
    }
}
