using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class HudCornerButtonsUI : MonoBehaviour
    {
        [SerializeField] Button settingsButton;
        [SerializeField] Button guideLineButton;
        [SerializeField] Button missionButton;

        UINavigationController navigation;

        void Awake()
        {
            navigation = GetComponentInParent<UINavigationController>();
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => navigation?.ToggleSettings());
            if (missionButton != null)
                missionButton.onClick.AddListener(() => navigation?.ToggleQuestLog());
            if (guideLineButton != null)
                guideLineButton.onClick.AddListener(() => Debug.Log("[BES] Guide line button clicked."));
        }
    }
}
