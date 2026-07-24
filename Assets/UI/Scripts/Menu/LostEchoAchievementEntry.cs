using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public class LostEchoAchievementEntry : MonoBehaviour
    {
        [SerializeField] string achievementId;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text buffDescriptionText;
        [SerializeField] List<DiscoverableRelicSlot> relicSlots = new();
        [SerializeField] Button activateButton;
        [SerializeField] TMP_Text energyCostText;
        [SerializeField, Min(0)] int energyCost = 30;
        [SerializeField] UnityEvent<string, int> onActivateRequested;

        public string AchievementId => achievementId;
        public int EnergyCost => energyCost;

        void Awake()
        {
            if (activateButton != null) activateButton.onClick.AddListener(Activate);
            Refresh();
        }

        public void Activate() => onActivateRequested?.Invoke(achievementId, energyCost);
        public void SetRelicDiscovered(int index, bool value)
        {
            if (index >= 0 && index < relicSlots.Count) relicSlots[index]?.SetDiscovered(value);
        }
        public void Refresh()
        {
            if (energyCostText != null) energyCostText.text = energyCost.ToString();
            foreach (var slot in relicSlots) slot?.Refresh();
        }
    }
}
