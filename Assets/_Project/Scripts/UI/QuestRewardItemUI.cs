using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class QuestRewardItemUI : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] Image star;
        [SerializeField] RawImage itemImage;
        [SerializeField] TMP_Text itemNameText;
        [SerializeField] Color commonColor = new Color(0.45f, 0.65f, 1f, 0.95f);
        [SerializeField] Color rareColor = new Color(0.75f, 0.45f, 1f, 0.95f);
        [SerializeField] Color epicColor = new Color(1f, 0.75f, 0.18f, 0.95f);

        public void Setup(string itemName, int rarity, Texture texture = null)
        {
            var color = GetRarityColor(rarity);
            if (background != null)
                background.color = new Color(color.r, color.g, color.b, 0.35f);
            if (star != null)
                star.color = color;
            if (itemImage != null)
            {
                itemImage.texture = texture;
                itemImage.color = texture != null ? Color.white : new Color(1f, 1f, 1f, 0.12f);
            }
            if (itemNameText != null)
                itemNameText.text = itemName;
        }

        Color GetRarityColor(int rarity)
        {
            if (rarity >= 5)
                return epicColor;
            if (rarity >= 4)
                return rareColor;
            return commonColor;
        }
    }
}
