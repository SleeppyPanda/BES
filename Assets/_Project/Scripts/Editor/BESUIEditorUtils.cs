#if UNITY_EDITOR
using System.IO;
using BES.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.Editor
{
    public static class BESUIEditorUtils
    {
        public static UITheme LoadOrCreateTheme()
        {
            var path = UIAssetPaths.ThemeAsset;
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(path);
            if (theme != null)
                return theme;

            EnsureFolder("Assets/_Project/Data/UI");
            theme = ScriptableObject.CreateInstance<UITheme>();
            AssetDatabase.CreateAsset(theme, path);
            AssetDatabase.SaveAssets();
            return theme;
        }

        public static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        public static Sprite LoadSpriteFlexible(string fileName)
        {
            var exact = LoadSpriteInFolder(fileName, UIAssetPaths.Backgrounds)
                ?? LoadSpriteInFolder(fileName, UIAssetPaths.Icons)
                ?? LoadSpriteInFolder(fileName, UIAssetPaths.HudArt)
                ?? LoadSpriteInFolder(fileName, UIAssetPaths.Frames)
                ?? LoadSpriteInFolder(fileName, UIAssetPaths.WeaponArt)
                ?? LoadSpriteInFolder(fileName, UIAssetPaths.Common);
            if (exact != null)
                return exact;

            var stem = Path.GetFileNameWithoutExtension(fileName);
            var guids = AssetDatabase.FindAssets(stem + " t:Sprite", new[] { UIAssetPaths.ArtRoot });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/" + fileName, System.StringComparison.OrdinalIgnoreCase))
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(fileName))
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return LoadSprite(UIAssetPaths.Backgrounds + "/" + fileName);
        }

        public static Sprite LoadSpriteInFolder(string fileName, string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return null;
            return LoadSprite(folder + "/" + fileName);
        }

        public static GameObject CreateCanvasRoot(string name, out Canvas canvas)
        {
            var go = new GameObject(name);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<UICanvasFit>();
            return go;
        }

        public static Image CreateBackground(Transform parent, Sprite sprite, string name = "Background")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null ? Image.Type.Simple : Image.Type.Sliced;
            img.preserveAspect = false;
            img.color = sprite != null ? Color.white : new Color(0.08f, 0.07f, 0.14f, 0.95f);
            return img;
        }

        public static Button CreateMenuHitArea(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.Center(rect, size);
            rect.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            return btn;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, System.Action<RectTransform> applyAnchor = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            if (applyAnchor != null)
                applyAnchor(rect);
            else
            {
                UIAnchorPresets.Center(rect, size);
                rect.anchoredPosition = anchoredPos;
            }

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.13f, 0.22f, 0.9f);
            var btn = go.AddComponent<Button>();
            CreateButtonLabel(go.transform, label);
            return btn;
        }

        static void CreateButtonLabel(Transform parent, string label)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);
            rect.offsetMin = new Vector2(8f, 4f);
            rect.offsetMax = new Vector2(-8f, -4f);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 16f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        public static TMP_Text CreateText(Transform parent, string name, string content, Vector2 anchoredPos, float fontSize = 18f, TextAlignmentOptions align = TextAlignmentOptions.Center, System.Action<RectTransform> applyAnchor = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            if (applyAnchor != null)
                applyAnchor(rect);
            else
            {
                rect.sizeDelta = new Vector2(400, 40);
                rect.anchoredPosition = anchoredPos;
            }

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            return text;
        }

        public static Slider CreateFilledSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color fillColor)
        {
            return CreateFilledSlider(parent, name, anchoredPos, size, null, null, fillColor);
        }

        public static Slider CreateFilledSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Sprite bgSprite, Sprite fillSprite, Color fallbackFill)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(bgRect);
            var bgImg = bg.AddComponent<Image>();
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }
            else
                bgImg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(1f, 1f);
            fillAreaRect.offsetMax = new Vector2(-1f, -1f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(fillRect);
            var fillImg = fill.AddComponent<Image>();
            if (fillSprite != null)
            {
                fillImg.sprite = fillSprite;
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.color = Color.white;
            }
            else
                fillImg.color = fallbackFill;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        public static Image CreateSpriteImage(Transform parent, string name, Sprite sprite, System.Action<RectTransform> applyAnchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            applyAnchor(rect);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = sprite != null ? Color.white : new Color(0.15f, 0.13f, 0.22f, 0.75f);
            return img;
        }

        public static Button CreateIconButton(Transform parent, string name, Sprite icon, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var img = go.AddComponent<RawImage>();
            if (icon != null)
            {
                img.texture = icon.texture;
                img.color = Color.white;
            }
            else
                img.color = new Color(1f, 1f, 1f, 0.15f);

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = img;
            return btn;
        }

        public static HUDSpriteManifest LoadHudManifest()
        {
            var manifest = AssetDatabase.LoadAssetAtPath<HUDSpriteManifest>(UIAssetPaths.HudManifestAsset);
            if (manifest == null)
            {
                BESUIDataSetup.EnsureHudManifest();
                manifest = AssetDatabase.LoadAssetAtPath<HUDSpriteManifest>(UIAssetPaths.HudManifestAsset);
            }

            return manifest;
        }

        public static void SavePrefab(GameObject root, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace("\\", "/"));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        public static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            var parts = folder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        public static Sprite LoadBg(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            var sprite = LoadSprite(assetPath);
            if (sprite != null)
                return sprite;

            return LoadSpriteFlexible(Path.GetFileName(assetPath));
        }

        public static void AttachScreenBackground(GameObject root, UIScreenBackgroundId screenId, bool raycastTarget = true)
        {
            var binder = root.GetComponent<UIScreenBackground>();
            if (binder == null)
                binder = root.AddComponent<UIScreenBackground>();

            SetPrivateField(binder, "screenId", screenId);
            SetPrivateField(binder, "raycastTarget", raycastTarget);
        }
    }
}
#endif
