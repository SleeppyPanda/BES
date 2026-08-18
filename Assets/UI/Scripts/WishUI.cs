using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class WishUI : UIScreenBase
    {
        enum CurrencyMode { Money, Gem }

        [SerializeField] GachaBannerDefinition banner;
        [SerializeField] TMP_Text bannerText;
        [SerializeField] TMP_Text coinsText;
        [SerializeField] TMP_Text gemsText;
        [SerializeField] TMP_Text resultText;
        [SerializeField] Button moneyButton;
        [SerializeField] Button gemButton;
        [SerializeField] Button wishOneButton;
        [SerializeField] Button wishTenButton;
        [SerializeField] Button closeButton;
        [SerializeField] Transform resultCardsContainer;
        [SerializeField] GameObject resultCardPrefab;
        [SerializeField] GameObject controlsRoot;
        [SerializeField] float cardFlyDuration = 0.42f;
        [SerializeField] float cardStaggerDelay = 0.1f;
        [SerializeField] Vector2 cardSpacing = new Vector2(160f, 210f);
        [SerializeField] Vector2 cardSize = new Vector2(108f, 180f);

        readonly System.Random rng = new System.Random();
        CurrencyMode currencyMode = CurrencyMode.Gem;
        bool isAnimating;

        void Awake()
        {
            banner ??= Resources.Load<GachaBannerDefinition>("Data/DefaultGachaBanner");
            EnsureBannerDrops();
            if (root == null)
                root = gameObject;

            Hide();
            if (moneyButton != null) moneyButton.onClick.AddListener(() => SetCurrency(CurrencyMode.Money));
            if (gemButton != null) gemButton.onClick.AddListener(() => SetCurrency(CurrencyMode.Gem));
            if (wishOneButton != null) wishOneButton.onClick.AddListener(() => Pull(1));
            if (wishTenButton != null) wishTenButton.onClick.AddListener(() => Pull(10));
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        void OnEnable()
        {
            if (PlayerWallet.Instance != null)
                PlayerWallet.Instance.WalletChanged += RefreshWallet;
        }

        void OnDisable()
        {
            if (PlayerWallet.Instance != null)
                PlayerWallet.Instance.WalletChanged -= RefreshWallet;
        }

        public override void Refresh()
        {
            if (bannerText != null)
                bannerText.text = banner != null ? banner.displayName : "Character Wish";

            RefreshWallet();
            ClearResultCards();
            RefreshCurrencyButtons();
            SetControlsVisible(true);
            if (resultText != null)
                resultText.text = "Select Wish x1 or Wish x10";
        }

        void RefreshWallet()
        {
            if (coinsText != null && PlayerWallet.Instance != null)
                coinsText.text = $"Money {PlayerWallet.Instance.Coins}";
            if (gemsText != null && PlayerWallet.Instance != null)
                gemsText.text = $"GEM {PlayerWallet.Instance.Gems}";
        }

        void Pull(int count)
        {
            if (isAnimating || banner == null || PlayerWallet.Instance == null)
                return;

            if (banner.drops == null || banner.drops.Count == 0)
            {
                if (resultText != null)
                    resultText.text = "Banner drop table is empty.";
                return;
            }

            var cost = GetCost(count);
            if (!TrySpend(cost))
            {
                if (resultText != null)
                    resultText.text = currencyMode == CurrencyMode.Gem ? "Not enough GEM." : "Not enough Money.";
                return;
            }

            var entries = new List<GachaDropEntry>();
            var labels = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var entry = RollWithPity();
                entries.Add(entry);
                labels.Add(GachaRewardService.ApplyReward(entry));
            }

            StartCoroutine(ShowResultCardsRoutine(entries, labels));
        }

        void ClearResultCards()
        {
            if (resultCardsContainer == null)
                return;

            for (var i = resultCardsContainer.childCount - 1; i >= 0; i--)
                Destroy(resultCardsContainer.GetChild(i).gameObject);
        }

        IEnumerator ShowResultCardsRoutine(List<GachaDropEntry> entries, List<string> labels)
        {
            isAnimating = true;
            SetControlsVisible(false);
            ClearResultCards();

            if (resultCardsContainer == null)
            {
                isAnimating = false;
                SetControlsVisible(true);
                yield break;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var go = CreateResultCard();
                if (go == null)
                    continue;

                var rect = go.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = cardSize;
                    var target = GetCardTargetPosition(i, entries.Count);
                    var start = target + (i < 5 ? new Vector2(0f, 560f) : new Vector2(0f, -560f));
                    rect.anchoredPosition = start;
                    StartCoroutine(AnimateCard(rect, start, target));
                }

                var label = i < labels.Count ? labels[i] : string.Empty;
                var card = go.GetComponent<GachaCardUI>();
                if (card != null)
                    card.Setup(entries[i], label);
                else
                {
                    var text = go.GetComponentInChildren<TMP_Text>();
                    if (text != null)
                        text.text = label;
                }

                yield return new WaitForSeconds(cardStaggerDelay);
            }

            yield return new WaitForSeconds(cardFlyDuration);
            if (resultText != null)
                resultText.text = entries.Count == 1 ? "Wish x1 complete!" : "Wish x10 complete!";
            RefreshWallet();
            SetControlsVisible(true);
            isAnimating = false;
        }

        GameObject CreateResultCard()
        {
            if (resultCardPrefab != null)
                return Instantiate(resultCardPrefab, resultCardsContainer);

            Debug.LogWarning("[BES] WishUI resultCardPrefab is missing. Assign UIGachaCard prefab in Unity.");
            return null;
        }

        IEnumerator AnimateCard(RectTransform rect, Vector2 start, Vector2 target)
        {
            var elapsed = 0f;
            while (elapsed < cardFlyDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / cardFlyDuration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                rect.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
                yield return null;
            }

            rect.anchoredPosition = target;
        }

        Vector2 GetCardTargetPosition(int index, int count)
        {
            if (count == 1)
                return Vector2.zero;

            var column = index % 5;
            var row = index / 5;
            var x = (column - 2f) * cardSpacing.x;
            var y = row == 0 ? cardSpacing.y * 0.5f : -cardSpacing.y * 0.5f;
            return new Vector2(x, y);
        }

        GachaDropEntry RollWithPity()
        {
            var isWeapon = banner != null && banner.isWeaponBanner;
            GachaDropEntry drop;
            if (GachaPityState.Instance != null && GachaPityState.Instance.ShouldForceFiveStar(isWeapon))
            {
                drop = banner.drops.Find(d => d != null && d.rarity >= 5);
                if (drop == null)
                    drop = new GachaDropEntry { entryId = "c5_01", rewardType = isWeapon ? GachaRewardType.Weapon : GachaRewardType.Character, rewardId = isWeapon ? "weapon_flame_blade" : "char_limited_01", rarity = 5, weight = 4, displayLabel = isWeapon ? "Bane of Flame and Water" : "Limited Hero" };
            }
            else
            {
                drop = banner.Roll(rng);
            }

            if (drop == null)
                return null;

            if (drop.rarity == 5)
            {
                if (isWeapon)
                {
                    if (rng.Next(0, 100) < 70)
                    {
                        return new GachaDropEntry
                        {
                            entryId = "c5_standard_weapon",
                            rewardType = GachaRewardType.Weapon,
                            rewardId = "weapon_flame_blade",
                            rarity = 5,
                            displayLabel = "Bane of Flame and Water"
                        };
                    }
                }
                else
                {
                    var pityState = GachaPityState.Instance;
                    var guaranteed = pityState != null && pityState.ConsecutiveOffRates >= 2;

                    if (guaranteed)
                    {
                        pityState?.ResetOffRates();
                        return drop;
                    }
                    else
                    {
                        if (rng.Next(0, 100) < 80)
                        {
                            pityState?.IncrementOffRates();
                            return new GachaDropEntry
                            {
                                entryId = "c5_standard_char",
                                rewardType = GachaRewardType.Character,
                                rewardId = "hero_01",
                                rarity = 5,
                                displayLabel = "Đau hơn NYC bạn"
                            };
                        }
                        else
                        {
                            pityState?.ResetOffRates();
                        }
                    }
                }
            }
            else if (drop.rarity == 4)
            {
                if (isWeapon)
                {
                    if (rng.Next(0, 100) < 70)
                    {
                        return new GachaDropEntry
                        {
                            entryId = "c4_standard_weapon",
                            rewardType = GachaRewardType.Weapon,
                            rewardId = "weapon_void_edge",
                            rarity = 4,
                            displayLabel = "Void Edge"
                        };
                    }
                }
                else
                {
                    if (rng.Next(0, 100) < 80)
                    {
                        var choice = rng.Next(0, 2);
                        if (choice == 0)
                        {
                            return new GachaDropEntry
                            {
                                entryId = "c4_standard_char1",
                                rewardType = GachaRewardType.Character,
                                rewardId = "hero_03",
                                rarity = 4,
                                displayLabel = "Pháp sư trung hoa"
                            };
                        }
                        else
                        {
                            return new GachaDropEntry
                            {
                                entryId = "c4_standard_char2",
                                rewardType = GachaRewardType.Character,
                                rewardId = "hero_04",
                                rarity = 4,
                                displayLabel = "Hỗ trợ tâm lý"
                            };
                        }
                    }
                }
            }

            return drop;
        }

        void SetCurrency(CurrencyMode mode)
        {
            currencyMode = mode;
            RefreshCurrencyButtons();
        }

        void RefreshCurrencyButtons()
        {
            if (moneyButton != null)
                moneyButton.interactable = currencyMode != CurrencyMode.Money;
            if (gemButton != null)
                gemButton.interactable = currencyMode != CurrencyMode.Gem;
        }

        int GetCost(int count)
        {
            if (currencyMode == CurrencyMode.Money)
                return count == 1 ? banner.singleCostCoins : banner.tenPullCostCoins;
            return count == 1 ? banner.singleCostGems : banner.tenPullCostGems;
        }

        bool TrySpend(int cost)
        {
            return currencyMode == CurrencyMode.Money
                ? PlayerWallet.Instance.TrySpendCoins(cost)
                : PlayerWallet.Instance.TrySpendGems(cost);
        }

        void SetControlsVisible(bool visible)
        {
            if (controlsRoot != null)
            {
                controlsRoot.SetActive(visible);
                return;
            }

            if (moneyButton != null) moneyButton.gameObject.SetActive(visible);
            if (gemButton != null) gemButton.gameObject.SetActive(visible);
            if (wishOneButton != null) wishOneButton.gameObject.SetActive(visible);
            if (wishTenButton != null) wishTenButton.gameObject.SetActive(visible);
            if (closeButton != null) closeButton.gameObject.SetActive(visible);
        }

        void EnsureBannerDrops()
        {
            if (banner == null || (banner.drops != null && banner.drops.Count > 0))
                return;

            banner.drops = new List<GachaDropEntry>
            {
                new GachaDropEntry { entryId = "c5_01", rewardType = GachaRewardType.Character, rewardId = "char_limited_01", rarity = 5, weight = 4, displayLabel = "Limited Hero" },
                new GachaDropEntry { entryId = "c4_01", rewardType = GachaRewardType.Character, rewardId = "hero_01", rarity = 4, weight = 12, displayLabel = "Hero 01" },
                new GachaDropEntry { entryId = "c4_02", rewardType = GachaRewardType.Character, rewardId = "hero_02", rarity = 4, weight = 12, displayLabel = "Hero 02" },
                new GachaDropEntry { entryId = "c4_03", rewardType = GachaRewardType.Character, rewardId = "hero_03", rarity = 4, weight = 12, displayLabel = "Hero 03" },
                new GachaDropEntry { entryId = "c4_04", rewardType = GachaRewardType.Character, rewardId = "hero_04", rarity = 4, weight = 12, displayLabel = "Hero 04" },
                new GachaDropEntry { entryId = "c3_01", rewardType = GachaRewardType.Character, rewardId = "hero_05", rarity = 3, weight = 20, displayLabel = "Hero 05" },
                new GachaDropEntry { entryId = "i3_01", rewardType = GachaRewardType.Item, rewardId = "gacha_item_01", itemAmount = 1, rarity = 3, weight = 40, displayLabel = "Gacha Item 01" }
            };
        }
    }
}
