using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BES.UI.Menu
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class StorySelectionSortFilterUI : MonoBehaviour
    {
        [Header("Created buttons")]
        [SerializeField] Button sortCombatPowerButton;
        [SerializeField] Button sortConstellationButton;
        [SerializeField] Button sortQualityButton;

        [Header("Default layout")]
        [SerializeField] Sprite buttonSprite;
        [SerializeField] Sprite combatPowerButtonSprite;
        [SerializeField] Sprite constellationButtonSprite;
        [SerializeField] Sprite qualityButtonSprite;
        [SerializeField] Color buttonColor = new(0.96f, 0.91f, 0.82f, 1f);
        [SerializeField] Color labelColor = new(0.48f, 0.18f, 0.15f, 1f);
        [SerializeField] bool createTextLabels;
        [SerializeField] Vector2 buttonSize = new(205f, 58f);
        [SerializeField] float topY = 86f;
        [SerializeField] float spacingY = 82f;
        [SerializeField] float labelFontSize = 30f;

        public Button SortCombatPowerButton => sortCombatPowerButton;
        public Button SortConstellationButton => sortConstellationButton;
        public Button SortQualityButton => sortQualityButton;

        void Reset()
        {
            EnsureButtons();
        }

        void Awake()
        {
            if (Application.isPlaying)
                EnsureButtons();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying)
                EditorApplication.delayCall += EnsureButtonsIfAlive;
        }

        void EnsureButtonsIfAlive()
        {
            if (this == null) return;
            EnsureButtons();
        }
#endif

        [ContextMenu("Ensure Sort Filter Buttons")]
        public void EnsureButtons()
        {
            sortCombatPowerButton = EnsureButton(sortCombatPowerButton, "SortCombatPower", "Chiến lực", combatPowerButtonSprite, topY);
            sortConstellationButton = EnsureButton(sortConstellationButton, "SortConstellation", "Tinh Hồn", constellationButtonSprite, topY - spacingY);
            sortQualityButton = EnsureButton(sortQualityButton, "SortQuality", "Phẩm Chất", qualityButtonSprite, topY - spacingY * 2f);
        }

        Button EnsureButton(Button current, string objectName, string label, Sprite sprite, float anchoredY)
        {
            if (current == null)
                current = FindChildButton(objectName);

            if (current == null)
            {
                var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                current = go.GetComponent<Button>();

                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, anchoredY);
                rect.sizeDelta = buttonSize;

                var image = go.GetComponent<Image>();
                image.sprite = sprite != null ? sprite : buttonSprite;
                image.color = buttonColor;
                image.type = Image.Type.Simple;
                current.targetGraphic = image;

                if (createTextLabels)
                    CreateLabel(go.transform, label);

#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(go, $"Create {objectName}");
                EditorUtility.SetDirty(gameObject);
#endif
            }

            return current;
        }

        Button FindChildButton(string objectName)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button != null && button.name == objectName)
                    return button;
            }
            return null;
        }

        void CreateLabel(Transform parent, string value)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.color = labelColor;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = labelFontSize;
            text.raycastTarget = false;
        }
    }
}
