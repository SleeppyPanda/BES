using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class UITeamSlot : MonoBehaviour
    {
        [SerializeField] Image portrait;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Button button;

        public int SlotIndex { get; private set; }

        public void Setup(int index, string displayName, Sprite portraitSprite, System.Action<int> onClick)
        {
            SlotIndex = index;
            if (nameLabel != null)
                nameLabel.text = string.IsNullOrEmpty(displayName) ? "Empty" : displayName;
            if (portrait != null)
            {
                portrait.sprite = portraitSprite;
                portrait.color = portraitSprite != null ? Color.white : new Color(0.2f, 0.2f, 0.28f, 0.9f);
            }
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(index));
            }
        }
    }
}
