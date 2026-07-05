using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    [CreateAssetMenu(fileName = "EventDefinition", menuName = "BES/Event Definition")]
    public class EventDefinition : ScriptableObject
    {
        public string eventId;
        public string displayName;
        [TextArea] public string description;
        public bool checkInAvailable = true;
        public int checkInRewardGems = 60;
        public int totalDays = 7;
        public int gemsPerDay = 60;

        public int GetStreakDay() =>
            MetaProgressState.Instance != null
                ? MetaProgressState.Instance.EventStreakDay
                : PlayerPrefs.GetInt(GetStreakKey(), 0);

        public bool IsDayClaimed(int day)
        {
            if (MetaProgressState.Instance != null)
                return MetaProgressState.Instance.IsEventDayClaimed(day);
            return PlayerPrefs.GetInt(GetDayKey(day), 0) == 1;
        }

        public void MarkDayClaimed(int day)
        {
            if (MetaProgressState.Instance != null)
            {
                MetaProgressState.Instance.MarkEventDayClaimed(day);
                return;
            }

            PlayerPrefs.SetInt(GetDayKey(day), 1);
            var streak = GetStreakDay();
            if (day > streak)
                PlayerPrefs.SetInt(GetStreakKey(), day);
        }

        public int GetRewardForDay(int day) => gemsPerDay > 0 ? gemsPerDay : checkInRewardGems;

        string GetStreakKey() => $"BES_Event_{eventId}_streak";
        string GetDayKey(int day) => $"BES_Event_{eventId}_day_{day}";
    }
}
