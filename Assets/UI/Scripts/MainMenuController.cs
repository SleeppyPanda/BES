using BES.Core;
using UnityEngine;
using UnityEngine.UI;

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
        }

        void Start()
        {
            Wire(regionButton, OpenServerPicker);
            Wire(quitButton, () => Application.Quit());
            Wire(clickToBeginButton, OnClickToBegin);
            Wire(profileButton, () => playerProfileUI?.Show());
            Wire(settingsButton, () => settingsUI?.Show());
            Wire(eventButton, () => eventUI?.Show());
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

        void StartNewGame()
        {
            LoadingScreenUI.ShowStatic("Starting new game...");
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadMenu();
        }

        bool IsDevBypassEnabled() => devMode;
    }
}
