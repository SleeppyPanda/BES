using System;
using System.Collections.Generic;
using BES.Core;
using BES.Gameplay;
using BES.UI;
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
        public TMP_Text levelText;
        public GameObject emptyState;
    }

    [Serializable]
    public class StoryRosterCardBinding
    {
        public Button button;
        public Image portrait;
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
        [Tooltip("Optional direct buttons for each party slot. Element 0 opens slot 0, Element 1 opens slot 1, etc.")]
        [SerializeField] List<Button> openSelectionButtonsBySlot = new();
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
        [Header("Debug")]
        [SerializeField] bool debugStoryFlow = true;

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
            LogStory($"Awake active={gameObject.activeSelf} database={database != null} storyDialogueUI={storyDialogueUI != null}");
            EnsureEditableDialogueUI();
            EnsureFixedPartySlots();
            EnsureSelectionSlotBindings();
            HideLegacyStoryProgressUi();
            BindButtons();
            database = ChapterOneStoryRuntime.Apply(database);
            database = ChapterTwoStoryRuntime.Apply(database);
            LogStory($"Awake after runtime apply chapters={database?.storyChapters?.Count ?? 0}");
            SelectChapter(chapterIndex);
            ShowPhase(StoryPartyPhase.Main);
        }
        void OnEnable()
        {
            if (!Application.isPlaying) return;
            LogStory("OnEnable begin");
            GameEvents.OnPartyChanged += RefreshAll;
            EnsureDialogueUI();
            EnsureSelectionSlotBindings();
            HideLegacyStoryProgressUi();
            database = ChapterOneStoryRuntime.Apply(database);
            database = ChapterTwoStoryRuntime.Apply(database);
            
            var save = GameManager.Instance?.Save?.Current;
            if (save != null)
            {
                chapterIndex = Mathf.Clamp(save.storyChapterIndex, 0, database.storyChapters.Count - 1);
                LogStory($"Loaded save chapterIndex={chapterIndex} activeStage='{save.activeStoryStageId}' storyStageIndex={save.storyStageIndex} party='{JoinIds(save.storyPartyCharacterIds)}'");
            }
            else
            {
                LogStory("No save object available");
            }
            
            ShowPhase(StoryPartyPhase.Main);
            SelectChapter(chapterIndex);
            TryPlayChapterIntro();
        }

        void OnDisable()
        {
            GameEvents.OnPartyChanged -= RefreshAll;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && !Application.isPlaying)
                    EnsureEditableDialogueUI();
            };
        }
