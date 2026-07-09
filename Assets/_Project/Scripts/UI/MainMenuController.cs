using BES.Core;
using BES.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class MainMenuController : MonoBehaviour
    {
        const string DevBypassAccountKey = "BES_DevBypassAccount";

        [SerializeField] Button newGameButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button clickToBeginButton;
        [SerializeField] Button profileButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button eventButton;
        [SerializeField] PlayerProfileUI playerProfileUI;
        [SerializeField] SettingsUI settingsUI;
        [SerializeField] ServerPickerUI serverPickerUI;
        [SerializeField] EventUI eventUI;

        Toggle devBypassToggle;

        void Awake()
        {
            var canvas = GetComponentInParent<Canvas>()?.transform ?? transform.root;
            MainMenuLayout.Apply(canvas);
            EnsureMainMenuPresentation(canvas);
        }

        void Start()
        {
            Wire(newGameButton, OpenServerPicker);
            Wire(continueButton, OnContinue);
            Wire(quitButton, () => Application.Quit());
            Wire(clickToBeginButton, OnClickToBegin);
            Wire(profileButton, () => playerProfileUI?.Show());
            Wire(settingsButton, () => settingsUI?.Show());
            Wire(eventButton, () => eventUI?.Show());

            if (serverPickerUI != null)
                serverPickerUI.OnServerSelected += OnServerSelected;

            if (continueButton != null)
            {
                var hasSave = GameManager.Instance?.Save?.HasSave == true;
                continueButton.gameObject.SetActive(hasSave);
            }
        }

        void OnDestroy()
        {
            if (serverPickerUI != null)
                serverPickerUI.OnServerSelected -= OnServerSelected;
        }

        static void Wire(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null)
                btn.onClick.AddListener(action);
        }

        void OpenServerPicker() => serverPickerUI?.Show();

        void OnClickToBegin()
        {
            if (!PlayerProfileUI.HasAccount && !IsDevBypassEnabled())
            {
                playerProfileUI?.Show();
                return;
            }

            var server = ServerPickerUI.GetSelectedServer();
            if (string.IsNullOrEmpty(server))
            {
                OpenServerPicker();
                return;
            }

            StartNewGame();
        }

        void OnServerSelected(string serverId)
        {
            // Server is persisted by ServerPickerUI; this hook is reserved for later UI feedback.
        }

        void StartNewGame()
        {
            LoadingScreenUI.ShowStatic("Starting new game...");
            GameManager.Instance?.NewGame();
        }

        void OnContinue()
        {
            LoadingScreenUI.ShowStatic("Loading save...");
            GameManager.Instance?.ContinueGame();
        }

        bool IsDevBypassEnabled() => devBypassToggle != null && devBypassToggle.isOn;

        void EnsureMainMenuPresentation(Transform canvas)
        {
            EnsureButtonLabel(newGameButton, "Region");
            EnsureButtonLabel(quitButton, "Logout");
            EnsureButtonLabel(profileButton, "Account");
            EnsureButtonLabel(settingsButton, "Settings");
            EnsureButtonLabel(eventButton, "Event");
            EnsureButtonLabel(clickToBeginButton, "Click to begin");
            EnsureButtonLabel(continueButton, "Continue");
            EnsureLogo(canvas);
            EnsureDevToggle(canvas);
        }

        static void EnsureButtonLabel(Button button, string label)
        {
            if (button == null || button.GetComponentInChildren<TMP_Text>(true) != null)
                return;

            var image = button.GetComponent<Image>();
            if (image != null && image.color.a <= 0.01f)
                image.color = new Color(0.05f, 0.07f, 0.11f, 0.62f);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(button.transform, false);
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = label.Length > 10 ? 18f : 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        static void EnsureLogo(Transform canvas)
        {
            if (canvas == null || canvas.Find("MainMenuLogo") != null)
                return;

            var logoGo = new GameObject("MainMenuLogo", typeof(RectTransform), typeof(Image));
            logoGo.transform.SetParent(canvas, false);
            var rect = logoGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 160f);
            rect.anchoredPosition = new Vector2(0f, 80f);

            var image = logoGo.GetComponent<Image>();
            image.color = new Color(0.05f, 0.07f, 0.11f, 0.58f);
            image.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(logoGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = "BES";
            text.fontSize = 54f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.96f, 0.78f, 0.32f, 1f);
            text.raycastTarget = false;
        }

        void EnsureDevToggle(Transform canvas)
        {
            if (canvas == null)
                return;

            var existing = canvas.Find("DevBypassAccountToggle");
            if (existing != null)
            {
                devBypassToggle = existing.GetComponent<Toggle>();
                return;
            }

            var toggleGo = new GameObject("DevBypassAccountToggle", typeof(RectTransform), typeof(Toggle));
            toggleGo.transform.SetParent(canvas, false);
            var rect = toggleGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(220f, 36f);
            rect.anchoredPosition = new Vector2(48f, 118f);

            var boxGo = new GameObject("Box", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(toggleGo.transform, false);
            var boxRect = boxGo.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0f, 0.5f);
            boxRect.sizeDelta = new Vector2(28f, 28f);
            boxRect.anchoredPosition = Vector2.zero;
            boxGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.22f);

            var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(boxGo.transform, false);
            var checkRect = checkGo.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.sizeDelta = new Vector2(18f, 18f);
            checkRect.anchoredPosition = Vector2.zero;
            checkGo.GetComponent<Image>().color = new Color(0.95f, 0.78f, 0.28f, 1f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(toggleGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(40f, 0f);
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "Dev bypass account";
            label.fontSize = 15f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;
            label.raycastTarget = false;

            devBypassToggle = toggleGo.GetComponent<Toggle>();
            devBypassToggle.targetGraphic = boxGo.GetComponent<Image>();
            devBypassToggle.graphic = checkGo.GetComponent<Image>();
            devBypassToggle.isOn = PlayerPrefs.GetInt(DevBypassAccountKey, 0) == 1;
            devBypassToggle.onValueChanged.AddListener(value => PlayerPrefs.SetInt(DevBypassAccountKey, value ? 1 : 0));
        }
    }
}
