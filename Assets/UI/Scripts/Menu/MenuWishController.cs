using System;
using System.Collections;
using System.Collections.Generic;
using BES.Core;
using BES.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BES.UI.Menu
{
    public enum WishCurrency
    {
        Coins,
        Gems
    }

    [Serializable]
    public class MenuWishReward
    {
        public string itemId;
        public string displayName;
        [TextArea(2, 6)] public string description;
        public Sprite icon;
        [Range(3, 5)] public int rarity = 3;
        [Min(1)] public int weight = 10;
        [Min(1)] public int amount = 1;
        public bool unlockAsCharacter;
    }

    [Serializable]
    public class WishResultCardView
    {
        public RectTransform root;
        public CanvasGroup canvasGroup;
        public Image rarityGlow;
        public Image cardBackground;
        public Image itemIcon;
        public TMP_Text itemNameText;
        public TMP_Text rarityText;
        public WishResultCardHover hover;
    }

    public class MenuWishController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] MenuContentDatabase database;
        [SerializeField] InventorySystem inventory;
        [SerializeField] MenuHomeController homeController;
        [SerializeField] List<MenuWishReward> rewards = new();

        [Header("Currency")]
        [SerializeField] Button coinsButton;
        [SerializeField] Button gemsButton;
        [SerializeField] TMP_Text coinsAmountText;
        [SerializeField] TMP_Text gemsAmountText;
        [SerializeField, Min(0)] int singleCoinCost = 1600;
        [SerializeField, Min(0)] int tenCoinCost = 16000;
        [SerializeField, Min(0)] int singleGemCost = 160;
        [SerializeField, Min(0)] int tenGemCost = 1600;
        [SerializeField] WishCurrency initialCurrency = WishCurrency.Gems;

        [Header("Controls")]
        [SerializeField] GameObject rollControls;
        [SerializeField] Button rollOneButton;
        [SerializeField] Button rollTenButton;
        [SerializeField] Button claimButton;
        [SerializeField] TMP_Text feedbackText;

        [Header("Cards")]
        [SerializeField] List<WishResultCardView> resultCards = new();
        [SerializeField] Sprite fourStarGlow;
        [SerializeField] Sprite fiveStarGlow;
        [Tooltip("Thời gian mỗi thẻ di chuyển từ ngoài màn hình vào vị trí đích. Giá trị lớn hơn = chậm hơn.")]
        [SerializeField, Min(.05f)] float cardFlyDuration = .85f;
        [Tooltip("Khoảng nghỉ giữa thời điểm bắt đầu di chuyển của từng thẻ khi Roll x10.")]
        [SerializeField, Min(0f)] float cardStaggerDelay = .14f;
        [SerializeField] float cardSpawnDistance = 720f;
        [SerializeField] AnimationCurve cardEasing =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Hovered details")]
        [SerializeField] GameObject detailPanel;
        [SerializeField] Image detailCardBackground;
        [SerializeField] Image detailItemIcon;
        [SerializeField] TMP_Text detailNameText;
        [SerializeField] TMP_Text detailDescriptionText;
        [SerializeField] TMP_Text detailRarityText;

        [Header("Gacha Video Clips")]
        [SerializeField] private VideoClip clip4Star;
        [SerializeField] private VideoClip clip5Star;
        [SerializeField] private VideoPlayer globalVideoPlayer;
        [SerializeField] private RawImage videoOverlayImage;
        [SerializeField] private Button continueButton;

        private bool continuePressed;

        readonly List<MenuWishReward> currentResults = new();
        readonly System.Random random = new();
        readonly List<Vector2> targetPositions = new();
        WishCurrency selectedCurrency;
        bool isAnimating;
        int visibleCardCount;

        void Awake()
        {
            ResolveInventory();
            selectedCurrency = initialCurrency;
            CacheTargetPositions();
            coinsButton?.onClick.AddListener(() => SelectCurrency(WishCurrency.Coins));
            gemsButton?.onClick.AddListener(() => SelectCurrency(WishCurrency.Gems));
            rollOneButton?.onClick.AddListener(() => Roll(1));
            rollTenButton?.onClick.AddListener(() => Roll(10));
            claimButton?.onClick.AddListener(ClaimResults);
            ResetPresentation();
        }

        void OnEnable()
        {
            ResolveInventory();
            RefreshCurrency();
            if (!isAnimating) ResetPresentation();
        }

        public void SelectCurrency(WishCurrency currency)
        {
            if (isAnimating) return;
            selectedCurrency = currency;
            RefreshCurrency();
        }

        public void SwapCurrency()
        {
            SelectCurrency(selectedCurrency == WishCurrency.Gems
                ? WishCurrency.Coins
                : WishCurrency.Gems);
        }

        public void RollOne() => Roll(1);
        public void RollTen() => Roll(10);

        public void Roll(int count)
        {
            if (isAnimating || (count != 1 && count != 10)) return;
            if (rewards.Count == 0)
            {
                SetFeedback("WISH POOL IS EMPTY");
                return;
            }
            if (!TrySpend(count))
            {
                SetFeedback(selectedCurrency == WishCurrency.Gems
                    ? "NOT ENOUGH GEMS"
                    : "NOT ENOUGH COINS");
                return;
            }

            ResolveInventory();
            currentResults.Clear();
            for (var i = 0; i < count; i++)
            {
                var reward = RollReward();
                if (reward == null) continue;
                currentResults.Add(reward);
                if (reward.unlockAsCharacter)
                    ApplyCharacterReward(reward);
                else
                    inventory?.AddItem(reward.itemId, reward.amount);
                GachaPityState.Instance?.RegisterPull(reward.rarity);
            }
            GameManager.Instance?.SaveGame();
            StartCoroutine(RevealRoutine());
        }

        public void ClaimResults()
        {
            if (isAnimating) return;
            ResetPresentation();
        }

        public void ShowCardDetails(int index)
        {
            if (index < 0 || index >= currentResults.Count) return;
            var reward = currentResults[index];
            if (detailPanel != null) detailPanel.SetActive(true);
            if (detailItemIcon != null)
            {
                detailItemIcon.sprite = reward.icon;
                detailItemIcon.enabled = reward.icon != null;
            }
            if (detailNameText != null) detailNameText.text = reward.displayName;
            if (detailDescriptionText != null) detailDescriptionText.text = reward.description;
            if (detailRarityText != null) detailRarityText.text = $"{reward.rarity} ★";
            if (detailCardBackground != null && index < resultCards.Count)
                detailCardBackground.sprite = resultCards[index].cardBackground?.sprite;
        }

        public void HideCardDetails(int index)
        {
            if (detailPanel != null) detailPanel.SetActive(false);
        }

        IEnumerator RevealRoutine()
        {
            isAnimating = true;
            visibleCardCount = currentResults.Count;
            rollControls?.SetActive(false);
            claimButton?.gameObject.SetActive(false);
            detailPanel?.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            SetFeedback(string.Empty);

            // Determine highest rarity in current roll
            bool has5Star = false;
            bool has4Star = false;
            foreach (var reward in currentResults)
            {
                if (reward.rarity >= 5) has5Star = true;
                else if (reward.rarity >= 4) has4Star = true;
            }

            // 1. Play pre-pull clip if configured
            var chosenClip = has5Star ? clip5Star : (has4Star ? clip4Star : null);
            if (chosenClip != null && globalVideoPlayer != null && videoOverlayImage != null)
            {
                videoOverlayImage.gameObject.SetActive(true);
                globalVideoPlayer.clip = chosenClip;
                globalVideoPlayer.Prepare();
                while (!globalVideoPlayer.isPrepared)
                {
                    yield return null;
                }
                videoOverlayImage.texture = globalVideoPlayer.texture;
                globalVideoPlayer.Play();

                // Wait for clip to finish or double click to skip
                float lastClickTime = 0f;
                bool skipPrePull = false;
                while (globalVideoPlayer.isPlaying)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (Time.unscaledTime - lastClickTime < 0.35f)
                        {
                            skipPrePull = true;
                        }
                        lastClickTime = Time.unscaledTime;
                    }
                    if (skipPrePull)
                    {
                        globalVideoPlayer.Stop();
                        break;
                    }
                    yield return null;
                }
                videoOverlayImage.gameObject.SetActive(false);
            }

            // Hide all cards initially
            for (var i = 0; i < resultCards.Count; i++)
            {
                resultCards[i].root?.gameObject.SetActive(false);
            }

            // 2. Fly in cards one by one
            for (var i = 0; i < visibleCardCount; i++)
            {
                var cardView = resultCards[i];
                cardView.root?.gameObject.SetActive(true);
                SetupCard(i, currentResults[i]);

                var target = TargetFor(i, visibleCardCount);
                var fromTop = visibleCardCount == 1 || i < 5;
                var start = target + Vector2.up * (fromTop ? cardSpawnDistance : -cardSpawnDistance);
                cardView.root.anchoredPosition = start;

                float elapsed = 0f;
                while (elapsed < cardFlyDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var normalized = Mathf.Clamp01(elapsed / cardFlyDuration);
                    var t = cardEasing != null ? cardEasing.Evaluate(normalized) : normalized;
                    if (cardView.root != null)
                        cardView.root.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
                    if (cardView.canvasGroup != null)
                        cardView.canvasGroup.alpha = normalized;
                    yield return null;
                }
                if (cardView.root != null)
                    cardView.root.anchoredPosition = target;
                if (cardView.canvasGroup != null)
                    cardView.canvasGroup.alpha = 1f;

                // Check for 5-star character custom reveal clip
                var reward = currentResults[i];
                var charId = CharacterIdFor(reward);
                var character = database != null ? database.FindCharacter(charId) : null;
                bool is5StarChar = reward.unlockAsCharacter && character != null && character.rarity >= 5;

                if (is5StarChar && character.revealVideoClip != null && globalVideoPlayer != null && videoOverlayImage != null)
                {
                    videoOverlayImage.gameObject.SetActive(true);
                    globalVideoPlayer.clip = character.revealVideoClip;
                    globalVideoPlayer.Prepare();
                    while (!globalVideoPlayer.isPrepared)
                    {
                        yield return null;
                    }
                    videoOverlayImage.texture = globalVideoPlayer.texture;
                    globalVideoPlayer.Play();

                    // Wait for character clip to finish or double click to skip
                    float lastClickTime = 0f;
                    bool skipChar = false;
                    while (globalVideoPlayer.isPlaying)
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            if (Time.unscaledTime - lastClickTime < 0.35f)
                            {
                                skipChar = true;
                            }
                            lastClickTime = Time.unscaledTime;
                        }
                        if (skipChar)
                        {
                            globalVideoPlayer.Stop();
                            break;
                        }
                        yield return null;
                    }

                    // Show continue button and wait for player click
                    if (continueButton != null)
                    {
                        continueButton.gameObject.SetActive(true);
                        continueButton.onClick.RemoveAllListeners();
                        continuePressed = false;
                        continueButton.onClick.AddListener(() => continuePressed = true);

                        while (!continuePressed)
                        {
                            yield return null;
                        }
                        continueButton.gameObject.SetActive(false);
                    }

                    videoOverlayImage.gameObject.SetActive(false);
                }
            }

            RefreshCurrency();
            claimButton?.gameObject.SetActive(true);
            SetFeedback(visibleCardCount == 1 ? "WISH ×1 COMPLETE" : "WISH ×10 COMPLETE");
            isAnimating = false;
        }

        void SetupCard(int index, MenuWishReward reward)
        {
            var view = resultCards[index];
            view.hover?.ResetVisual();
            if (view.rarityGlow != null)
            {
                view.rarityGlow.enabled = reward.rarity >= 4;
                view.rarityGlow.sprite = reward.rarity >= 5 ? fiveStarGlow : fourStarGlow;
            }
            if (view.itemIcon != null)
            {
                view.itemIcon.sprite = reward.icon;
                view.itemIcon.enabled = reward.icon != null;
            }
            if (view.itemNameText != null) view.itemNameText.text = reward.displayName;
            if (view.rarityText != null) view.rarityText.text = $"{reward.rarity} ★";
        }

        MenuWishReward RollReward()
        {
            var total = 0;
            foreach (var reward in rewards)
                if (reward != null) total += Mathf.Max(1, reward.weight);
            if (total <= 0) return null;
            var value = random.Next(0, total);
            var accumulated = 0;
            foreach (var reward in rewards)
            {
                if (reward == null) continue;
                accumulated += Mathf.Max(1, reward.weight);
                if (value < accumulated) return reward;
            }
            return rewards[^1];
        }

        string CharacterIdFor(MenuWishReward reward)
        {
            if (reward == null || string.IsNullOrWhiteSpace(reward.itemId))
                return string.Empty;

            var id = reward.itemId.StartsWith("wish_", StringComparison.OrdinalIgnoreCase)
                ? reward.itemId[5..]
                : reward.itemId;
            return database != null && database.FindCharacter(id) != null ? id : reward.itemId;
        }

        void ApplyCharacterReward(MenuWishReward reward)
        {
            var id = CharacterIdFor(reward);
            var roster = PartyRoster.Instance;
            var wasOwned = roster != null && roster.IsCharacterUnlocked(id);
            roster?.UnlockCharacter(id, reward.displayName);
            if (wasOwned)
            {
                var definition = CharacterDatabaseLoader.Load()?.Get(id);
                var amount = Mathf.Max(1, definition?.duplicateShardReward ?? 1);
                CharacterProgressionState.AddDuplicateShards(id, amount);
                SetFeedback($"DUPLICATE: +{amount} CONSTELLATION SHARD");
            }
            GameEvents.RaisePartyChanged();
        }

        bool TrySpend(int count)
        {
            var currencyId = selectedCurrency == WishCurrency.Gems ? "gems" : "coins";
            var entry = database?.currencies.Find(x => x.id == currencyId);
            var cost = selectedCurrency == WishCurrency.Gems
                ? count == 1 ? singleGemCost : tenGemCost
                : count == 1 ? singleCoinCost : tenCoinCost;
            if (entry == null || entry.amount < cost) return false;
            entry.amount -= cost;
            homeController?.Refresh();
            return true;
        }

        void RefreshCurrency()
        {
            var coins = database?.currencies.Find(x => x.id == "coins")?.amount ?? 0;
            var gems = database?.currencies.Find(x => x.id == "gems")?.amount ?? 0;
            if (coinsAmountText != null) coinsAmountText.text = coins.ToString("N0");
            if (gemsAmountText != null) gemsAmountText.text = gems.ToString("N0");
            if (coinsButton != null)
                coinsButton.interactable = selectedCurrency != WishCurrency.Coins && !isAnimating;
            if (gemsButton != null)
                gemsButton.interactable = selectedCurrency != WishCurrency.Gems && !isAnimating;
        }

        void ResetPresentation()
        {
            StopAllCoroutines();
            isAnimating = false;
            visibleCardCount = 0;
            currentResults.Clear();
            foreach (var card in resultCards)
            {
                card.hover?.ResetVisual();
                card.root?.gameObject.SetActive(false);
            }
            detailPanel?.SetActive(false);
            claimButton?.gameObject.SetActive(false);
            rollControls?.SetActive(true);
            SetFeedback(string.Empty);
            RefreshCurrency();
        }

        void ResolveInventory()
        {
            inventory ??= GameManager.Instance != null
                ? GameManager.Instance.Inventory
                : FindAnyObjectByType<InventorySystem>();
        }

        void CacheTargetPositions()
        {
            targetPositions.Clear();
            foreach (var card in resultCards)
                targetPositions.Add(card.root != null ? card.root.anchoredPosition : Vector2.zero);
        }

        Vector2 TargetFor(int index, int count) =>
            count == 1 ? Vector2.zero :
            index >= 0 && index < targetPositions.Count ? targetPositions[index] : Vector2.zero;

        void SetFeedback(string value)
        {
            if (feedbackText != null) feedbackText.text = value;
        }
    }
}
