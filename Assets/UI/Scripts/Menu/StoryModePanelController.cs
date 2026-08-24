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
    public enum StoryRosterSortMode { CombatPower, Constellation, Quality, RequiredCharacter }

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
        [SerializeField] Button sortCombatPowerButton;
        [SerializeField] Button sortConstellationButton;
        [SerializeField] Button sortQualityButton;
        [SerializeField] Button sortRequiredCharacterButton;
        [SerializeField] StoryRosterSortMode rosterSortMode = StoryRosterSortMode.CombatPower;
        [Tooltip("Optional per-party-slot required character IDs. Used by RequiredCharacter sorting/filter mode. Empty slot = no forced character.")]
        [SerializeField] List<string> requiredCharacterIdsBySlot = new();
        [SerializeField] TMP_Text selectionRequirementText;
        [SerializeField] List<StoryRosterCardBinding> rosterCards = new();
        [SerializeField] List<StoryPartySlotBinding> selectionSlots = new();
        [Header("Chapter navigation")]
        [SerializeField] Button nextChapterButton;
        [SerializeField] Button prevChapterButton;

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

        void ApplyStoryRuntime(int index)
        {
            if (index == 0)
                database = ChapterOneStoryRuntime.Apply(database);
            else if (index == 1)
                database = ChapterTwoStoryRuntime.Apply(database);
        }
        void Awake()
        {
            EnsureFixedPartySlots();
            EnsureSelectionSlotBindings();
            HideLegacyStoryProgressUi();
            BindButtons();
            database = ChapterOneStoryRuntime.Apply(database);
            database = ChapterTwoStoryRuntime.Apply(database);
            SelectChapter(chapterIndex);
            ShowPhase(StoryPartyPhase.Main);
        }
        void OnEnable()
        {
            if (!Application.isPlaying) return;
            EnsureDialogueUI();
            EnsureSelectionSlotBindings();
            HideLegacyStoryProgressUi();
            database = ChapterOneStoryRuntime.Apply(database);
            database = ChapterTwoStoryRuntime.Apply(database);
            
            var save = GameManager.Instance?.Save?.Current;
            if (save != null)
            {
                chapterIndex = Mathf.Clamp(save.storyChapterIndex, 0, database.storyChapters.Count - 1);
            }
            
            SelectChapter(chapterIndex);
            TryPlayChapterIntro();
            ShowPhase(StoryPartyPhase.Main);
        }

        void BindButtons()
        {
            AutoResolveStorySelectionButtons();
            Add(openSelectionButton, OpenFirstAvailableSlot);
            Add(beforeBackButton, BackToHome);
            Add(selectionBackButton, CloseCharacterSelection);
            Add(confirmPartyButton, ConfirmParty);
            Add(sortCombatPowerButton, () => SetRosterSortMode(StoryRosterSortMode.CombatPower));
            Add(sortConstellationButton, () => SetRosterSortMode(StoryRosterSortMode.Constellation));
            Add(sortQualityButton, () => SetRosterSortMode(StoryRosterSortMode.Quality));
            Add(sortRequiredCharacterButton, () => SetRosterSortMode(StoryRosterSortMode.RequiredCharacter));
            Add(nextChapterButton, NextChapter);
            Add(prevChapterButton, PrevChapter);

            for (var i = 0; i < rosterCards.Count; i++)
            {
                var index = i;
                Add(rosterCards[i].button, () => SelectCharacterForTargetSlot(index));
            }
            BindSlotSelection(beforeSlots);
            BindSlotSelection(selectionSlots);
        }

        void AutoResolveStorySelectionButtons()
        {
            var sortFilterUi = characterSelectionPanel != null
                ? characterSelectionPanel.GetComponentInChildren<StorySelectionSortFilterUI>(true)
                : GetComponentInChildren<StorySelectionSortFilterUI>(true);
            if (sortFilterUi != null)
            {
                sortFilterUi.EnsureButtons();
                sortCombatPowerButton = sortFilterUi.SortCombatPowerButton;
                sortConstellationButton = sortFilterUi.SortConstellationButton;
                sortQualityButton = sortFilterUi.SortQualityButton;
            }

            sortCombatPowerButton ??= FindButton("SortCombatPower", "CombatPower", "Chiến Lực", "ChienLuc");
            sortConstellationButton ??= FindButton("SortConstellation", "Constellation", "Tinh Hồn", "TinhHon");
            sortQualityButton ??= FindButton("SortQuality", "Quality", "Phẩm Chất", "PhamChat");
            sortRequiredCharacterButton ??= FindButton("RequiredCharacter", "Required", "Yêu Cầu", "YeuCau");
        }

        Button FindButton(params string[] names)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;
                foreach (var name in names)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (button.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        return button;
                }
            }
            return null;
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
            database = ChapterTwoStoryRuntime.Apply(database);
            
            var newChapterIndex = Mathf.Clamp(index, 0, database.storyChapters.Count - 1);

            // Save old chapter progress ONLY if we are actually switching chapters!
            if (newChapterIndex != chapterIndex)
            {
                PlayerPrefs.SetString($"StoryActiveStageId_{chapterIndex}", currentStageId);
                PlayerPrefs.Save();
            }

            chapterIndex = newChapterIndex;
            ApplyStoryRuntime(chapterIndex);

            // Load progress for the new chapter (with fallback to active save for migration)
            if (PlayerPrefs.HasKey($"StoryActiveStageId_{chapterIndex}"))
            {
                currentStageId = PlayerPrefs.GetString($"StoryActiveStageId_{chapterIndex}");
            }
            else
            {
                var save = GameManager.Instance?.Save?.Current;
                if (save != null && save.storyChapterIndex == chapterIndex && !string.IsNullOrWhiteSpace(save.activeStoryStageId))
                {
                    currentStageId = save.activeStoryStageId;
                }
                else
                {
                    currentStageId = string.Empty;
                }
                PlayerPrefs.SetString($"StoryActiveStageId_{chapterIndex}", currentStageId);
                PlayerPrefs.Save();
            }

            EnsureCurrentStageId();

            // Sync with save data
            var saveObj = GameManager.Instance?.Save?.Current;
            if (saveObj != null)
            {
                saveObj.storyChapterIndex = chapterIndex;
                saveObj.activeStoryStageId = currentStageId;
                saveObj.storyStageIndex = CurrentStageOrdinal();
            }
            GameManager.Instance?.SaveGame();

            if (saveObj != null)
            {
                RestoreSavedStoryParty(saveObj.storyPartyCharacterIds);
            }
            AlignPartyWithFixedAllies();

            var chapter = database.storyChapters[chapterIndex];
            foreach (var title in chapterTitles) if (title != null) title.text = chapter.title;
            foreach (var summary in chapterSummaries) if (summary != null) summary.text = chapter.summary;
            RefreshChapterButtons();
            RefreshAll();
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
            if (!string.IsNullOrWhiteSpace(RequiredCharacterIdForSlot(targetSlotIndex)))
                rosterSortMode = StoryRosterSortMode.RequiredCharacter;
            ShowPhase(StoryPartyPhase.Selecting);
            if (characterSelectionPanel != null) characterSelectionPanel.transform.SetAsLastSibling();
        }

        void SetRosterSortMode(StoryRosterSortMode mode)
        {
            rosterSortMode = mode;
            RefreshRoster();
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
            ApplyRosterSortAndFilter();
            if (rosterIndex < 0 || rosterIndex >= ownedRoster.Count) return;
            EnsureFixedPartySlots();
            var character = ownedRoster[rosterIndex];
            var existing = selectedParty.FindIndex(x => x != null && (x == character || x.id == character.id));

            if (existing == targetSlotIndex)
            {
                selectedParty[targetSlotIndex] = null;
                RefreshAll();
                return;
            }

            if (existing >= 0) selectedParty[existing] = null;
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
            SelectChapter(chapterIndex);
            ShowPhase(StoryPartyPhase.Main);
            RefreshAll();
        }

        void TryPlayChapterIntro()
        {
            if (playChapterIntroOnce && (chapterIntroPlayed || PlayerPrefs.GetInt($"ChapterIntroPlayed_{chapterIndex}", 0) == 1)) return;
            if (database == null || database.storyChapters.Count == 0 || storyDialogueUI == null) return;
            var chapter = database.storyChapters[Mathf.Clamp(chapterIndex, 0, database.storyChapters.Count - 1)];
            if (chapter?.introDialogue == null || chapter.introDialogue.beats.Count == 0) return;
            
            chapterIntroPlayed = true;
            PlayerPrefs.SetInt($"ChapterIntroPlayed_{chapterIndex}", 1);
            PlayerPrefs.Save();
            storyDialogueUI.Play(chapter.introDialogue);
        }

        void EnsureDialogueUI()
        {
            if (storyDialogueUI != null) return;
            storyDialogueUI = FindAnyObjectByType<DialogueSequenceUI>(FindObjectsInactive.Include);
            if (storyDialogueUI == null)
                Debug.LogWarning("[BES] StoryModePanelController is missing storyDialogueUI. Assign DialogueSequenceUI in Unity; runtime UI creation is disabled.");
        }

        void BackToHome() => navigator?.Back();

        public void CompleteStoryBattle()
        {
            AdvanceCurrentStageId();
            SaveStoryProgress();
        }

        public void FailStoryBattle(string failedStageId = null)
        {
            var chapter = CurrentChapter();
            if (!string.IsNullOrWhiteSpace(failedStageId) &&
                chapter?.stages != null &&
                chapter.stages.Exists(x => x != null && string.Equals(x.id, failedStageId, StringComparison.OrdinalIgnoreCase)))
            {
                currentStageId = failedStageId;
            }

            SaveStoryState(CurrentStage());
            RefreshAll();
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

            // Also save to PlayerPrefs persistently
            PlayerPrefs.SetString($"StoryActiveStageId_{chapterIndex}", currentStageId);
            PlayerPrefs.Save();
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
            if (current < chapter.stages.Count - 1)
            {
                currentStageId = chapter.stages[current + 1]?.id ?? currentStageId;
            }
            else
            {
                // Transition to Chapter 2 automatically
                if (database != null && chapterIndex < database.storyChapters.Count - 1)
                {
                    chapterIndex++;
                    var nextChapter = database.storyChapters[chapterIndex];
                    currentStageId = FirstStageId(nextChapter);
                    
                    var saveObj = GameManager.Instance?.Save?.Current;
                    if (saveObj != null)
                    {
                        saveObj.storyChapterIndex = chapterIndex;
                        saveObj.activeStoryStageId = currentStageId;
                        saveObj.storyStageIndex = 0;
                    }
                    GameManager.Instance?.SaveGame();
                    
                    PlayerPrefs.SetString($"StoryActiveStageId_{chapterIndex}", currentStageId);
                    PlayerPrefs.Save();

                    chapterIntroPlayed = false;
                    TryPlayChapterIntro();
                }
            }
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
            var stage = CurrentStage();
            var fixedCount = 0;
            if (stage != null && StageHasCombat(stage) && stage.battlePhases != null && stage.battlePhases.Count > 0 && stage.battlePhases[0]?.allies != null)
            {
                fixedCount = stage.battlePhases[0].allies.Count;
            }

            for (var i = 0; i < selectedParty.Count; i++)
            {
                if (i >= fixedCount && selectedParty[i] != null)
                {
                    result.Add(selectedParty[i].id);
                }
            }
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
            RefreshChapterButtons();
        }

        void RefreshStoryRequirementImages()
        {
            for (var i = 0; i < storyRequirements.Count; i++)
            {
                var binding = storyRequirements[i];
                binding.root?.SetActive(false);
            }
        }

        public bool MeetsPartyRequirements()
        {
            if (!StageHasCombat(CurrentStage())) return true;
            return AssignedCharacterCount() >= 1;
        }

        string BuildRequirementStatus()
        {
            var requiredId = RequiredCharacterIdForSlot(targetSlotIndex);
            if (!string.IsNullOrWhiteSpace(requiredId))
                return $"Yêu cầu ô này: {requiredId}";
            return $"Cần tối thiểu 1 nhân vật để tiếp tục. Đã chọn: {AssignedCharacterCount()}";
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
                if (rect.name == "StoryProgress" || rect.name == "StoryProgressMarker" || rect.name == "ProgressBar")
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
            var stage = CurrentStage();
            var fixedCount = 0;
            if (stage != null && StageHasCombat(stage) && stage.battlePhases != null && stage.battlePhases.Count > 0 && stage.battlePhases[0]?.allies != null)
            {
                fixedCount = stage.battlePhases[0].allies.Count;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var character = i < selectedParty.Count ? selectedParty[i] : null;
                Apply(slots[i], character);

                if (slots[i].button != null)
                {
                    slots[i].button.interactable = (i >= fixedCount);
                }
            }
        }

        void RefreshRoster()
        {
            RebuildOwnedRoster();
            ApplyRosterSortAndFilter();
            for (var i = 0; i < rosterCards.Count; i++)
            {
                var card = rosterCards[i];
                var character = i < ownedRoster.Count ? ownedRoster[i] : null;
                if (card.button != null) card.button.gameObject.SetActive(character != null);
                if (character == null) continue;
                ForceVisible(card.button?.image);
                if (card.button != null && card.button.image != null)
                    card.button.image.sprite = character.cardBackground;
                if (card.portrait != null)
                {
                    card.portrait.sprite = character.portrait;
                    ForceVisible(card.portrait);
                }
                if (card.elementIcon != null)
                {
                    card.elementIcon.sprite = character.elementIcon;
                    card.elementIcon.enabled = character.elementIcon != null;
                    if (character.elementIcon != null) ForceVisible(card.elementIcon);
                }
                if (card.nameText != null) card.nameText.text = character.displayName;
                if (card.levelText != null) card.levelText.text = $"Lv. {CharacterProgressionState.GetLevel(character.id)}";
                if (card.selectedState != null)
                    ApplyRosterSelectedState(card, selectedParty.Exists(x => x != null && (x == character || x.id == character.id)));
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

        void ApplyRosterSortAndFilter()
        {
            if (rosterSortMode == StoryRosterSortMode.RequiredCharacter)
            {
                var requiredId = RequiredCharacterIdForSlot(targetSlotIndex);
                if (!string.IsNullOrWhiteSpace(requiredId))
                {
                    ownedRoster.RemoveAll(character => !IsRequiredCharacter(character, requiredId));
                    return;
                }
            }

            ownedRoster.Sort((left, right) => SortValue(right).CompareTo(SortValue(left)));
        }

        int SortValue(CharacterEntry character)
        {
            if (character == null) return int.MinValue;
            return rosterSortMode switch
            {
                StoryRosterSortMode.Constellation => CharacterProgressionState.GetConstellation(character.id),
                StoryRosterSortMode.Quality => character.quality,
                StoryRosterSortMode.RequiredCharacter => IsRequiredCharacter(character, RequiredCharacterIdForSlot(targetSlotIndex)) ? 1 : 0,
                _ => character.combatPower
            };
        }

        string RequiredCharacterIdForSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < requiredCharacterIdsBySlot.Count)
                return requiredCharacterIdsBySlot[slotIndex]?.Trim() ?? string.Empty;

            var stage = CurrentStage();
            var requirements = stage?.partyRequirements;
            if (requirements != null && slotIndex >= 0 && slotIndex < requirements.Count)
            {
                var value = requirements[slotIndex]?.attributeId;
                if (!string.IsNullOrWhiteSpace(value) && IsCharacterIdOrName(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        bool IsCharacterIdOrName(string value)
        {
            if (database?.characters == null || string.IsNullOrWhiteSpace(value)) return false;
            foreach (var character in database.characters)
                if (IsRequiredCharacter(character, value))
                    return true;
            return false;
        }

        static bool IsRequiredCharacter(CharacterEntry character, string requiredIdOrName)
        {
            if (character == null || string.IsNullOrWhiteSpace(requiredIdOrName)) return false;
            return string.Equals(character.id, requiredIdOrName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(character.displayName, requiredIdOrName, StringComparison.OrdinalIgnoreCase);
        }

        void EnsureFixedPartySlots()
        {
            while (selectedParty.Count < requiredPartySize) selectedParty.Add(null);
            if (selectedParty.Count > requiredPartySize)
                selectedParty.RemoveRange(requiredPartySize, selectedParty.Count - requiredPartySize);
        }

        void EnsureSelectionSlotBindings()
        {
            if (HasAnySlotBinding(selectionSlots)) return;
            selectionSlots = beforeSlots;
        }

        static bool HasAnySlotBinding(List<StoryPartySlotBinding> slots)
        {
            if (slots == null) return false;
            foreach (var slot in slots)
                if (slot != null && (slot.button != null || slot.portrait != null || slot.nameText != null || slot.levelText != null || slot.emptyState != null))
                    return true;
            return false;
        }

        int AssignedCharacterCount()
        {
            var stage = CurrentStage();
            var fixedCount = 0;
            if (stage != null && StageHasCombat(stage) && stage.battlePhases != null && stage.battlePhases.Count > 0 && stage.battlePhases[0]?.allies != null)
            {
                fixedCount = stage.battlePhases[0].allies.Count;
            }

            var count = 0;
            for (var i = fixedCount; i < selectedParty.Count; i++)
            {
                if (selectedParty[i] != null) count++;
            }
            return count;
        }

        void AlignPartyWithFixedAllies()
        {
            EnsureFixedPartySlots();
            var stage = CurrentStage();
            if (stage == null) return;

            var fixedAllies = new List<CharacterEntry>();
            if (StageHasCombat(stage) && stage.battlePhases != null && stage.battlePhases.Count > 0 && stage.battlePhases[0]?.allies != null)
            {
                foreach (var ally in stage.battlePhases[0].allies)
                {
                    if (ally == null) continue;
                    var character = database?.FindCharacter(ally.id);
                    if (character != null)
                        fixedAllies.Add(character);
                }
            }

            var fixedCount = fixedAllies.Count;
            var userSelected = new List<CharacterEntry>();
            foreach (var charEntry in selectedParty)
            {
                if (charEntry != null && !fixedAllies.Exists(x => string.Equals(x.id, charEntry.id, StringComparison.OrdinalIgnoreCase)))
                    userSelected.Add(charEntry);
            }

            for (var i = 0; i < selectedParty.Count; i++)
            {
                if (i < fixedCount)
                {
                    selectedParty[i] = fixedAllies[i];
                }
                else
                {
                    var userIndex = i - fixedCount;
                    selectedParty[i] = userIndex < userSelected.Count ? userSelected[userIndex] : null;
                }
            }
        }

        static void Apply(StoryPartySlotBinding slot, CharacterEntry character)
        {
            if (slot.portrait != null)
            {
                slot.portrait.sprite = character?.portrait;
                slot.portrait.enabled = character != null;
                if (character != null) ForceVisible(slot.portrait);
            }
            if (slot.elementIcon != null)
            {
                slot.elementIcon.sprite = character?.elementIcon;
                slot.elementIcon.enabled = character != null && character.elementIcon != null;
                if (character != null && character.elementIcon != null) ForceVisible(slot.elementIcon);
            }
            if (slot.nameText != null) slot.nameText.text = character?.displayName ?? string.Empty;
            if (slot.levelText != null) slot.levelText.text = character == null ? string.Empty : $"Lv. {CharacterProgressionState.GetLevel(character.id)}";
            if (slot.emptyState != null) slot.emptyState.SetActive(character == null);
        }

        static void ApplyRosterSelectedState(StoryRosterCardBinding card, bool selected)
        {
            if (card?.selectedState == null) return;

            if (card.button != null && card.selectedState == card.button.gameObject)
            {
                ForceVisible(card.button.image);
                return;
            }

            card.selectedState.SetActive(selected);
            var image = card.selectedState.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
                image.color = selected ? new Color(.35f, .35f, .35f, .55f) : new Color(.35f, .35f, .35f, 0f);
            }
            card.selectedState.transform.SetAsLastSibling();
        }

        static void ForceVisible(Image image)
        {
            if (image == null) return;
            var color = image.color;
            if (color.a < .99f)
            {
                color.a = 1f;
                image.color = color;
            }
            image.enabled = true;
        }

        void NextChapter()
        {
            if (database == null || chapterIndex >= database.storyChapters.Count - 1) return;
            SelectChapter(chapterIndex + 1);
            SaveStoryProgress();
            chapterIntroPlayed = false;
            TryPlayChapterIntro();
        }

        void PrevChapter()
        {
            if (chapterIndex <= 0) return;
            SelectChapter(chapterIndex - 1);
            SaveStoryProgress();
            chapterIntroPlayed = false;
            TryPlayChapterIntro();
        }

        void RefreshChapterButtons()
        {
            if (prevChapterButton != null)
                prevChapterButton.gameObject.SetActive(chapterIndex > 0);
            if (nextChapterButton != null)
            {
                bool hasNext = database != null && chapterIndex < database.storyChapters.Count - 1;
                nextChapterButton.gameObject.SetActive(hasNext);
                nextChapterButton.interactable = hasNext;
            }
        }
    }
}



