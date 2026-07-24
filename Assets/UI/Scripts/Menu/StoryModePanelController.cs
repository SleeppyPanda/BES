using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public enum StoryPartyPhase { Main, Selecting }

    [Serializable]
    public class StoryPartySlotBinding
    {
        public Button button;
        public Image portrait;
        public Image elementIcon;
        public TMP_Text nameText;
        public TMP_Text levelText;
        public GameObject emptyState;
    }

    [Serializable]
    public class StoryRosterCardBinding
    {
        public Button button;
        public Image portrait;
        public Image elementIcon;
        public TMP_Text nameText;
        public TMP_Text levelText;
        public GameObject selectedState;
    }

    [Serializable]
    public class StoryRequirementImageBinding
    {
        public GameObject root;
        public Image requirementImage;
        public GameObject satisfiedState;
    }

    public class StoryModePanelController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] MenuContentDatabase database;
        [SerializeField] MenuNavigator navigator;
        [SerializeField, Min(1)] int requiredPartySize = 4;
        [SerializeField, Min(0)] int stageIndex;

        [Header("Story panels")]
        [SerializeField] GameObject beforeSelectionPanel;
        [SerializeField] GameObject characterSelectionPanel;

        [Header("Chapter display")]
        [SerializeField] Image[] chapterBackgrounds;
        [SerializeField] TMP_Text[] chapterTitles;
        [SerializeField] TMP_Text[] chapterSummaries;

        [Header("Before selection")]
        [SerializeField] Button openSelectionButton;
        [SerializeField] Button beforeBackButton;
        [SerializeField] List<StoryPartySlotBinding> beforeSlots = new();
        [SerializeField] List<StoryRequirementImageBinding> storyRequirements = new();

        [Header("Character selection")]
        [SerializeField] Button selectionBackButton;
        [SerializeField] Button confirmPartyButton;
        [SerializeField] TMP_Text selectionRequirementText;
        [SerializeField] List<StoryRosterCardBinding> rosterCards = new();
        [SerializeField] List<StoryPartySlotBinding> selectionSlots = new();

        [SerializeField] UnityEvent<List<string>> onPartyConfirmed;

        readonly List<CharacterEntry> selectedParty = new();
        StoryPartyPhase phase;
        int chapterIndex;
        int targetSlotIndex;

        public StoryPartyPhase Phase => phase;
        public IReadOnlyList<CharacterEntry> SelectedParty => selectedParty;

        void Awake()
        {
            EnsureFixedPartySlots();
            BindButtons();
            SelectChapter(chapterIndex);
            ShowPhase(StoryPartyPhase.Main);
        }

        void OnEnable()
        {
            SelectChapter(chapterIndex);
            RefreshAll();
        }

        void BindButtons()
        {
            Add(openSelectionButton, OpenFirstAvailableSlot);
            Add(beforeBackButton, BackToHome);
            Add(selectionBackButton, CloseCharacterSelection);
            Add(confirmPartyButton, ConfirmParty);

            for (var i = 0; i < rosterCards.Count; i++)
            {
                var index = i;
                Add(rosterCards[i].button, () => SelectCharacterForTargetSlot(index));
            }
            BindSlotSelection(beforeSlots);
            BindSlotSelection(selectionSlots);
        }

        static void Add(Button button, UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        void BindSlotSelection(List<StoryPartySlotBinding> slots)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var index = i;
                Add(slots[i].button, () => OpenCharacterSelection(index));
            }
        }

        public void SelectChapter(int index)
        {
            if (database == null || database.storyChapters.Count == 0) return;
            chapterIndex = Mathf.Clamp(index, 0, database.storyChapters.Count - 1);
            var chapter = database.storyChapters[chapterIndex];
            foreach (var image in chapterBackgrounds) if (image != null) image.sprite = chapter.background;
            foreach (var title in chapterTitles) if (title != null) title.text = chapter.title;
            foreach (var summary in chapterSummaries) if (summary != null) summary.text = chapter.summary;
        }

        public void ShowPhase(StoryPartyPhase next)
        {
            phase = next;
            if (beforeSelectionPanel != null) beforeSelectionPanel.SetActive(true);
            if (characterSelectionPanel != null) characterSelectionPanel.SetActive(next == StoryPartyPhase.Selecting);
            RefreshAll();
        }

        public void OpenCharacterSelection(int slotIndex)
        {
            EnsureFixedPartySlots();
            targetSlotIndex = Mathf.Clamp(slotIndex, 0, requiredPartySize - 1);
            ShowPhase(StoryPartyPhase.Selecting);
            if (characterSelectionPanel != null) characterSelectionPanel.transform.SetAsLastSibling();
        }

        void OpenFirstAvailableSlot()
        {
            EnsureFixedPartySlots();
            var empty = selectedParty.FindIndex(character => character == null);
            OpenCharacterSelection(empty >= 0 ? empty : 0);
        }

        public void CloseCharacterSelection() => ShowPhase(StoryPartyPhase.Main);

        void SelectCharacterForTargetSlot(int rosterIndex)
        {
            if (database == null || rosterIndex < 0 || rosterIndex >= database.characters.Count) return;
            EnsureFixedPartySlots();
            var character = database.characters[rosterIndex];
            var existing = selectedParty.FindIndex(x => x != null && (x == character || x.id == character.id));
            if (existing >= 0 && existing != targetSlotIndex) selectedParty[existing] = null;
            selectedParty[targetSlotIndex] = character;
            RefreshAll();
            CloseCharacterSelection();
        }

        public void RemovePartyMember(int slotIndex)
        {
            EnsureFixedPartySlots();
            if (slotIndex >= 0 && slotIndex < selectedParty.Count) selectedParty[slotIndex] = null;
            RefreshAll();
        }

        public void ConfirmParty()
        {
            if (!MeetsPartyRequirements()) return;
            onPartyConfirmed?.Invoke(CurrentIds());
            ShowPhase(StoryPartyPhase.Main);
        }

        void BackToHome() => navigator?.Back();

        List<string> CurrentIds()
        {
            var result = new List<string>();
            foreach (var character in selectedParty) if (character != null) result.Add(character.id);
            return result;
        }

        void RefreshAll()
        {
            RefreshSlots(beforeSlots);
            RefreshSlots(selectionSlots);
            RefreshRoster();
            var valid = MeetsPartyRequirements();
            if (confirmPartyButton != null)
            {
                confirmPartyButton.interactable = valid;
                confirmPartyButton.gameObject.SetActive(valid);
            }
            RefreshStoryRequirementImages();
            var status = BuildRequirementStatus();
            if (selectionRequirementText != null) selectionRequirementText.text = status;
        }

        void RefreshStoryRequirementImages()
        {
            var stage = CurrentStage();
            var requirements = stage?.partyRequirements;
            for (var i = 0; i < storyRequirements.Count; i++)
            {
                var binding = storyRequirements[i];
                var hasRequirement = requirements != null && i < requirements.Count &&
                                     requirements[i] != null &&
                                     !string.IsNullOrWhiteSpace(requirements[i].attributeId);
                binding.root?.SetActive(hasRequirement);
                if (!hasRequirement) continue;

                var requirement = requirements[i];
                if (binding.requirementImage != null)
                    binding.requirementImage.sprite = requirement.icon;
                if (binding.satisfiedState != null)
                {
                    var count = 0;
                    foreach (var character in selectedParty)
                        if (HasAttribute(character, requirement.attributeId)) count++;
                    binding.satisfiedState.SetActive(count >= Mathf.Max(1, requirement.minimumCount));
                }
            }
        }

        public bool MeetsPartyRequirements()
        {
            if (AssignedCharacterCount() != requiredPartySize) return false;
            var stage = CurrentStage();
            if (stage == null) return true;
            foreach (var requirement in stage.partyRequirements)
            {
                if (requirement == null || string.IsNullOrWhiteSpace(requirement.attributeId)) continue;
                var count = 0;
                foreach (var character in selectedParty)
                    if (HasAttribute(character, requirement.attributeId)) count++;
                if (count < Mathf.Max(1, requirement.minimumCount)) return false;
            }
            return true;
        }

        string BuildRequirementStatus()
        {
            var lines = new List<string> { $"Party: {AssignedCharacterCount()}/{requiredPartySize}" };
            var stage = CurrentStage();
            if (stage != null)
            {
                foreach (var requirement in stage.partyRequirements)
                {
                    if (requirement == null || string.IsNullOrWhiteSpace(requirement.attributeId)) continue;
                    var count = 0;
                    foreach (var character in selectedParty)
                        if (HasAttribute(character, requirement.attributeId)) count++;
                    var needed = Mathf.Max(1, requirement.minimumCount);
                    lines.Add($"{requirement.attributeId}: {count}/{needed}");
                }
            }
            return string.Join("   |   ", lines);
        }

        StageEntry CurrentStage()
        {
            if (database == null || database.storyChapters.Count == 0) return null;
            var chapter = database.storyChapters[Mathf.Clamp(chapterIndex, 0, database.storyChapters.Count - 1)];
            if (chapter == null || chapter.stages.Count == 0) return null;
            return chapter.stages[Mathf.Clamp(stageIndex, 0, chapter.stages.Count - 1)];
        }

        static bool HasAttribute(CharacterEntry character, string attributeId)
        {
            if (character == null || character.attributes == null) return false;
            return character.attributes.Exists(value => string.Equals(value?.Trim(), attributeId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        void RefreshSlots(List<StoryPartySlotBinding> slots)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var character = i < selectedParty.Count ? selectedParty[i] : null;
                Apply(slots[i], character);
            }
        }

        void RefreshRoster()
        {
            for (var i = 0; i < rosterCards.Count; i++)
            {
                var card = rosterCards[i];
                var character = database != null && i < database.characters.Count ? database.characters[i] : null;
                if (card.button != null) card.button.gameObject.SetActive(character != null);
                if (character == null) continue;
                if (card.portrait != null) card.portrait.sprite = character.portrait;
                if (card.elementIcon != null) card.elementIcon.sprite = character.elementIcon;
                if (card.nameText != null) card.nameText.text = character.displayName;
                if (card.levelText != null) card.levelText.text = $"Lv. {character.level}";
                if (card.selectedState != null)
                    card.selectedState.SetActive(selectedParty.Exists(x => x != null && (x == character || x.id == character.id)));
            }
        }

        void EnsureFixedPartySlots()
        {
            while (selectedParty.Count < requiredPartySize) selectedParty.Add(null);
            if (selectedParty.Count > requiredPartySize)
                selectedParty.RemoveRange(requiredPartySize, selectedParty.Count - requiredPartySize);
        }

        int AssignedCharacterCount()
        {
            var count = 0;
            foreach (var character in selectedParty) if (character != null) count++;
            return count;
        }

        static void Apply(StoryPartySlotBinding slot, CharacterEntry character)
        {
            if (slot.portrait != null) { slot.portrait.sprite = character?.portrait; slot.portrait.enabled = character != null; }
            if (slot.elementIcon != null) { slot.elementIcon.sprite = character?.elementIcon; slot.elementIcon.enabled = character != null; }
            if (slot.nameText != null) slot.nameText.text = character?.displayName ?? string.Empty;
            if (slot.levelText != null) slot.levelText.text = character == null ? string.Empty : $"Lv. {character.level}";
            if (slot.emptyState != null) slot.emptyState.SetActive(character == null);
        }
    }
}
