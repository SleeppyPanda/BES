using System;
using System.Collections.Generic;
using BES.Core;
using BES.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public class PlayPartyPanelController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] MenuContentDatabase database;
        [SerializeField] MenuNavigator navigator;
        [SerializeField, Min(1)] int maxPartySize = 4;
        [SerializeField, Min(1)] int minimumPartySize = 1;

        [Header("Panels")]
        [SerializeField] GameObject beforeSelectionPanel;
        [SerializeField] GameObject characterSelectionPanel;

        [Header("Stage display")]
        [SerializeField] TMP_Text[] titleTexts;
        [SerializeField] TMP_Text[] descriptionTexts;

        [Header("Before selection")]
        [SerializeField] Button openSelectionButton;
        [Tooltip("Optional direct buttons for each party slot. Element 0 opens slot 0, Element 1 opens slot 1, etc.")]
        [SerializeField] List<Button> openSelectionButtonsBySlot = new();
        [SerializeField] Button backButton;
        [SerializeField] Button confirmPartyButton;
        [SerializeField] List<StoryPartySlotBinding> partySlots = new();

        [Header("Character selection")]
        [SerializeField] Button selectionBackButton;
        [SerializeField] Button sortCombatPowerButton;
        [SerializeField] Button sortConstellationButton;
        [SerializeField] Button sortQualityButton;
        [SerializeField] TMP_Text selectionRequirementText;
        [SerializeField] List<StoryRosterCardBinding> rosterCards = new();
        [SerializeField] List<StoryPartySlotBinding> selectionSlots = new();
        [SerializeField] StoryRosterSortMode rosterSortMode = StoryRosterSortMode.CombatPower;

        [Header("Events")]
        [SerializeField] UnityEvent<List<string>> onPartyConfirmed;

        [Header("Debug")]
        [SerializeField] bool debugLogs = true;

        readonly List<CharacterEntry> selectedParty = new();
        readonly List<CharacterEntry> ownedRoster = new();
        int targetSlotIndex;
        bool initialized;

        void Awake() => Initialize();

        void OnEnable()
        {
            Initialize();
            ResolveDatabase();
            EnsurePartySlots();
            RestoreSavedParty();
            ShowMain();
            RefreshAll();
            Log($"OnEnable stage='{TurnBattleUI.ActiveStageId}' group='{TurnBattleUI.ActivePlayModeStageGroupId}' selected='{JoinIds(CurrentIds())}'");
        }

        void Initialize()
        {
            if (initialized) return;
            initialized = true;

            beforeSelectionPanel ??= gameObject;
            ResolveNavigator();
            AutoResolveButtons();

            Add(openSelectionButton, OpenFirstAvailableSlot);
            BindOpenSelectionButtonsBySlot();
            Add(backButton, Back);
            Add(selectionBackButton, ShowMain);
            Add(confirmPartyButton, ConfirmParty);
            Add(sortCombatPowerButton, () => SetRosterSortMode(StoryRosterSortMode.CombatPower));
            Add(sortConstellationButton, () => SetRosterSortMode(StoryRosterSortMode.Constellation));
            Add(sortQualityButton, () => SetRosterSortMode(StoryRosterSortMode.Quality));

            BindSlots(partySlots);
            BindSlots(selectionSlots);

            Log($"Initialize navigator='{NameOf(navigator)}' before='{NameOf(beforeSelectionPanel)}' selection='{NameOf(characterSelectionPanel)}' partySlots={partySlots.Count} selectionSlots={selectionSlots.Count} rosterCards={rosterCards.Count}");
        }

        void ResolveDatabase()
        {
            if (database != null)
            {
                database.EnsureDefaultPlayModeStages();
                return;
            }

            database = Resources.Load<MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
            if (database == null)
                database = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
#endif
            database?.EnsureDefaultPlayModeStages();
        }

        void ResolveNavigator()
        {
            if (navigator != null) return;
            navigator = GetComponentInParent<MenuNavigator>(true);
            if (navigator == null) navigator = FindFirstObjectByType<MenuNavigator>(FindObjectsInactive.Include);
        }

        void AutoResolveButtons()
        {
            var sortFilterUi = characterSelectionPanel != null
                ? characterSelectionPanel.GetComponentInChildren<StorySelectionSortFilterUI>(true)
                : GetComponentInChildren<StorySelectionSortFilterUI>(true);
            if (sortFilterUi != null)
            {
                sortFilterUi.EnsureButtons();
                sortCombatPowerButton ??= sortFilterUi.SortCombatPowerButton;
                sortConstellationButton ??= sortFilterUi.SortConstellationButton;
                sortQualityButton ??= sortFilterUi.SortQualityButton;
            }

            openSelectionButton ??= FindButton("ActiveButton", "OpenSelection", "ChooseParty");
            confirmPartyButton ??= FindButton("ConfirmParty", "BattleButton", "StartButton", "ActiveButton", "Chiến đấu", "Chien dau");
            backButton ??= FindButton("BackButton");
            selectionBackButton ??= characterSelectionPanel != null
                ? FindButtonIn(characterSelectionPanel.transform, "BackButton", "CloseButton")
                : FindButton("BackButton", "CloseButton");
            sortCombatPowerButton ??= FindButton("SortCombatPower", "CombatPower", "Chiến Lực", "ChienLuc");
            sortConstellationButton ??= FindButton("SortConstellation", "Constellation", "Tinh Hồn", "TinhHon");
            sortQualityButton ??= FindButton("SortQuality", "Quality", "Phẩm Chất", "PhamChat");
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

        void BindSlots(List<StoryPartySlotBinding> slots)
        {
            if (slots == null) return;
            for (var i = 0; i < slots.Count; i++)
            {
                var index = i;
                Add(slots[i]?.button, () => OpenCharacterSelection(index));
            }
        }

        public void ShowMain()
        {
            if (beforeSelectionPanel != null) beforeSelectionPanel.SetActive(true);
            if (characterSelectionPanel != null) characterSelectionPanel.SetActive(false);
            RefreshAll();
        }

        public void OpenCharacterSelection(int slotIndex)
        {
            EnsurePartySlots();
            targetSlotIndex = Mathf.Clamp(slotIndex, 0, maxPartySize - 1);
            if (beforeSelectionPanel != null) beforeSelectionPanel.SetActive(true);
            if (characterSelectionPanel != null)
            {
                characterSelectionPanel.SetActive(true);
                characterSelectionPanel.transform.SetAsLastSibling();
            }
            Log($"OpenCharacterSelection slot={targetSlotIndex}");
            RefreshAll();
        }

        void OpenFirstAvailableSlot()
        {
            EnsurePartySlots();
            var emptyIndex = selectedParty.FindIndex(x => x == null);
            OpenCharacterSelection(emptyIndex >= 0 ? emptyIndex : 0);
        }

        void SetRosterSortMode(StoryRosterSortMode mode)
        {
            rosterSortMode = mode;
            Log($"SetRosterSortMode {mode}");
            RefreshRoster();
        }

        void ConfirmParty()
        {
            var ids = CurrentIds();
            var valid = ids.Count >= minimumPartySize;
            Log($"ConfirmParty valid={valid} count={ids.Count} stage='{TurnBattleUI.ActiveStageId}' group='{TurnBattleUI.ActivePlayModeStageGroupId}' ids='{JoinIds(ids)}'");
            if (!valid) return;

            TurnBattleUI.SelectedPartyCharacterIds = ids;
            TurnBattleUI.IsPlayModeBattle = true;
            SavePlayBattleState(ids);
            onPartyConfirmed?.Invoke(ids);

            if (navigator == null)
            {
                Log("ConfirmParty blocked: navigator is NULL.");
                return;
            }
            navigator.Open(MenuScreenId.Battle);
        }

        void Back()
        {
            Log("Back");
            navigator?.Back();
        }

        void SavePlayBattleState(List<string> ids)
        {
            var save = GameManager.Instance?.Save?.Current;
            if (save == null) return;
            save.activeBattleStageId = TurnBattleUI.ActiveStageId;
            save.activeBattleIsPlayMode = true;
            save.activePlayModeStageGroupId = TurnBattleUI.ActivePlayModeStageGroupId;
            save.storyPartyCharacterIds = new List<string>(ids);
            GameManager.Instance.SaveGame();
        }

        void RestoreSavedParty()
        {
            EnsurePartySlots();
            for (var i = 0; i < selectedParty.Count; i++)
                selectedParty[i] = null;

            var ids = TurnBattleUI.SelectedPartyCharacterIds;
            if ((ids == null || ids.Count == 0) && GameManager.Instance?.Save?.Current?.storyPartyCharacterIds != null)
                ids = GameManager.Instance.Save.Current.storyPartyCharacterIds;

            if (ids == null || database == null) return;
            for (var i = 0; i < ids.Count && i < selectedParty.Count; i++)
            {
                var character = database.FindCharacter(ids[i]);
                if (character != null && CharacterOwnership.Owns(character.id))
                    selectedParty[i] = character;
            }
        }

        void RefreshAll()
        {
            ResolveDatabase();
            EnsurePartySlots();
            EnsureSlotBindings();
            RefreshStageDisplay();
            RefreshSlots(partySlots);
            RefreshSlots(selectionSlots);
            RefreshRoster();

            var count = CurrentIds().Count;
            var valid = count >= minimumPartySize;
            if (confirmPartyButton != null)
            {
                confirmPartyButton.gameObject.SetActive(valid);
                confirmPartyButton.interactable = valid;
            }
            if (selectionRequirementText != null)
                selectionRequirementText.text = $"Chọn tối thiểu {minimumPartySize} nhân vật. Đã chọn: {count}/{maxPartySize}";
        }

        void RefreshStageDisplay()
        {
            var stage = CurrentStage();
            var title = stage != null ? stage.title : "Chọn Đội Play Mode";
            var description = stage != null
                ? stage.description
                : "Chọn tối thiểu 1 nhân vật đã sở hữu để vào trận.";

            if (titleTexts != null)
                foreach (var text in titleTexts)
                    if (text != null) text.text = title;
            if (descriptionTexts != null)
                foreach (var text in descriptionTexts)
                    if (text != null) text.text = description;
        }

        StageEntry CurrentStage()
        {
            ResolveDatabase();
            if (database == null || string.IsNullOrWhiteSpace(TurnBattleUI.ActiveStageId)) return null;

            foreach (var stages in AllPlayStageLists())
            {
                var found = stages?.Find(stage => stage != null && string.Equals(stage.id, TurnBattleUI.ActiveStageId, StringComparison.OrdinalIgnoreCase));
                if (found != null) return found;
            }
            return null;
        }

        IEnumerable<List<StageEntry>> AllPlayStageLists()
        {
            yield return database.resourceStages;
            yield return database.sanctumStages;
            yield return database.weaponStages;
            if (database.playModeStageGroups == null) yield break;
            foreach (var group in database.playModeStageGroups)
                if (group != null) yield return group.stages;
        }

        void RefreshSlots(List<StoryPartySlotBinding> slots)
        {
            if (slots == null) return;
            for (var i = 0; i < slots.Count; i++)
            {
                var character = i < selectedParty.Count ? selectedParty[i] : null;
                ApplySlot(slots[i], character);
            }
        }

        void RefreshRoster()
        {
            EnsureRosterCardBindings();
            RebuildOwnedRoster();
            SortRoster();
            Log($"RefreshRoster cards={rosterCards.Count} owned={ownedRoster.Count} sort={rosterSortMode}");

            for (var i = 0; i < rosterCards.Count; i++)
            {
                var card = rosterCards[i];
                if (card == null) continue;
                var character = i < ownedRoster.Count ? ownedRoster[i] : null;
                var root = card.button != null ? card.button.gameObject : card.portrait != null ? card.portrait.gameObject : null;
                if (root != null) root.SetActive(character != null);

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

                var index = i;
                if (card.button != null)
                {
                    card.button.onClick.RemoveAllListeners();
                    card.button.onClick.AddListener(() => SelectRosterCharacter(index));
                }

                ApplyRosterCard(card, character);
            }
        }

        void SelectRosterCharacter(int rosterIndex)
        {
            RebuildOwnedRoster();
            SortRoster();
            if (rosterIndex < 0 || rosterIndex >= ownedRoster.Count) return;
            EnsurePartySlots();

            var character = ownedRoster[rosterIndex];
            var existing = selectedParty.FindIndex(x => x != null && CharacterIdentity.Same(x.id, character.id, database));
            if (existing == targetSlotIndex)
            {
                selectedParty[targetSlotIndex] = null;
                Log($"Toggle off '{character.id}' from slot={targetSlotIndex}");
            }
            else
            {
                if (existing >= 0) selectedParty[existing] = null;
                selectedParty[targetSlotIndex] = character;
                CharacterOwnership.Focus(character.id);
                Log($"Assign '{character.id}' to slot={targetSlotIndex}");
            }

            RefreshAll();
            ShowMain();
        }

        void EnsureRosterCardBindings()
        {
            var content = characterSelectionPanel != null ? FindDeep(characterSelectionPanel.transform, "RosterContent") : null;
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

            if (resolved.Count > 0) rosterCards = resolved;
        }

        void RebuildOwnedRoster()
        {
            ownedRoster.Clear();
            foreach (var character in CharacterOwnership.GetOwnedEntries(database))
            {
                if (character != null && character.playable && !ownedRoster.Exists(x => CharacterIdentity.Same(x.id, character.id, database)))
                    ownedRoster.Add(character);
            }
        }

        void SortRoster()
        {
            ownedRoster.Sort((left, right) => SortValue(right).CompareTo(SortValue(left)));
        }

        int SortValue(CharacterEntry character)
        {
            if (character == null) return int.MinValue;
            return rosterSortMode switch
            {
                StoryRosterSortMode.Constellation => CharacterProgressionState.GetConstellation(character.id),
                StoryRosterSortMode.Quality => character.quality,
                _ => character.combatPower
            };
        }

        void EnsurePartySlots()
        {
            while (selectedParty.Count < maxPartySize) selectedParty.Add(null);
            if (selectedParty.Count > maxPartySize)
                selectedParty.RemoveRange(maxPartySize, selectedParty.Count - maxPartySize);
        }

        void EnsureSlotBindings()
        {
            if (!HasAnySlotBinding(selectionSlots))
                selectionSlots = partySlots;
        }

        static bool HasAnySlotBinding(List<StoryPartySlotBinding> slots)
        {
            if (slots == null) return false;
            foreach (var slot in slots)
                if (slot != null && (slot.button != null || slot.portrait != null || slot.levelText != null || slot.emptyState != null))
                    return true;
            return false;
        }

        List<string> CurrentIds()
        {
            var result = new List<string>();
            foreach (var character in selectedParty)
            {
                if (character == null) continue;
                var canonical = CharacterIdentity.Canonical(character.id, database);
                if (!string.IsNullOrWhiteSpace(canonical) && !result.Contains(canonical))
                    result.Add(canonical);
            }
            return result;
        }

        static void ApplySlot(StoryPartySlotBinding slot, CharacterEntry character)
        {
            if (slot == null) return;
            var image = slot.portrait != null ? slot.portrait : slot.button != null ? slot.button.image : null;
            if (image != null)
            {
                image.sprite = character?.cardBackground;
                image.enabled = character != null;
                if (character != null) ForceVisible(image);
            }
            if (slot.levelText != null)
                slot.levelText.text = character == null ? string.Empty : $"Lv. {CharacterProgressionState.GetLevel(character.id)}";
            if (slot.emptyState != null)
                slot.emptyState.SetActive(character == null);
        }

        static void ApplyRosterCard(StoryRosterCardBinding card, CharacterEntry character)
        {
            ForceVisible(card.button?.image);
            if (card.button != null && card.button.image != null)
                card.button.image.sprite = character.cardBackground;
            if (card.portrait != null)
            {
                card.portrait.sprite = character.cardBackground;
                ForceVisible(card.portrait);
            }
            if (card.levelText != null)
                card.levelText.text = $"Lv. {CharacterProgressionState.GetLevel(character.id)}";
            if (card.selectedState != null)
            {
                card.selectedState.SetActive(false);
                card.selectedState.transform.SetAsLastSibling();
            }
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

        Button FindButton(params string[] names) => FindButtonIn(transform, names);

        static Button FindButtonIn(Transform root, params string[] names)
        {
            if (root == null || names == null) return null;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
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

        void Log(string message)
        {
            if (!debugLogs) return;
            Debug.Log($"[BES][PlayPartyPanelController][{name}] {message}", this);
        }

        static string JoinIds(IReadOnlyList<string> ids) => ids == null || ids.Count == 0 ? string.Empty : string.Join(", ", ids);
        static string NameOf(UnityEngine.Object target) => target != null ? target.name : "NULL";
    }
}
