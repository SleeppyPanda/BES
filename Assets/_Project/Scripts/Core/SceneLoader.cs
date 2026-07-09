using System.Collections;
using BES.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BES.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [SerializeField] float fadeDuration = 0.5f;
        [SerializeField] float minimumLoadingDuration = 0.75f;

        bool isLoading;
        LoadingScreenUI activeLoadingView;

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

            activeLoadingView = Object.FindAnyObjectByType<LoadingScreenUI>(FindObjectsInactive.Include);
            if (activeLoadingView != null)
            {
                activeLoadingView.Show();
                activeLoadingView.SetStatus("Preparing world data...");
                activeLoadingView.SetProgress(0f);
            }

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

        public void LoadMainMenu() => LoadScene(SceneNames.MainMenu);
        public void LoadGameplay() => LoadScene(SceneNames.Gameplay);
        public void LoadPrototype() => LoadScene(SceneNames.Prototype);
    }
}
