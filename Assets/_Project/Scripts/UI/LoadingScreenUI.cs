using BES.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            if (statusText != null)
                statusText.text = $"Loading {sceneName}...";
            if (progressBar != null)
                progressBar.value = 0.2f;
        }

        void OnLoadCompleted(string sceneName)
        {
            if (progressBar != null)
                progressBar.value = 1f;
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
                tipText.text = "Tip: Nhấn F gần NPC để tương tác.";
        }

        public static void ShowStatic(string message)
        {
            if (instance == null)
                return;
            if (instance.statusText != null)
                instance.statusText.text = message;
            instance.Show();
        }
    }
}
