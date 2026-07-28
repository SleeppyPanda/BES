using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class ShopTabOpenButton : MonoBehaviour
    {
        [SerializeField] CashShopPanelController shop;
        [SerializeField, Min(0)] int mainTabIndex;
        [SerializeField] int packSubTabIndex = -1;

        void Awake()
        {
            var button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(Open);
        }

        public void Configure(CashShopPanelController controller, int mainIndex, int subIndex = -1)
        {
            shop = controller;
            mainTabIndex = Mathf.Max(0, mainIndex);
            packSubTabIndex = subIndex;
        }

        public void Open() => shop?.OpenDestination(mainTabIndex, packSubTabIndex);
    }
}
