using UnityEngine;

namespace BES.UI
{
    [CreateAssetMenu(fileName = "UITheme", menuName = "BES/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Colors")]
        public Color backgroundDark = new(0.08f, 0.07f, 0.14f, 0.95f);
        public Color accentGold = new(0.95f, 0.78f, 0.28f, 1f);
        public Color textPrimary = Color.white;
        public Color textSecondary = new(0.75f, 0.75f, 0.85f, 1f);
        public Color rarity3Star = new(0.32f, 0.68f, 0.95f, 1f);
        public Color rarity4Star = new(0.64f, 0.49f, 0.95f, 1f);
        public Color rarity5Star = new(0.95f, 0.78f, 0.28f, 1f);
        public Color hpFill = new(0.35f, 0.85f, 0.45f, 1f);
        public Color staminaFill = new(0.55f, 0.55f, 0.6f, 1f);
        public Color manaFill = new(0.35f, 0.55f, 0.95f, 1f);

        [Header("Typography")]
        public float titleFontSize = 28f;
        public float bodyFontSize = 18f;
        public float smallFontSize = 14f;

        [Header("Layout")]
        public Vector2 referenceResolution = new(1920f, 1080f);
        public float panelPadding = 24f;

        public Color GetRarityColor(int stars)
        {
            return stars switch
            {
                >= 5 => rarity5Star,
                4 => rarity4Star,
                _ => rarity3Star
            };
        }
    }
}
