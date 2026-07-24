using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BES.UI
{
    public class GachaCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] RectTransform cardRoot;
        [SerializeField] RawImage artworkImage;
        [SerializeField] Image rarityGlow;
        [SerializeField] Image frameImage;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text rarityText;
        [SerializeField] TMP_Text detailText;
        [SerializeField] GameObject hiddenInfoRoot;
        [SerializeField] float hoverScale = 1.2f;
        [SerializeField] Color threeStarColor = new Color(0.45f, 0.65f, 1f, 0.95f);
        [SerializeField] Color fourStarColor = new Color(0.75f, 0.45f, 1f, 0.95f);
        [SerializeField] Color fiveStarColor = new Color(1f, 0.75f, 0.18f, 0.95f);

        Vector3 defaultScale = Vector3.one;

        void Awake()
        {
            if (cardRoot == null)
                cardRoot = transform as RectTransform;
            defaultScale = cardRoot != null ? cardRoot.localScale : Vector3.one;
            if (hiddenInfoRoot != null)
                hiddenInfoRoot.SetActive(false);
        }

        public void Setup(GachaDropEntry entry, string label)
        {
            var rarity = entry != null ? entry.rarity : 3;
            var color = GetRarityColor(rarity);

            if (rarityGlow != null)
                rarityGlow.color = new Color(color.r, color.g, color.b, rarity >= 5 ? 0.85f : 0.58f);
            if (frameImage != null)
                frameImage.color = color;
            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(label) ? "Reward" : label;
            if (rarityText != null)
                rarityText.text = $"{rarity} Star";
            if (detailText != null)
                detailText.text = entry != null ? $"{entry.rewardType} | {entry.rewardId}" : "Reward detail";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (cardRoot != null)
                cardRoot.localScale = defaultScale * hoverScale;
            if (hiddenInfoRoot != null)
                hiddenInfoRoot.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (cardRoot != null)
                cardRoot.localScale = defaultScale;
            if (hiddenInfoRoot != null)
                hiddenInfoRoot.SetActive(false);
        }

        Color GetRarityColor(int rarity)
        {
            if (rarity >= 5)
                return fiveStarColor;
            if (rarity == 4)
                return fourStarColor;
            return threeStarColor;
        }
    }
}
