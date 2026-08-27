using System;
using System.Collections.Generic;
using BES.Core;
using BES.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    [Serializable]
    public class MissionCardBinding
    {
        public string missionId;
        public RectTransform root;
        public Image cardImage;
        public Button claimButton;
        public Image claimButtonImage;
        public Sprite normalSprite;
        public Sprite expandedSprite;
        public Sprite claimAvailableSprite;
        public Sprite claimedSprite;
        public string rewardId;
        [Min(0)] public int rewardAmount = 1;
        public Vector2 normalSize = new(189f, 740f);
        public Vector2 expandedSize = new(343f, 944f);
        public bool claimed;
    }

    public class MissionPanelController : MonoBehaviour
    {
        [SerializeField] List<MissionCardBinding> cards = new();
        [SerializeField, Min(0f)] float neighborShift = 72f;
        [Tooltip("Y offset applied only to the currently expanded card. Use a negative value to move it down.")]
        [SerializeField] float expandedYOffset;
        [SerializeField, Min(.01f)] float smoothTime = .11f;
        [SerializeField] bool saveClaimedState = true;
        [SerializeField] UnityEvent<string> onMissionClaimed;

        readonly List<Vector2> basePositions = new();
        readonly List<Vector2> positionVelocities = new();
        readonly List<Vector2> sizeVelocities = new();
        readonly List<int> siblingIndices = new();
        int hoveredIndex = -1;

        void Awake()
        {
            CacheLayout();
            for (var i = 0; i < cards.Count; i++)
            {
                var index = i;
                var card = cards[i];
                if (card == null) continue;
                if (saveClaimedState && !string.IsNullOrWhiteSpace(card.missionId))
                    card.claimed = IsMissionClaimed(card.missionId);
                card.claimButton?.onClick.AddListener(() => Claim(index));
                RefreshClaimState(card);
            }
            ApplyImmediate();
        }

        void OnEnable()
        {
            hoveredIndex = -1;
            if (basePositions.Count == cards.Count) ApplyImmediate();
        }

        void Update()
        {
            if (basePositions.Count != cards.Count) CacheLayout();
            var delta = Time.unscaledDeltaTime;
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card?.root == null) continue;
                var expanded = i == hoveredIndex;
                var targetPosition =
                    basePositions[i] +
                    NeighborOffset(i) +
                    (expanded ? Vector2.up * expandedYOffset : Vector2.zero);
                var targetSize = expanded ? card.expandedSize : card.normalSize;
                var positionVelocity = positionVelocities[i];
                card.root.anchoredPosition = Vector2.SmoothDamp(
                    card.root.anchoredPosition,
                    targetPosition,
                    ref positionVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    delta);
                positionVelocities[i] = positionVelocity;
                var sizeVelocity = sizeVelocities[i];
                card.root.sizeDelta = Vector2.SmoothDamp(
                    card.root.sizeDelta,
                    targetSize,
                    ref sizeVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    delta);
                sizeVelocities[i] = sizeVelocity;
            }
        }

        public void SetHoveredCard(int index)
        {
            if (index < 0 || index >= cards.Count || hoveredIndex == index) return;
            hoveredIndex = index;
            RefreshCardSprites();
            cards[index]?.root?.SetAsLastSibling();
        }

        public void ClearHoveredCard(int index)
        {
            if (hoveredIndex != index) return;
            hoveredIndex = -1;
            RestoreSiblingOrder();
            RefreshCardSprites();
        }

        public void Claim(int index)
        {
            if (index < 0 || index >= cards.Count) return;
            var card = cards[index];
            if (card == null || card.claimed) return;
            card.claimed = true;
            RewardGrantService.Grant(card.rewardId, card.rewardAmount, card.missionId);
            if (saveClaimedState && !string.IsNullOrWhiteSpace(card.missionId))
            {
                SaveClaimedMission(card.missionId);
            }
            RefreshClaimState(card);
            onMissionClaimed?.Invoke(card.missionId);
        }

        void CacheLayout()
        {
            basePositions.Clear();
            positionVelocities.Clear();
            sizeVelocities.Clear();
            siblingIndices.Clear();
            foreach (var card in cards)
            {
                basePositions.Add(card?.root != null ? card.root.anchoredPosition : Vector2.zero);
                positionVelocities.Add(Vector2.zero);
                sizeVelocities.Add(Vector2.zero);
                siblingIndices.Add(card?.root != null ? card.root.GetSiblingIndex() : 0);
            }
        }

        void ApplyImmediate()
        {
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card?.root == null) continue;
                card.root.anchoredPosition = basePositions[i];
                card.root.sizeDelta = card.normalSize;
            }
            RestoreSiblingOrder();
            RefreshCardSprites();
        }

        Vector2 NeighborOffset(int index)
        {
            if (hoveredIndex < 0 || index == hoveredIndex) return Vector2.zero;
            var direction = index < hoveredIndex ? -1f : 1f;
            var distance = Mathf.Abs(index - hoveredIndex);
            var weight = Mathf.Max(.55f, 1f - (distance - 1f) * .15f);
            return Vector2.right * direction * neighborShift * weight;
        }

        void RefreshCardSprites()
        {
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card?.cardImage == null) continue;
                card.cardImage.sprite =
                    i == hoveredIndex && card.expandedSprite != null
                        ? card.expandedSprite
                        : card.normalSprite;
            }
        }

        static void RefreshClaimState(MissionCardBinding card)
        {
            if (card.claimButton != null) card.claimButton.interactable = !card.claimed;
            if (card.claimButtonImage != null)
            {
                var sprite = card.claimed ? card.claimedSprite : card.claimAvailableSprite;
                if (sprite != null) card.claimButtonImage.sprite = sprite;
                card.claimButtonImage.color = card.claimed
                    ? new Color(1f, 1f, 1f, .65f)
                    : Color.white;
            }
        }

        void RestoreSiblingOrder()
        {
            for (var i = 0; i < cards.Count; i++)
                if (cards[i]?.root != null)
                    cards[i].root.SetSiblingIndex(siblingIndices[i]);
        }

        bool IsMissionClaimed(string missionId)
        {
            var savedClaims = GameManager.Instance?.Save?.Current?.claimedMissionIds;
            return savedClaims != null && savedClaims.Contains(missionId);
        }

        static void SaveClaimedMission(string missionId)
        {
            var save = GameManager.Instance?.Save?.Current;
            if (save == null || string.IsNullOrWhiteSpace(missionId))
                return;
            save.claimedMissionIds ??= new List<string>();
            if (!save.claimedMissionIds.Contains(missionId))
                save.claimedMissionIds.Add(missionId);
            GameManager.Instance?.SaveGame();
        }
    }
}
