using System.Threading.Tasks;
using UnityEngine;
using BES.UI;
using BES.Gameplay;

namespace BES.Tests
{
    public class AuthSystemTest : MonoBehaviour
    {
        [SerializeField] bool runOnStart = false;

        async void Start()
        {
            if (runOnStart)
            {
                await RunTestSequenceAsync();
            }
        }

        public async Task RunTestSequenceAsync()
        {
            Debug.Log("<color=cyan>====== STARTING AUTHENTICATION SYSTEM TESTS =====</color>");
            
            AuthManager.IsTesting = true;
            var auth = AuthManager.Instance;
            if (auth == null)
            {
                Debug.LogError("AuthManager instance is null!");
                return;
            }

            // Clean up any existing session first
            auth.SignOut();

            // Test 1: Initial state
            Assert(!auth.IsAuthenticated, "Test 1: Player should not be authenticated initially.");
            Debug.Log("✔ Test 1: Initial state verified.");

            // Test 2: Guest Login
            Debug.Log("Testing Guest Login...");
            string guestError = await auth.SignInAnonymouslyAsync();
            Assert(string.IsNullOrEmpty(guestError), $"Guest login failed: {guestError}");
            Assert(auth.IsAuthenticated, "Player should be authenticated after guest login.");
            Assert(auth.IsGuest, "Player should be flagged as guest.");
            Assert(auth.CurrentUserId.StartsWith("GUEST_"), $"Guest user ID must start with GUEST_. Got: {auth.CurrentUserId}");
            Debug.Log($"✔ Test 2: Guest Login verified (UID: {auth.CurrentUserId}).");

            // Test 3: Sign Out
            auth.SignOut();
            Assert(!auth.IsAuthenticated, "Player should be unauthenticated after sign out.");
            Assert(string.IsNullOrEmpty(auth.CurrentUserId), "User ID should be cleared on sign out.");
            Debug.Log("✔ Test 3: Sign Out verified.");

            // Test 4: Validation checks (Invalid email)
            Debug.Log("Testing validation check for invalid email...");
            string validationError1 = await auth.SignInWithEmailAsync("invalid-email", "password123");
            Assert(!string.IsNullOrEmpty(validationError1), "Login should fail for invalid email format.");
            Debug.Log($"✔ Test 4: Invalid email validation verified (Returned error: '{validationError1}').");

            // Test 5: Validation checks (Password too short)
            Debug.Log("Testing validation check for short password...");
            string validationError2 = await auth.SignInWithEmailAsync("user@bes.com", "123");
            Assert(!string.IsNullOrEmpty(validationError2), "Login should fail for password shorter than 6 characters.");
            Debug.Log($"✔ Test 5: Short password validation verified (Returned error: '{validationError2}').");

            // Test 6: Sign Up Success
            Debug.Log("Testing Sign Up...");
            string email = "player_" + Random.Range(1000, 9999) + "@bes.com";
            string signUpError = await auth.SignUpWithEmailAsync(email, "mypassword123", "TestPlayer");
            Assert(string.IsNullOrEmpty(signUpError), $"Sign up failed: {signUpError}");
            Assert(auth.IsAuthenticated, "Player should be authenticated after sign up.");
            Assert(!auth.IsGuest, "Player should not be guest after email sign up.");
            Assert(auth.CurrentUserName == "TestPlayer", $"Expected username 'TestPlayer', got '{auth.CurrentUserName}'");
            Debug.Log($"✔ Test 6: Sign Up verified (UID: {auth.CurrentUserId}, Username: {auth.CurrentUserName}).");

            // Test 6b: Sign Up Duplicate Check
            Debug.Log("Testing Duplicate Sign Up...");
            string signUpDupError = await auth.SignUpWithEmailAsync(email, "otherpassword", "TestPlayer2");
            Assert(!string.IsNullOrEmpty(signUpDupError), "Signing up with an existing email should fail.");
            Debug.Log($"✔ Test 6b: Duplicate Sign Up validation verified (Returned error: '{signUpDupError}').");

            // Test 6c: Sign In with Incorrect Password
            Debug.Log("Testing Sign In with incorrect password...");
            auth.SignOut();
            string signInWrongError = await auth.SignInWithEmailAsync(email, "wrongpassword");
            Assert(!string.IsNullOrEmpty(signInWrongError), "Sign in with wrong password should fail.");
            Assert(signInWrongError == "Incorrect password.", $"Expected 'Incorrect password.', got '{signInWrongError}'");
            Debug.Log($"✔ Test 6c: Incorrect password sign in verified.");

            // Test 6d: Sign In with Correct Password
            Debug.Log("Testing Sign In with correct password...");
            string signInOkError = await auth.SignInWithEmailAsync(email, "mypassword123");
            Assert(string.IsNullOrEmpty(signInOkError), $"Sign in failed: {signInOkError}");
            Assert(auth.IsAuthenticated, "Player should be authenticated after correct login.");
            Assert(auth.CurrentUserName == "TestPlayer", "Username must match the registered username.");
            Debug.Log("✔ Test 6d: Correct password sign in verified.");

            // Test 6e: Forgot Password and Reset Code Verification
            Debug.Log("Testing Forgot Password flow...");
            string sendCodeError = await auth.SendResetCodeAsync(email);
            Assert(string.IsNullOrEmpty(sendCodeError), $"Send reset code failed: {sendCodeError}");
            
            string resetPassError = await auth.ResetPasswordWithCodeAsync(email, "123456", "newpassword123");
            Assert(string.IsNullOrEmpty(resetPassError), $"Password reset failed: {resetPassError}");
            
            // Test Login with new password
            auth.SignOut();
            string loginNewError = await auth.SignInWithEmailAsync(email, "newpassword123");
            Assert(string.IsNullOrEmpty(loginNewError), $"Login with new password failed: {loginNewError}");
            Debug.Log("✔ Test 6e: Forgot Password and password reset verified.");

            // Test 7: Cloud Save Sync Upload/Download
            Debug.Log("Testing Cloud Save synchronization...");
            var saveGo = new GameObject("TempSaveSystem");
            var saveSystem = saveGo.AddComponent<SaveSystem>();

            // Setup dummy save data
            saveSystem.CreateNewSave();
            saveSystem.Current.playerHealth = 75;
            saveSystem.Current.playerMana = 40;
            saveSystem.Current.playerPosX = 10f;
            saveSystem.Current.playerPosY = 20f;
            saveSystem.Current.playerPosZ = 30f;

            // Upload
            await saveSystem.SyncSaveToCloudAsync();
            Debug.Log("✔ Cloud save upload simulated successfully.");

            // Clear local save in memory
            saveSystem.CreateNewSave();
            Assert(saveSystem.Current.playerHealth != 75, "Save system should be reset.");

            // Download
            bool downloaded = await saveSystem.SyncSaveFromCloudAsync();
            Assert(downloaded, "Cloud save download should succeed.");
            Assert(saveSystem.Current.playerHealth == 75, $"Expected health to be 75, got {saveSystem.Current.playerHealth}");
            Assert(saveSystem.Current.playerPosX == 10f, $"Expected position X to be 10, got {saveSystem.Current.playerPosX}");
            Debug.Log("✔ Test 7: Cloud Save upload and download synchronization verified.");

            Destroy(saveGo);

            // Test 8: Session Persistence (Auto-login)
            Debug.Log("Testing session auto-login...");
            string savedUid = auth.CurrentUserId;
            // Force reload instance to simulate restart
            auth.SignOut(); // Clear
            
            // Re-save session manually using encryption to simulate auto-login on startup
            PlayerPrefs.SetString("BES_SessionToken", auth.Encrypt("mock_token_email_" + savedUid));
            PlayerPrefs.SetString("BES_SessionUserId", auth.Encrypt(savedUid));
            PlayerPrefs.SetString("BES_SessionUserEmail", auth.Encrypt(email));
            PlayerPrefs.SetString("BES_SessionUserName", auth.Encrypt("TestPlayer"));
            PlayerPrefs.SetInt("BES_SessionIsGuest", 0);
            PlayerPrefs.Save();

            // Simulate startup trigger by accessing Instance or calling Awake equivalent
            var autoLoginMethod = typeof(AuthManager).GetMethod("TryAutoLogin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            autoLoginMethod?.Invoke(auth, null);

            Assert(auth.IsAuthenticated, "Player should be auto-logged in from PlayerPrefs.");
            Assert(auth.CurrentUserId == savedUid, $"Expected auto-logged UID '{savedUid}', got '{auth.CurrentUserId}'");
            Debug.Log("✔ Test 8: Session persistence and auto-login verified.");

            AuthManager.IsTesting = false;
            Debug.Log("<color=green>====== ALL AUTHENTICATION SYSTEM TESTS PASSED SUCCESSFULLY! =====</color>");
        }

        private void Assert(bool condition, string errorMessage)
        {
            if (!condition)
            {
                Debug.LogError($"<color=red>[Assertion Failed] {errorMessage}</color>");
                throw new System.Exception(errorMessage);
            }
        }
    }
}
