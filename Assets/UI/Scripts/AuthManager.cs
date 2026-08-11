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

        private const string SessionTokenKey = "BES_SessionToken";
        private const string SessionUserIdKey = "BES_SessionUserId";
        private const string SessionUserEmailKey = "BES_SessionUserEmail";
        private const string SessionUserNameKey = "BES_SessionUserName";
        private const string SessionIsGuestKey = "BES_SessionIsGuest";

        public event Action OnAuthStatusChanged;

        public bool IsAuthenticated { get; private set; }
        public bool IsGuest { get; private set; }
        public string CurrentUserId { get; private set; }
        public string CurrentUserEmail { get; private set; }
        public string CurrentUserName { get; private set; }

        private void Awake()
        {
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

        private void TryAutoLogin()
        {
            if (PlayerPrefs.HasKey(SessionTokenKey))
            {
                CurrentUserId = PlayerPrefs.GetString(SessionUserIdKey, "UID_100000001");
                CurrentUserEmail = PlayerPrefs.GetString(SessionUserEmailKey, "");
                CurrentUserName = PlayerPrefs.GetString(SessionUserNameKey, "Player");
                IsGuest = PlayerPrefs.GetInt(SessionIsGuestKey, 0) == 1;
                IsAuthenticated = true;
            }
        }

        public async Task<string> SignInWithEmailAsync(string email, string password)
        {
            // Simulate network latency
            await Task.Delay(1000);

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return "Invalid email address format.";
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return "Password must be at least 6 characters long.";
            }

            // Mock database validation
            if (email.ToLower() == "admin@bes.com" && password != "admin123")
            {
                return "Incorrect password.";
            }

            // Mock login success
            IsAuthenticated = true;
            IsGuest = false;
            CurrentUserId = "UID_" + Mathf.Abs(email.GetHashCode());
            CurrentUserEmail = email;
            
            // Try to extract name from email
            var namePart = email.Split('@')[0];
            CurrentUserName = char.ToUpper(namePart[0]) + (namePart.Length > 1 ? namePart.Substring(1) : "");

            SaveSession("mock_token_email_" + CurrentUserId);
            OnAuthStatusChanged?.Invoke();
            return null; // Return null means success
        }

        public async Task<string> SignUpWithEmailAsync(string email, string password, string username)
        {
            await Task.Delay(1200);

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

            // Mock sign up success
            IsAuthenticated = true;
            IsGuest = false;
            CurrentUserId = "UID_" + Mathf.Abs(email.GetHashCode());
            CurrentUserEmail = email;
            CurrentUserName = username;

            SaveSession("mock_token_email_" + CurrentUserId);
            OnAuthStatusChanged?.Invoke();
            return null; // Return null means success
        }

        public async Task<string> SignInAnonymouslyAsync()
        {
            await Task.Delay(800);

            IsAuthenticated = true;
            IsGuest = true;
            CurrentUserId = "GUEST_" + UnityEngine.Random.Range(100000, 999999);
            CurrentUserEmail = "";
            CurrentUserName = "Guest " + CurrentUserId.Split('_')[1];

            SaveSession("mock_token_guest_" + CurrentUserId);
            OnAuthStatusChanged?.Invoke();
            return null;
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

        private void SaveSession(string token)
        {
            PlayerPrefs.SetString(SessionTokenKey, token);
            PlayerPrefs.SetString(SessionUserIdKey, CurrentUserId);
            PlayerPrefs.SetString(SessionUserEmailKey, CurrentUserEmail);
            PlayerPrefs.SetString(SessionUserNameKey, CurrentUserName);
            PlayerPrefs.SetInt(SessionIsGuestKey, IsGuest ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
