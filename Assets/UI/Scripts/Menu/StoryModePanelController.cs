using System;
using System.Collections.Generic;
using BES.Core;
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

        [Header("Story panels")]
        [SerializeField] GameObject beforeSelectionPanel;
        [SerializeField] GameObject characterSelectionPanel;
        [SerializeField] DialogueSequenceUI storyDialogueUI;
        [SerializeField] bool playChapterIntroOnce = true;

        [Header("Chapter display")]
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
        readonly List<CharacterEntry> ownedRoster = new();
        StoryPartyPhase phase;
        int chapterIndex;
        int targetSlotIndex;
        string currentStageId;
        bool chapterIntroPlayed;

        public StoryPartyPhase Phase => phase;
        public IReadOnlyList<CharacterEntry> SelectedParty => selectedParty;

        void Awake()
        {
            EnsureFixedPartySlots();
            HideLegacyStoryProgressUi();
            BindButtons();
            database = ChapterOneStoryRuntime.Apply(database);
            SelectChapter(chapterIndex);
            ShowPhase(StoryPartyPhase.Main);
        }

        void OnEnable()
        {
            EnsureDialogueUI();
            HideLegacyStoryProgressUi();
            database = ChapterOneStoryRuntime.Apply(database);
            LoadStoryState();
            SelectChapter(chapterIndex);
            TryPlayChapterIntro();
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
            database = ChapterOneStoryRuntime.Apply(database);
            chapterIndex = Mathf.Clamp(index, 0, database.storyChapters.Count - 1);
            var chapter = database.storyChapters[chapterIndex];
            foreach (var title in chapterTitles) if (title != null) title.text = chapter.title;
            foreach (var summary in chapterSummaries) if (summary != null) summary.text = chapter.summary;
            EnsureCurrentStageId();
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
            RebuildOwnedRoster();
            if (rosterIndex < 0 || rosterIndex >= ownedRoster.Count) return;
            EnsureFixedPartySlots();
            var character = ownedRoster[rosterIndex];
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
            TurnBattleUI.SelectedPartyCharacterIds = CurrentIds();
            TurnBattleUI.IsPlayModeBattle = false;
            var stage = CurrentStage();
            TurnBattleUI.ActiveStageId = stage?.id;
            SaveStoryState(stage);
            onPartyConfirmed?.Invoke(CurrentIds());
            ShowPhase(StoryPartyPhase.Main);
            var hasCombat = StageHasCombat(stage);
            if (stage?.preBattleDialogue != null && stage.preBattleDialogue.beats.Count > 0 && storyDialogueUI != null)
            {
                storyDialogueUI.Play(stage.preBattleDialogue, hasCombat ? OpenBattle : CompleteStoryOnlyStage);
            }
            else if (!hasCombat)
            {
                CompleteStoryOnlyStage();
            }
            else
            {
                OpenBattle();
            }
        }

        void OpenBattle() => navigator?.Open(MenuScreenId.Battle);

        void CompleteStoryOnlyStage()
        {
            CompleteStoryBattle();
            ShowPhase(StoryPartyPhase.Main);
            RefreshAll();
        }

        void TryPlayChapterIntro()
        {
            if (playChapterIntroOnce && chapterIntroPlayed) return;
            if (database == null || database.storyChapters.Count == 0 || storyDialogueUI == null) return;
            var chapter = database.storyChapters[Mathf.Clamp(chapterIndex, 0, database.storyChapters.Count - 1)];
            if (chapter?.introDialogue == null || chapter.introDialogue.beats.Count == 0) return;
            chapterIntroPlayed = true;
            storyDialogueUI.Play(chapter.introDialogue);
        }

        void EnsureDialogueUI()
        {
            if (storyDialogueUI != null) return;
            storyDialogueUI = FindAnyObjectByType<DialogueSequenceUI>(FindObjectsInactive.Include);
            if (storyDialogueUI == null)
                storyDialogueUI = DialogueSequenceUI.CreateRuntimeOverlay("RuntimeStoryDialogueUI");
        }

        void BackToHome() => navigator?.Back();

        public void CompleteStoryBattle()
        {
            AdvanceCurrentStageId();
            SaveStoryProgress();
        }

        void LoadStoryState()
        {
            var save = GameManager.Instance?.Save?.Current;
            if (save == null || database == null || database.storyChapters.Count == 0) return;

            chapterIndex = Mathf.Clamp(save.storyChapterIndex, 0, database.storyChapters.Count - 1);
            var chapter = database.storyChapters[chapterIndex];
            currentStageId = !string.IsNullOrWhiteSpace(save.activeStoryStageId) &&
                             chapter?.stages != null &&
                             chapter.stages.Exists(x => x != null && x.id == save.activeStoryStageId)
                ? save.activeStoryStageId
                : FirstStageId(chapter);

            RestoreSavedStoryParty(save.storyPartyCharacterIds);
        }

        void RestoreSavedStoryParty(List<string> ids)
        {
            if (ids == null || ids.Count == 0 || database == null) return;
            EnsureFixedPartySlots();
            for (var i = 0; i < selectedParty.Count; i++) selectedParty[i] = null;
            for (var i = 0; i < ids.Count && i < selectedParty.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(ids[i])) continue;
                selectedParty[i] = database.FindCharacter(ids[i]);
            }
        }

        void SaveStoryProgress()
        {
            var save = GameManager.Instance?.Save?.Current;
            if (save != null)
            {
                save.storyChapterIndex = chapterIndex;
                save.storyStageIndex = CurrentStageOrdinal();
                save.activeStoryStageId = CurrentStage()?.id ?? string.Empty;
            }
            GameManager.Instance?.SaveGame();
        }

        void SaveStoryState(StageEntry stage)
        {
            var save = GameManager.Instance?.Save?.Current;
            if (save == null) return;
            save.storyChapterIndex = chapterIndex;
            save.storyStageIndex = CurrentStageOrdinal();
            save.activeStoryStageId = stage?.id ?? string.Empty;
            save.activeBattleStageId = StageHasCombat(stage) ? stage?.id ?? string.Empty : string.Empty;
            save.activeBattleIsPlayMode = false;
            save.storyPartyCharacterIds = CurrentIds();
            GameManager.Instance?.SaveGame();
        }

        void EnsureCurrentStageId()
        {
            var chapter = CurrentChapter();
            if (chapter?.stages == null || chapter.stages.Count == 0)
            {
                currentStageId = string.Empty;
                return;
            }
            if (!string.IsNullOrWhiteSpace(currentStageId) &&
                chapter.stages.Exists(x => x != null && x.id == currentStageId))
                return;
            currentStageId = FirstStageId(chapter);
        }

        void AdvanceCurrentStageId()
        {
            var chapter = CurrentChapter();
            if (chapter?.stages == null || chapter.stages.Count == 0) return;
            var current = CurrentStageOrdinal();
            var next = Mathf.Min(current + 1, chapter.stages.Count - 1);
            currentStageId = chapter.stages[next]?.id ?? currentStageId;
        }

        int CurrentStageOrdinal()
        {
            var chapter = CurrentChapter();
            if (chapter?.stages == null || chapter.stages.Count == 0) return 0;
            var index = chapter.stages.FindIndex(x => x != null && x.id == currentStageId);
            return Mathf.Max(0, index);
        }

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
            var stage = CurrentStage();
            if (!StageHasCombat(stage)) return true;
            if (AssignedCharacterCount() != RequiredSelectablePartySize(stage)) return false;
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
            var stage = CurrentStage();
            var lines = new List<string> { $"Party: {AssignedCharacterCount()}/{RequiredSelectablePartySize(stage)}" };
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
            var chapter = CurrentChapter();
            if (chapter == null || chapter.stages.Count == 0) return null;
            EnsureCurrentStageId();
            var stage = chapter.stages.Find(x => x != null && x.id == currentStageId);
            return stage ?? chapter.stages[0];
        }

        StoryChapterEntry CurrentChapter()
        {
            if (database == null || database.storyChapters == null || database.storyChapters.Count == 0) return null;
            return database.storyChapters[Mathf.Clamp(chapterIndex, 0, database.storyChapters.Count - 1)];
        }

        static string FirstStageId(StoryChapterEntry chapter)
        {
            return chapter?.stages != null && chapter.stages.Count > 0 ? chapter.stages[0]?.id ?? string.Empty : string.Empty;
        }

        void HideLegacyStoryProgressUi()
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == null) continue;
                if (rect.name == "StoryProgress" || rect.name == "StoryProgressMarker")
                    rect.gameObject.SetActive(false);
            }
        }

        static bool StageHasCombat(StageEntry stage)
        {
            if (stage == null) return false;
            if (stage.battlePhases != null && stage.battlePhases.Count > 0) return true;
            if (stage.enemies != null && stage.enemies.Count > 0) return true;
            return stage.boss != null;
        }

        int RequiredSelectablePartySize(StageEntry stage)
        {
            if (!StageHasCombat(stage)) return 0;
            var fixedAllyCount = 0;
            if (stage?.battlePhases != null && stage.battlePhases.Count > 0)
                fixedAllyCount = Mathf.Max(0, stage.battlePhases[0]?.allies?.Count ?? 0);
            return Mathf.Clamp(requiredPartySize - fixedAllyCount, 0, requiredPartySize);
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
            RebuildOwnedRoster();
            for (var i = 0; i < rosterCards.Count; i++)
            {
                var card = rosterCards[i];
                var character = i < ownedRoster.Count ? ownedRoster[i] : null;
                if (card.button != null) card.button.gameObject.SetActive(character != null);
                if (character == null) continue;
                if (card.button != null && card.button.image != null)
                    card.button.image.sprite = character.cardBackground;
                if (card.portrait != null) card.portrait.sprite = character.portrait;
                if (card.elementIcon != null) card.elementIcon.sprite = character.elementIcon;
                if (card.nameText != null) card.nameText.text = character.displayName;
                if (card.levelText != null) card.levelText.text = $"Lv. {CharacterProgressionState.GetLevel(character.id)}";
                if (card.selectedState != null)
                    card.selectedState.SetActive(selectedParty.Exists(x => x != null && (x == character || x.id == character.id)));
            }
        }

        void RebuildOwnedRoster()
        {
            ownedRoster.Clear();
            var roster = PartyRoster.Instance ?? FindAnyObjectByType<PartyRoster>();
            if (roster == null || database == null) return;
            foreach (var member in roster.GetUnlockedRosterMembers())
            {
                var character = database.FindCharacter(member.characterId);
                if (character != null && !ownedRoster.Contains(character)) ownedRoster.Add(character);
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
            if (slot.levelText != null) slot.levelText.text = character == null ? string.Empty : $"Lv. {CharacterProgressionState.GetLevel(character.id)}";
            if (slot.emptyState != null) slot.emptyState.SetActive(character == null);
        }
    }
}


