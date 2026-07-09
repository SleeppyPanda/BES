using System.Collections;
using BES.UI;
using UnityEngine;
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
        LoadingScreenUI activeLoadingView;
        CanvasGroup fadeGroup;

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
                yield return FadeScreen(1f, fadeDuration);

            if (sceneName == SceneNames.Loading)
            {
                yield return LoadSceneDirect(sceneName);
                if (fadeDuration > 0f)
                    yield return FadeScreen(0f, fadeDuration);
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

            activeLoadingView = Object.FindAnyObjectByType<LoadingScreenUI>(FindObjectsInactive.Include);
            if (activeLoadingView != null)
            {
                activeLoadingView.Show();
                activeLoadingView.PlayIntroFade();
                activeLoadingView.SetStatus("Preparing world data...");
                activeLoadingView.SetProgress(0f);
            }

            if (fadeDuration > 0f)
                yield return FadeScreen(0f, fadeDuration);

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
                if (activeLoadingView != null)
                {
                    activeLoadingView.SetStatus(GetLoadingStatus(normalizedProgress, targetSceneName));
                    activeLoadingView.SetProgress(normalizedProgress);
                }
                yield return null;
            }

            if (activeLoadingView != null)
            {
                activeLoadingView.SetStatus("Finalizing scene...");
                activeLoadingView.SetProgress(1f);
            }

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

        IEnumerator FadeScreen(float targetAlpha, float duration)
        {
            EnsureFadeOverlay();
            if (fadeGroup == null)
                yield break;

            fadeGroup.blocksRaycasts = targetAlpha > 0f;
            fadeGroup.interactable = false;

            var startAlpha = fadeGroup.alpha;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
                yield return null;
            }

            fadeGroup.alpha = targetAlpha;
            fadeGroup.blocksRaycasts = targetAlpha > 0f;
        }

        void EnsureFadeOverlay()
        {
            if (fadeGroup != null)
                return;

            var canvasGo = new GameObject("SceneFadeCanvas");
            DontDestroyOnLoad(canvasGo);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UIAnchorPresets.RefWidth, UIAnchorPresets.RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            scaler.matchWidthOrHeight = 0.5f;

            fadeGroup = canvasGo.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;

            var imageGo = new GameObject("FadeOverlay");
            imageGo.transform.SetParent(canvasGo.transform, false);
            var rect = imageGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = imageGo.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
        }

        public void LoadMainMenu() => LoadScene(SceneNames.MainMenu);
        public void LoadGameplay() => LoadScene(SceneNames.Gameplay);
        public void LoadPrototype() => LoadScene(SceneNames.Prototype);
    }
}
