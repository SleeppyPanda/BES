using UnityEngine;

namespace BES.UI.Menu
{
    /// <summary>
    /// Controls which shared MenuHub currencies belong to the visible screen
    /// without changing their authored RectTransform values.
    /// </summary>
    public class CurrencyVisibilityController : MonoBehaviour
    {
        [SerializeField] GameObject coins;
        [SerializeField] GameObject gems;
        [SerializeField] GameObject crystalApple;
        [SerializeField] GameObject energy;
        [SerializeField] GameObject homePanel;
        [SerializeField] GameObject playModePanel;
        [SerializeField] GameObject storyModePanel;
        [SerializeField] GameObject cashShopPanel;
        [SerializeField] GameObject battlePanel;

        void OnEnable() => Apply();
        void LateUpdate() => Apply();

        void Apply()
        {
            var battleVisible = IsVisible(battlePanel);
            var shopVisible = IsVisible(cashShopPanel);
            var showShared = !battleVisible && !shopVisible;

            SetIfChanged(coins, showShared);
            SetIfChanged(gems, showShared);
            SetIfChanged(energy, showShared && IsVisible(storyModePanel));
            SetIfChanged(
                crystalApple,
                showShared && (IsVisible(homePanel) || IsVisible(playModePanel)));
        }

        static bool IsVisible(GameObject target) =>
            target != null && target.activeInHierarchy;

        static void SetIfChanged(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value)
                target.SetActive(value);
        }
    }
}
