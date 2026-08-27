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
    public class PartySlotView
    {
        public Button button;
        public Image portrait;
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
        readonly List<CharacterEntry> rosterCharacters = new();
        int activeSlot;
        int chapterIndex;

        void Start()
        {
            ResolveDatabase();
            if (rosterToggleButton != null) rosterToggleButton.onClick.AddListener(ToggleRoster);
            if (startButton != null) startButton.onClick.AddListener(StartStory);
            if (backButton != null) backButton.onClick.AddListener(() => navigator?.Back());
            if (database != null)
            {
                database = ChapterOneStoryRuntime.Apply(database);
                database = ChapterTwoStoryRuntime.Apply(database);
            }
            BuildRoster();
            SelectChapter(0);
            RefreshParty();
        }

        void ApplyStoryRuntime(int index)
        {
            if (index == 0)
                database = ChapterOneStoryRuntime.Apply(database);
            else if (index == 1)
                database = ChapterTwoStoryRuntime.Apply(database);
        }

        public void SelectChapter(int index)
        {
            if (database == null || database.storyChapters.Count == 0) return;
            if (TurnBattleUI.IsPlayModeBattle)
            {
                ApplyPlayModeHeader();
                return;
            }

            database = ChapterOneStoryRuntime.Apply(database);
            chapterIndex = Mathf.Clamp(index, 0, database.storyChapters.Count - 1);
            ApplyStoryRuntime(chapterIndex);
            var chapter = database.storyChapters[chapterIndex];
            if (chapterBackground != null) chapterBackground.sprite = chapter.background;
            if (chapterTitle != null) chapterTitle.text = chapter.title;
            if (chapterSummary != null) chapterSummary.text = chapter.summary;
        }

        void BuildRoster()
        {
            ResolveDatabase();
            if (database == null || rosterRoot == null) return;
            RebuildRosterCharacters();
            for (var i = 0; i < rosterRoot.childCount; i++)
            {
                var slot = rosterRoot.GetChild(i);
                var character = i < rosterCharacters.Count ? rosterCharacters[i] : null;
                slot.gameObject.SetActive(character != null);
                if (character == null) continue;

                var captured = character;
                var button = slot.GetComponent<Button>() ?? slot.GetComponentInChildren<Button>(true);
                var image = button != null ? button.GetComponent<Image>() : slot.GetComponent<Image>();
                if (image != null) image.sprite = character.cardBackground;
                var portrait = FindDeep(slot, "AssignablePortrait")?.GetComponent<Image>()
                    ?? FindDeep(slot, "Portrait")?.GetComponent<Image>();
                if (portrait != null && portrait != image)
                {
                    portrait.sprite = character.cardBackground;
                    portrait.enabled = character.cardBackground != null;
                }
                var label = FindDeep(slot, "CharacterLevel")?.GetComponent<TMP_Text>()
                    ?? FindDeep(slot, "Level")?.GetComponent<TMP_Text>()
                    ?? button.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = $"Lv. {CharacterProgressionState.GetLevel(character.id)}";
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => AssignCharacter(captured));
                }
            }
        }

        public void SelectSlot(int index) { activeSlot = Mathf.Clamp(index, 0, Mathf.Max(0, maxPartySize - 1)); }
        void AssignCharacter(CharacterEntry character)
        {
            party.Remove(character);
            while (party.Count <= activeSlot) party.Add(null);
            party[activeSlot] = character;
            CharacterOwnership.Focus(character.id);
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
                var background = slot.portrait != null ? slot.portrait : slot.button != null ? slot.button.image : null;
                if (background != null) { background.enabled = character != null; background.sprite = character?.cardBackground; }
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
            ResolveDatabase();
            BuildRoster();
            if (TurnBattleUI.IsPlayModeBattle)
            {
                ApplyPlayModeHeader();
            }
            else
            {
                SelectChapter(chapterIndex);
            }
        }

        void ResolveDatabase()
        {
            if (database == null)
            {
                database = Resources.Load<MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
                if (database == null)
                    database = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
#endif
            }

            database?.EnsureDefaultPlayModeStages();
        }

        void RebuildRosterCharacters()
        {
            rosterCharacters.Clear();
            foreach (var character in CharacterOwnership.GetOwnedEntries(database))
            {
                if (character != null && character.playable && !rosterCharacters.Contains(character))
                    rosterCharacters.Add(character);
            }
        }

        void ApplyPlayModeHeader()
        {
            if (chapterTitle != null) chapterTitle.text = "CHỌN ĐỘI PLAY MODE";
            if (chapterSummary != null) chapterSummary.text = "Chọn tối thiểu 1 nhân vật đã sở hữu để vào trận.";
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root == null || string.IsNullOrWhiteSpace(name)) return null;
            if (root.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var result = FindDeep(root.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
