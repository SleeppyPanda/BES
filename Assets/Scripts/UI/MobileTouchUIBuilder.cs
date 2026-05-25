using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a non-overlapping mobile control layout at runtime.
/// </summary>
public class MobileTouchUIBuilder : MonoBehaviour
{
    const float Margin = 28f;
    const float ButtonSize = 80f;
    const float ButtonGap = 14f;
    const float JoystickSize = 200f;

    [SerializeField] bool buildOnStart = true;

    void Start()
    {
        if (!buildOnStart)
            return;

        if (PlayerInputHub.Instance == null || !PlayerInputHub.Instance.UseTouchUI)
            return;

        BuildIfNeeded();
    }

    public void BuildIfNeeded()
    {
        if (transform.Find("MobileTouchCanvas") != null)
            return;

        UIEventSystemUtility.EnsureEventSystem();
        Canvas canvas = CreateCanvas(transform);
        RectTransform root = canvas.GetComponent<RectTransform>();

        BuildJoystick(root);
        BuildActionButtons(root);
    }

    static Canvas CreateCanvas(Transform parent)
    {
        GameObject go = new GameObject("MobileTouchCanvas");
        go.transform.SetParent(parent, false);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    void BuildJoystick(RectTransform root)
    {
        RectTransform bg = CreatePanel(root, "Joystick", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        bg.anchoredPosition = new Vector2(Margin, Margin);
        bg.sizeDelta = new Vector2(JoystickSize, JoystickSize);

        Image bgImage = bg.gameObject.AddComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.12f);
        bgImage.raycastTarget = true;

        RectTransform handle = CreatePanel(bg, "Handle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        handle.sizeDelta = new Vector2(72f, 72f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.45f);
        handleImage.raycastTarget = false;

        VirtualJoystick joystick = bg.gameObject.AddComponent<VirtualJoystick>();
        joystick.background = bg;
        joystick.handle = handle;
        joystick.handleRange = JoystickSize * 0.35f;
    }

    void BuildActionButtons(RectTransform root)
    {
        // Layout from bottom-right; each cell is ButtonSize + ButtonGap apart (no overlap).
        float step = ButtonSize + ButtonGap;
        float baseX = -Margin;
        float baseY = Margin;

        CreateActionButton(root, "M1", PlayerAction.AttackLight, baseX - step * 0, baseY + step * 1, "M1");
        CreateActionButton(root, "M2", PlayerAction.AttackHeavy, baseX - step * 1, baseY + step * 1, "M2");
        CreateActionButton(root, "Enhance", PlayerAction.SkillQ, baseX - step * 2, baseY + step * 1, "C", toggleEnhance: true, fireOnPress: false);

        CreateActionButton(root, "Jump", PlayerAction.Jump, baseX - step * 0, baseY + step * 0, "Jump");
        CreateActionButton(root, "Dash", PlayerAction.Dash, baseX - step * 1, baseY + step * 0, "Dash");

        CreateActionButton(root, "Q", PlayerAction.SkillQ, baseX - step * 0, baseY + step * 2, "Q");
        CreateActionButton(root, "E", PlayerAction.SkillE, baseX - step * 1, baseY + step * 2, "E");
        CreateActionButton(root, "R", PlayerAction.SkillR, baseX - step * 2, baseY + step * 2, "R");
        CreateActionButton(root, "T", PlayerAction.SkillT, baseX - step * 3, baseY + step * 2, "T");
    }

    void CreateActionButton(
        RectTransform parent,
        string controlId,
        PlayerAction action,
        float offsetX,
        float offsetY,
        string label,
        bool holdEnhance = false,
        bool toggleEnhance = false,
        bool fireOnPress = true)
    {
        RectTransform rect = CreatePanel(parent, controlId, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        rect.anchoredPosition = new Vector2(offsetX, offsetY);
        rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.45f, 0.85f, 0.45f);
        image.raycastTarget = true;

        ExclusiveTouchButton button = rect.gameObject.AddComponent<ExclusiveTouchButton>();
        button.controlId = controlId;
        button.actionOnPress = action;
        button.fireActionOnPress = fireOnPress;
        button.holdForEnhance = holdEnhance;
        button.toggleEnhanceOnPress = toggleEnhance;
        button.normalColor = image.color;
        button.pressedColor = new Color(0.35f, 0.6f, 1f, 0.75f);

        CreateLabel(rect, label);
    }

    static void CreateLabel(RectTransform parent, string text)
    {
        RectTransform labelRect = CreatePanel(parent, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        labelRect.sizeDelta = new Vector2(parent.sizeDelta.x - 8f, 28f);

        Text label = labelRect.gameObject.AddComponent<Text>();
        label.text = text;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.fontSize = 22;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.raycastTarget = false;
    }

    static RectTransform CreatePanel(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        return rect;
    }
}
