using BES.Core;
using System.Collections;
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
        [SerializeField] Text legacyStatusText;
        [SerializeField] Text legacyTipText;
        [SerializeField] Image introFadeOverlay;
        [SerializeField] float introFadeDuration = 0.5f;

        static LoadingScreenUI instance;
        string currentStatus = "Preparing...";
        float currentProgress;
        Coroutine introFadeRoutine;

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
            PlayIntroFade();
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
            SetProgress(Mathf.MoveTowards(progressBar.value, 0.95f, Time.deltaTime * 0.35f));
        }

        public override void Refresh()
        {
            if (tipText != null)
                tipText.text = "Tip: Press F near an NPC to interact.";
            if (legacyTipText != null)
                legacyTipText.text = "Tip: Press F near an NPC to interact.";
        }

        public void SetProgress(float value)
        {
            currentProgress = Mathf.Clamp01(value);
            if (progressBar != null)
                progressBar.value = currentProgress;
            RefreshStatusText();
        }

        public void SetStatus(string message)
        {
            currentStatus = string.IsNullOrEmpty(message) ? "Loading..." : message;
            RefreshStatusText();
        }

        public void PlayIntroFade()
        {
            if (introFadeOverlay == null)
                return;

            if (introFadeRoutine != null)
                StopCoroutine(introFadeRoutine);
            introFadeRoutine = StartCoroutine(IntroFadeRoutine());
        }

        IEnumerator IntroFadeRoutine()
        {
            var color = introFadeOverlay.color;
            color.a = 1f;
            introFadeOverlay.color = color;
            introFadeOverlay.raycastTarget = true;

            var elapsed = 0f;
            while (elapsed < introFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, introFadeDuration)));
                introFadeOverlay.color = color;
                yield return null;
            }

            color.a = 0f;
            introFadeOverlay.color = color;
            introFadeOverlay.raycastTarget = false;
            introFadeRoutine = null;
        }

        void RefreshStatusText()
        {
            var text = $"{currentStatus} {Mathf.RoundToInt(currentProgress * 100f)}%";
            if (statusText != null)
                statusText.text = text;
            if (legacyStatusText != null)
                legacyStatusText.text = text;
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
