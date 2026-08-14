using BES.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Dev")]
        [SerializeField] bool devMode;

        [Header("Scene Objects")]
        [SerializeField] GameObject logoObject;
        [SerializeField] Button regionButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button clickToBeginButton;
        [SerializeField] Button profileButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button eventButton;
        [SerializeField] PlayerProfileUI playerProfileUI;
        [SerializeField] SettingsUI settingsUI;
        [SerializeField] ServerPickerUI serverPickerUI;
        [SerializeField] EventUI eventUI;

        void Awake()
        {
            var canvas = GetComponentInParent<Canvas>()?.transform ?? transform.root;
            MainMenuLayout.Apply(canvas);
            if (logoObject != null)
                logoObject.SetActive(true);

            // Initialize AuthManager early for auto-login check
            var auth = AuthManager.Instance;

            // 1. Find and disable any text labels with "Click to begin" or variants on the canvas
            if (canvas != null)
            {
                var allTMPro = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in allTMPro)
                {
                    string t = tmp.text.ToLower();
                    if (t.Contains("click to begin") || t == "begin" || t == "click to" || t == "egin")
                    {
                        tmp.gameObject.SetActive(false);
                    }
                }

                var allStandardText = canvas.GetComponentsInChildren<Text>(true);
                foreach (var txt in allStandardText)
                {
                    string t = txt.text.ToLower();
                    if (t.Contains("click to begin") || t == "begin" || t == "click to" || t == "egin")
                    {
                        txt.gameObject.SetActive(false);
                    }
                }
            }

            // 2. Reposition and format ClickToBegin hit area as a full-screen "Tap to Start" overlay
            if (clickToBeginButton != null)
            {
                clickToBeginButton.interactable = true;

                // Make the button transparent (near-zero alpha to guarantee it intercepts raycasts in all Canvas configurations)
                var img = clickToBeginButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0f, 0f, 0f, 0.005f);
                    img.raycastTarget = true;
                }

                // Stretch the button to cover the entire screen
                var rect = clickToBeginButton.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                }

                // Place it right above the background (sibling index 1)
                // so that it receives clicks anywhere on screen, except when clicking on corner buttons which are on top
                clickToBeginButton.transform.SetSiblingIndex(1);

                Debug.Log($"[MainMenuController] clickToBeginButton initialized as Fullscreen (Tap to Start) at siblingIndex={clickToBeginButton.transform.GetSiblingIndex()}");
            }

            // 3. Anchor and size corner buttons properly to align with background visuals in any aspect ratio
            ConfigureCornerButton(quitButton, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(76f, 48f));
            ConfigureCornerButton(settingsButton, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-148f, 48f));
            ConfigureCornerButton(eventButton, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-148f, 128f));
            ConfigureCornerButton(profileButton, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-148f, 208f));
        }

        private void ConfigureCornerButton(Button button, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos)
        {
            if (button == null) return;
            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.anchoredPosition = anchoredPos;
                rect.sizeDelta = new Vector2(90f, 90f); // Increase hit box size to 90x90 for a comfortable, responsive click area
            }

            // Ensure the image blocks raycasts and has a small alpha or transparent color
            var img = button.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = true;
            }
        }

        void Start()
        {
            Wire(regionButton, OpenServerPicker);
            Wire(quitButton, OnLogoutClicked);
            Wire(clickToBeginButton, OnClickToBegin);
            Wire(profileButton, () => playerProfileUI?.Show());
            Wire(settingsButton, () => settingsUI?.Show());
            Wire(eventButton, () => eventUI?.Show());
        }

        void OnLogoutClicked()
        {
            Debug.Log("[MainMenuController] Logout clicked.");
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.SignOut();
            }
            if (playerProfileUI != null)
            {
                playerProfileUI.Show();
            }
        }

        static void Wire(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null)
                btn.onClick.AddListener(action);
        }

        void OpenServerPicker() => serverPickerUI?.Show();

        void OnClickToBegin()
        {
            Debug.Log("[MainMenuController] Click To Begin clicked!");
            bool hasAcc = PlayerProfileUI.HasAccount;
            bool devBypass = IsDevBypassEnabled();
            Debug.Log($"[MainMenuController] HasAccount: {hasAcc}, DevBypass: {devBypass}");

            if (!hasAcc && !devBypass)
            {
                Debug.Log("[MainMenuController] Showing playerProfileUI...");
                if (playerProfileUI != null)
                {
                    playerProfileUI.Show();
                }
                else
                {
                    Debug.LogError("[MainMenuController] playerProfileUI reference is null!");
                }
                return;
            }

            var server = ServerPickerUI.GetSelectedServer();
            Debug.Log($"[MainMenuController] Selected server: {server}");
            if (string.IsNullOrEmpty(server))
            {
                Debug.Log("[MainMenuController] Selected server is null or empty, opening ServerPicker...");
                OpenServerPicker();
                return;
            }

            StartNewGame();
        }

        void StartNewGame()
        {
            LoadingScreenUI.ShowStatic("Starting new game...");
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadMenu();
        }

        bool IsDevBypassEnabled() => devMode;
    }
}
