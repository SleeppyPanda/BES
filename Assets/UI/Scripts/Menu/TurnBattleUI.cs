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
        public Sprite portrait;
        public Sprite battlefieldSprite;
        public UIGifClip idleClip;
        public UIGifClip attackClip;
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
        [Header("Timing and animation")]
        [SerializeField, Min(0.05f)] float actionWindup = 0.45f;
        [SerializeField, Min(0.05f)] float actionRecovery = 0.35f;
        [SerializeField] List<Animator> panelAnimators = new();
        [Header("Battle events")]
        [SerializeField] UnityEvent onVictory;
        [SerializeField] UnityEvent onDefeat;
        [SerializeField] UnityEvent<int> onRoundStarted;
        [SerializeField] MenuContentDatabase menuContentDatabase;
        [Header("Story dialogue popups")]
        [SerializeField] DialogueSequenceUI combatDialogueUI;
        [SerializeField] bool pauseBattleDuringDialogue = true;

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

        void Awake()
        {
            EnsureDialogueUI();
            WireControls();
        }
        void OnEnable() { ResetBattle(); }
        void OnDisable() { paused = false; ApplyPlaybackSpeed(); }

        void Update()
        {
            if (paused || resolving || currentActor == null || !currentActor.IsAlive) return;
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
            GameManager.Instance?.SaveGame();
        }

        void ExitBattleToHome()
        {
            SaveBattleProgress();
            ActiveStageId = currentStage != null ? currentStage.id : ActiveStageId;
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            paused = false;
            ApplyPlaybackSpeed();
            navigator?.Open(MenuScreenId.Home);
            homeController?.Refresh();
        }

        void OpenCharacterDestination(CharacterCollectionPanel.CharacterCollectionDestination destination)
        {
            SaveBattleProgress();
            if (losePanel != null) losePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            paused = false;
            ApplyPlaybackSpeed();
            navigator?.Open(MenuScreenId.Home);
            characterCollection?.OpenDestination(destination, homeController != null ? homeController.CurrentCharacterId : null);
        }

        void OpenRecruit()
        {
            SaveBattleProgress();
            if (losePanel != null) losePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            paused = false;
            ApplyPlaybackSpeed();
            navigator?.Open(MenuScreenId.Home);
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
            pauseResumeButton ??= FindButton(pausePanel, "ResumeButton", "ContinueButton", "Continue", "Resume", "TiepTuc", "Tieptuc");
            if (pausePanel != null && pauseResumeButton == null)
                pauseResumeButton = CreateRuntimePauseResumeButton();

            winReturnButton ??= FindButton(winPanel, "ContinueButton", "Continue", "NextButton", "TiepTuc", "Tieptuc");

            loseReturnButton ??= FindButton(losePanel, "ExitButton", "CloseButton", "ReturnButton", "Thoat", "Exit");
            loseExitButton ??= FindButton(losePanel, "ExitButton", "CloseButton", "ReturnButton", "Thoat", "Exit");
            loseRetryButton ??= FindButton(losePanel, "RetryButton", "AgainButton", "ReplayButton", "ChoiLai");
            levelBtn ??= FindButton(losePanel, "LevelButton", "CharacterLevelButton", "CapNhanVat", "CharacterLevel");
            equipBtn ??= FindButton(losePanel, "EquipmentButton", "EquipButton", "TrangBi", "Equipment");
            skillBtn ??= FindButton(losePanel, "SkillButton", "KyNang", "Skill");
            constellationBtn ??= FindButton(losePanel, "ConstellationButton", "TinhMenh", "Constellation");
            recruitBtn ??= FindButton(losePanel, "RecruitButton", "WishButton", "ChieuMo", "Recruit", "Wish");
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
            InitializeTeam(allies, true); InitializeTeam(enemies, false);
            round = 0; queueIndex = 0; selectedSkillIndex = -1; currentActor = null;
            resolving = false; paused = false; autoMode = false; playbackSpeed = 1f;
            ResetCombatDialogueTriggers();
            if (pausePanel != null) pausePanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            ApplyPlaybackSpeed(); StartNextRound();
            TryPlayCombatDialogue(CombatDialogueTriggerType.BattleStart);
        }

        static void InitializeTeam(List<BattleUnitView> team, bool isPlayer)
        {
            for (var i = 0; i < team.Count; i++)
            {
                var unit = team[i]; if (unit == null || unit.definition == null) continue;
                unit.isPlayer = isPlayer; unit.setupIndex = i; unit.health = unit.definition.maxHealth;
                if (unit.root != null) unit.root.SetActive(true);

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
            onRoundStarted?.Invoke(round); BeginCurrentTurn();
            TryPlayCombatDialogue(CombatDialogueTriggerType.RoundStart, null, round);
        }

        static void AddAlive(List<BattleUnitView> destination, List<BattleUnitView> source) { foreach (var unit in source) if (unit != null && unit.IsAlive) destination.Add(unit); }
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

            System.Action onStrike = () =>
            {
                var raw = Mathf.RoundToInt(actor.definition.attack * (skill?.powerMultiplier ?? 1f));
                target.health = Mathf.Max(0, target.health - Mathf.Max(1, raw - target.definition.defense));
                
                if (target.gifPlayer != null && target.health == 0)
                {
                    target.gifPlayer.gameObject.SetActive(false);
                }
                else if (target.animator != null)
                {
                    target.animator.SetTrigger(target.health == 0 ? "Defeat" : "Hit");
                }

                RefreshUnit(target);
                TryPlayCombatDialogue(CombatDialogueTriggerType.BossHealthBelowPercent, target);

                if (target.health == 0)
                {
                    if (target.targetButton != null) target.targetButton.interactable = false;
                    if (target.battlefieldImage != null) target.battlefieldImage.gameObject.SetActive(false);
                    if (target.gifPlayer != null) target.gifPlayer.gameObject.SetActive(false);
                    if (target.root != null) target.root.SetActive(false);
                    TryPlayCombatDialogue(CombatDialogueTriggerType.EnemyDefeated, target);
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
        static BattleSkillDefinition GetSkill(BattleUnitView actor, int index)
        {
            if (actor?.definition?.skills == null || actor.definition.skills.Count == 0) return new BattleSkillDefinition();
            return actor.definition.skills[Mathf.Clamp(index, 0, actor.definition.skills.Count - 1)];
        }
        void ProcessCombatDrops()
        {
            var stageId = ActiveStageId;
            ActiveStageId = null;

            if (string.IsNullOrEmpty(stageId))
            {
                if (PlayerWallet.Instance != null)
                {
                    PlayerWallet.Instance.AddCoins(200);
                }
                return;
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
        }

        void FinishTurn()
        {
            resolving = false; selectedSkillIndex = -1;
            if (!AnyAlive(enemies))
            {
                HideSkills();
                RefreshTurnOrder(true);
                if (!TryPlayCombatDialogue(CombatDialogueTriggerType.BeforeVictory, null, 0, CompleteVictory))
                    CompleteVictory();
                return;
            }
            if (!AnyAlive(allies))
            {
                HideSkills();
                RefreshTurnOrder(true);
                if (losePanel != null) losePanel.SetActive(true);
                onDefeat?.Invoke();
                return;
            }
            queueIndex++; BeginCurrentTurn();
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
                navigator?.Open(MenuScreenId.ResourceStages);
            }
            else
            {
                navigator?.Open(MenuScreenId.StoryParty);
                storyModeController?.CompleteStoryBattle();
            }
        }

        void CompleteVictory()
        {
            ProcessCombatDrops();
            if (winPanel != null) winPanel.SetActive(true);
            onVictory?.Invoke();
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
                    if (unit != null && unit.IsAlive) turnOrderPreview.Add(unit);
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
            if (unit.healthText != null) unit.healthText.text = $"{unit.health}/{unit.definition.maxHealth}";
            if (unit.battlefieldImage != null) unit.battlefieldImage.color = Color.white;
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
        static bool AnyAlive(List<BattleUnitView> list) => list.Exists(unit => unit != null && unit.IsAlive);
        static BattleUnitView FirstAlive(List<BattleUnitView> list) => list.Find(unit => unit != null && unit.IsAlive);
        static BattleUnitView LowestHealthAlive(List<BattleUnitView> list)
        {
            BattleUnitView result = null; var ratio = float.MaxValue;
            foreach (var unit in list) { if (unit == null || !unit.IsAlive || unit.definition == null) continue; var value = unit.health / (float)unit.definition.maxHealth; if (value < ratio) { ratio = value; result = unit; } }
            return result;
        }

        void LoadPlayerParty()
        {
            if (menuContentDatabase == null)
            {
                menuContentDatabase = Resources.Load<MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
                if (menuContentDatabase == null)
                    menuContentDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/_Project/Data/UI/MenuContentDatabase.asset");
#endif
            }

            if (menuContentDatabase == null || SelectedPartyCharacterIds == null || SelectedPartyCharacterIds.Count == 0)
                return;

            for (var i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null && allies[i].root != null)
                    allies[i].root.SetActive(false);
            }

            for (var i = 0; i < Mathf.Min(SelectedPartyCharacterIds.Count, allies.Count); i++)
            {
                var characterId = SelectedPartyCharacterIds[i];
                var character = menuContentDatabase.FindCharacter(characterId);
                var view = allies[i];
                if (character == null || view == null) continue;

                var def = new BattleUnitDefinition();
                def.id = character.id;
                def.displayName = character.displayName;
                
                var level = CharacterProgressionState.GetLevel(character.id);
                float scale = 1f + (level - 1) * 0.1f;
                def.maxHealth = Mathf.RoundToInt(character.maxHealth * scale);
                def.attack = Mathf.RoundToInt(character.attack * scale);
                def.defense = 5;
                def.speed = 10;
                
                def.portrait = character.portrait;
                def.battlefieldSprite = character.chibi != null ? character.chibi : character.portrait;
                def.isRanged = character.attributes.Contains("Ranged") || character.attributes.Contains("tầm xa");

                var skill = new BattleSkillDefinition { id = "attack", displayName = "Tấn Công", powerMultiplier = 1f };
                def.skills = new List<BattleSkillDefinition> { skill };

                view.definition = def;
                if (view.root != null) view.root.SetActive(true);
            }
        }

        void LoadStageData()
        {
            if (menuContentDatabase == null)
            {
                menuContentDatabase = Resources.Load<MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
                if (menuContentDatabase == null)
                    menuContentDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/_Project/Data/UI/MenuContentDatabase.asset");
#endif
            }

            if (menuContentDatabase == null || string.IsNullOrEmpty(ActiveStageId))
                return;

            ChapterOneStoryRuntime.Apply(menuContentDatabase);

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

            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].root != null)
                    enemies[i].root.SetActive(false);
            }

            int enemyIndex = 0;
            if (stage.enemies != null)
            {
                for (; enemyIndex < Mathf.Min(stage.enemies.Count, 4); enemyIndex++)
                {
                    var view = enemies[enemyIndex];
                    if (view == null) continue;
                    view.definition = CloneAndScaleDefinition(stage.enemies[enemyIndex], stage.enemyLevel);
                    if (view.root != null) view.root.SetActive(true);
                }
            }

            if (stage.boss != null && enemies.Count > 4 && enemies[4] != null)
            {
                var bossView = enemies[4];
                bossView.definition = CloneAndScaleDefinition(stage.boss, stage.enemyLevel);
                if (bossView.root != null) bossView.root.SetActive(true);
            }
        }

        void ResetCombatDialogueTriggers()
        {
            if (currentStage?.combatDialogueTriggers == null) return;
            foreach (var trigger in currentStage.combatDialogueTriggers)
                if (trigger != null) trigger.played = false;
        }

        bool TryPlayCombatDialogue(CombatDialogueTriggerType triggerType, BattleUnitView unit = null, int roundValue = 0, Action completed = null)
        {
            if (currentStage?.combatDialogueTriggers == null || combatDialogueUI == null) return false;
            foreach (var trigger in currentStage.combatDialogueTriggers)
            {
                if (trigger == null || trigger.played || trigger.triggerType != triggerType) continue;
                if (trigger.dialogue == null || trigger.dialogue.beats.Count == 0) continue;
                if (triggerType == CombatDialogueTriggerType.RoundStart && trigger.round != roundValue) continue;
                if (!string.IsNullOrWhiteSpace(trigger.unitId) && unit?.definition?.id != trigger.unitId) continue;
                if (triggerType == CombatDialogueTriggerType.BossHealthBelowPercent && !HealthBelow(unit, trigger.healthPercent)) continue;

                trigger.played = true;
                var shouldPause = pauseBattleDuringDialogue && trigger.pauseCombat;
                if (shouldPause)
                {
                    paused = true;
                    ApplyPlaybackSpeed();
                }

                combatDialogueUI.Play(trigger.dialogue, () =>
                {
                    if (shouldPause)
                    {
                        paused = false;
                        ApplyPlaybackSpeed();
                    }
                    completed?.Invoke();
                });
                return true;
            }

            return false;
        }

        static bool HealthBelow(BattleUnitView unit, int percent)
        {
            if (unit?.definition == null || unit.definition.maxHealth <= 0) return false;
            return unit.health <= Mathf.CeilToInt(unit.definition.maxHealth * Mathf.Clamp(percent, 1, 100) / 100f);
        }

        BattleUnitDefinition CloneAndScaleDefinition(BattleUnitDefinition template, int level)
        {
            if (template == null) return null;
            var def = new BattleUnitDefinition();
            def.id = template.id;
            def.displayName = template.displayName;
            def.portrait = template.portrait;
            def.battlefieldSprite = template.battlefieldSprite;
            def.idleClip = template.idleClip;
            def.attackClip = template.attackClip;
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
