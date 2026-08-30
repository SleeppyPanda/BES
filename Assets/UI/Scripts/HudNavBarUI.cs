using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class HudNavBarUI : MonoBehaviour
    {
        [SerializeField] Button eventButton;
        [SerializeField] Button battlePassButton;
        [SerializeField] Button wishButton;
        [SerializeField] Button bagButton;
        [SerializeField] Button personalButton;
        [SerializeField] Button exitHubButton;

        UINavigationController navigation;

        void Awake()
        {
            navigation = GetComponentInParent<UINavigationController>();
            if (eventButton != null) eventButton.onClick.AddListener(() => navigation?.ToggleEvent());
            if (battlePassButton != null) battlePassButton.onClick.AddListener(() => navigation?.ToggleBattlePass());
            if (wishButton != null) wishButton.onClick.AddListener(() => navigation?.ToggleWish());
            if (bagButton != null) bagButton.onClick.AddListener(() => navigation?.ToggleInventory());
            if (personalButton != null) personalButton.onClick.AddListener(() => navigation?.ToggleCharacter());
            if (exitHubButton != null) exitHubButton.onClick.AddListener(ExitToMenuHub);
        }

        void ExitToMenuHub()
        {
            if (BES.Core.SceneLoader.Instance != null)
                BES.Core.SceneLoader.Instance.LoadScene("menuhub");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("menuhub");
        }
    }
}
