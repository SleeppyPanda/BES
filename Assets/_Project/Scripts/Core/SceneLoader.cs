using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BES.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [SerializeField] float fadeDuration = 0.5f;

        bool isLoading;

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

            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
                yield return null;

            GameEvents.RaiseSceneLoadCompleted(sceneName);
            isLoading = false;
        }

        public void LoadMainMenu() => LoadScene(SceneNames.MainMenu);
        public void LoadGameplay() => LoadScene(SceneNames.Gameplay);
        public void LoadPrototype() => LoadScene(SceneNames.Prototype);
    }
}
