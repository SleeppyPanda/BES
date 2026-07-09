#if UNITY_EDITOR
using BES.Core;
using BES.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BES.Editor
{
    public static class BESLoadingSceneBuilder
    {
        const string ScenesPath = "Assets/_Project/Scenes";
        const string LogoPath = "Assets/_Project/Art/UI/UI - UX/Logo game.png";
        const string LogoShadowPath = "Assets/_Project/Art/UI/UI - UX/logo black.png";

        [MenuItem("BES/Scenes/Rebuild Loading Scene")]
        public static void RebuildLoadingScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var uiRoot = CreateGroup("UI");
            var systemsRoot = CreateGroup("Systems");

            var canvas = CreateCanvas("LoadingCanvas");
            canvas.transform.SetParent(uiRoot, false);

            var panel = CreatePanel(canvas.transform);
            var loadingUI = panel.AddComponent<LoadingScreenUI>();

            CreateLogoShadow(panel.transform);
            CreateLogo(panel.transform);
            var progressBar = CreateProgressBar(panel.transform);
            var statusText = CreateStatusText(panel.transform);
            var tipText = CreateTipText(panel.transform);
            var fadeOverlay = CreateIntroFadeOverlay(panel.transform);

            SetPrivateField(loadingUI, "root", panel);
            SetPrivateField(loadingUI, "progressBar", progressBar);
            SetPrivateField(loadingUI, "statusText", statusText);
            SetPrivateField(loadingUI, "tipText", tipText);
            SetPrivateField(loadingUI, "introFadeOverlay", fadeOverlay);

            CreateEventSystem(systemsRoot);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), $"{ScenesPath}/{SceneNames.Loading}.unity");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BES] Loading scene rebuilt.");
        }

        static Transform CreateGroup(string name)
        {
            var go = new GameObject(name);
            return go.transform;
        }

        static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UIAnchorPresets.RefWidth, UIAnchorPresets.RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<UICanvasFit>();
            return canvas;
        }

        static GameObject CreatePanel(Transform parent)
        {
            var go = new GameObject("LoadingScreenUI");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.025f, 0.03f, 0.05f, 1f);
            image.raycastTarget = true;
            return go;
        }

        static RawImage CreateLogoShadow(Transform parent)
        {
            var go = new GameObject("LogoShadow");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.Center(rect, new Vector2(520f, 260f));
            rect.anchoredPosition = new Vector2(14f, 106f);

            var image = go.AddComponent<RawImage>();
            image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoShadowPath);
            image.color = new Color(0f, 0f, 0f, 0.55f);
            image.raycastTarget = false;
            return image;
        }

        static RawImage CreateLogo(Transform parent)
        {
            var go = new GameObject("Logo");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.Center(rect, new Vector2(520f, 260f));
            rect.anchoredPosition = new Vector2(0f, 120f);

            var image = go.AddComponent<RawImage>();
            image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            image.raycastTarget = false;
            return image;
        }

        static Slider CreateProgressBar(Transform parent)
        {
            var go = new GameObject("ProgressBar");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.Center(rect, new Vector2(720f, 26f));
            rect.anchoredPosition = new Vector2(0f, -160f);

            var background = go.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.18f);

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.transition = Selectable.Transition.None;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(3f, 3f);
            fillAreaRect.offsetMax = new Vector2(-3f, -3f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(fillRect);

            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.95f, 0.72f, 0.18f, 1f);
            fillImage.raycastTarget = false;

            slider.fillRect = fillRect;
            slider.targetGraphic = background;
            return slider;
        }

        static TMP_Text CreateStatusText(Transform parent)
        {
            var text = CreateText(parent, "StatusText", "Preparing world data... 0%", new Vector2(0f, -112f), 18f);
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        static TMP_Text CreateTipText(Transform parent)
        {
            var text = CreateText(parent, "TipText", "Tip: Press F near an NPC to interact.", new Vector2(0f, -220f), 14f);
            text.color = new Color(1f, 1f, 1f, 0.72f);
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        static TMP_Text CreateText(Transform parent, string name, string content, Vector2 anchoredPosition, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.Center(rect, new Vector2(920f, 36f));
            rect.anchoredPosition = anchoredPosition;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        static Image CreateIntroFadeOverlay(Transform parent)
        {
            var go = new GameObject("IntroFadeOverlay");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);
            go.transform.SetAsLastSibling();

            var image = go.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;
            return image;
        }

        static void CreateEventSystem(Transform parent)
        {
            var go = new GameObject("EventSystem");
            go.transform.SetParent(parent, false);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
#endif
