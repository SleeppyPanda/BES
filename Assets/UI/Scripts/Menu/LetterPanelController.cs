using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    [Serializable]
    public class LetterEntry
    {
        public string id;
        public string senderName;
        [TextArea(4, 10)] public string body;
        public Sprite senderPortrait;
        public Sprite rewardIcon;
        public string rewardName;
        [Min(1)] public int rewardAmount = 1;
        public bool claimed;
    }

    public class LetterPanelController : MonoBehaviour
    {
        [SerializeField] List<LetterEntry> letters = new();
        [SerializeField, Min(0)] int initialLetterIndex;
        [SerializeField] TMP_Text senderNameText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Image senderPortraitImage;
        [SerializeField] Image rewardIconImage;
        [SerializeField] TMP_Text rewardText;
        [SerializeField] Button claimButton;
        [SerializeField] GameObject claimedState;
        [SerializeField] GameObject emptyState;
        [SerializeField] bool saveClaimedState = true;
        [SerializeField] string saveKeyPrefix = "BES.LetterClaimed.";
        [SerializeField] UnityEvent<string> onLetterClaimed;

        int currentIndex;

        void Awake()
        {
            currentIndex = Mathf.Clamp(initialLetterIndex, 0, Mathf.Max(0, letters.Count - 1));
            LoadClaimedStates();
            if (claimButton != null) claimButton.onClick.AddListener(ClaimCurrentLetter);
            Refresh();
        }

        void OnEnable() => Refresh();

        void OnDestroy()
        {
            if (claimButton != null) claimButton.onClick.RemoveListener(ClaimCurrentLetter);
        }

        public void ShowLetter(int index)
        {
            currentIndex = Mathf.Clamp(index, 0, Mathf.Max(0, letters.Count - 1));
            Refresh();
        }

        public void ShowNextLetter()
        {
            if (letters.Count == 0) return;
            ShowLetter((currentIndex + 1) % letters.Count);
        }

        public void ShowPreviousLetter()
        {
            if (letters.Count == 0) return;
            ShowLetter((currentIndex - 1 + letters.Count) % letters.Count);
        }

        public void ClaimCurrentLetter()
        {
            if (currentIndex < 0 || currentIndex >= letters.Count) return;
            var letter = letters[currentIndex];
            if (letter == null || letter.claimed) return;

            letter.claimed = true;
            if (saveClaimedState && !string.IsNullOrWhiteSpace(letter.id))
            {
                PlayerPrefs.SetInt(saveKeyPrefix + letter.id, 1);
                PlayerPrefs.Save();
            }
            onLetterClaimed?.Invoke(letter.id);
            Refresh();
        }

        public void Refresh()
        {
            var hasLetter = currentIndex >= 0 && currentIndex < letters.Count;
            if (emptyState != null) emptyState.SetActive(!hasLetter);
            if (!hasLetter)
            {
                if (claimButton != null) claimButton.interactable = false;
                if (claimedState != null) claimedState.SetActive(false);
                return;
            }

            var letter = letters[currentIndex];
            if (senderNameText != null) senderNameText.text = letter.senderName;
            if (bodyText != null) bodyText.text = letter.body;
            if (senderPortraitImage != null)
            {
                senderPortraitImage.sprite = letter.senderPortrait;
                senderPortraitImage.enabled = letter.senderPortrait != null;
            }
            if (rewardIconImage != null)
            {
                rewardIconImage.sprite = letter.rewardIcon;
                rewardIconImage.enabled = letter.rewardIcon != null;
            }
            if (rewardText != null)
                rewardText.text = string.IsNullOrWhiteSpace(letter.rewardName)
                    ? string.Empty
                    : $"{letter.rewardName}  ×{letter.rewardAmount}";
            if (claimButton != null) claimButton.interactable = !letter.claimed;
            if (claimedState != null) claimedState.SetActive(letter.claimed);
        }

        void LoadClaimedStates()
        {
            if (!saveClaimedState) return;
            foreach (var letter in letters)
            {
                if (letter == null || string.IsNullOrWhiteSpace(letter.id)) continue;
                letter.claimed = PlayerPrefs.GetInt(saveKeyPrefix + letter.id, 0) != 0;
            }
        }
    }
}