#endif

        void BindButtons()
        {
            AutoResolveStorySelectionButtons();
            Add(openSelectionButton, OpenFirstAvailableSlot);
            BindOpenSelectionButtonsBySlot();
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

        void BindOpenSelectionButtonsBySlot()
        {
            if (openSelectionButtonsBySlot == null) return;
            for (var i = 0; i < openSelectionButtonsBySlot.Count; i++)
            {
                var index = i;
                Add(openSelectionButtonsBySlot[i], () => OpenCharacterSelection(index));
            }
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
            LogStory($"SelectChapter request index={index}");
            if (database == null || database.storyChapters.Count == 0)
            {
                LogStory("SelectChapter blocked: database/chapter list missing");
                return;
            }
            database = ChapterOneStoryRuntime.Apply(database);
            database = ChapterTwoStoryRuntime.Apply(database);
            
            var newChapterIndex = Mathf.Clamp(index, 0, database.storyChapters.Count - 1);

            chapterIndex = newChapterIndex;
            ApplyStoryRuntime(chapterIndex);
            LogStory($"SelectChapter applied chapterIndex={chapterIndex} chapterId='{database.storyChapters[chapterIndex]?.id}' stages={database.storyChapters[chapterIndex]?.stages?.Count ?? 0} introBeats={database.storyChapters[chapterIndex]?.introDialogue?.beats?.Count ?? 0}");

            // SaveData is the source of truth so old test data cannot skip the first story scene.
            var save = GameManager.Instance?.Save?.Current;
            if (save != null &&
                save.storyChapterIndex == chapterIndex &&
                !string.IsNullOrWhiteSpace(save.activeStoryStageId) &&
                database.storyChapters[chapterIndex].stages.Exists(stage =>
                    stage != null && string.Equals(stage.id, save.activeStoryStageId, StringComparison.OrdinalIgnoreCase)))
            {
                currentStageId = save.activeStoryStageId;
                LogStory($"SelectChapter using saved stage '{currentStageId}'");
            }
            else
            {
                currentStageId = string.Empty;
                LogStory("SelectChapter no valid saved stage -> use first stage");
            }

            EnsureCurrentStageId();
            LogStory($"SelectChapter currentStageId='{currentStageId}'");

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
            LogStory($"ShowPhase {next} beforePanel={beforeSelectionPanel?.activeSelf} selectionPanel={characterSelectionPanel?.activeSelf}");
            RefreshAll();
        }

        public void OpenCharacterSelection(int slotIndex)
        {
            EnsureFixedPartySlots();
            targetSlotIndex = Mathf.Clamp(slotIndex, 0, requiredPartySize - 1);
            if (!string.IsNullOrWhiteSpace(RequiredCharacterIdForSlot(targetSlotIndex)))
                rosterSortMode = StoryRosterSortMode.RequiredCharacter;
            LogStory($"OpenCharacterSelection requestedSlot={slotIndex} targetSlot={targetSlotIndex} sort={rosterSortMode} required='{RequiredCharacterIdForSlot(targetSlotIndex)}'");
            ShowPhase(StoryPartyPhase.Selecting);
            if (characterSelectionPanel != null) characterSelectionPanel.transform.SetAsLastSibling();
        }

        void SetRosterSortMode(StoryRosterSortMode mode)
        {
            rosterSortMode = mode;
            LogStory($"SetRosterSortMode {mode}");
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
            LogStory($"SelectCharacterForTargetSlot rosterIndex={rosterIndex} targetSlot={targetSlotIndex}");
            RebuildOwnedRoster();
            ApplyRosterSortAndFilter();
            if (rosterIndex < 0 || rosterIndex >= ownedRoster.Count)
            {
                LogStory($"SelectCharacterForTargetSlot blocked: rosterIndex out of range ownedRoster={ownedRoster.Count}");
                return;
            }
            EnsureFixedPartySlots();
            var character = ownedRoster[rosterIndex];
            var existing = selectedParty.FindIndex(x => x != null && (x == character || x.id == character.id));
            LogStory($"Selected roster character='{character?.id}' existingSlot={existing} beforeParty='{JoinSelectedParty()}'");

            if (existing == targetSlotIndex)
            {
                selectedParty[targetSlotIndex] = null;
                LogStory($"Toggle off character='{character?.id}' from slot={targetSlotIndex}");
                RefreshAll();
                return;
            }

            if (existing >= 0) selectedParty[existing] = null;
            selectedParty[targetSlotIndex] = character;
            CharacterOwnership.Focus(character.id);
            LogStory($"Assigned character='{character?.id}' to slot={targetSlotIndex} afterParty='{JoinSelectedParty()}'");
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
            var meets = MeetsPartyRequirements();
            LogStory($"ConfirmParty meets={meets} stage='{CurrentStage()?.id}' party='{JoinSelectedParty()}' currentIds='{JoinIds(CurrentIds())}'");
            if (!meets) return;
            BeginCurrentStoryStage();
        }

        void BeginCurrentStoryStage()
        {
            var ids = CurrentIds();
            TurnBattleUI.SelectedPartyCharacterIds = ids;
            TurnBattleUI.IsPlayModeBattle = false;
            var stage = CurrentStage();
            TurnBattleUI.ActiveStageId = stage?.id;
            SaveStoryState(stage);
            LogStory($"BeginCurrentStoryStage stage='{stage?.id}' hasCombat={StageHasCombat(stage)} selectedIds='{JoinIds(ids)}' preBattleBeats={stage?.preBattleDialogue?.beats?.Count ?? 0} battlePhases={stage?.battlePhases?.Count ?? 0}");
            onPartyConfirmed?.Invoke(ids);
            ShowPhase(StoryPartyPhase.Main);
            var hasCombat = StageHasCombat(stage);
            if (stage?.preBattleDialogue != null && stage.preBattleDialogue.beats.Count > 0 && storyDialogueUI != null)
            {
                LogStory("Playing preBattleDialogue before battle/story completion");
                storyDialogueUI.Play(stage.preBattleDialogue, hasCombat ? OpenBattle : CompleteStoryOnlyStage);
            }
            else if (!hasCombat)
            {
                LogStory("No combat -> CompleteStoryOnlyStage");
                CompleteStoryOnlyStage();
            }
            else
            {
                LogStory("No preBattleDialogue -> OpenBattle");
                OpenBattle();
            }
        }

        void OpenBattle()
        {
            LogStory($"OpenBattle activeStage='{TurnBattleUI.ActiveStageId}' selectedIds='{JoinIds(TurnBattleUI.SelectedPartyCharacterIds)}'");
            navigator?.Open(MenuScreenId.Battle);
        }

        void CompleteStoryOnlyStage()
        {
            CompleteStoryBattle();
            SelectChapter(chapterIndex);
            ShowPhase(StoryPartyPhase.Main);
            RefreshAll();
        }

        void TryPlayChapterIntro()
        {
            LogStory($"TryPlayChapterIntro playOnce={playChapterIntroOnce} introPlayed={chapterIntroPlayed} currentStage='{currentStageId}'");
            if (playChapterIntroOnce && chapterIntroPlayed)
            {
                LogStory("TryPlayChapterIntro blocked: already played");
                return;
            }
            EnsureDialogueUI();
            if (database == null || database.storyChapters.Count == 0 || storyDialogueUI == null)
            {
                LogStory($"TryPlayChapterIntro blocked: database={database != null} chapters={database?.storyChapters?.Count ?? 0} storyDialogueUI={storyDialogueUI != null}");
                return;
            }
            var chapter = database.storyChapters[Mathf.Clamp(chapterIndex, 0, database.storyChapters.Count - 1)];
            if (chapter?.introDialogue == null || chapter.introDialogue.beats.Count == 0)
            {
                LogStory($"TryPlayChapterIntro blocked: no intro beats chapter='{chapter?.id}'");
                return;
            }
            var firstStageId = chapter.stages != null && chapter.stages.Count > 0 ? chapter.stages[0]?.id : string.Empty;
            if (!string.IsNullOrWhiteSpace(firstStageId) &&
                !string.IsNullOrWhiteSpace(currentStageId) &&
                !string.Equals(currentStageId, firstStageId, StringComparison.OrdinalIgnoreCase))
            {
                LogStory($"TryPlayChapterIntro blocked: currentStage '{currentStageId}' != firstStage '{firstStageId}'");
                return;
            }
            
            chapterIntroPlayed = true;
            LogStory($"TryPlayChapterIntro PLAY chapter='{chapter.id}' beats={chapter.introDialogue.beats.Count}");
            storyDialogueUI.Play(chapter.introDialogue, ContinueAfterChapterIntro);
        }

        void ContinueAfterChapterIntro()
        {
            LogStory($"ContinueAfterChapterIntro currentStage='{CurrentStage()?.id}' hasCombat={StageHasCombat(CurrentStage())}");
            ShowPhase(StoryPartyPhase.Main);
            RefreshAll();

            if (StageHasCombat(CurrentStage()))
            {
                OpenFirstAvailableSlot();
            }
            else
            {
                BeginCurrentStoryStage();
            }
        }

        void EnsureDialogueUI()
        {
            if (storyDialogueUI != null) return;
            var existing = FindDeep(transform, "StoryDialogueUI");
            if (existing != null)
                storyDialogueUI = existing.GetComponent<DialogueSequenceUI>();

            if (storyDialogueUI == null)
                Debug.LogWarning("[BES] StoryModePanelController is missing storyDialogueUI. Assign the prefab StoryDialogueUI object in Unity.");
        }

        [ContextMenu("BES/Ensure Editable Story Dialogue UI")]
        void EnsureEditableDialogueUI()
        {
            if (storyDialogueUI == null)
            {
                var existing = FindDeep(transform, "StoryDialogueUI");
                if (existing != null)
                    storyDialogueUI = existing.GetComponent<DialogueSequenceUI>();
            }

            if (storyDialogueUI == null && !Application.isPlaying)
            {
                Debug.LogWarning("[BES] StoryDialogueUI is not present in the prefab. Create it in Unity and assign it to StoryModePanelController.storyDialogueUI.");
                return;
            }

            if (storyDialogueUI != null && !Application.isPlaying)
            {
                storyDialogueUI.gameObject.SetActive(false);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(storyDialogueUI);
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        void BackToHome() => navigator?.Back();

        public void CompleteStoryBattle()
        {
            LogStory($"CompleteStoryBattle stageBefore='{currentStageId}'");
            AdvanceCurrentStageId();
            LogStory($"CompleteStoryBattle stageAfter='{currentStageId}'");
            SaveStoryProgress();
        }

        public void FailStoryBattle(string failedStageId = null)
        {
            LogStory($"FailStoryBattle failedStageId='{failedStageId}' currentStageBefore='{currentStageId}'");
            var chapter = CurrentChapter();
            if (!string.IsNullOrWhiteSpace(failedStageId) &&
                chapter?.stages != null &&
                chapter.stages.Exists(x => x != null && string.Equals(x.id, failedStageId, StringComparison.OrdinalIgnoreCase)))
            {
                currentStageId = failedStageId;
            }

            SaveStoryState(CurrentStage());
            RefreshAll();
            LogStory($"FailStoryBattle saved currentStage='{currentStageId}'");
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
                var character = database.FindCharacter(ids[i]);
                if (character != null && CharacterOwnership.Owns(character.id))
                    selectedParty[i] = character;
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
            save.activePlayModeStageGroupId = string.Empty;
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
                    result.Add(CharacterIdentity.Canonical(selectedParty[i].id, database));
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
            RefreshDeployInventory();
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
            EnsureRosterCardBindings();
            RebuildOwnedRoster();
            ApplyRosterSortAndFilter();
            LogStory($"RefreshRoster cards={rosterCards.Count} owned={ownedRoster.Count} sort={rosterSortMode} targetSlot={targetSlotIndex}");
            for (var i = 0; i < rosterCards.Count; i++)
            {
                var card = rosterCards[i];
                if (card == null) continue;
                var character = i < ownedRoster.Count ? ownedRoster[i] : null;
                if (card.button != null) card.button.gameObject.SetActive(character != null);
                else if (card.portrait != null) card.portrait.gameObject.SetActive(character != null);

                if (character == null)
                {
                    if (card.portrait != null)
                    {
                        card.portrait.sprite = null;
                        card.portrait.enabled = false;
                    }
                    if (card.levelText != null) card.levelText.text = string.Empty;
                    if (card.selectedState != null) card.selectedState.SetActive(false);
                    continue;
                }

                var characterIndex = i;
                if (card.button != null)
                {
                    card.button.onClick.RemoveAllListeners();
                    card.button.onClick.AddListener(() => SelectCharacterForTargetSlot(characterIndex));
                }

                ForceVisible(card.button?.image);
                if (card.button != null && card.button.image != null)
                    card.button.image.sprite = character.cardBackground;
                if (card.portrait != null)
                {
                    card.portrait.sprite = character.cardBackground;
                    ForceVisible(card.portrait);
                }
                if (card.levelText != null) card.levelText.text = $"Lv. {CharacterProgressionState.GetLevel(character.id)}";
                if (card.selectedState != null)
                    ApplyRosterSelectedState(card, selectedParty.Exists(x => x != null && (x == character || x.id == character.id)));
            }
        }

        void EnsureRosterCardBindings()
        {
            var content = characterSelectionPanel != null
                ? FindDeep(characterSelectionPanel.transform, "RosterContent")
                : null;
            if (content == null) return;

            var resolved = new List<StoryRosterCardBinding>();
            for (var i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                if (child == null) continue;
                if (child.name.IndexOf("RosterCard", StringComparison.OrdinalIgnoreCase) < 0 &&
                    child.GetComponent<Button>() == null &&
                    child.GetComponentInChildren<Button>(true) == null)
                    continue;

                var button = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(true);
                var portraitRoot = FindDeep(child, "AssignablePortrait");
                var levelRoot = FindDeep(child, "CharacterLevel");
                var selectedRoot = FindDeep(child, "SelectedState");

                resolved.Add(new StoryRosterCardBinding
                {
                    button = button,
                    portrait = portraitRoot != null ? portraitRoot.GetComponent<Image>() : child.GetComponent<Image>(),
                    levelText = levelRoot != null ? levelRoot.GetComponent<TMP_Text>() : child.GetComponentInChildren<TMP_Text>(true),
                    selectedState = selectedRoot != null ? selectedRoot.gameObject : null
                });
            }

            if (resolved.Count == 0) return;
            rosterCards = resolved;
        }

        void RebuildOwnedRoster()
        {
            ownedRoster.Clear();
            foreach (var character in CharacterOwnership.GetOwnedEntries(database))
            {
                if (character != null && !ownedRoster.Contains(character))
                    ownedRoster.Add(character);
            }
            LogStory($"RebuildOwnedRoster owned={JoinOwnedRoster()}");
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

        bool IsRequiredCharacter(CharacterEntry character, string requiredIdOrName)
        {
            if (character == null || string.IsNullOrWhiteSpace(requiredIdOrName)) return false;
            return CharacterIdentity.Same(character.id, requiredIdOrName, database) ||
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
                if (slot != null && (slot.button != null || slot.portrait != null || slot.levelText != null || slot.emptyState != null))
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
            var background = slot.portrait != null ? slot.portrait : slot.button != null ? slot.button.image : null;
            if (background != null)
            {
                background.sprite = character?.cardBackground;
                background.enabled = character != null;
                if (character != null) ForceVisible(background);
            }
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

        void LogStory(string message)
        {
            if (!debugStoryFlow) return;
            Debug.Log($"[BES][StoryFlow][{name}] {message}");
        }

        string JoinSelectedParty()
        {
            if (selectedParty == null || selectedParty.Count == 0) return string.Empty;
            var parts = new List<string>();
            for (var i = 0; i < selectedParty.Count; i++)
                parts.Add(selectedParty[i] != null ? $"{i}:{selectedParty[i].id}" : $"{i}:empty");
            return string.Join(", ", parts);
        }

        string JoinOwnedRoster()
        {
            if (ownedRoster == null || ownedRoster.Count == 0) return string.Empty;
            var parts = new List<string>();
            for (var i = 0; i < ownedRoster.Count; i++)
                parts.Add(ownedRoster[i] != null ? ownedRoster[i].id : "null");
            return string.Join(", ", parts);
        }

        static string JoinIds(IReadOnlyList<string> ids)
        {
            return ids == null || ids.Count == 0 ? string.Empty : string.Join(", ", ids);
        }

        void EnsureDeployInventory()
        {
            // UI phải được tạo/gán sẵn trong Unity để tránh runtime làm reset layout prefab.
        }

        void RefreshDeployInventory()
        {
            if (characterSelectionPanel == null || phase != StoryPartyPhase.Selecting) return;
            var row = characterSelectionPanel.transform.Find("DeployInventory/ItemRow");
            if (row == null) return;
            foreach (Transform child in row)
                if (child != null) child.gameObject.SetActive(false);

            var target = targetSlotIndex >= 0 && targetSlotIndex < selectedParty.Count ? selectedParty[targetSlotIndex] : null;
            var characterId = target != null ? target.id : CharacterOwnership.FocusedCharacterId;
            var inventory = GameManager.Instance?.Inventory;
            if (inventory == null || string.IsNullOrEmpty(characterId)) return;

            var added = 0;
            foreach (var pair in inventory.Items)
            {
                if (added >= row.childCount || pair.Value <= 0) continue;
                var definition = inventory.GetDefinition(pair.Key);
                if (definition == null) continue;
                if (definition.itemType != ItemType.Consumable && definition.itemType != ItemType.Quest &&
                    definition.affinityGain == 0 && definition.characterExperience <= 0 &&
                    !pair.Key.Contains("exp", StringComparison.OrdinalIgnoreCase))
                    continue;

                var itemId = pair.Key;
                var slot = row.GetChild(added);
                if (slot == null) continue;
                slot.gameObject.SetActive(true);
                var image = slot.GetComponent<Image>() ?? slot.GetComponentInChildren<Image>(true);
                if (image != null)
                {
                    image.sprite = definition.icon;
                    image.color = Color.white;
                    image.enabled = definition.icon != null;
                }
                var button = slot.GetComponent<Button>() ?? slot.GetComponentInChildren<Button>(true);
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (CharacterOwnership.TryUseInventoryOnCharacter(itemId, characterId))
                            RefreshAll();
                    });
                }
                added++;
            }
        }
    }
}



