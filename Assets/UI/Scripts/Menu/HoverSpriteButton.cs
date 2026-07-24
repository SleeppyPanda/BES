using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace BES.UI.Menu
{
    public class HoverSpriteButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] Image targetImage;
        [SerializeField] Sprite normalSprite;
        [SerializeField] Sprite hoverSprite;
        [Header("Text state")]
        [SerializeField] TMP_Text targetText;
        [SerializeField] Color normalTextColor = new Color(0.35f, 0.16f, 0.13f, 1f);
        [SerializeField] Color hoverTextColor = Color.white;
        [SerializeField] bool selectedUsesHover = true;
        bool pointerInside;
        bool selected;

        void Awake()
        {
            targetImage ??= GetComponent<Image>();
            targetText ??= GetComponentInChildren<TMP_Text>(true);
            if (normalSprite == null && targetImage != null) normalSprite = targetImage.sprite;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData) { pointerInside = true; Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { pointerInside = false; Refresh(); }
        public void OnSelect(BaseEventData eventData) { selected = true; Refresh(); }
        public void OnDeselect(BaseEventData eventData) { selected = false; Refresh(); }
        void OnDisable() { pointerInside = false; selected = false; Apply(normalSprite, normalTextColor); }

        public void Refresh()
        {
            var useHover = pointerInside || (selectedUsesHover && selected);
            Apply(useHover && hoverSprite != null ? hoverSprite : normalSprite,
                useHover ? hoverTextColor : normalTextColor);
        }

        void Apply(Sprite sprite, Color textColor)
        {
            if (targetImage != null && sprite != null) targetImage.sprite = sprite;
            if (targetText != null) targetText.color = textColor;
        }
    }
}
