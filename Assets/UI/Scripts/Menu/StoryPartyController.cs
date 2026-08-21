using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    [Serializable]
    public class PartySlotView
    {
        public Button button;
        public Image portrait;
        public Image elementIcon;
        public TMP_Text nameText;
        public TMP_Text levelText;
        public GameObject emptyState;
    }

    public class StoryPartyController : MonoBehaviour
    {
        [SerializeField] MenuContentDatabase database;
        [SerializeField] MenuNavigator navigator;
        [SerializeField] Image chapterBackground;
        [SerializeField] TMP_Text chapterTitle;
        [SerializeField] TMP_Text chapterSummary;
        [SerializeField] List<PartySlotView> partySlots = new();
        [SerializeField] Transform rosterRoot;
        [SerializeField] Button characterButtonPrefab;
        [SerializeField] Button rosterToggleButton;
        [SerializeField] GameObject rosterPanel;
        [SerializeField] Button startButton;
        [SerializeField] Button backButton;
        [SerializeField] int maxPartySize = 4;
        [SerializeField] UnityEvent<List<string>> onPartyConfirmed;
        readonly List<CharacterEntry> party = new();
        int activeSlot;
        int chapterIndex;

        void Start()
        {
            if (rosterToggleButton != null) rosterToggleButton.onClick.AddListener(ToggleRoster);
            if (startButton != null) startButton.onClick.AddListener(StartStory);
            if (backButton != null) backButton.onClick.AddListener(() => navigator?.Back());
            BuildRoster();
            SelectChapter(0);
            RefreshParty();
        }

        public void SelectChapter(int index)
        {
            if (database == null || database.storyChapters.Count == 0) return;
            ChapterOneStoryRuntime.Apply(database);
            chapterIndex = Mathf.Clamp(index, 0, database.storyChapters.Count - 1);
            var chapter = database.storyChapters[chapterIndex];
            if (chapterBackground != null) chapterBackground.sprite = chapter.background;
            if (chapterTitle != null) chapterTitle.text = chapter.title;
            if (chapterSummary != null) chapterSummary.text = chapter.summary;
        }

        void BuildRoster()
        {
            if (database == null || rosterRoot == null || characterButtonPrefab == null) return;
            foreach (Transform child in rosterRoot) Destroy(child.gameObject);
            foreach (var character in database.characters)
            {
                var captured = character;
                var button = Instantiate(characterButtonPrefab, rosterRoot);
                var image = button.GetComponent<Image>();
                if (image != null) image.sprite = character.portrait;
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = character.displayName;
                button.onClick.AddListener(() => AssignCharacter(captured));
            }
        }

        public void SelectSlot(int index) { activeSlot = Mathf.Clamp(index, 0, Mathf.Max(0, maxPartySize - 1)); }
        void AssignCharacter(CharacterEntry character)
        {
            party.Remove(character);
            while (party.Count <= activeSlot) party.Add(null);
            party[activeSlot] = character;
            RefreshParty();
        }

        public void RemoveFromSlot(int index)
        {
            if (index >= 0 && index < party.Count) party[index] = null;
            RefreshParty();
        }

        void RefreshParty()
        {
            for (var i = 0; i < partySlots.Count; i++)
            {
                var character = i < party.Count ? party[i] : null;
                var slot = partySlots[i];
                if (slot.portrait != null) { slot.portrait.enabled = character != null; slot.portrait.sprite = character?.portrait; }
                if (slot.elementIcon != null) { slot.elementIcon.enabled = character != null; slot.elementIcon.sprite = character?.elementIcon; }
                if (slot.nameText != null) slot.nameText.text = character?.displayName ?? string.Empty;
                if (slot.levelText != null) slot.levelText.text = character == null ? string.Empty : $"Lv. {CharacterProgressionState.GetLevel(character.id)}";
                slot.emptyState?.SetActive(character == null);
            }
            if (startButton != null) startButton.interactable = party.Exists(x => x != null);
        }

        void ToggleRoster() { if (rosterPanel != null) rosterPanel.SetActive(!rosterPanel.activeSelf); }
        void StartStory()
        {
            var ids = new List<string>();
            foreach (var member in party) if (member != null) ids.Add(member.id);
            TurnBattleUI.SelectedPartyCharacterIds = ids;
            onPartyConfirmed?.Invoke(ids);
            navigator?.Open(MenuScreenId.Battle);
        }

        void OnEnable()
        {
            if (TurnBattleUI.IsPlayModeBattle)
            {
                if (chapterTitle != null) chapterTitle.text = "CHỌN ĐỘI HÌNH BÍ CẢNH";
                if (chapterSummary != null) chapterSummary.text = "Hãy chọn những nhân vật mạnh nhất để chinh phục bí cảnh.";
            }
            else
            {
                SelectChapter(chapterIndex);
            }
        }
    }
}
