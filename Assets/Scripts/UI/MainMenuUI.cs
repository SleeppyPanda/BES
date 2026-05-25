using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds main menu UI at runtime and loads gameplay scene with fade transition.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    public string gameplaySceneName = BESGameScenes.Gameplay;

    void Start()
    {
        EnsureTransitionManager();
        UIEventSystemUtility.EnsureEventSystem();
        BuildMenu();
    }

    static void EnsureTransitionManager()
    {
        if (SceneTransitionManager.Instance != null)
            return;

        GameObject go = new GameObject("SceneTransitionManager");
        go.AddComponent<SceneTransitionManager>();
    }

    void BuildMenu()
    {
        GameObject canvasGo = new GameObject("MainMenuCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        CreateTitle(canvasGo.transform);
        CreatePlayButton(canvasGo.transform);
    }

    void CreateTitle(Transform parent)
    {
        GameObject titleGo = new GameObject("Title");
        RectTransform rect = titleGo.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.72f);
        rect.anchorMax = new Vector2(0.5f, 0.72f);
        rect.sizeDelta = new Vector2(900f, 80f);

        Text text = titleGo.AddComponent<Text>();
        text.text = "Beneath Enchanted Sky";
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 48;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void CreatePlayButton(Transform parent)
    {
        GameObject buttonGo = new GameObject("PlayButton");
        RectTransform rect = buttonGo.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.42f);
        rect.anchorMax = new Vector2(0.5f, 0.42f);
        rect.sizeDelta = new Vector2(320f, 72f);

        Image image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.2f, 0.5f, 0.9f, 0.9f);

        Button button = buttonGo.AddComponent<Button>();
        button.onClick.AddListener(OnPlayClicked);

        GameObject labelGo = new GameObject("Label");
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.SetParent(buttonGo.transform, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelGo.AddComponent<Text>();
        label.text = "Chơi";
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 32;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void OnPlayClicked()
    {
        if (SceneTransitionManager.Instance == null)
            SceneManager.LoadScene(gameplaySceneName);
        else
            SceneTransitionManager.Instance.LoadScene(gameplaySceneName);
    }
}
