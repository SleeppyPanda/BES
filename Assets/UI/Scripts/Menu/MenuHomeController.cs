using System;
using System.Collections.Generic;
using BES.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    [Serializable]
    public class HomeButtonAction
    {
        public string label;
        public Button button;
        public UnityEvent action;
    }

    [Serializable]
    public class HomeCurrencyView
    {
        public string currencyId;
        public Image background;
        public Image icon;
        public TMP_Text amountText;
        public Button addButton;
        public UnityEvent onAddPressed;
    }

    public class MenuHomeController : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] MenuContentDatabase database;
        [SerializeField] MenuNavigator navigator;
        [SerializeField] HomeModeSwitcher modeSwitcher;

        [Header("Persistent left half - Account profile")]
        [SerializeField] Image accountAvatar;
        [SerializeField] Image currentCharacterImage;
        [SerializeField] TMP_Text playerNameText;
        [SerializeField] TMP_Text accountIdText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] string playerName = "Traveler";
        [SerializeField] string accountId = "000000001";
        [SerializeField, Min(1)] int playerLevel = 1;
        [SerializeField] string currentCharacterId;
        [SerializeField] Sprite defaultAccountAvatar;
        [SerializeField] Sprite defaultCharacter;

        [Header("Persistent left buttons")]
        [SerializeField] Button settingsButton;
        [SerializeField] Button letterButton;
        [SerializeField] Button eventButton;
        [SerializeField] Button bagButton;
        [SerializeField] Button chatButton;
        [SerializeField] SimpleModalPanel settingsPanel;
        [SerializeField] LetterUI letterPanel;
        [SerializeField] SimpleModalPanel eventPanel;
        [SerializeField] InventoryUI inventoryPanel;
        [SerializeField] SimpleModalPanel chatPanel;

        [Header("Character rank-up")]
        [SerializeField] Button rankUpButton;
        [SerializeField] Image rankUpBanner;
        [SerializeField] SimpleModalPanel rankUpPanel;
        [SerializeField] List<Image> rankStars = new();
        [SerializeField] Sprite emptyStar;
        [SerializeField] Sprite filledStar;
        [SerializeField, Range(0, 5)] int currentRank;

        [Header("Persistent right half - Currencies")]
        [SerializeField] List<HomeCurrencyView> currencies = new();

        [Header("Persistent right bottom")]
        [SerializeField] Button cashShopButton;
        [SerializeField] Button battlePassButton;
        [SerializeField] Button missionButton;
        [SerializeField] CashShopUI cashShopPanel;
        [SerializeField] SimpleModalPanel battlePassPanel;
        [SerializeField] MissionUI missionPanel;

        [Header("Story mode content")]
        [SerializeField] TMP_Text currentChapterText;
        [SerializeField] TMP_Text currentQuestText;
        [SerializeField] TMP_Text currentStageText;
        [SerializeField] string currentChapter = "Chapter I: The Inherited Flame";
        [SerializeField] string currentQuest = "Divine Seal Quest";
        [SerializeField] string currentStage = "1-1";
        [SerializeField] Button enterStoryButton;
        [SerializeField] Button wishButton;
        [SerializeField] Button characterInfoButton;
        [SerializeField] Button galleryButton;
        [SerializeField] SimpleModalPanel wishPanel;
        [SerializeField] SimpleModalPanel characterInfoPanel;
        [SerializeField] SimpleModalPanel galleryPanel;

        [Header("Play mode content")]
        [Tooltip("Six main play-mode buttons. Each action can open a panel or invoke game logic.")]
        [SerializeField] List<HomeButtonAction> playModeActions = new();
        [SerializeField] Button gatheringValeButton;
        [SerializeField] SimpleModalPanel gatheringValePanel;

        public string CurrentCharacterId => currentCharacterId;
        public int CurrentRank => currentRank;

        void Start()
        {
            Wire(settingsButton, () => settingsPanel?.Open());
            Wire(letterButton, () => letterPanel?.Open());
            Wire(eventButton, () => eventPanel?.Open());
            Wire(bagButton, () => inventoryPanel?.Open());
            Wire(chatButton, () => chatPanel?.Open());
            Wire(rankUpButton, () => rankUpPanel?.Open());
            Wire(cashShopButton, () => cashShopPanel?.Open());
            Wire(battlePassButton, () => battlePassPanel?.Open());
            Wire(missionButton, () => missionPanel?.Open());
            Wire(enterStoryButton, () => navigator?.Open(MenuScreenId.StoryParty));
            Wire(wishButton, () => wishPanel?.Open());
            Wire(characterInfoButton, () => characterInfoPanel?.Open());
            Wire(galleryButton, () => galleryPanel?.Open());
            Wire(gatheringValeButton, () => gatheringValePanel?.Open());

            foreach (var action in playModeActions)
            {
                var captured = action;
                Wire(captured.button, () => captured.action?.Invoke());
            }
            foreach (var view in currencies)
            {
                var captured = view;
                if (captured.addButton != null)
                    captured.addButton.onClick.RemoveAllListeners();
                Wire(captured.addButton, () => OpenCurrencyShop(captured));
            }
            EnsureStartingCharacter();
            Refresh();
        }

        void OnEnable() => Refresh();

        static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        void OpenCurrencyShop(HomeCurrencyView view)
        {
            if (cashShopPanel == null)
                return;

            cashShopPanel.OpenTab(view.currencyId switch
            {
                "energy" => CashShopTab.LightPurchase,
                "gems" => CashShopTab.DiamondPurchase,
                "coins" => CashShopTab.GoldenExchange,
                _ => CashShopTab.DiamondPurchase
            });
        }

        void EnsureStartingCharacter()
        {
            if (database == null || database.characters.Count == 0) return;
            if (string.IsNullOrWhiteSpace(currentCharacterId) || database.FindCharacter(currentCharacterId) == null)
                currentCharacterId = database.characters[0].id;
        }

        public void SelectCharacter(string characterId)
        {
            if (database == null || database.FindCharacter(characterId) == null) return;
            currentCharacterId = characterId;
            currentRank = 0;
            Refresh();
        }

        public void SetRank(int rank)
        {
            currentRank = Mathf.Clamp(rank, 0, 5);
            RefreshRank();
        }

        public void SetStoryProgress(string chapter, string quest, string stage)
        {
            currentChapter = chapter;
            currentQuest = quest;
            currentStage = stage;
            Refresh();
        }

        public void SetCurrency(string currencyId, int amount)
        {
            var entry = database?.currencies.Find(x => x.id == currencyId);
            if (entry == null) return;
            entry.amount = Mathf.Max(0, amount);
            RefreshCurrencies();
        }

        public void IncreaseRank()
        {
            if (currentRank >= 5) return;
            currentRank++;
            RefreshRank();
        }

        public void Refresh()
        {
            if (playerNameText != null) playerNameText.text = playerName;
            if (accountIdText != null) accountIdText.text = $"ID: {accountId}";
            if (levelText != null) levelText.text = playerLevel.ToString();
            if (accountAvatar != null) accountAvatar.sprite = defaultAccountAvatar;

            var character = database?.FindCharacter(currentCharacterId);
            if (currentCharacterImage != null)
                currentCharacterImage.sprite = character?.fullBody != null ? character.fullBody : defaultCharacter;

            if (currentChapterText != null) currentChapterText.text = currentChapter;
            if (currentQuestText != null) currentQuestText.text = currentQuest;
            if (currentStageText != null) currentStageText.text = currentStage;
            RefreshCurrencies();
            RefreshRank();
        }

        void RefreshCurrencies()
        {
            if (database == null) return;
            foreach (var view in currencies)
            {
                var entry = database.currencies.Find(x => x.id == view.currencyId);
                if (entry == null) continue;
                if (view.icon != null) view.icon.sprite = entry.icon;
                if (view.amountText != null) view.amountText.text = entry.amount.ToString("N0");
            }
        }

        void RefreshRank()
        {
            for (var i = 0; i < rankStars.Count; i++)
            {
                if (rankStars[i] == null) continue;
                rankStars[i].sprite = i < currentRank ? filledStar : emptyStar;
            }
        }
    }
}
