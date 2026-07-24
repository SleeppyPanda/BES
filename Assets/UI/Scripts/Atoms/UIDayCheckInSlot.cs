using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class UIDayCheckInSlot : MonoBehaviour
    {
        [SerializeField] TMP_Text dayLabel;
        [SerializeField] TMP_Text rewardLabel;
        [SerializeField] Image frame;
        [SerializeField] Button button;

        public int DayIndex { get; private set; }
        public bool IsClaimed { get; private set; }

        public void Setup(int day, int rewardGems, bool claimed, bool available, System.Action<int> onClick)
        {
            DayIndex = day;
            IsClaimed = claimed;
            if (dayLabel != null) dayLabel.text = $"Day {day}";
            if (rewardLabel != null) rewardLabel.text = $"+{rewardGems}";
            if (frame != null)
                frame.color = claimed ? new Color(0.4f, 0.4f, 0.45f, 0.9f) : available ? Color.white : new Color(0.25f, 0.25f, 0.3f, 0.85f);
            if (button != null)
            {
                button.interactable = available && !claimed;
                button.onClick.RemoveAllListeners();
                if (available && !claimed)
                    button.onClick.AddListener(() => onClick?.Invoke(day));
            }
        }
    }
}
