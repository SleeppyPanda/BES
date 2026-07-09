using BES.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class LoadingScreenUI : UIScreenBase
    {
        [SerializeField] Slider progressBar;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text tipText;

        static LoadingScreenUI instance;

        void Awake()
        {
            instance = this;
            if (root == null)
                root = gameObject;
            Hide();
        }

        void OnEnable()
        {
            GameEvents.OnSceneLoadStarted += OnLoadStarted;
            GameEvents.OnSceneLoadCompleted += OnLoadCompleted;
        }

        void OnDisable()
        {
            GameEvents.OnSceneLoadStarted -= OnLoadStarted;
            GameEvents.OnSceneLoadCompleted -= OnLoadCompleted;
        }

        void OnLoadStarted(string sceneName)
        {
            Show();
            SetStatus($"Loading {sceneName}...");
            SetProgress(0.2f);
        }

        void OnLoadCompleted(string sceneName)
        {
            SetProgress(1f);
            Hide();
        }

        void Update()
        {
            if (!IsOpen || progressBar == null)
                return;
            progressBar.value = Mathf.MoveTowards(progressBar.value, 0.95f, Time.deltaTime * 0.35f);
        }

        public override void Refresh()
        {
            if (tipText != null)
                tipText.text = "Tip: Press F near an NPC to interact.";
        }

        public void SetProgress(float value)
        {
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(value);
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        public static void ShowStatic(string message)
        {
            if (instance == null)
                return;
            instance.SetStatus(message);
            instance.Show();
        }
    }
}
