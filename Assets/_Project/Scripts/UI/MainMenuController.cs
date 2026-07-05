using BES.Core;
using BES.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class MainMenuController : MonoBehaviour
    {
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

        void Awake()
        {
            var canvas = GetComponentInParent<Canvas>()?.transform ?? transform.root;
            MainMenuLayout.Apply(canvas);
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
            // Server đã lưu trong ServerPickerUI; chỉ đóng picker.
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
    }
}
