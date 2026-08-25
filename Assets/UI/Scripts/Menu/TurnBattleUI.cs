using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using BES.Gameplay;
using BES.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BES.UI.Menu
{
    [Serializable]
    public class BattleSkillDefinition
    {
        public string id = "attack";
        public string displayName = "Attack";
        public Sprite icon;
        [Min(0.1f)] public float powerMultiplier = 1f;
    }

    [Serializable]
    public class BattleUnitDefinition
    {
        public string id;
        public string displayName;
        public string element;
        public Sprite portrait;
        public Sprite battlefieldSprite;
        public UIGifClip idleClip;
        public UIGifClip attackClip;
        public List<GameObject> attackEffectPrefabs = new();
        public Vector3 attackEffectOffset = Vector3.zero;
        public Vector3 attackEffectScale = Vector3.one;
        public bool isRanged;
        [Min(1)] public int maxHealth = 100;
        [Min(1)] public int attack = 20;
        [Min(0)] public int defense = 5;
        [Min(1)] public int speed = 10;
        public List<BattleSkillDefinition> skills = new();
    }

    [Serializable]
    public class BattleUnitView
    {
        public BattleUnitDefinition definition = new();
        public GameObject root;
        public Button targetButton;
        public Image battlefieldImage;
        public UIGifPlayer gifPlayer;
        public Image portrait;
        public Slider healthBar;
        [Tooltip("Optional free-layout HP fill. Uses Image.fillAmount and is not driven by Slider.")]
        public Image healthFill;
        public TMP_Text healthText;
        public Animator animator;
        [NonSerialized] public int health;
        [NonSerialized] public int shield;
        [NonSerialized] public bool usedOneShotSkill;
        [NonSerialized] public int attackEffectCursor;
        [NonSerialized] public bool isPlayer;
        [NonSerialized] public int setupIndex;
        public bool IsAlive => health > 0;
        public int Speed => definition != null ? definition.speed : 0;
    }

    [Serializable]
    public class TurnOrderEntryView
    {
        public GameObject root;
        public Image portrait;
        public GameObject playerMarker;
        public GameObject enemyMarker;
    }

    public class TurnBattleUI : MonoBehaviour
    {
        [Header("Configurable combatants")]
        [SerializeField] List<BattleUnitView> allies = new();
        [SerializeField] List<BattleUnitView> enemies = new();
        [Header("Turn order rail")]
        [SerializeField] List<TurnOrderEntryView> turnOrderEntries = new();
        [Header("Skill and target selection")]
        [SerializeField] GameObject skillPanel;
        [SerializeField] List<Button> skillButtons = new();
        [SerializeField] List<Image> skillIcons = new();
        [SerializeField] List<TMP_Text> skillLabels = new();
        [SerializeField] TMP_Text selectionHintText;
        [Header("Header controls")]
        [SerializeField] TMP_Text roundText;
        [SerializeField] TMP_Text currentActorText;
        [SerializeField] Button speedButton;
        [SerializeField] TMP_Text speedText;
        [SerializeField] Button autoButton;
        [SerializeField] TMP_Text autoText;
        [SerializeField] Button pauseButton;
        [SerializeField] GameObject pausePanel;
        [SerializeField] Button pauseResumeButton;
        [SerializeField] GameObject winPanel;
        [SerializeField] Button winReturnButton;
        [SerializeField] Button winExitButton;
        [SerializeField] GameObject losePanel;
        [SerializeField] Button loseReturnButton;
        [SerializeField] Button loseExitButton;
        [SerializeField] Button loseRetryButton;
        [SerializeField] Button levelBtn;
        [SerializeField] Button equipBtn;
        [SerializeField] Button skillBtn;
        [SerializeField] Button constellationBtn;
        [SerializeField] Button recruitBtn;
        [SerializeField] MenuNavigator navigator;
        [SerializeField] MenuHomeController homeController;
        [SerializeField] CharacterCollectionPanel characterCollection;
        [SerializeField] SimpleModalPanel wishPanel;
        [SerializeField] StoryModePanelController storyModeController;
        [Header("Result UI Art")]
        [SerializeField] Sprite winPanelArt;
        [SerializeField] Sprite losePanelArt;
        [Header("Result reveal animation")]
        [SerializeField] bool animateResultPanels = true;
        [SerializeField] Color resultBackdropColor = new(0f, 0f, 0f, 0.62f);
        [SerializeField] Color winRevealColor = new(1f, 0.86f, 0.2f, 0.95f);
        [SerializeField] Color loseRevealColor = new(0.72f, 0.28f, 1f, 0.95f);
        [SerializeField, Min(0.01f)] float resultLineSweepDuration = 0.28f;
        [SerializeField, Min(0.01f)] float resultExpandDuration = 0.32f;
        [SerializeField, Min(1f)] float resultLineHeight = 8f;
        [SerializeField, Min(1f)] float resultLineBlurHeight = 92f;
        [SerializeField] AnimationCurve resultRevealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Header("Timing and animation")]
        [SerializeField, Min(0.05f)] float actionWindup = 0.45f;
        [SerializeField, Min(0.05f)] float actionRecovery = 0.35f;
        [SerializeField] List<Animator> panelAnimators = new();
        [Header("Damage popup")]
        [SerializeField] RectTransform damagePopupParent;
        [SerializeField] TMP_Text damagePopupPrefab;
        [SerializeField] TMP_FontAsset damagePopupFont;
        [SerializeField] Color damagePopupColor = new(1f, 0.2f, 0.12f, 1f);
        [SerializeField] Color shieldDamagePopupColor = new(0.45f, 0.85f, 1f, 1f);
        [SerializeField] Color healPopupColor = new(0.35f, 1f, 0.35f, 1f);
        [SerializeField, Min(8f)] float damagePopupFontSize = 42f;
        [SerializeField, Min(0.1f)] float damagePopupDuration = 0.8f;
        [SerializeField] Vector2 damagePopupOffset = new(0f, 85f);
        [SerializeField] Vector2 damagePopupTravel = new(0f, 80f);
        [Header("Battle events")]
        [SerializeField] UnityEvent onVictory;
        [SerializeField] UnityEvent onDefeat;
        [SerializeField] UnityEvent<int> onRoundStarted;
        [SerializeField] MenuContentDatabase menuContentDatabase;
        [Header("Story dialogue popups")]
        [SerializeField] DialogueSequenceUI combatDialogueUI;
        [SerializeField] bool pauseBattleDuringDialogue = true;
        [SerializeField] bool hideCombatHudDuringDialogue = true;
        [Tooltip("Optional extra UI roots to hide while a combat trigger dialogue is playing. Do not put battlefield/background roots here.")]
        [SerializeField] List<GameObject> extraHudRootsToHideDuringDialogue = new();

        public static string ActiveStageId;
        public static List<string> SelectedPartyCharacterIds = new();
        public static bool IsPlayModeBattle;

        readonly List<BattleUnitView> turnQueue = new();
        readonly List<BattleUnitView> turnOrderPreview = new();
        readonly List<BattleUnitView> nextRoundPreview = new();
        int queueIndex;
        int round;
        int selectedSkillIndex = -1;
        BattleUnitView currentActor;
        bool resolving;
        bool autoMode;
        bool paused;
        float playbackSpeed = 1f;
        StageEntry currentStage;
        int currentPhaseIndex;
        BattlePhaseEntry currentPhase;
        bool battleEnded;
        Coroutine resultRevealRoutine;
        GameObject resultRevealOverlay;
        Image resultRevealBackdrop;
        Image resultRevealLine;
        Image resultRevealGlow;
        RectTransform resultRevealLineRect;
        RectTransform resultRevealGlowRect;

        void Awake()
        {
            EnsureDialogueUI();
            WireControls();
        }
        void OnEnable() { ResetBattle(); }
        void OnDisable()
        {
            paused = false;
            HideResultRevealOverlay();
            ApplyPlaybackSpeed();
        }

        void Update()
        {
            if (battleEnded || paused || resolving || currentActor == null || !currentActor.IsAlive) return;
            if (currentActor.isPlayer && autoMode && selectedSkillIndex < 0)
            {
                SelectSkill(0);
                var target = FirstAlive(enemies);
                if (target != null) SelectTarget(target);
            }
        }

        void WireControls()
        {
            AutoResolveMenuReferences();
            AutoResolveResultButtons();
            ApplyResultPanelArt();
            for (var i = 0; i < skillButtons.Count; i++) { var index = i; if (skillButtons[i] != null) skillButtons[i].onClick.AddListener(() => SelectSkill(index)); }
            foreach (var enemy in enemies) { var captured = enemy; if (enemy?.targetButton != null) enemy.targetButton.onClick.AddListener(() => SelectTarget(captured)); }
            if (speedButton != null) speedButton.onClick.AddListener(ToggleSpeed);
            if (autoButton != null) autoButton.onClick.AddListener(ToggleAuto);
            if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);
            if (pauseResumeButton != null) pauseResumeButton.onClick.AddListener(ResumeBattle);
            if (winReturnButton != null) winReturnButton.onClick.AddListener(ReturnToStoryMode);
            if (winExitButton != null) winExitButton.onClick.AddListener(ExitBattleToHome);

            if (loseReturnButton != null) loseReturnButton.onClick.AddListener(ExitBattleToHome);
            if (loseExitButton != null) loseExitButton.onClick.AddListener(ExitBattleToHome);
            if (loseRetryButton != null) loseRetryButton.onClick.AddListener(ResetBattle);
            if (levelBtn != null) levelBtn.onClick.AddListener(() => OpenCharacterDestination(CharacterCollectionPanel.CharacterCollectionDestination.Level));
            if (equipBtn != null) equipBtn.onClick.AddListener(() => OpenCharacterDestination(CharacterCollectionPanel.CharacterCollectionDestination.Equipment));
            if (skillBtn != null) skillBtn.onClick.AddListener(() => OpenCharacterDestination(CharacterCollectionPanel.CharacterCollectionDestination.Skill));
            if (constellationBtn != null) constellationBtn.onClick.AddListener(() => OpenCharacterDestination(CharacterCollectionPanel.CharacterCollectionDestination.Constellation));
            if (recruitBtn != null) recruitBtn.onClick.AddListener(OpenRecruit);
        }

        void OpenScreen(MenuScreenId screenId)
        {
            if (losePanel != null) losePanel.SetActive(false);
            HideResultRevealOverlay();
            navigator?.Open(screenId);
        }

        void ResumeBattle()
        {
            paused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            ApplyPlaybackSpeed();
        }

        void SaveBattleProgress()
        {
            var save = GameManager.Instance?.Save?.Current;
            if (save != null)
            {
                var stageId = currentStage != null ? currentStage.id : ActiveStageId;
                if (!string.IsNullOrWhiteSpace(stageId))
                    save.activeBattleStageId = stageId;
                save.activeBattleIsPlayMode = IsPlayModeBattle;
                if (!IsPlayModeBattle && !string.IsNullOrWhiteSpace(stageId))
                {
                    save.activeStoryStageId = stageId;
                    SaveStoryStageIndexes(save, stageId);
                }
                if (SelectedPartyCharacterIds != null && SelectedPartyCharacterIds.Count > 0)
                    save.storyPartyCharacterIds = new List<string>(SelectedPartyCharacterIds);
            }
            GameManager.Instance?.SaveGame();
        }

        void SaveStoryStageIndexes(SaveData save, string stageId)
        {
            if (save == null || string.IsNullOrWhiteSpace(stageId)) return;
            if (menuContentDatabase == null)
            {
                menuContentDatabase = Resources.Load<MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
                if (menuContentDatabase == null)
                    menuContentDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
#endif
            }
            if (menuContentDatabase == null) return;
            menuContentDatabase = ChapterOneStoryRuntime.Apply(menuContentDatabase);

            for (var chapterIndex = 0; chapterIndex < menuContentDatabase.storyChapters.Count; chapterIndex++)
            {
                var chapter = menuContentDatabase.storyChapters[chapterIndex];
                if (chapter?.stages == null) continue;
                var stageIndex = chapter.stages.FindIndex(stage => stage != null && string.Equals(stage.id, stageId, StringComparison.OrdinalIgnoreCase));
                if (stageIndex < 0) continue;
                save.storyChapterIndex = chapterIndex;
                save.storyStageIndex = stageIndex;
                save.activeStoryStageId = stageId;
                return;
            }
        }

        void ExitBattleToHome()
        {
            SaveBattleProgress();
            ActiveStageId = currentStage != null ? currentStage.id : ActiveStageId;
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            HideResultRevealOverlay();
            paused = false;
            ApplyPlaybackSpeed();
            navigator?.OpenAsRoot(MenuScreenId.Home);
            homeController?.Refresh();
        }

        void OpenCharacterDestination(CharacterCollectionPanel.CharacterCollectionDestination destination)
        {
            SaveBattleProgress();
            if (losePanel != null) losePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            HideResultRevealOverlay();
            paused = false;
            ApplyPlaybackSpeed();
            navigator?.OpenAsRoot(MenuScreenId.Home);
            characterCollection?.OpenDestination(destination, homeController != null ? homeController.CurrentCharacterId : null);
        }

        void OpenRecruit()
        {
            SaveBattleProgress();
            if (losePanel != null) losePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            HideResultRevealOverlay();
            paused = false;
            ApplyPlaybackSpeed();
            navigator?.OpenAsRoot(MenuScreenId.Home);
            if (characterCollection != null)
                characterCollection.OpenRateUp();
            wishPanel?.Open();
        }

        void AutoResolveMenuReferences()
        {
            navigator ??= FindAnyObjectByType<MenuNavigator>(FindObjectsInactive.Include);
            homeController ??= FindAnyObjectByType<MenuHomeController>(FindObjectsInactive.Include);
            characterCollection ??= FindAnyObjectByType<CharacterCollectionPanel>(FindObjectsInactive.Include);
            storyModeController ??= FindAnyObjectByType<StoryModePanelController>(FindObjectsInactive.Include);
            if (wishPanel == null)
            {
                var wish = FindDeep(transform.root, "WishPanel") ?? FindDeep(transform.root, "WishContent");
                wishPanel = wish != null ? wish.GetComponentInParent<SimpleModalPanel>(true) : null;
            }
        }

        void AutoResolveResultButtons()
        {
            pausePanel ??= FindPanel("PausePanel", "PauseOverlay", "Pause");
            winPanel ??= FindPanel("WinPanel", "VictoryPanel", "ResultWinPanel");
            losePanel ??= FindPanel("LosePanel", "DefeatPanel", "ResultLosePanel");

            pauseResumeButton ??= FindButton(pausePanel, "ResumeButton", "ContinueButton", "Continue", "Resume", "TiepTuc", "Tieptuc");
            if (pausePanel != null && pauseResumeButton == null)
                pauseResumeButton = CreateRuntimePauseResumeButton();

            winReturnButton ??= FindButton(winPanel, "ContinueButton", "Continue", "NextButton", "TiepTuc", "Tieptuc");
            winExitButton ??= FindButton(winPanel, "ExitButton", "CloseButton", "ReturnButton", "Thoat", "Exit");

            loseReturnButton ??= FindButton(losePanel, "ExitButton", "CloseButton", "ReturnButton", "Thoat", "Exit");
            loseExitButton ??= FindButton(losePanel, "ExitButton", "CloseButton", "ReturnButton", "Thoat", "Exit");
            loseRetryButton ??= FindButton(losePanel, "RetryButton", "AgainButton", "ReplayButton", "ChoiLai");
            levelBtn ??= FindButton(losePanel, "LevelButton", "CharacterLevelButton", "CapNhanVat", "CharacterLevel");
            equipBtn ??= FindButton(losePanel, "EquipmentButton", "EquipButton", "TrangBi", "Equipment");
            skillBtn ??= FindButton(losePanel, "SkillButton", "KyNang", "Skill");
            constellationBtn ??= FindButton(losePanel, "ConstellationButton", "TinhMenh", "Constellation");
            recruitBtn ??= FindButton(losePanel, "RecruitButton", "WishButton", "ChieuMo", "Recruit", "Wish");
        }

        GameObject FindPanel(params string[] names)
        {
            if (names == null) return null;
            foreach (var name in names)
            {
                var found = FindDeep(transform, name) ?? FindDeep(transform.root, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        Button FindButton(GameObject root, params string[] names)
        {
            if (root == null || names == null) return null;
            foreach (var name in names)
            {
                var found = FindDeep(root.transform, name);
                var button = found != null ? found.GetComponent<Button>() : null;
                if (button != null) return button;
            }
            return null;
        }

        static Transform FindDeep(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrEmpty(objectName)) return null;
            if (root.name.Equals(objectName, StringComparison.OrdinalIgnoreCase)) return root;
            foreach (Transform child in root)
            {
                var result = FindDeep(child, objectName);
                if (result != null) return result;
            }
            return null;
        }

        Button CreateRuntimePauseResumeButton()
        {
            var buttonObject = new GameObject("ResumeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(pausePanel.transform, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.42f, .42f);
            rect.anchorMax = new Vector2(.58f, .50f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(.92f, .78f, .48f, .95f);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "TIẾP TỤC";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;
            label.color = new Color(.24f, .12f, .07f, 1f);

            return buttonObject.GetComponent<Button>();
        }

        void ApplyResultPanelArt()
        {
#if UNITY_EDITOR
            winPanelArt ??= AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 427323086.png");
            losePanelArt ??= AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 427323096.png");
#endif
            ApplyRootImage(winPanel, winPanelArt);
            ApplyRootImage(losePanel, losePanelArt);
        }

        static void ApplyRootImage(GameObject panel, Sprite sprite)
        {
            if (panel == null || sprite == null) return;
            var image = panel.GetComponent<Image>();
            if (image == null) return;
            image.sprite = sprite;
            image.color = Color.white;
        }

        public void ResetBattle()
        {
            StopAllCoroutines();
            EnsureDialogueUI();
            LoadPlayerParty();
            LoadStageData();
            currentPhaseIndex = 0;
            LoadCurrentBattlePhase();
            InitializeTeam(allies, true); InitializeTeam(enemies, false);
            round = 0; queueIndex = 0; selectedSkillIndex = -1; currentActor = null;
            resolving = false; paused = false; autoMode = false; playbackSpeed = 1f; battleEnded = false;
            ResetCombatDialogueTriggers();
            if (pausePanel != null) pausePanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            HideResultRevealOverlay();
            SetResultPanelAlpha(winPanel, 1f, true);
            SetResultPanelAlpha(losePanel, 1f, true);
            ApplyPlaybackSpeed(); StartNextRound();
            TryPlayCombatDialogue(CombatDialogueTriggerType.BattleStart);
            TryPlayCombatDialogue(CombatDialogueTriggerType.PhaseStart);
        }

        static void InitializeTeam(List<BattleUnitView> team, bool isPlayer)
        {
            for (var i = 0; i < team.Count; i++)
            {
                var unit = team[i]; if (unit == null || unit.definition == null) continue;
                if (unit.root != null && !unit.root.activeSelf) continue;
                SetUnitVisualsActive(unit, true);
                unit.isPlayer = isPlayer; unit.setupIndex = i; unit.health = unit.definition.maxHealth; unit.shield = 0; unit.usedOneShotSkill = false; unit.attackEffectCursor = 0;

                if (unit.gifPlayer == null && unit.root != null)
                {
                    unit.gifPlayer = unit.root.GetComponentInChildren<UIGifPlayer>(true);
                }

                if (unit.gifPlayer != null && unit.definition.idleClip != null)
                {
                    unit.gifPlayer.gameObject.SetActive(true);
                    unit.gifPlayer.SetClip(unit.definition.idleClip, true);
                    if (unit.battlefieldImage != null) unit.battlefieldImage.gameObject.SetActive(false);
                }
                else
                {
                    if (unit.gifPlayer != null) unit.gifPlayer.gameObject.SetActive(false);
                    if (unit.battlefieldImage != null)
                    {
                        unit.battlefieldImage.gameObject.SetActive(true);
                        unit.battlefieldImage.sprite = unit.definition.battlefieldSprite;
                    }
                }

                if (unit.portrait != null) unit.portrait.sprite = unit.definition.portrait;
                RefreshUnit(unit);
            }
        }

        void StartNextRound()
        {
            round++; turnQueue.Clear(); AddAlive(turnQueue, allies); AddAlive(turnQueue, enemies);
            turnQueue.Sort(CompareTurnOrder); queueIndex = 0;
            if (roundText != null) roundText.text = $"ROUND {round}";
            onRoundStarted?.Invoke(round);
            if (TryPlayCombatDialogue(CombatDialogueTriggerType.RoundStart, null, round, BeginCurrentTurn)) return;
            BeginCurrentTurn();
        }

        static void AddAlive(List<BattleUnitView> destination, List<BattleUnitView> source) { foreach (var unit in source) if (IsBattleActive(unit)) destination.Add(unit); }
        static int CompareTurnOrder(BattleUnitView left, BattleUnitView right)
        {
            var speed = right.Speed.CompareTo(left.Speed); if (speed != 0) return speed;
            if (left.isPlayer != right.isPlayer) return left.isPlayer ? -1 : 1;
            return left.setupIndex.CompareTo(right.setupIndex);
        }

        void BeginCurrentTurn()
        {
            selectedSkillIndex = -1;
            while (queueIndex < turnQueue.Count && !turnQueue[queueIndex].IsAlive) queueIndex++;
            if (queueIndex >= turnQueue.Count) { StartNextRound(); return; }
            currentActor = turnQueue[queueIndex];
            if (currentActorText != null) currentActorText.text = currentActor.definition.displayName;
            RefreshTurnOrder();
            if (currentActor.isPlayer) { ShowSkills(currentActor); if (selectionHintText != null) selectionHintText.text = "SELECT A SKILL"; }
            else { HideSkills(); StartCoroutine(ResolveEnemyTurn()); }
        }

        void ShowSkills(BattleUnitView actor)
        {
            if (skillPanel != null) skillPanel.SetActive(true);
            for (var i = 0; i < skillButtons.Count; i++)
            {
                var available = actor.definition.skills != null && i < actor.definition.skills.Count;
                if (skillButtons[i] != null) skillButtons[i].gameObject.SetActive(available);
                if (!available) continue;
                var skill = actor.definition.skills[i];
                if (i < skillIcons.Count && skillIcons[i] != null) skillIcons[i].sprite = skill.icon;
                if (i < skillLabels.Count && skillLabels[i] != null) skillLabels[i].text = skill.displayName;
            }
            SetEnemyTargeting(false);
        }

        void HideSkills() { if (skillPanel != null) skillPanel.SetActive(false); SetEnemyTargeting(false); }
        public void SelectSkill(int skillIndex)
        {
            if (paused || resolving || currentActor == null || !currentActor.isPlayer) return;
            if (currentActor.definition.skills == null || skillIndex < 0 || skillIndex >= currentActor.definition.skills.Count) return;
            selectedSkillIndex = skillIndex; SetEnemyTargeting(true);
            if (selectionHintText != null) selectionHintText.text = "SELECT AN ENEMY";
        }
        public void SelectEnemy(int enemyIndex) { if (enemyIndex >= 0 && enemyIndex < enemies.Count) SelectTarget(enemies[enemyIndex]); }
        void SelectTarget(BattleUnitView target)
        {
            if (paused || resolving || currentActor == null || !currentActor.isPlayer || selectedSkillIndex < 0 || target == null || !target.IsAlive) return;
            StartCoroutine(ResolveAction(currentActor, target, selectedSkillIndex));
        }
        void SetEnemyTargeting(bool enabled) { foreach (var enemy in enemies) if (enemy?.targetButton != null) enemy.targetButton.interactable = enabled && enemy.IsAlive; }

        IEnumerator ResolveEnemyTurn()
        {
            resolving = true; yield return ScaledWait(actionRecovery);
            var target = LowestHealthAlive(allies); if (target != null) yield return PerformAction(currentActor, target, 0);
            FinishTurn();
        }
        IEnumerator ResolveAction(BattleUnitView actor, BattleUnitView target, int skillIndex) { resolving = true; HideSkills(); yield return PerformAction(actor, target, skillIndex); FinishTurn(); }
        IEnumerator PerformAction(BattleUnitView actor, BattleUnitView target, int skillIndex)
        {
            var skill = GetSkill(actor, skillIndex);
            if (skill != null && string.Equals(skill.id, "summon", StringComparison.OrdinalIgnoreCase))
            {
                yield return ScaledWait(actionWindup);
                SummonInactiveEnemies(2);
                yield return ScaledWait(actionRecovery);
                yield break;
            }
            if (skill != null && TryResolveSpecialEnemySkill(actor, skill))
            {
                yield return ScaledWait(actionWindup);
                yield return ScaledWait(actionRecovery);
                yield break;
            }

            System.Action onStrike = () =>
            {
                PlayAttackEffect(actor, target);
                var raw = Mathf.RoundToInt(actor.definition.attack * (skill?.powerMultiplier ?? 1f) * ElementMultiplier(actor.definition, target.definition));
                DamageUnit(target, Mathf.Max(1, raw - target.definition.defense));
                
                if (target.gifPlayer != null && target.health == 0)
                {
                    target.gifPlayer.gameObject.SetActive(false);
                }
                else if (target.animator != null)
                {
                    target.animator.SetTrigger(target.health == 0 ? "Defeat" : "Hit");
                }

            };

            if (actor.definition.isRanged)
            {
                // Ranged attack: play attack animation at spot
                if (actor.gifPlayer != null && actor.definition.attackClip != null)
                {
                    actor.gifPlayer.SetClip(actor.definition.attackClip, true);
                }
                else if (actor.animator != null)
                {
                    actor.animator.SetTrigger(skillIndex == 0 ? "Attack" : "Skill");
                }

                yield return ScaledWait(actionWindup);
                onStrike();
                yield return ScaledWait(actionRecovery);

                if (actor.gifPlayer != null && actor.definition.idleClip != null)
                {
                    actor.gifPlayer.SetClip(actor.definition.idleClip, true);
                }
            }
            else
            {
                // Melee attack: move quickly to target, strike, return
                var actorRect = MovementRectFor(actor);
                var targetRect = MovementRectFor(target);

                if (actorRect != null && targetRect != null)
                {
                    var startPos = actorRect.position;
                    var offset = actor.isPlayer ? new Vector3(-150f, 0f, 0f) : new Vector3(150f, 0f, 0f);
                    var targetPos = targetRect.position + offset;

                    // Start attack GIF
                    if (actor.gifPlayer != null && actor.definition.attackClip != null)
                    {
                        actor.gifPlayer.SetClip(actor.definition.attackClip, true);
                    }
                    else if (actor.animator != null)
                    {
                        actor.animator.SetTrigger(skillIndex == 0 ? "Attack" : "Skill");
                    }

                    // Dash forward (0.15s)
                    float dashDuration = 0.15f;
                    float elapsed = 0f;
                    while (elapsed < dashDuration)
                    {
                        if (!paused)
                        {
                            elapsed += Time.unscaledDeltaTime * playbackSpeed;
                            actorRect.position = Vector3.Lerp(startPos, targetPos, elapsed / dashDuration);
                        }
                        yield return null;
                    }
                    actorRect.position = targetPos;

                    // Wait for actionWindup - dash duration
                    float remainingWindup = Mathf.Max(0f, actionWindup - dashDuration);
                    yield return ScaledWait(remainingWindup);

                    onStrike();

                    // Recovery wait before moving back
                    yield return ScaledWait(actionRecovery);

                    // Switch back to idle GIF
                    if (actor.gifPlayer != null && actor.definition.idleClip != null)
                    {
                        actor.gifPlayer.SetClip(actor.definition.idleClip, true);
                    }

                    // Dash back (0.15s)
                    elapsed = 0f;
                    while (elapsed < dashDuration)
                    {
                        if (!paused)
                        {
                            elapsed += Time.unscaledDeltaTime * playbackSpeed;
                            actorRect.position = Vector3.Lerp(targetPos, startPos, elapsed / dashDuration);
                        }
                        yield return null;
                    }
                    actorRect.position = startPos;
                }
                else
                {
                    // Fallback to standard
                    if (actor.animator != null) actor.animator.SetTrigger(skillIndex == 0 ? "Attack" : "Skill");
                    yield return ScaledWait(actionWindup);
                    onStrike();
                    yield return ScaledWait(actionRecovery);
                }
            }
        }

        void PlayAttackEffect(BattleUnitView actor, BattleUnitView target)
        {
            var effects = actor?.definition?.attackEffectPrefabs;
            if (effects == null || effects.Count == 0) return;

            GameObject prefab = null;
            for (var i = 0; i < effects.Count; i++)
            {
                var index = Mathf.Abs(actor.attackEffectCursor + i) % effects.Count;
                if (effects[index] == null) continue;
                prefab = effects[index];
                actor.attackEffectCursor = index + 1;
                break;
            }
            if (prefab == null) return;

            var targetRect = MovementRectFor(target);
            var actorRect = MovementRectFor(actor);
            var basePosition = targetRect != null
                ? targetRect.position
                : actorRect != null ? actorRect.position : transform.position;
            var instance = Instantiate(prefab, basePosition + actor.definition.attackEffectOffset, Quaternion.identity);
            instance.transform.localScale = actor.definition.attackEffectScale == Vector3.zero
                ? Vector3.one
                : actor.definition.attackEffectScale;
            Destroy(instance, 4f);
        }

        bool TryResolveSpecialEnemySkill(BattleUnitView actor, BattleSkillDefinition skill)
        {
            if (actor == null || actor.isPlayer || skill == null || string.IsNullOrWhiteSpace(skill.id)) return false;

            switch (skill.id)
            {
                case "enemy_sand_random_5_percent":
                    var randomTarget = RandomAlive(allies);
                    if (randomTarget != null)
                    {
                        PlayAttackEffect(actor, randomTarget);
                        DamageUnit(randomTarget, PercentOfMaxHealth(randomTarget, 0.05f));
                    }
                    return true;
                case "enemy_blue_heal_20_percent":
                    HealTeam(enemies, actor, 0.2f);
                    return true;
                case "enemy_coffin_shield_2000_once":
                    if (actor.usedOneShotSkill) return true;
                    actor.usedOneShotSkill = true;
                    GrantShieldToTeam(enemies, actor, 2000);
                    RemoveUnitFromBattle(actor);
                    return true;
                case "enemy_fire_aoe_5_percent":
                    foreach (var ally in allies)
                        if (ally != null && ally.IsAlive)
                        {
                            PlayAttackEffect(actor, ally);
                            DamageUnit(ally, PercentOfMaxHealth(ally, 0.05f));
                        }
                    return true;
                default:
                    return false;
            }
        }

        BattleUnitView RandomAlive(List<BattleUnitView> team)
        {
            var alive = new List<BattleUnitView>();
            foreach (var unit in team)
                if (unit != null && unit.IsAlive)
                    alive.Add(unit);
            return alive.Count == 0 ? null : alive[UnityEngine.Random.Range(0, alive.Count)];
        }

        static int PercentOfMaxHealth(BattleUnitView target, float percent)
        {
            return Mathf.Max(1, Mathf.CeilToInt((target?.definition?.maxHealth ?? 1) * percent));
        }

        static float ElementMultiplier(BattleUnitDefinition attacker, BattleUnitDefinition defender)
        {
            var attackElement = NormalizeElement(attacker?.element);
            var defendElement = NormalizeElement(defender?.element);
            if (string.IsNullOrEmpty(attackElement) || string.IsNullOrEmpty(defendElement) || attackElement == defendElement)
                return 1f;

            if (IsStrongAgainst(attackElement, defendElement)) return 1.5f;
            if (IsStrongAgainst(defendElement, attackElement)) return 0.75f;
            return 1f;
        }

        static bool IsStrongAgainst(string attackElement, string defendElement)
        {
            return (attackElement == "fire" && defendElement == "grass") ||
                   (attackElement == "water" && defendElement == "fire") ||
                   (attackElement == "grass" && defendElement == "water") ||
                   (attackElement == "wind" && defendElement == "lightning") ||
                   (attackElement == "lightning" && defendElement == "water") ||
                   (attackElement == "void" && defendElement == "wind");
        }

        static string NormalizeElement(string element)
        {
            if (string.IsNullOrWhiteSpace(element)) return string.Empty;
            element = element.Trim().ToLowerInvariant();
            return element switch
            {
                "hỏa" or "hoa" or "fire" => "fire",
                "thủy" or "thuy" or "water" => "water",
                "thảo" or "thao" or "grass" or "wood" => "grass",
                "phong" or "wind" => "wind",
                "lôi" or "loi" or "lightning" or "thunder" => "lightning",
                "lỗi" or "loix" or "void" or "dark" => "void",
                _ => element
            };
        }

        void HealTeam(List<BattleUnitView> team, BattleUnitView actor, float percent)
        {
            foreach (var unit in team)
            {
                if (unit == null || unit == actor || !unit.IsAlive) continue;
                unit.health = Mathf.Min(unit.definition.maxHealth, unit.health + PercentOfMaxHealth(unit, percent));
                RefreshUnit(unit);
            }
        }

        void GrantShieldToTeam(List<BattleUnitView> team, BattleUnitView actor, int shieldAmount)
        {
            foreach (var unit in team)
            {
                if (unit == null || unit == actor || !unit.IsAlive) continue;
                unit.shield += Mathf.Max(0, shieldAmount);
                RefreshUnit(unit);
            }
        }

        void DamageUnit(BattleUnitView target, int damage)
        {
            if (target == null || !target.IsAlive) return;
            var remaining = Mathf.Max(0, damage);
            var requestedDamage = remaining;
            var blockedTotal = 0;
            if (target.shield > 0)
            {
                var blocked = Mathf.Min(target.shield, remaining);
                target.shield -= blocked;
                remaining -= blocked;
                blockedTotal += blocked;
            }

            if (remaining > 0)
                target.health = Mathf.Max(0, target.health - remaining);

            if (requestedDamage > 0)
            {
                if (remaining > 0) ShowDamagePopup(target, remaining, damagePopupColor, "-");
                if (blockedTotal > 0) ShowDamagePopup(target, blockedTotal, shieldDamagePopupColor, "-");
            }

            RefreshUnit(target);
            TryPlayCombatDialogue(CombatDialogueTriggerType.BossHealthBelowPercent, target);

            if (target.health == 0)
            {
                if (!target.isPlayer && HasSkill(target, "enemy_blue_heal_20_percent"))
                    DamageEnemyTeamExcept(target, 0.1f);
                RemoveUnitFromBattle(target);
                if (!target.isPlayer)
                    TryPlayCombatDialogue(CombatDialogueTriggerType.EnemyDefeated, target);
            }
        }

        static bool HasSkill(BattleUnitView unit, string skillId)
        {
            if (unit?.definition?.skills == null) return false;
            foreach (var skill in unit.definition.skills)
                if (skill != null && string.Equals(skill.id, skillId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        void DamageEnemyTeamExcept(BattleUnitView source, float percent)
        {
            foreach (var enemy in enemies)
                if (enemy != null && enemy != source && enemy.IsAlive)
                    DamageUnit(enemy, PercentOfMaxHealth(enemy, percent));
        }

        void ShowDamagePopup(BattleUnitView target, int amount, Color color, string prefix = "")
        {
            if (amount <= 0) return;
            var parent = ResolveDamagePopupParent(target);
            if (parent == null) return;

            TMP_Text popup;
            if (damagePopupPrefab != null)
            {
                popup = Instantiate(damagePopupPrefab, parent);
                popup.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("DamagePopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.transform.SetParent(parent, false);
                popup = go.GetComponent<TMP_Text>();
            }

            popup.text = $"{prefix}{amount}";
            popup.color = color;
            popup.fontSize = damagePopupFontSize;
            popup.alignment = TextAlignmentOptions.Center;
            popup.raycastTarget = false;
            if (damagePopupFont != null) popup.font = damagePopupFont;

            var rect = popup.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(240f, 80f);
            rect.anchoredPosition = ResolveDamagePopupPosition(target, parent) + damagePopupOffset;
            rect.SetAsLastSibling();

            StartCoroutine(AnimateDamagePopup(popup, rect.anchoredPosition));
        }

        RectTransform ResolveDamagePopupParent(BattleUnitView target)
        {
            if (damagePopupParent != null) return damagePopupParent;
            var canvas = target?.root != null ? target.root.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
            return canvas != null ? canvas.transform as RectTransform : transform as RectTransform;
        }

        Vector2 ResolveDamagePopupPosition(BattleUnitView target, RectTransform parent)
        {
            if (target == null || parent == null) return Vector2.zero;
            var source = target.battlefieldImage != null ? target.battlefieldImage.rectTransform : target.root != null ? target.root.transform as RectTransform : null;
            if (source == null) return Vector2.zero;

            var canvas = parent.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var world = source.TransformPoint(new Vector3(source.rect.center.x, source.rect.yMax, 0f));
            var screen = RectTransformUtility.WorldToScreenPoint(camera, world);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out var local) ? local : Vector2.zero;
        }

        IEnumerator AnimateDamagePopup(TMP_Text popup, Vector2 start)
        {
            if (popup == null) yield break;
            var rect = popup.rectTransform;
            var duration = Mathf.Max(0.1f, damagePopupDuration);
            var elapsed = 0f;
            var startColor = popup.color;
            while (elapsed < duration && popup != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 2f);
                rect.anchoredPosition = start + damagePopupTravel * eased;
                var color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, t);
                popup.color = color;
                yield return null;
            }
            if (popup != null) Destroy(popup.gameObject);
        }

        void RemoveUnitFromBattle(BattleUnitView unit)
        {
            if (unit == null) return;
            unit.health = 0;
            RefreshUnit(unit);
            if (unit.targetButton != null) unit.targetButton.interactable = false;
            if (unit.battlefieldImage != null) unit.battlefieldImage.gameObject.SetActive(false);
            if (unit.gifPlayer != null) unit.gifPlayer.gameObject.SetActive(false);
            if (unit.root != null) unit.root.SetActive(false);
        }
        static BattleSkillDefinition GetSkill(BattleUnitView actor, int index)
        {
            if (actor?.definition?.skills == null || actor.definition.skills.Count == 0) return new BattleSkillDefinition();
            return actor.definition.skills[Mathf.Clamp(index, 0, actor.definition.skills.Count - 1)];
        }
        List<string> ProcessCombatDrops()
        {
            var stageId = ActiveStageId;

            if (string.IsNullOrEmpty(stageId))
            {
                if (PlayerWallet.Instance != null)
                {
                    PlayerWallet.Instance.AddCoins(200);
                }
                return new List<string> { "+ 200 Vàng" };
            }

            var inventory = GameManager.Instance?.Inventory;
            var wallet = PlayerWallet.Instance;
            var rewardsList = new List<string>();

            if (wallet != null)
            {
                wallet.AddCoins(1000);
                rewardsList.Add("+ 1000 Vàng");
            }

            if (inventory != null)
            {
                inventory.AddItem("item_exp_green", 1);
                rewardsList.Add("+ 1 Lọ EXP Xanh Lá (100%)");

                if (UnityEngine.Random.Range(0, 100) < 50)
                {
                    inventory.AddItem("item_exp_blue", 1);
                    rewardsList.Add("+ 1 Lọ EXP Xanh Dương (50%)");
                }

                if (UnityEngine.Random.Range(0, 100) < 5)
                {
                    inventory.AddItem("item_exp_gold", 1);
                    rewardsList.Add("+ 1 Lọ EXP Vàng (5%)");
                }
            }

            if (winPanel != null)
            {
                var texts = winPanel.GetComponentsInChildren<TMP_Text>(true);
                foreach (var txt in texts)
                {
                    if (txt != null && (txt.name == "VictoryText" || txt.text.Contains("VICTORY") || txt.text.Contains("Chiến Thắng") || txt.text.Contains("victory") || txt.text.Contains("Victory")))
                    {
                        txt.text = $"VICTORY!\n\n<size=22><color=yellow>Phần thưởng nhận được:</color>\n" + string.Join("\n", rewardsList) + "</size>";
                        break;
                    }
                }
            }

            return rewardsList;
        }

        void FinishTurn()
        {
            resolving = false; selectedSkillIndex = -1;
            if (TryPlayAggregateCombatTriggers()) return;
            if (!AnyAlive(enemies))
            {
                HideSkills();
                RefreshTurnOrder(true);
                if (HasNextPhase())
                {
                    if (!TryPlayCombatDialogue(CombatDialogueTriggerType.PhaseVictory, null, 0, StartNextPhase))
                        StartNextPhase();
                }
                else if (!TryPlayCombatDialogue(CombatDialogueTriggerType.PhaseVictory, null, 0, CompleteVictory) &&
                         !TryPlayCombatDialogue(CombatDialogueTriggerType.BeforeVictory, null, 0, CompleteVictory))
                {
                    CompleteVictory();
                }
                return;
            }
            if (!AnyAlive(allies))
            {
                HideSkills();
                RefreshTurnOrder(true);
                if (TryPlayCombatDialogue(CombatDialogueTriggerType.AllAlliesDefeated, null, 0, CompleteDefeat))
                    return;
                CompleteDefeat();
                return;
            }
            queueIndex++; BeginCurrentTurn();
        }

        void CompleteDefeat()
        {
            if (battleEnded) return;
            battleEnded = true;
            resolving = false;
            paused = true;
            ApplyPlaybackSpeed();
            SaveBattleProgress();

            if (!IsPlayModeBattle)
            {
                storyModeController ??= FindAnyObjectByType<StoryModePanelController>(FindObjectsInactive.Include);
                storyModeController?.FailStoryBattle(currentStage?.id ?? ActiveStageId);
            }

            if (winPanel != null) winPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            ShowResultPanel(losePanel, false);
            onDefeat?.Invoke();
        }

        IEnumerator ScaledWait(float duration)
        {
            var elapsed = 0f; while (elapsed < duration) { if (!paused) elapsed += Time.unscaledDeltaTime * playbackSpeed; yield return null; }
        }
        void ToggleSpeed() { playbackSpeed = playbackSpeed > 1f ? 1f : 2f; ApplyPlaybackSpeed(); }
        void ApplyPlaybackSpeed()
        {
            var value = paused ? 0f : playbackSpeed;
            foreach (var animator in panelAnimators) if (animator != null) animator.speed = value;
            foreach (var unit in allies) if (unit?.animator != null) unit.animator.speed = value;
            foreach (var unit in enemies) if (unit?.animator != null) unit.animator.speed = value;
            if (speedText != null) speedText.text = playbackSpeed > 1f ? "2X" : "1X";
        }
        void ToggleAuto() { autoMode = !autoMode; if (autoText != null) autoText.text = autoMode ? "AUTO ON" : "AUTO"; }
        void TogglePause() { paused = !paused; if (pausePanel != null) pausePanel.SetActive(paused); ApplyPlaybackSpeed(); }
        void ReturnToStoryMode()
        {
            if (winPanel != null) winPanel.SetActive(false);
            HideResultRevealOverlay();
            if (!IsPlayModeBattle && currentStage?.victoryDialogue != null && currentStage.victoryDialogue.beats.Count > 0 && combatDialogueUI != null)
            {
                combatDialogueUI.Play(currentStage.victoryDialogue, ReturnToStoryModeAfterDialogue);
                return;
            }

            ReturnToStoryModeAfterDialogue();
        }

        void ReturnToStoryModeAfterDialogue()
        {
            SaveBattleProgress();
            if (IsPlayModeBattle)
            {
                navigator?.OpenAsRoot(MenuScreenId.ResourceStages);
            }
            else
            {
                navigator?.OpenAsRoot(MenuScreenId.StoryParty);
                storyModeController?.CompleteStoryBattle();
            }
        }

        void CompleteVictory()
        {
            if (battleEnded) return;
            battleEnded = true;
            resolving = false;
            paused = true;
            ApplyPlaybackSpeed();
            ProcessCombatDrops();
            if (losePanel != null) losePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            ShowResultPanel(winPanel, true);
            onVictory?.Invoke();
        }

        void ShowResultPanel(GameObject panel, bool victory)
        {
            if (panel == null) return;
            panel.SetActive(true);
            if (resultRevealRoutine != null)
            {
                StopCoroutine(resultRevealRoutine);
                resultRevealRoutine = null;
            }

            if (!animateResultPanels)
            {
                HideResultRevealOverlay();
                SetResultPanelAlpha(panel, 1f, true);
                return;
            }

            var canvasGroup = EnsureResultPanelCanvasGroup(panel);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
            resultRevealRoutine = StartCoroutine(PlayResultReveal(panel, canvasGroup, victory));
        }

        IEnumerator PlayResultReveal(GameObject panel, CanvasGroup canvasGroup, bool victory)
        {
            EnsureResultRevealOverlay(panel);
            var revealColor = victory ? winRevealColor : loseRevealColor;
            SetRevealColors(revealColor, 0f, 0f);
            SetRevealLineWidth(0f);
            SetRevealGlowHeight(resultLineBlurHeight);
            resultRevealOverlay.SetActive(true);
            resultRevealOverlay.transform.SetAsLastSibling();
            panel.transform.SetAsLastSibling();

            var elapsed = 0f;
            while (elapsed < resultLineSweepDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, resultLineSweepDuration));
                t = resultRevealCurve != null ? resultRevealCurve.Evaluate(t) : t;
                SetRevealColors(revealColor, 0f, t);
                SetRevealLineWidth(t);
                yield return null;
            }

            SetRevealLineWidth(1f);
            elapsed = 0f;
            while (elapsed < resultExpandDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, resultExpandDuration));
                var eased = resultRevealCurve != null ? resultRevealCurve.Evaluate(t) : t;
                SetRevealColors(revealColor, eased, 1f - eased);
                SetRevealGlowHeight(Mathf.Lerp(resultLineBlurHeight, GetRevealRootHeight(), eased));
                if (canvasGroup != null)
                    canvasGroup.alpha = eased;
                yield return null;
            }

            SetRevealColors(revealColor, 1f, 0f);
            SetRevealLineWidth(1f);
            SetRevealGlowHeight(GetRevealRootHeight());
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            resultRevealRoutine = null;
        }

        void EnsureResultRevealOverlay(GameObject panel)
        {
            if (resultRevealOverlay != null) return;
            var parent = panel.transform.parent != null ? panel.transform.parent : transform;

            resultRevealOverlay = new GameObject("BattleResultRevealOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            resultRevealOverlay.transform.SetParent(parent, false);
            var rootRect = resultRevealOverlay.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            resultRevealBackdrop = resultRevealOverlay.GetComponent<Image>();
            resultRevealBackdrop.raycastTarget = false;

            resultRevealGlow = CreateRevealImage("RevealGlow", resultRevealOverlay.transform, out resultRevealGlowRect);
            resultRevealLine = CreateRevealImage("RevealLine", resultRevealOverlay.transform, out resultRevealLineRect);
            resultRevealOverlay.SetActive(false);
        }

        Image CreateRevealImage(string objectName, Transform parent, out RectTransform rect)
        {
            var obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        CanvasGroup EnsureResultPanelCanvasGroup(GameObject panel)
        {
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panel.AddComponent<CanvasGroup>();
            return canvasGroup;
        }

        void SetResultPanelAlpha(GameObject panel, float alpha, bool interactable)
        {
            if (panel == null) return;
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) return;
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        void HideResultRevealOverlay()
        {
            if (resultRevealRoutine != null)
            {
                StopCoroutine(resultRevealRoutine);
                resultRevealRoutine = null;
            }
            if (resultRevealOverlay != null)
                resultRevealOverlay.SetActive(false);
        }

        void SetRevealColors(Color revealColor, float backdropProgress, float lineProgress)
        {
            if (resultRevealBackdrop != null)
            {
                var color = resultBackdropColor;
                color.a *= Mathf.Clamp01(backdropProgress);
                resultRevealBackdrop.color = color;
            }
            if (resultRevealLine != null)
            {
                var color = revealColor;
                color.a *= Mathf.Clamp01(lineProgress);
                resultRevealLine.color = color;
            }
            if (resultRevealGlow != null)
            {
                var color = revealColor;
                color.a *= 0.28f * Mathf.Clamp01(lineProgress);
                resultRevealGlow.color = color;
            }
        }

        void SetRevealLineWidth(float normalizedWidth)
        {
            if (resultRevealLineRect == null) return;
            var width = GetRevealRootWidth() * Mathf.Clamp01(normalizedWidth);
            resultRevealLineRect.sizeDelta = new Vector2(width, resultLineHeight);
        }

        void SetRevealGlowHeight(float height)
        {
            if (resultRevealGlowRect == null) return;
            resultRevealGlowRect.sizeDelta = new Vector2(GetRevealRootWidth(), Mathf.Max(1f, height));
        }

        float GetRevealRootWidth()
        {
            var rect = resultRevealOverlay != null ? resultRevealOverlay.GetComponent<RectTransform>() : null;
            return rect != null && rect.rect.width > 1f ? rect.rect.width : Screen.width;
        }

        float GetRevealRootHeight()
        {
            var rect = resultRevealOverlay != null ? resultRevealOverlay.GetComponent<RectTransform>() : null;
            return rect != null && rect.rect.height > 1f ? rect.rect.height : Screen.height;
        }

        void EnsureDialogueUI()
        {
            if (combatDialogueUI != null) return;
            combatDialogueUI = FindAnyObjectByType<DialogueSequenceUI>(FindObjectsInactive.Include);
            if (combatDialogueUI == null)
                combatDialogueUI = DialogueSequenceUI.CreateRuntimeOverlay("RuntimeCombatDialogueUI");
        }
        void RefreshTurnOrder(bool battleFinished = false)
        {
            BuildTurnOrderPreview(battleFinished);
            for (var i = 0; i < turnOrderEntries.Count; i++)
            {
                var hasUnit = i < turnOrderPreview.Count; var entry = turnOrderEntries[i];
                if (entry?.root != null) entry.root.SetActive(hasUnit); if (!hasUnit) continue;
                var unit = turnOrderPreview[i]; if (entry.portrait != null) entry.portrait.sprite = unit.definition.portrait;
                if (entry.playerMarker != null) entry.playerMarker.SetActive(unit.isPlayer);
                if (entry.enemyMarker != null) entry.enemyMarker.SetActive(!unit.isPlayer);
            }
        }

        void BuildTurnOrderPreview(bool battleFinished)
        {
            turnOrderPreview.Clear();

            // The first rail card is the unit currently taking its turn. Dead
            // units are skipped immediately, even if they still exist in this
            // round's original speed queue.
            if (!battleFinished)
            {
                for (var i = queueIndex; i < turnQueue.Count && turnOrderPreview.Count < turnOrderEntries.Count; i++)
                {
                    var unit = turnQueue[i];
                    if (IsBattleActive(unit)) turnOrderPreview.Add(unit);
                }
            }

            // Fill the remaining rail from the next speed-sorted round. Repeat
            // that living roster when fewer than eight combatants remain, so
            // the rail always previews eight upcoming turns during battle.
            nextRoundPreview.Clear();
            AddAlive(nextRoundPreview, allies);
            AddAlive(nextRoundPreview, enemies);
            nextRoundPreview.Sort(CompareTurnOrder);
            if (nextRoundPreview.Count == 0) return;

            var nextIndex = 0;
            while (turnOrderPreview.Count < turnOrderEntries.Count)
            {
                turnOrderPreview.Add(nextRoundPreview[nextIndex]);
                nextIndex = (nextIndex + 1) % nextRoundPreview.Count;
            }
        }
        static void RefreshUnit(BattleUnitView unit)
        {
            if (unit == null || unit.definition == null) return;
            if (unit.healthBar != null) { unit.healthBar.maxValue = unit.definition.maxHealth; unit.healthBar.value = unit.health; }
            if (unit.healthFill != null)
                unit.healthFill.fillAmount = unit.health / (float)Mathf.Max(1, unit.definition.maxHealth);
            if (unit.healthText != null)
                unit.healthText.text = unit.shield > 0 ? $"{unit.health}/{unit.definition.maxHealth} +{unit.shield}" : $"{unit.health}/{unit.definition.maxHealth}";
            if (unit.battlefieldImage != null) unit.battlefieldImage.color = Color.white;
        }

        static void SetUnitVisualsActive(BattleUnitView unit, bool active)
        {
            if (unit == null) return;
            if (unit.root != null) unit.root.SetActive(active);
            if (unit.targetButton != null) unit.targetButton.gameObject.SetActive(active);
            if (unit.battlefieldImage != null) unit.battlefieldImage.gameObject.SetActive(active);
            if (unit.gifPlayer != null) unit.gifPlayer.gameObject.SetActive(active);
            if (unit.portrait != null) unit.portrait.gameObject.SetActive(active);
            if (unit.healthBar != null) unit.healthBar.gameObject.SetActive(active);
            if (unit.healthFill != null) unit.healthFill.gameObject.SetActive(active);
            if (unit.healthText != null) unit.healthText.gameObject.SetActive(active);
        }

        static RectTransform MovementRectFor(BattleUnitView unit)
        {
            if (unit == null) return null;
            if (unit.gifPlayer != null && unit.gifPlayer.gameObject.activeInHierarchy)
                return unit.gifPlayer.GetComponent<RectTransform>();
            if (unit.battlefieldImage != null)
                return unit.battlefieldImage.rectTransform;
            return unit.root != null ? unit.root.GetComponent<RectTransform>() : null;
        }
        static bool AnyAlive(List<BattleUnitView> list) => list.Exists(IsBattleActive);
        static BattleUnitView FirstAlive(List<BattleUnitView> list) => list.Find(IsBattleActive);
        static bool IsBattleActive(BattleUnitView unit) => unit != null && unit.IsAlive && (unit.root == null || unit.root.activeInHierarchy);
        static BattleUnitView LowestHealthAlive(List<BattleUnitView> list)
        {
            BattleUnitView result = null; var ratio = float.MaxValue;
            foreach (var unit in list) { if (!IsBattleActive(unit) || unit.definition == null) continue; var value = unit.health / (float)unit.definition.maxHealth; if (value < ratio) { ratio = value; result = unit; } }
            return result;
        }

        void LoadPlayerParty()
        {
            if (menuContentDatabase == null)
            {
                menuContentDatabase = Resources.Load<MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
                if (menuContentDatabase == null)
                    menuContentDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
#endif
            }

            for (var i = 0; i < allies.Count; i++)
            {
                SetUnitVisualsActive(allies[i], false);
            }

            if (menuContentDatabase == null || SelectedPartyCharacterIds == null || SelectedPartyCharacterIds.Count == 0)
                return;

            for (var i = 0; i < Mathf.Min(SelectedPartyCharacterIds.Count, allies.Count); i++)
            {
                var characterId = SelectedPartyCharacterIds[i];
                var character = menuContentDatabase.FindCharacter(characterId);
                var view = allies[i];
                if (character == null || view == null) continue;

                view.definition = BuildBattleDefinitionFromCharacter(character);
                SetUnitVisualsActive(view, true);
            }
        }

        void LoadStageData()
        {
            if (menuContentDatabase == null)
            {
                menuContentDatabase = Resources.Load<MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
                if (menuContentDatabase == null)
                    menuContentDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
#endif
            }

            if (menuContentDatabase == null || string.IsNullOrEmpty(ActiveStageId))
                return;

            menuContentDatabase.EnsureDefaultPlayModeStages();
            menuContentDatabase = ChapterOneStoryRuntime.Apply(menuContentDatabase);
            if (ActiveStageId.StartsWith("chapter_2_", StringComparison.OrdinalIgnoreCase))
                menuContentDatabase = ChapterTwoStoryRuntime.Apply(menuContentDatabase);

            currentStage = null;
            StageEntry stage = null;
            foreach (var chapter in menuContentDatabase.storyChapters)
            {
                stage = chapter.stages.Find(x => x.id == ActiveStageId);
                if (stage != null) break;
            }
            if (stage == null) stage = menuContentDatabase.resourceStages.Find(x => x.id == ActiveStageId);
            if (stage == null) stage = menuContentDatabase.sanctumStages.Find(x => x.id == ActiveStageId);
            if (stage == null) stage = menuContentDatabase.weaponStages.Find(x => x.id == ActiveStageId);

            if (stage == null) return;
            currentStage = stage;
        }

        void LoadCurrentBattlePhase()
        {
            currentPhase = null;
            if (currentStage?.battlePhases != null &&
                currentStage.battlePhases.Count > 0 &&
                currentPhaseIndex >= 0 &&
                currentPhaseIndex < currentStage.battlePhases.Count)
                currentPhase = currentStage.battlePhases[currentPhaseIndex];

            if (currentPhase?.allies != null && currentPhase.allies.Count > 0)
            {
                for (var i = 0; i < allies.Count; i++)
                {
                    if (allies[i] != null && allies[i].root != null)
                        allies[i].root.SetActive(false);
                }

                var allyPhaseLevel = currentPhase.enemyLevel > 0 ? currentPhase.enemyLevel : currentStage?.enemyLevel ?? 1;
                for (var i = 0; i < Mathf.Min(currentPhase.allies.Count, allies.Count); i++)
                {
                    var view = allies[i];
                    if (view == null) continue;
                    view.definition = CloneAndScaleDefinition(currentPhase.allies[i], allyPhaseLevel);
                    if (view.root != null) view.root.SetActive(true);
                }

                ApplySelectedPartyToAllySlots(currentPhase.allies.Count);
            }

            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].root != null)
                    enemies[i].root.SetActive(false);
            }

            var phaseEnemies = currentPhase != null ? currentPhase.enemies : currentStage?.enemies;
            var phaseBoss = currentPhase != null ? currentPhase.boss : currentStage?.boss;
            var level = currentPhase != null && currentPhase.enemyLevel > 0 ? currentPhase.enemyLevel : currentStage?.enemyLevel ?? 1;

            var enemyIndex = 0;
            if (phaseEnemies != null)
            {
                for (; enemyIndex < Mathf.Min(phaseEnemies.Count, 4); enemyIndex++)
                {
                    var view = enemies[enemyIndex];
                    if (view == null) continue;
                    view.definition = CloneAndScaleDefinition(phaseEnemies[enemyIndex], level);
                    if (view.root != null) view.root.SetActive(true);
                }
            }

            if (phaseBoss != null && !string.IsNullOrWhiteSpace(phaseBoss.id) && enemies.Count > 4 && enemies[4] != null)
            {
                var bossView = enemies[4];
                bossView.definition = CloneAndScaleDefinition(phaseBoss, level);
                if (bossView.root != null) bossView.root.SetActive(true);
            }
        }

        void SummonInactiveEnemies(int maxActiveSummons)
        {
            var activeSummons = 0;
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.definition == null) continue;
                if (string.Equals(enemy.definition.id, "nephkar", StringComparison.OrdinalIgnoreCase)) continue;
                if (enemy.root != null && enemy.root.activeSelf && enemy.IsAlive) activeSummons++;
            }

            foreach (var enemy in enemies)
            {
                if (activeSummons >= maxActiveSummons) break;
                if (enemy == null || enemy.definition == null) continue;
                if (string.Equals(enemy.definition.id, "nephkar", StringComparison.OrdinalIgnoreCase)) continue;
                if (enemy.root != null && enemy.root.activeSelf && enemy.IsAlive) continue;
                enemy.health = enemy.definition.maxHealth;
                if (enemy.root != null) enemy.root.SetActive(true);
                if (enemy.battlefieldImage != null) enemy.battlefieldImage.gameObject.SetActive(true);
                RefreshUnit(enemy);
                activeSummons++;
            }
            RefreshTurnOrder();
        }

        void ApplySelectedPartyToAllySlots(int startSlot)
        {
            if (menuContentDatabase == null || SelectedPartyCharacterIds == null) return;
            startSlot = Mathf.Clamp(startSlot, 0, allies.Count);
            for (var i = 0; i < SelectedPartyCharacterIds.Count && startSlot + i < allies.Count; i++)
            {
                var character = menuContentDatabase.FindCharacter(SelectedPartyCharacterIds[i]);
                var view = allies[startSlot + i];
                if (character == null || view == null) continue;

                view.definition = BuildBattleDefinitionFromCharacter(character);
                SetUnitVisualsActive(view, true);
            }
        }

        BattleUnitDefinition BuildBattleDefinitionFromCharacter(CharacterEntry character)
        {
            if (character == null) return null;

            var level = CharacterProgressionState.GetLevel(character.id);
            var constellation = CharacterProgressionState.GetConstellation(character.id);
            var levelScale = 1f + (Mathf.Max(1, level) - 1) * 0.065f;
            var constellationScale = 1f + Mathf.Clamp(constellation, 0, CharacterProgressionState.ConstellationCount) * 0.04f;
            var statScale = levelScale * constellationScale;

            var weaponAttack = EquippedWeaponState.Instance?.GetDisplayAtk() ?? 0;
            var artifactAttack = 0;
            var artifactHealth = 0;
            var artifact = MetaProgressState.Instance?.GetEquippedArtifact();
            if (artifact != null)
            {
                artifactAttack = artifact.atkBonus + artifact.setBonusAtk;
                artifactHealth = artifact.hpBonus;
            }

            var definition = new BattleUnitDefinition
            {
                id = character.id,
                displayName = character.displayName,
                maxHealth = Mathf.Max(1, Mathf.RoundToInt(character.maxHealth * statScale) + artifactHealth),
                attack = Mathf.Max(1, Mathf.RoundToInt(character.attack * statScale) + weaponAttack + artifactAttack),
                defense = Mathf.Max(0, Mathf.RoundToInt(character.defense * statScale) + Mathf.RoundToInt(artifactAttack * 0.08f)),
                speed = Mathf.Max(1, character.speed + Mathf.FloorToInt((level - 1) / 20f)),
                element = character.element,
                portrait = character.portrait,
                battlefieldSprite = character.chibi != null ? character.chibi : character.portrait,
                attackEffectPrefabs = character.attackEffectPrefabs != null ? new List<GameObject>(character.attackEffectPrefabs) : new List<GameObject>(),
                attackEffectOffset = character.attackEffectOffset,
                attackEffectScale = character.attackEffectScale == Vector3.zero ? Vector3.one : character.attackEffectScale,
                isRanged = character.attributes.Contains("Ranged") || character.attributes.Contains("tầm xa"),
                skills = new List<BattleSkillDefinition> { new BattleSkillDefinition { id = "attack", displayName = "Tấn Công", powerMultiplier = 1f } }
            };

            return definition;
        }

        void ResetCombatDialogueTriggers()
        {
            foreach (var trigger in ActiveCombatTriggers())
                if (trigger != null) trigger.played = false;
        }

        bool TryPlayCombatDialogue(CombatDialogueTriggerType triggerType, BattleUnitView unit = null, int roundValue = 0, Action completed = null)
        {
            if (combatDialogueUI == null) return false;
            foreach (var trigger in ActiveCombatTriggers())
            {
                if (trigger == null || trigger.played || trigger.triggerType != triggerType) continue;
                if (trigger.dialogue == null || trigger.dialogue.beats.Count == 0) continue;
                if (triggerType == CombatDialogueTriggerType.RoundStart && trigger.round != roundValue) continue;
                if (!string.IsNullOrWhiteSpace(trigger.unitId) && unit?.definition?.id != trigger.unitId) continue;
                if (triggerType == CombatDialogueTriggerType.BossHealthBelowPercent && !HealthBelow(unit, trigger.healthPercent)) continue;
                if (triggerType == CombatDialogueTriggerType.TotalEnemyHealthBelowPercent && !TotalEnemyHealthBelow(trigger.healthPercent)) continue;
                if (triggerType == CombatDialogueTriggerType.EnemyCountAtOrBelow && AliveCount(enemies) > trigger.enemyCount) continue;

                trigger.played = true;
                var shouldPause = pauseBattleDuringDialogue && trigger.pauseCombat;
                if (shouldPause)
                {
                    paused = true;
                    ApplyPlaybackSpeed();
                }
                if (hideCombatHudDuringDialogue)
                    SetCombatHudSuppressed(true);

                combatDialogueUI.Play(trigger.dialogue, () =>
                {
                    if (hideCombatHudDuringDialogue)
                        SetCombatHudSuppressed(false);
                    if (shouldPause)
                    {
                        paused = false;
                        ApplyPlaybackSpeed();
                    }
                    ApplyTriggerAction(trigger, unit);
                    if (trigger.actionAfterDialogue != CombatTriggerActionType.ReturnToStoryWithoutResult)
                        completed?.Invoke();
                });
                return true;
            }

            return false;
        }

        void SetCombatHudSuppressed(bool suppressed)
        {
            var visible = !suppressed;
            if (skillPanel != null) skillPanel.SetActive(visible);
            if (selectionHintText != null) selectionHintText.gameObject.SetActive(visible);
            if (roundText != null) roundText.gameObject.SetActive(visible);
            if (currentActorText != null) currentActorText.gameObject.SetActive(visible);
            if (speedButton != null) speedButton.gameObject.SetActive(visible);
            if (speedText != null) speedText.gameObject.SetActive(visible);
            if (autoButton != null) autoButton.gameObject.SetActive(visible);
            if (autoText != null) autoText.gameObject.SetActive(visible);
            if (pauseButton != null) pauseButton.gameObject.SetActive(visible);

            foreach (var entry in turnOrderEntries)
                if (entry?.root != null) entry.root.SetActive(visible);

            SetUnitHudVisible(allies, visible);
            SetUnitHudVisible(enemies, visible);

            foreach (var root in extraHudRootsToHideDuringDialogue)
                if (root != null) root.SetActive(visible);
        }

        static void SetUnitHudVisible(List<BattleUnitView> units, bool visible)
        {
            if (units == null) return;
            foreach (var unit in units)
            {
                if (unit == null) continue;
                if (unit.portrait != null) unit.portrait.gameObject.SetActive(visible);
                if (unit.healthBar != null) unit.healthBar.gameObject.SetActive(visible);
                if (unit.healthFill != null) unit.healthFill.gameObject.SetActive(visible);
                if (unit.healthText != null) unit.healthText.gameObject.SetActive(visible);
            }
        }

        IEnumerable<CombatDialogueTrigger> ActiveCombatTriggers()
        {
            if (currentPhase?.combatDialogueTriggers != null && currentPhase.combatDialogueTriggers.Count > 0)
                return currentPhase.combatDialogueTriggers;
            return currentStage?.combatDialogueTriggers ?? new List<CombatDialogueTrigger>();
        }

        bool TryPlayAggregateCombatTriggers()
        {
            if (TryPlayCombatDialogue(CombatDialogueTriggerType.TotalEnemyHealthBelowPercent)) return true;
            if (TryPlayCombatDialogue(CombatDialogueTriggerType.EnemyCountAtOrBelow)) return true;
            return false;
        }

        bool HasNextPhase() => currentStage?.battlePhases != null && currentPhaseIndex + 1 < currentStage.battlePhases.Count;

        void StartNextPhase()
        {
            if (!HasNextPhase()) { CompleteVictory(); return; }
            currentPhaseIndex++;
            LoadCurrentBattlePhase();
            InitializeTeam(enemies, false);
            round = 0;
            queueIndex = 0;
            selectedSkillIndex = -1;
            currentActor = null;
            resolving = false;
            ResetCombatDialogueTriggers();
            ApplyPlaybackSpeed();
            StartNextRound();
            TryPlayCombatDialogue(CombatDialogueTriggerType.PhaseStart);
        }

        void ApplyTriggerAction(CombatDialogueTrigger trigger, BattleUnitView unit)
        {
            if (trigger == null) return;
            switch (trigger.actionAfterDialogue)
            {
                case CombatTriggerActionType.StartNextPhase:
                    StartNextPhase();
                    break;
                case CombatTriggerActionType.ConvertUnitToAlly:
                    ConvertEnemyToAlly(ResolveUnit(trigger.convertUnitId, unit));
                    break;
                case CombatTriggerActionType.ConvertUnitToAllyAndStartNextPhase:
                    ConvertEnemyToAlly(ResolveUnit(trigger.convertUnitId, unit));
                    StartNextPhase();
                    break;
                case CombatTriggerActionType.KillAllEnemiesAndPlayPhaseVictory:
                    KillAllEnemies();
                    if (!TryPlayCombatDialogue(CombatDialogueTriggerType.PhaseVictory, null, 0, CompleteVictory))
                        CompleteVictory();
                    break;
                case CombatTriggerActionType.ReturnToStoryWithoutResult:
                    ReturnToStoryModeAfterDialogue();
                    break;
                case CombatTriggerActionType.SetElioHealthToTenPercentAndPlayPhaseVictory:
                    SetAllyHealthPercent("elio", 10);
                    if (!TryPlayCombatDialogue(CombatDialogueTriggerType.PhaseVictory, null, 0))
                        BeginCurrentTurn();
                    break;
                case CombatTriggerActionType.HealElioToThirtyFivePercent:
                    SetAllyHealthPercent("elio", 35);
                    break;
                case CombatTriggerActionType.AddAurelianAlly:
                    AddDatabaseCharacterToAllySlot("Aurelian", 2, 100);
                    break;
            }
        }

        void SetAllyHealthPercent(string characterId, int percent)
        {
            var unit = ResolveUnit(characterId, null);
            if (unit == null || unit.definition == null) return;
            unit.health = Mathf.Clamp(Mathf.CeilToInt(unit.definition.maxHealth * Mathf.Clamp(percent, 1, 100) / 100f), 1, unit.definition.maxHealth);
            RefreshUnit(unit);
        }

        void AddDatabaseCharacterToAllySlot(string characterName, int slotIndex, int healthPercent)
        {
            if (menuContentDatabase == null || allies == null || allies.Count == 0) return;
            slotIndex = Mathf.Clamp(slotIndex, 0, allies.Count - 1);
            var character = menuContentDatabase.characters?.Find(x =>
                string.Equals(x.id, characterName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.displayName, characterName, StringComparison.OrdinalIgnoreCase));
            var view = allies[slotIndex];
            if (character == null || view == null) return;

            view.definition = BuildBattleDefinitionFromCharacter(character);
            view.isPlayer = true;
            view.health = Mathf.Clamp(Mathf.CeilToInt(view.definition.maxHealth * Mathf.Clamp(healthPercent, 1, 100) / 100f), 1, view.definition.maxHealth);
            if (view.root != null) view.root.SetActive(true);
            RefreshUnit(view);
        }

        void KillAllEnemies()
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                enemy.health = 0;
                RefreshUnit(enemy);
                if (enemy.root != null) enemy.root.SetActive(false);
            }
            turnQueue.RemoveAll(unit => unit == null || !unit.IsAlive);
            RefreshTurnOrder(true);
            HideSkills();
            resolving = false;
        }

        BattleUnitView ResolveUnit(string unitId, BattleUnitView fallback)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return fallback;
            foreach (var enemy in enemies)
                if (enemy?.definition != null && string.Equals(enemy.definition.id, unitId, StringComparison.OrdinalIgnoreCase))
                    return enemy;
            foreach (var ally in allies)
                if (ally?.definition != null && string.Equals(ally.definition.id, unitId, StringComparison.OrdinalIgnoreCase))
                    return ally;
            return fallback;
        }

        void ConvertEnemyToAlly(BattleUnitView enemy)
        {
            if (enemy == null || enemy.definition == null) return;
            var allySlot = allies.Find(x => x != null && (x.root == null || !x.root.activeSelf || !x.IsAlive));
            if (allySlot == null) return;

            var hp = Mathf.Clamp(enemy.health, 1, enemy.definition.maxHealth);
            allySlot.definition = CloneDefinition(enemy.definition);
            allySlot.health = hp;
            allySlot.isPlayer = true;
            if (allySlot.root != null) allySlot.root.SetActive(true);
            RefreshUnit(allySlot);

            enemy.health = 0;
            if (enemy.root != null) enemy.root.SetActive(false);
            RefreshTurnOrder();
        }

        BattleUnitDefinition CloneDefinition(BattleUnitDefinition template)
        {
            if (template == null) return null;
            return new BattleUnitDefinition
            {
                id = template.id,
                displayName = template.displayName,
                element = template.element,
                portrait = template.portrait,
                battlefieldSprite = template.battlefieldSprite,
                idleClip = template.idleClip,
                attackClip = template.attackClip,
                attackEffectPrefabs = template.attackEffectPrefabs != null ? new List<GameObject>(template.attackEffectPrefabs) : new List<GameObject>(),
                attackEffectOffset = template.attackEffectOffset,
                attackEffectScale = template.attackEffectScale == Vector3.zero ? Vector3.one : template.attackEffectScale,
                isRanged = template.isRanged,
                maxHealth = template.maxHealth,
                attack = template.attack,
                defense = template.defense,
                speed = template.speed,
                skills = template.skills != null ? new List<BattleSkillDefinition>(template.skills) : new List<BattleSkillDefinition>()
            };
        }

        static bool HealthBelow(BattleUnitView unit, int percent)
        {
            if (unit?.definition == null || unit.definition.maxHealth <= 0) return false;
            return unit.health <= Mathf.CeilToInt(unit.definition.maxHealth * Mathf.Clamp(percent, 1, 100) / 100f);
        }

        bool TotalEnemyHealthBelow(int percent)
        {
            var max = 0;
            var current = 0;
            foreach (var enemy in enemies)
            {
                if (enemy?.definition == null || enemy.root == null || !enemy.root.activeSelf) continue;
                max += Mathf.Max(1, enemy.definition.maxHealth);
                current += Mathf.Clamp(enemy.health, 0, enemy.definition.maxHealth);
            }
            if (max <= 0) return false;
            return current <= Mathf.CeilToInt(max * Mathf.Clamp(percent, 1, 100) / 100f);
        }

        static int AliveCount(List<BattleUnitView> units)
        {
            var count = 0;
            foreach (var unit in units)
                if (unit != null && unit.IsAlive) count++;
            return count;
        }

        BattleUnitDefinition CloneAndScaleDefinition(BattleUnitDefinition template, int level)
        {
            if (template == null || string.IsNullOrWhiteSpace(template.id)) return null;
            var def = new BattleUnitDefinition();
            def.id = template.id;
            def.displayName = template.displayName;
            def.element = template.element;
            def.portrait = template.portrait;
            def.battlefieldSprite = template.battlefieldSprite;
            def.idleClip = template.idleClip;
            def.attackClip = template.attackClip;
            def.attackEffectPrefabs = template.attackEffectPrefabs != null ? new List<GameObject>(template.attackEffectPrefabs) : new List<GameObject>();
            def.attackEffectOffset = template.attackEffectOffset;
            def.attackEffectScale = template.attackEffectScale == Vector3.zero ? Vector3.one : template.attackEffectScale;
            def.isRanged = template.isRanged;
            
            float scale = 1f + (level - 1) * 0.1f;
            def.maxHealth = Mathf.RoundToInt(template.maxHealth * scale);
            def.attack = Mathf.RoundToInt(template.attack * scale);
            def.defense = Mathf.RoundToInt(template.defense * scale);
            def.speed = template.speed;
            def.skills = new List<BattleSkillDefinition>(template.skills);
            return def;
        }
    }
}



