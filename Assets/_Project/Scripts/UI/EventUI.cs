using BES.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class EventUI : UIScreenBase
    {
        [SerializeField] EventDefinition eventData;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descText;
        [SerializeField] Button checkInButton;
        [SerializeField] Button closeButton;
        [SerializeField] Transform daySlotsContainer;
        [SerializeField] GameObject daySlotPrefab;

        void Awake()
        {
            eventData ??= Resources.Load<EventDefinition>("Data/DefaultEvent");
            if (root == null)
                root = gameObject;
            Hide();
            if (checkInButton != null) checkInButton.onClick.AddListener(OnCheckIn);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public override void Refresh()
        {
            if (titleText != null)
                titleText.text = eventData != null ? eventData.displayName : "Event";
            if (descText != null)
                descText.text = eventData != null ? eventData.description : "Daily check-in available.";
            RefreshDaySlots();
        }

        void RefreshDaySlots()
        {
            if (daySlotsContainer == null || daySlotPrefab == null || eventData == null)
                return;

            for (var i = daySlotsContainer.childCount - 1; i >= 0; i--)
                Destroy(daySlotsContainer.GetChild(i).gameObject);

            var streak = eventData.GetStreakDay();
            var days = eventData.totalDays > 0 ? eventData.totalDays : 7;
            for (var d = 1; d <= days; d++)
            {
                var go = Instantiate(daySlotPrefab, daySlotsContainer);
                var slot = go.GetComponent<UIDayCheckInSlot>();
                var claimed = eventData.IsDayClaimed(d);
                var available = d == streak + 1 && !claimed;
                slot?.Setup(d, eventData.GetRewardForDay(d), claimed, available, OnDayClaim);
            }
        }

        void OnDayClaim(int day)
        {
            if (eventData == null || eventData.IsDayClaimed(day))
                return;
            eventData.MarkDayClaimed(day);
            if (PlayerWallet.Instance != null)
                PlayerWallet.Instance.AddGems(eventData.GetRewardForDay(day));
            GameManager.Instance?.SaveGame();
            Refresh();
        }

        void OnCheckIn()
        {
            if (eventData == null)
                return;
            var nextDay = eventData.GetStreakDay() + 1;
            if (nextDay <= (eventData.totalDays > 0 ? eventData.totalDays : 7))
                OnDayClaim(nextDay);
        }
    }
}
