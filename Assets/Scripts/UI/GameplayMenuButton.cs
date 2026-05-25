using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optional in-game button to return to main menu with fade transition.
/// </summary>
public class GameplayMenuButton : MonoBehaviour
{
    void Start()
    {
        BuildButton();
    }

    void BuildButton()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null && canvas.name == "GameplayMenuCanvas")
            return;

        GameObject canvasGo = new GameObject("GameplayMenuCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject buttonGo = new GameObject("MenuButton");
        RectTransform rect = buttonGo.AddComponent<RectTransform>();
        rect.SetParent(canvasGo.transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(140f, 48f);

        Image image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 0.75f);

        Button button = buttonGo.AddComponent<Button>();
        button.onClick.AddListener(OnMenuClicked);

        GameObject labelGo = new GameObject("Label");
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.SetParent(buttonGo.transform, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelGo.AddComponent<Text>();
        label.text = "Menu";
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 20;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void OnMenuClicked()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(BESGameScenes.MainMenu);
    }
}
