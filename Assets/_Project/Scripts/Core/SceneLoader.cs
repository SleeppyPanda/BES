using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BES.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [SerializeField] float fadeDuration = 0.5f;
        [SerializeField] float minimumLoadingDuration = 0.75f;

        bool isLoading;
        LoadingView activeLoadingView;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(string sceneName)
        {
            if (isLoading || string.IsNullOrEmpty(sceneName))
                return;

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        IEnumerator LoadSceneRoutine(string sceneName)
        {
            isLoading = true;
            GameEvents.RaiseSceneLoadStarted(sceneName);

            if (fadeDuration > 0f)
                yield return new WaitForSeconds(fadeDuration);

            if (sceneName == SceneNames.Loading)
            {
                yield return LoadSceneDirect(sceneName);
            }
            else
            {
                yield return LoadViaLoadingScene(sceneName);
            }

            GameEvents.RaiseSceneLoadCompleted(sceneName);
            isLoading = false;
        }

        IEnumerator LoadSceneDirect(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
                yield return null;
        }

        IEnumerator LoadViaLoadingScene(string targetSceneName)
        {
            var loadingOp = SceneManager.LoadSceneAsync(SceneNames.Loading);
            while (loadingOp != null && !loadingOp.isDone)
                yield return null;

            activeLoadingView = LoadingView.Create();
            activeLoadingView.SetStatus("Preparing world data...");
            activeLoadingView.SetProgress(0f);

            yield return null;

            var targetOp = SceneManager.LoadSceneAsync(targetSceneName);
            if (targetOp == null)
                yield break;

            targetOp.allowSceneActivation = false;
            var elapsed = 0f;

            while (targetOp.progress < 0.9f)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalizedProgress = Mathf.Clamp01(targetOp.progress / 0.9f);
                activeLoadingView.SetStatus(GetLoadingStatus(normalizedProgress, targetSceneName));
                activeLoadingView.SetProgress(normalizedProgress);
                yield return null;
            }

            activeLoadingView.SetStatus("Finalizing scene...");
            activeLoadingView.SetProgress(1f);

            while (elapsed < minimumLoadingDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            targetOp.allowSceneActivation = true;
            while (!targetOp.isDone)
                yield return null;
        }

        static string GetLoadingStatus(float progress, string sceneName)
        {
            if (progress < 0.25f)
                return $"Loading {sceneName} assets...";
            if (progress < 0.55f)
                return "Building gameplay systems...";
            if (progress < 0.85f)
                return "Spawning world content...";
            return "Almost ready...";
        }

        public void LoadMainMenu() => LoadScene(SceneNames.MainMenu);
        public void LoadGameplay() => LoadScene(SceneNames.Gameplay);
        public void LoadPrototype() => LoadScene(SceneNames.Prototype);

        sealed class LoadingView
        {
            readonly Image progressFill;
            readonly TMP_Text statusText;
            readonly RectTransform shine;

            LoadingView(Image progressFill, TMP_Text statusText, RectTransform shine)
            {
                this.progressFill = progressFill;
                this.statusText = statusText;
                this.shine = shine;
            }

            public static LoadingView Create()
            {
                EnsureEventSystem();

                var canvasGo = new GameObject("LoadingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;

                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0f;

                var root = CreateRect(canvasGo.transform, "Root");
                Stretch(root);
                var background = root.gameObject.AddComponent<Image>();
                background.color = new Color(0.03f, 0.035f, 0.055f, 1f);

                var logoPanel = CreateRect(root, "LogoImage");
                Center(logoPanel, new Vector2(360f, 180f), new Vector2(0f, 80f));
                var logoImage = logoPanel.gameObject.AddComponent<Image>();
                logoImage.color = new Color(0.12f, 0.16f, 0.24f, 0.92f);

                var logoText = CreateText(logoPanel, "LogoText", "BES", 54f, TextAlignmentOptions.Center);
                Stretch(logoText.rectTransform);
                logoText.color = new Color(0.95f, 0.82f, 0.42f, 1f);

                var status = CreateText(root, "LoadingStatus", "Preparing...", 18f, TextAlignmentOptions.Center);
                Center(status.rectTransform, new Vector2(760f, 42f), new Vector2(0f, -84f));
                status.color = new Color(0.92f, 0.94f, 1f, 0.92f);

                var barFrame = CreateRect(root, "ProgressFrame");
                Center(barFrame, new Vector2(680f, 24f), new Vector2(0f, -140f));
                var frameImage = barFrame.gameObject.AddComponent<Image>();
                frameImage.color = new Color(1f, 1f, 1f, 0.18f);

                var fillRect = CreateRect(barFrame, "ProgressFill");
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                var fill = fillRect.gameObject.AddComponent<Image>();
                fill.color = new Color(0.95f, 0.72f, 0.18f, 1f);

                var shineRect = CreateRect(barFrame, "ProgressShine");
                shineRect.anchorMin = new Vector2(0f, 0f);
                shineRect.anchorMax = new Vector2(0f, 1f);
                shineRect.pivot = new Vector2(0.5f, 0.5f);
                shineRect.sizeDelta = new Vector2(80f, 0f);
                var shineImage = shineRect.gameObject.AddComponent<Image>();
                shineImage.color = new Color(1f, 1f, 1f, 0.32f);

                return new LoadingView(fill, status, shineRect);
            }

            public void SetProgress(float value)
            {
                value = Mathf.Clamp01(value);
                if (progressFill != null)
                    progressFill.rectTransform.anchorMax = new Vector2(value, 1f);
                if (shine != null)
                {
                    shine.anchorMin = new Vector2(value, 0f);
                    shine.anchorMax = new Vector2(value, 1f);
                    shine.anchoredPosition = Vector2.zero;
                }
            }

            public void SetStatus(string status)
            {
                if (statusText != null)
                    statusText.text = status;
            }

            static void EnsureEventSystem()
            {
                if (Object.FindAnyObjectByType<EventSystem>() != null)
                    return;

                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Object.DontDestroyOnLoad(eventSystem);
            }

            static RectTransform CreateRect(Transform parent, string name)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return go.GetComponent<RectTransform>();
            }

            static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var label = go.AddComponent<TextMeshProUGUI>();
                label.text = text;
                label.fontSize = fontSize;
                label.alignment = alignment;
                label.raycastTarget = false;
                return label;
            }

            static void Stretch(RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            static void Center(RectTransform rect, Vector2 size, Vector2 position)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = position;
            }
        }
    }
}
