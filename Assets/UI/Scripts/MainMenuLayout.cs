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
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(UIAnchorPresets.RefWidth, UIAnchorPresets.RefHeight);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }

            ApplyMainMenuBackground(canvasRoot);
        }

        static void ApplyMainMenuBackground(Transform canvasRoot)
        {
            var manifest = UIScreenBackgroundManifestLoader.Load();
            var sprite = manifest != null ? manifest.GetSprite(UIScreenBackgroundId.MainMenu) : null;

            var bg = canvasRoot.Find("Background");
            if (bg == null)
                return;

            var img = bg.GetComponent<Image>();
            if (img == null)
                return;
            if (sprite != null)
                img.sprite = sprite;
            img.preserveAspect = false;
            img.color = Color.white;
            img.raycastTarget = false;
        }

    }
}
