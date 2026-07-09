using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class WishUI : UIScreenBase
    {
        [SerializeField] GachaBannerDefinition banner;
        [SerializeField] TMP_Text bannerText;
        [SerializeField] TMP_Text coinsText;
        [SerializeField] TMP_Text gemsText;
        [SerializeField] TMP_Text resultText;
        [SerializeField] Button wishOneButton;
        [SerializeField] Button wishTenButton;
        [SerializeField] Button closeButton;
        [SerializeField] Transform resultCardsContainer;
        [SerializeField] GameObject resultCardPrefab;

        readonly System.Random rng = new();

        void Awake()
        {
            banner ??= Resources.Load<GachaBannerDefinition>("Data/DefaultGachaBanner");
            EnsureBannerDrops();
            if (root == null)
                root = gameObject;
            Hide();
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
            if (resultText != null)
                resultText.text = "Chọn Wish x1 hoặc Wish x10";
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
            if (banner == null || PlayerWallet.Instance == null)
                return;

            if (banner.drops == null || banner.drops.Count == 0)
            {
                if (resultText != null)
                    resultText.text = "Banner chưa có drop table.";
                return;
            }

            var cost = count == 1 ? banner.singleCostGems : banner.tenPullCostGems;
            if (!PlayerWallet.Instance.TrySpendGems(cost))
            {
                if (resultText != null)
                    resultText.text = "Không đủ gems.";
                return;
            }

            var results = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var entry = RollWithPity();
                results.Add(GachaRewardService.ApplyReward(entry));
            }

            ShowResultCards(results);
            if (resultText != null)
                resultText.text = count == 1 ? "Wish x1 hoàn tất!" : "Wish x10 hoàn tất!";
        }

        void ClearResultCards()
        {
            if (resultCardsContainer == null)
                return;
            for (var i = resultCardsContainer.childCount - 1; i >= 0; i--)
                Destroy(resultCardsContainer.GetChild(i).gameObject);
        }

        void ShowResultCards(List<string> results)
        {
            ClearResultCards();
            if (resultCardsContainer == null)
                return;

            foreach (var r in results)
            {
                GameObject go;
                if (resultCardPrefab != null)
                    go = Instantiate(resultCardPrefab, resultCardsContainer);
                else
                {
                    go = new GameObject("ResultCard");
                    go.transform.SetParent(resultCardsContainer, false);
                    go.AddComponent<RectTransform>().sizeDelta = new Vector2(64, 96);
                    go.AddComponent<Image>().color = new Color(0.2f, 0.18f, 0.28f, 0.95f);
                    var label = new GameObject("Label").AddComponent<TextMeshProUGUI>();
                    label.transform.SetParent(go.transform, false);
                    label.text = r;
                    label.fontSize = 10f;
                    label.alignment = TextAlignmentOptions.Center;
                }

                var text = go.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = r;
            }
        }

        GachaDropEntry RollWithPity()
        {
            if (GachaPityState.Instance != null && GachaPityState.Instance.ShouldForceFiveStar())
            {
                foreach (var drop in banner.drops)
                {
                    if (drop != null && drop.rarity >= 5)
                        return drop;
                }
            }

            return banner.Roll(rng);
        }

        void EnsureBannerDrops()
        {
            if (banner == null)
                return;

            if (banner.drops != null && banner.drops.Count > 0)
                return;

            banner.drops = new System.Collections.Generic.List<GachaDropEntry>
            {
                new() { entryId = "w5", rewardType = GachaRewardType.Weapon, rewardId = "weapon_flame_blade", rarity = 5, weight = 5, displayLabel = "Bane of Flame and Water" },
                new() { entryId = "w4", rewardType = GachaRewardType.Weapon, rewardId = "weapon_void_edge", rarity = 4, weight = 25, displayLabel = "Void Edge" },
                new() { entryId = "w3", rewardType = GachaRewardType.Item, rewardId = "material_ore", itemAmount = 5, rarity = 3, weight = 40, displayLabel = "Ore Bundle" },
                new() { entryId = "c5", rewardType = GachaRewardType.Character, rewardId = "char_limited_01", rarity = 5, weight = 3, displayLabel = "Limited Hero" },
                new() { entryId = "c4", rewardType = GachaRewardType.Character, rewardId = "hero_02", rarity = 4, weight = 27, displayLabel = "Ally A" }
            };
        }
    }
}
