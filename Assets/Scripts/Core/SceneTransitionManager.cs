using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Basic fade-out / load / fade-in scene transitions. Persists across scenes.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade")]
    public float fadeDuration = 0.45f;
    public Color fadeColor = Color.black;

    Canvas fadeCanvas;
    Image fadeImage;
    bool isTransitioning;

    public bool IsTransitioning => isTransitioning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureFadeOverlay();
        SetFadeAlpha(0f);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadScene(string sceneName)
    {
        if (isTransitioning || string.IsNullOrEmpty(sceneName))
            return;

        StartCoroutine(TransitionRoutine(sceneName));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;

        if (PlayerInputHub.Instance != null)
            PlayerInputHub.Instance.ClearBufferedActions();

        yield return FadeTo(1f);

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
        loadOp.allowSceneActivation = true;
        while (!loadOp.isDone)
            yield return null;

        yield return null;
        yield return FadeTo(0f);

        isTransitioning = false;
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        EnsureFadeOverlay();
        float start = fadeImage.color.a;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(start, targetAlpha, t);
            SetFadeAlpha(alpha);
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    void EnsureFadeOverlay()
    {
        if (fadeCanvas != null)
            return;

        GameObject canvasGo = new GameObject("SceneFadeCanvas");
        canvasGo.transform.SetParent(transform, false);

        fadeCanvas = canvasGo.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;
        canvasGo.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);

        RectTransform rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = imageGo.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = isTransitioning;
    }

    void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color c = fadeColor;
        c.a = Mathf.Clamp01(alpha);
        fadeImage.color = c;
        fadeImage.raycastTarget = alpha > 0.01f;
    }
}
