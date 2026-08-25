using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public class SanctumDomainEntry : MonoBehaviour
    {
        [SerializeField] string domainId;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] List<Image> rewardSlots = new();
        [SerializeField] Button enterButton;
        [SerializeField] TMP_Text energyCostText;
        [SerializeField, Min(0)] int energyCost = 10;
        [SerializeField] UnityEvent<string, int> onEnterRequested;

        public string DomainId => domainId;
        public IReadOnlyList<Image> RewardSlots => rewardSlots;
        public TMP_Text EnergyCostText => energyCostText;
        public TMP_Text TitleText => titleText;
        public TMP_Text ProgressText => progressText;
        public Button EnterButton => enterButton;
        public int EnergyCost { get => energyCost; set => energyCost = Mathf.Max(0, value); }

        public void SetDomainId(string value)
        {
            if (!string.IsNullOrEmpty(value)) domainId = value;
        }

        public void RefreshEnergyText()
        {
            if (energyCostText != null) energyCostText.text = energyCost.ToString();
        }

        void Awake()
        {
            if (enterButton != null) enterButton.onClick.AddListener(Enter);
            RefreshEnergyText();
        }

        public void Enter()
        {
            TurnBattleUI.ActiveStageId = domainId;
            onEnterRequested?.Invoke(domainId, energyCost);
        }
        public void SetReward(int index, Sprite sprite)
        {
            if (index >= 0 && index < rewardSlots.Count && rewardSlots[index] != null)
                rewardSlots[index].sprite = sprite;
        }
    }
}
