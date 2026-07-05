using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public static class MainMenuLayout
    {
        public static void Apply(Transform canvasRoot)
        {
            if (canvasRoot == null)
                return;

            var canvas = canvasRoot.GetComponent<Canvas>();
            if (canvas != null)
            {
                var scaler = canvasRoot.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.referenceResolution = new Vector2(UIAnchorPresets.RefWidth, UIAnchorPresets.RefHeight);
                    scaler.matchWidthOrHeight = 0f;
                }
            }

            ApplyMainMenuBackground(canvasRoot);

            ApplyHit(canvasRoot, "ClickToBegin", UIAnchorPresets.ApplyMainMenuClickHit);
            ApplyHit(canvasRoot, "NewGameButton", UIAnchorPresets.ApplyMainMenuServerHit);
            ApplyHit(canvasRoot, "EventButton", UIAnchorPresets.ApplyMainMenuEventHit);
            ApplyHit(canvasRoot, "ContinueButton", UIAnchorPresets.ApplyMainMenuContinueHit);
            ApplyHit(canvasRoot, "QuitButton", UIAnchorPresets.ApplyMainMenuQuitHit);
            ApplyHit(canvasRoot, "ProfileButton", UIAnchorPresets.ApplyMainMenuProfileHit);
            ApplyHit(canvasRoot, "SettingsButton", UIAnchorPresets.ApplyMainMenuSettingsHit);
        }

        static void ApplyMainMenuBackground(Transform canvasRoot)
        {
            var manifest = UIScreenBackgroundManifestLoader.Load();
            var sprite = manifest != null ? manifest.GetSprite(UIScreenBackgroundId.MainMenu) : null;

            var bg = canvasRoot.Find("Background");
            if (bg == null)
            {
                var go = new GameObject("Background");
                go.transform.SetParent(canvasRoot, false);
                go.transform.SetAsFirstSibling();
                var rect = go.AddComponent<RectTransform>();
                UIAnchorPresets.StretchFull(rect);
                bg = go.transform;
            }

            var img = bg.GetComponent<Image>() ?? bg.gameObject.AddComponent<Image>();
            if (sprite != null)
                img.sprite = sprite;
            img.preserveAspect = false;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        static void ApplyHit(Transform root, string name, System.Action<RectTransform> anchor)
        {
            var t = root.Find(name);
            if (t == null)
                return;
            var rect = t.GetComponent<RectTransform>();
            if (rect != null)
                anchor(rect);
        }
    }
}
