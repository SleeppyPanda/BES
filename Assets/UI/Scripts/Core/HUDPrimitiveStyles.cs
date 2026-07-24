using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    /// <summary>
    /// Màu và panel runtime cho gameplay HUD — không dùng mockup slice full-screen.
    /// </summary>
    public static class HUDPrimitiveStyles
    {
        public static readonly Color MiniMapBackground = new(0.04f, 0.05f, 0.08f, 0.82f);
        public static readonly Color MiniMapRing = new(1f, 1f, 1f, 0.55f);
        public static readonly Color QuestPanelBackground = new(0.04f, 0.05f, 0.08f, 0.72f);
        public static readonly Color SlotBackground = new(0.08f, 0.09f, 0.14f, 0.78f);
        public static readonly Color PartyPillBackground = new(0.06f, 0.07f, 0.11f, 0.82f);
        public static readonly Color NavIconFallback = new(1f, 1f, 1f, 0.18f);
        public static readonly Color SkillKeyLabel = new(0.95f, 0.93f, 0.88f, 0.95f);
        public static readonly Color HpBarBackground = new(0.12f, 0.12f, 0.16f, 0.92f);
        public static readonly Color HpBarFill = new(0.45f, 0.88f, 0.38f, 1f);
        public static readonly Color StaminaBarFill = new(0.92f, 0.9f, 0.82f, 0.95f);
        public static readonly Color ManaBarFill = new(0.38f, 0.58f, 0.95f, 0.9f);

        public static void ApplySolidPanel(Image image, Color color)
        {
            if (image == null)
                return;

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = color;
        }

        /// <summary>
        /// Chỉ gắn sprite frame nhỏ từ Frames/ (Rectangle*), không dùng Group mockup HUD.
        /// </summary>
        public static bool TryApplySmallFrame(Image image, Sprite sprite)
        {
            if (image == null || sprite == null || !IsWhitelistedFrameSprite(sprite))
            {
                ApplySolidPanel(image, SlotBackground);
                return false;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;
            return true;
        }

        public static bool IsWhitelistedFrameSprite(Sprite sprite)
        {
            if (sprite == null)
                return false;

            var name = sprite.name;
            return name.StartsWith("Rectangle ") || name.StartsWith("Ellipse ");
        }

        static Sprite _minimapRingSprite;
        static Sprite _minimapFaceSprite;

        public static Sprite GetMinimapRingSprite()
        {
            _minimapRingSprite ??= CreateRingSprite(128, 4f, new Color(1f, 1f, 1f, 0.72f));
            return _minimapRingSprite;
        }

        public static Sprite GetMinimapFaceSprite()
        {
            _minimapFaceSprite ??= CreateFilledCircleSprite(112, MiniMapBackground);
            return _minimapFaceSprite;
        }

        static Sprite CreateRingSprite(int size, float thickness, Color ringColor)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = size * 0.5f;
            var outer = size * 0.5f - 1f;
            var inner = outer - thickness;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= outer && dist >= inner ? ringColor : Color.clear);
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite CreateFilledCircleSprite(int size, Color fill)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = size * 0.5f;
            var radius = size * 0.5f - 1f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= radius ? fill : Color.clear);
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static bool IsWhitelistedMinimapRingSprite(Sprite sprite)
        {
            if (sprite == null)
                return false;

            var name = sprite.name;
            return name.StartsWith("Union");
        }

        public static bool IsWhitelistedIconSprite(Sprite sprite)
        {
            if (sprite == null || IsRejectedHudSprite(sprite))
                return false;

            var name = sprite.name;
            if (name is "User" or "Required" or "Click to begin")
                return false;

            return name.StartsWith("Object") ||
                   name.StartsWith("Star") ||
                   name.StartsWith("Vector") ||
                   name.StartsWith("Polygon") ||
                   name.StartsWith("Subtract") ||
                   name.StartsWith("Mask") ||
                   name.StartsWith("image ");
        }

        public static bool IsRejectedHudSprite(Sprite sprite)
        {
            if (sprite == null)
                return true;

            var name = sprite.name;
            return name.StartsWith("Group 427") ||
                   name.Contains("Main play") ||
                   name is "Mission";
        }
    }
}
