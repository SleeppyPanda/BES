using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public class WishResultCardHover : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] MenuWishController controller;
        [SerializeField] int cardIndex;
        [SerializeField] Image cardBackground;
        [SerializeField] Sprite normalSprite;
        [SerializeField] Sprite hoverSprite;

        public void Configure(
            MenuWishController owner,
            int index,
            Image background,
            Sprite normal,
            Sprite hover)
        {
            controller = owner;
            cardIndex = index;
            cardBackground = background;
            normalSprite = normal;
            hoverSprite = hover;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (cardBackground != null && hoverSprite != null)
                cardBackground.sprite = hoverSprite;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (cardBackground != null && normalSprite != null)
                cardBackground.sprite = normalSprite;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
        }

        public void ResetVisual()
        {
            if (cardBackground != null && normalSprite != null)
                cardBackground.sprite = normalSprite;
        }
    }
}
