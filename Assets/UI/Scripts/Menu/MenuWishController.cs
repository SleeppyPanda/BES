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
        [Header("Immersive Mode UI")]
        [SerializeField] private GameObject appleCurrencyBar;
        [SerializeField] private GameObject closeButton;
        [Header("Global UI References (Auto-detected)")]
        [SerializeField] private GameObject globalAppleBar;
        [SerializeField] private GameObject globalGemsBar;
        [SerializeField] private GameObject globalCoinsBar;
        [SerializeField] private GameObject globalCloseButton;

        private bool continuePressed;

        readonly List<MenuWishReward> currentResults = new();
        readonly System.Random random = new();
        readonly List<Vector2> targetPositions = new();
        WishCurrency selectedCurrency;
        bool isAnimating;
        int visibleCardCount;
        private RenderTexture videoRT;

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

            // Auto-detect Apple currency bar and Close button from WishContent children
            if (coinsButton != null && (appleCurrencyBar == null || closeButton == null))
            {
                Transform parent = coinsButton.transform.parent;
                if (parent != null)
                {
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        Transform child = parent.GetChild(i);
                        string childName = child.name.ToLower();
                        if (appleCurrencyBar == null && (childName.Contains("apple") || childName.Contains("crystal")))
                        {
                            appleCurrencyBar = child.gameObject;
                        }
                        else if (closeButton == null && (childName.Contains("close") || childName.Contains("back") || childName == "x" || childName.Contains("btn_close")))
                        {
                            closeButton = child.gameObject;
                        }
                    }
                }
            }

            ResetPresentation();
        }

        void OnEnable()
        {
            ResolveInventory();
            RefreshCurrency();
            if (homeController != null)
            {
                homeController.gameObject.SetActive(false);
            }
            ResetPresentation();
        }

        void OnDisable()
        {
            if (homeController != null)
            {
                homeController.gameObject.SetActive(true);
            }
            ResetPresentation();
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

            // Hide top bar UI for immersive playback
            if (coinsButton != null) coinsButton.gameObject.SetActive(false);
            if (gemsButton != null) gemsButton.gameObject.SetActive(false);
            if (appleCurrencyBar != null) appleCurrencyBar.SetActive(false);
            if (closeButton != null) closeButton.SetActive(false);

            // Find global currency bars and close button under topParent
            Transform topParent = transform;
            while (topParent.parent != null)
            {
                topParent = topParent.parent;
            }
            var allTransforms = topParent.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t.name == "Currency_crystal_apple") globalAppleBar = t.gameObject;
                else if (t.name == "Currency_gems") globalGemsBar = t.gameObject;
                else if (t.name == "Currency_coins") globalCoinsBar = t.gameObject;
                else if (t.name == "Close" || t.name == "CloseButton")
                {
                    var p = t.parent;
                    if (p != null && (p.name == "Background" || p.name == "WishPanel" || p.name == "MenuHub" || p.name == "WishContent"))
                    {
                        globalCloseButton = t.gameObject;
                    }
                }
            }

            Debug.Log($"[BES] Gacha UI Hide - globalAppleBar: {globalAppleBar != null}, globalGemsBar: {globalGemsBar != null}, globalCoinsBar: {globalCoinsBar != null}, globalCloseButton: {globalCloseButton != null}");

            if (globalAppleBar != null) globalAppleBar.SetActive(false);
            if (globalGemsBar != null) globalGemsBar.SetActive(false);
            if (globalCoinsBar != null) globalCoinsBar.SetActive(false);
            if (globalCloseButton != null) globalCloseButton.SetActive(false);

            // Determine highest rarity in current roll
            bool has5Star = false;
            bool has4Star = false;
            foreach (var reward in currentResults)
            {
                if (reward.rarity >= 5) has5Star = true;
                else if (reward.rarity >= 4) has4Star = true;
            }

            var chosenClip = has5Star ? clip5Star : (has4Star ? clip4Star : null);
            if (chosenClip != null && globalVideoPlayer != null && videoOverlayImage != null)
            {
                videoOverlayImage.gameObject.SetActive(true);
                
                int w = chosenClip.width > 0 ? (int)chosenClip.width : 1920;
                int h = chosenClip.height > 0 ? (int)chosenClip.height : 1080;
                if (videoRT != null) { videoRT.Release(); Destroy(videoRT); }
                videoRT = new RenderTexture(w, h, 0, RenderTextureFormat.Default);
                videoRT.Create();

                globalVideoPlayer.Stop(); // Force stop before modifying
                globalVideoPlayer.playOnAwake = false;
                globalVideoPlayer.skipOnDrop = false;
                globalVideoPlayer.waitForFirstFrame = false;

                globalVideoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                globalVideoPlayer.targetTexture = videoRT;
                videoOverlayImage.texture = videoRT;

                globalVideoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; // Disable audio sync issues
                globalVideoPlayer.clip = chosenClip;
                globalVideoPlayer.Prepare();
                while (!globalVideoPlayer.isPrepared)
                {
                    yield return null;
                }
                globalVideoPlayer.Play();
                Debug.Log($"[BES] Gacha Pre-pull Video started: {chosenClip.name} | Total Frames: {globalVideoPlayer.frameCount} | Duration: {globalVideoPlayer.length}s");

                // Wait for it to start playing or timeout
                float playStartTime = Time.unscaledTime;
                while (!globalVideoPlayer.isPlaying && (Time.unscaledTime - playStartTime < 2f))
                {
                    yield return null;
                }

                // Wait for clip to finish or double click to skip
                float lastClickTime = 0f;
                bool skipPrePull = false;
                float videoDuration = (float)globalVideoPlayer.length;
                if (videoDuration <= 0f) videoDuration = 10f;
                float startLoopTime = Time.unscaledTime;

                float lastLogTime = 0f;
                while (globalVideoPlayer.isPlaying && (Time.unscaledTime - startLoopTime < videoDuration + 0.5f))
                {
                    if (Time.unscaledTime - lastLogTime > 1.0f)
                    {
                        Debug.Log($"[BES] Gacha Video Frame: {globalVideoPlayer.frame}/{globalVideoPlayer.frameCount} | Time: {globalVideoPlayer.time:F2}s");
                        lastLogTime = Time.unscaledTime;
                    }

                    if (globalVideoPlayer.frameCount > 0 && globalVideoPlayer.frame >= (long)globalVideoPlayer.frameCount - 2)
                    {
                        break;
                    }

                    if (IsPointerClicked())
                    {
                        if (Time.unscaledTime - lastClickTime < 0.35f)
                        {
                            skipPrePull = true;
                        }
                        lastClickTime = Time.unscaledTime;
                    }
                    if (skipPrePull)
                    {
                        break;
                    }
                    yield return null;
                }
                globalVideoPlayer.Stop();
                globalVideoPlayer.clip = null; // Unload clip asset
                globalVideoPlayer.targetTexture = null;
                videoOverlayImage.texture = null;
                if (videoRT != null) { videoRT.Release(); Destroy(videoRT); videoRT = null; }
                videoOverlayImage.gameObject.SetActive(false);
            }

            // Hide all cards initially
            for (var i = 0; i < resultCards.Count; i++)
            {
                resultCards[i].root?.gameObject.SetActive(false);
            }

            // 1.5. Play reveal videos for all 5-star characters in the roll results BEFORE showing the cards screen
            if (globalVideoPlayer != null && videoOverlayImage != null)
            {
                RawImage fadeImg = CreateTransitionOverlay();

                foreach (var reward in currentResults)
                {
                    var charId = CharacterIdFor(reward);
                    var character = database != null ? database.FindCharacter(charId) : null;
                    bool is5StarChar = reward.unlockAsCharacter && character != null && character.rarity >= 5;

                    if (is5StarChar && character.revealVideoClip != null)
                    {
                        // 1.5a. Fade screen to black (tối dần)
                        if (fadeImg != null)
                        {
                            fadeImg.gameObject.SetActive(true);
                            fadeImg.color = Color.black;
                            yield return FadeOverlay(fadeImg, 0f, 1f, 0.6f);
                        }

                        // 1.5b. Prepare the reveal video while screen is black
                        int w = character.revealVideoClip.width > 0 ? (int)character.revealVideoClip.width : 1920;
                        int h = character.revealVideoClip.height > 0 ? (int)character.revealVideoClip.height : 1080;
                        if (videoRT != null) { videoRT.Release(); Destroy(videoRT); }
                        videoRT = new RenderTexture(w, h, 0, RenderTextureFormat.Default);
                        videoRT.Create();

                        globalVideoPlayer.Stop(); // Force stop before modifying
                        globalVideoPlayer.playOnAwake = false;
                        globalVideoPlayer.skipOnDrop = false;
                        globalVideoPlayer.waitForFirstFrame = false;

                        globalVideoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                        globalVideoPlayer.targetTexture = videoRT;
                        
                        videoOverlayImage.gameObject.SetActive(true);
                        videoOverlayImage.texture = videoRT;
                        videoOverlayImage.color = Color.white; // Tint to white to show video properly

                        globalVideoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; // Disable audio sync issues
                        globalVideoPlayer.clip = character.revealVideoClip;
                        globalVideoPlayer.Prepare();
                        while (!globalVideoPlayer.isPrepared)
                        {
                            yield return null;
                        }

                        // Start playing the video
                        globalVideoPlayer.Play();
                        Debug.Log($"[BES] 5-Star Character Reveal Video started: {character.revealVideoClip.name} | Total Frames: {globalVideoPlayer.frameCount} | Duration: {globalVideoPlayer.length}s");

                        // Wait for character clip to start playing or timeout
                        float charPlayStartTime = Time.unscaledTime;
                        while (!globalVideoPlayer.isPlaying && (Time.unscaledTime - charPlayStartTime < 2f))
                        {
                            yield return null;
                        }

                        // 1.5c. Fade black screen to transparent (sáng dần ra video)
                        if (fadeImg != null)
                        {
                            yield return FadeOverlay(fadeImg, 1f, 0f, 0.6f);
                        }

                        // 1.5d. Wait for character clip to finish or double click to skip
                        float lastClickTime = 0f;
                        bool skipChar = false;
                        float charVideoDuration = (float)globalVideoPlayer.length;
                        if (charVideoDuration <= 0f) charVideoDuration = 10f;
                        float charStartLoopTime = Time.unscaledTime;

                        float lastLogTime = 0f;
                        while (globalVideoPlayer.isPlaying && (Time.unscaledTime - charStartLoopTime < charVideoDuration + 0.5f))
                        {
                            if (Time.unscaledTime - lastLogTime > 1.0f)
                            {
                                Debug.Log($"[BES] Reveal Video Frame: {globalVideoPlayer.frame}/{globalVideoPlayer.frameCount} | Time: {globalVideoPlayer.time:F2}s");
                                lastLogTime = Time.unscaledTime;
                            }

                            if (globalVideoPlayer.frameCount > 0 && globalVideoPlayer.frame >= (long)globalVideoPlayer.frameCount - 2)
                            {
                                break;
                            }

                            if (IsPointerClicked())
                            {
                                if (Time.unscaledTime - lastClickTime < 0.35f)
                                {
                                    skipChar = true;
                                }
                                lastClickTime = Time.unscaledTime;
                            }
                            if (skipChar)
                            {
                                break;
                            }
                            yield return null;
                        }

                        // Pause video player at the final frame (do NOT stop or clear texture yet!)
                        if (globalVideoPlayer.isPlaying)
                        {
                            globalVideoPlayer.Pause();
                        }

                        // 1.5e. Show continue button and wait for player click (or click anywhere on screen) with 0.2s cooldown
                        if (continueButton != null)
                        {
                            continueButton.gameObject.SetActive(true);
                            continueButton.onClick.RemoveAllListeners();
                            continuePressed = false;
                            continueButton.onClick.AddListener(() => {
                                Debug.Log("[BES] continueButton onClick event fired!");
                                continuePressed = true;
                            });

                            float waitStartTime = Time.unscaledTime;
                            while (!continuePressed)
                            {
                                if (Time.unscaledTime - waitStartTime > 0.2f && IsPointerClicked())
                                {
                                    Debug.Log("[BES] Screen pointer click detected!");
                                    continuePressed = true;
                                }
                                yield return null;
                            }
                            continueButton.gameObject.SetActive(false);
                        }
                        else
                        {
                            bool clickedAnywhere = false;
                            float waitStartTime = Time.unscaledTime;
                            while (!clickedAnywhere)
                            {
                                if (Time.unscaledTime - waitStartTime > 0.2f && IsPointerClicked())
                                {
                                    Debug.Log("[BES] Screen pointer click detected (no continueButton)!");
                                    clickedAnywhere = true;
                                }
                                yield return null;
                            }
                        }
                    }
                }

                // 1.5f. Flash white and fade out gradually at the very end of all videos
                if (fadeImg != null)
                {
                    fadeImg.gameObject.SetActive(true);
                    fadeImg.texture = null;
                    fadeImg.color = Color.white;

                    // Clean up video player now
                    globalVideoPlayer.Stop();
                    globalVideoPlayer.clip = null;
                    globalVideoPlayer.targetTexture = null;
                    videoOverlayImage.texture = null;
                    if (videoRT != null) { videoRT.Release(); Destroy(videoRT); videoRT = null; }
                    videoOverlayImage.gameObject.SetActive(false);

                    yield return FadeOverlay(fadeImg, 1f, 0f, 0.8f);
                    Destroy(fadeImg.gameObject);
                }
            }

            // 2. Fly in all cards one by one (the 10-cards show screen)
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
            if (globalVideoPlayer != null)
            {
                globalVideoPlayer.Stop();
                globalVideoPlayer.clip = null;
                globalVideoPlayer.targetTexture = null;
            }
            if (videoOverlayImage != null)
            {
                videoOverlayImage.texture = null;
                videoOverlayImage.gameObject.SetActive(false);
            }
            if (videoRT != null)
            {
                videoRT.Release();
                Destroy(videoRT);
                videoRT = null;
            }
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

            // Restore top bar UI
            if (coinsButton != null) coinsButton.gameObject.SetActive(true);
            if (gemsButton != null) gemsButton.gameObject.SetActive(true);
            if (appleCurrencyBar != null) appleCurrencyBar.SetActive(true);
            if (closeButton != null) closeButton.SetActive(true);

            if (globalAppleBar != null) globalAppleBar.SetActive(true);
            if (globalGemsBar != null) globalGemsBar.SetActive(true);
            if (globalCoinsBar != null) globalCoinsBar.SetActive(true);
            if (globalCloseButton != null) globalCloseButton.SetActive(true);

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

        private RawImage CreateTransitionOverlay()
        {
            var videoParent = videoOverlayImage != null ? videoOverlayImage.transform.parent : null;
            if (videoParent == null) return null;

            GameObject fadeObj = new GameObject("GachaFadeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            fadeObj.transform.SetParent(videoParent, false);
            
            // Set as sibling just after videoOverlayImage so it renders on top of the video but below continueButton
            if (videoOverlayImage != null)
            {
                fadeObj.transform.SetSiblingIndex(videoOverlayImage.transform.GetSiblingIndex() + 1);
            }

            RectTransform rt = fadeObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            RawImage fadeImg = fadeObj.GetComponent<RawImage>();
            fadeImg.color = new Color(0f, 0f, 0f, 0f);
            fadeImg.raycastTarget = false;
            return fadeImg;
        }

        private IEnumerator FadeOverlay(RawImage fadeImg, float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
                fadeImg.color = new Color(fadeImg.color.r, fadeImg.color.g, fadeImg.color.b, alpha);
                yield return null;
            }
            fadeImg.color = new Color(fadeImg.color.r, fadeImg.color.g, fadeImg.color.b, toAlpha);
        }

        private bool IsPointerClicked()
        {
            try
            {
                // Check raw mouse click (never consumed by UI EventSystem)
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Debug.Log("[BES] Raw mouse click detected!");
                    return true;
                }
                
                // Check raw touchscreen press (never consumed by UI EventSystem)
                if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    Debug.Log("[BES] Raw touch press detected!");
                    return true;
                }

                // Fallback to active pointer state
                if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                {
                    Debug.Log("[BES] Active pointer press detected!");
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BES] Error reading new InputSystem: {ex.Message}");
            }

            // Fallback for legacy input API
            try
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Debug.Log("[BES] Legacy mouse click detected!");
                    return true;
                }
            }
            catch { }

            return false;
        }
    }
}
