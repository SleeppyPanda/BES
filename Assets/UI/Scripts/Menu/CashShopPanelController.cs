using System;
using System.Collections.Generic;
using BES.Core;
using BES.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    [Serializable]
    public class ShopCurrencyView
    {
        public string currencyId;
        public Image icon;
        public TMP_Text amountText;
    }

    [Serializable]
    public class ShopItemBinding
    {
        public string id;
        public int tabIndex;
        public GameObject root;
        public Image artwork;
        public TMP_Text nameText;
        public TMP_Text priceText;
        public Button purchaseButton;
        public string currencyId = "coins";
        [Min(0)] public int price = 100;
        public string rewardId;
        [Min(1)] public int rewardAmount = 1;
        public bool oneTimePurchase;
        public GameObject soldOutState;
        [NonSerialized] public bool purchased;
    }

    public class CashShopPanelController : MonoBehaviour
    {
        [SerializeField] MenuContentDatabase database;
        [SerializeField] MenuHomeController homeController;
        [SerializeField] List<Button> mainTabButtons = new();
        [SerializeField] List<GameObject> mainTabPanels = new();
        [SerializeField] SmoothTabGroup mainTabGroup;
        [SerializeField] List<SmoothTabGroup> subTabGroups = new();
        [SerializeField] TMP_Text tabTitle;
        [SerializeField] List<string> mainTabTitles = new();
        [SerializeField] List<ShopCurrencyView> currencies = new();
        [SerializeField] List<ShopItemBinding> items = new();
        [SerializeField] TMP_Text feedbackText;
        [SerializeField] UnityEvent<string> onItemPurchased;

        int selectedMainTab;

        void Awake()
        {
            for (var i = 0; i < mainTabButtons.Count; i++)
            {
                var index = i;
                if (mainTabButtons[i] != null)
                    mainTabButtons[i].onClick.AddListener(() => SelectMainTab(index));
            }
            foreach (var item in items)
            {
                var captured = item;
                if (item?.purchaseButton != null)
                    item.purchaseButton.onClick.AddListener(() => Purchase(captured));
            }
        }

        void OnEnable()
        {
            LoadPurchasedStates();
            mainTabGroup?.ShowImmediate(selectedMainTab);
            for (var i = 0; i < subTabGroups.Count; i++)
                subTabGroups[i]?.ShowImmediate(subTabGroups[i].CurrentIndex);
            ApplyMainTabState();
            Refresh();
        }

        public void SelectMainTab(int index)
        {
            selectedMainTab = Mathf.Clamp(index, 0, Mathf.Max(0, mainTabPanels.Count - 1));
            mainTabGroup?.Show(selectedMainTab);
            ApplyMainTabState();
        }

        void ApplyMainTabState()
        {
            for (var i = 0; i < mainTabButtons.Count; i++)
                if (mainTabButtons[i] != null) mainTabButtons[i].interactable = i != selectedMainTab;
            if (tabTitle != null && selectedMainTab < mainTabTitles.Count)
                tabTitle.text = mainTabTitles[selectedMainTab];
            if (feedbackText != null) feedbackText.text = string.Empty;
        }

        public void OpenDestination(int mainIndex, int packSubIndex = -1)
        {
            selectedMainTab = Mathf.Clamp(mainIndex, 0, Mathf.Max(0, mainTabPanels.Count - 1));
            var wasActive = gameObject.activeSelf;
            if (!wasActive) gameObject.SetActive(true);
            if (wasActive) SelectMainTab(selectedMainTab);
            else
            {
                mainTabGroup?.ShowImmediate(selectedMainTab);
                ApplyMainTabState();
            }
            if (packSubIndex >= 0 &&
                selectedMainTab < subTabGroups.Count &&
                subTabGroups[selectedMainTab] != null)
            {
                if (wasActive) subTabGroups[selectedMainTab].Show(packSubIndex);
                else subTabGroups[selectedMainTab].ShowImmediate(packSubIndex);
            }
            Refresh();
        }

        public void Refresh()
        {
            foreach (var view in currencies)
            {
                var currency = database?.currencies.Find(x => x.id == view.currencyId);
                if (view.icon != null) view.icon.sprite = currency?.icon;
                if (view.amountText != null) view.amountText.text = (currency?.amount ?? 0).ToString("N0");
            }

            foreach (var item in items)
            {
                if (item == null) continue;
                if (item.nameText != null) item.nameText.text = item.id;
                if (item.priceText != null) item.priceText.text = item.price.ToString("N0");
                var soldOut = item.oneTimePurchase && item.purchased;
                if (item.purchaseButton != null) item.purchaseButton.interactable = !soldOut;
                if (item.soldOutState != null) item.soldOutState.SetActive(soldOut);
            }
        }

        void Purchase(ShopItemBinding item)
        {
            if (item == null || (item.oneTimePurchase && item.purchased)) return;
            var currency = database?.currencies.Find(x => x.id == item.currencyId);
            if (currency == null || currency.amount < item.price)
            {
                if (feedbackText != null) feedbackText.text = "NOT ENOUGH CURRENCY";
                return;
            }

            var wallet = PlayerWallet.Instance;
            if (wallet != null)
            {
                var spent =
                    string.Equals(item.currencyId, "gems", StringComparison.OrdinalIgnoreCase)
                        ? wallet.TrySpendGems(item.price)
                        : string.Equals(item.currencyId, "coins", StringComparison.OrdinalIgnoreCase)
                            ? wallet.TrySpendCoins(item.price)
                            : true;
                if (!spent)
                {
                    if (feedbackText != null) feedbackText.text = "NOT ENOUGH CURRENCY";
                    return;
                }
            }
            if (wallet == null ||
                (!string.Equals(item.currencyId, "gems", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(item.currencyId, "coins", StringComparison.OrdinalIgnoreCase)))
            {
                currency.amount -= item.price;
            }
            item.purchased = true;
            RewardGrantService.Grant(item.rewardId, item.rewardAmount, item.id);
            SavePurchasedState(item);
            if (feedbackText != null)
                feedbackText.text = $"PURCHASED {item.rewardId} x{item.rewardAmount}";
            onItemPurchased?.Invoke(item.id);
            homeController?.Refresh();
            Refresh();
        }

        void LoadPurchasedStates()
        {
            var purchased = GameManager.Instance?.Save?.Current?.purchasedShopItemIds;
            if (purchased == null || purchased.Count == 0)
                return;

            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id))
                    continue;
                item.purchased = purchased.Contains(item.id);
            }
        }

        static void SavePurchasedState(ShopItemBinding item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id))
                return;
            var save = GameManager.Instance?.Save?.Current;
            if (save == null)
                return;
            save.purchasedShopItemIds ??= new List<string>();
            if (!save.purchasedShopItemIds.Contains(item.id))
                save.purchasedShopItemIds.Add(item.id);
            GameManager.Instance?.SaveGame();
        }
    }
}
