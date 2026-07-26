using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
        [SerializeField] GameObject winPanel;
        [SerializeField] Button winReturnButton;
        [SerializeField] MenuNavigator navigator;
        [SerializeField] StoryModePanelController storyModeController;
        [Header("Timing and animation")]
        [SerializeField, Min(0.05f)] float actionWindup = 0.45f;
        [SerializeField, Min(0.05f)] float actionRecovery = 0.35f;
        [SerializeField] List<Animator> panelAnimators = new();
        [Header("Battle events")]
        [SerializeField] UnityEvent onVictory;
        [SerializeField] UnityEvent onDefeat;
        [SerializeField] UnityEvent<int> onRoundStarted;

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

        void Awake() { WireControls(); }
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
            for (var i = 0; i < skillButtons.Count; i++) { var index = i; if (skillButtons[i] != null) skillButtons[i].onClick.AddListener(() => SelectSkill(index)); }
            foreach (var enemy in enemies) { var captured = enemy; if (enemy?.targetButton != null) enemy.targetButton.onClick.AddListener(() => SelectTarget(captured)); }
            if (speedButton != null) speedButton.onClick.AddListener(ToggleSpeed);
            if (autoButton != null) autoButton.onClick.AddListener(ToggleAuto);
            if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);
            if (winReturnButton != null) winReturnButton.onClick.AddListener(ReturnToStoryMode);
        }

        public void ResetBattle()
        {
            StopAllCoroutines();
            InitializeTeam(allies, true); InitializeTeam(enemies, false);
            round = 0; queueIndex = 0; selectedSkillIndex = -1; currentActor = null;
            resolving = false; paused = false; autoMode = false; playbackSpeed = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            ApplyPlaybackSpeed(); StartNextRound();
        }

        static void InitializeTeam(List<BattleUnitView> team, bool isPlayer)
        {
            for (var i = 0; i < team.Count; i++)
            {
                var unit = team[i]; if (unit == null || unit.definition == null) continue;
                unit.isPlayer = isPlayer; unit.setupIndex = i; unit.health = unit.definition.maxHealth;
                if (unit.root != null) unit.root.SetActive(true);
                if (unit.battlefieldImage != null) unit.battlefieldImage.gameObject.SetActive(true);
                if (unit.battlefieldImage != null) unit.battlefieldImage.sprite = unit.definition.battlefieldSprite;
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
            if (actor.animator != null) actor.animator.SetTrigger(skillIndex == 0 ? "Attack" : "Skill");
            yield return ScaledWait(actionWindup);
            var raw = Mathf.RoundToInt(actor.definition.attack * (skill?.powerMultiplier ?? 1f));
            target.health = Mathf.Max(0, target.health - Mathf.Max(1, raw - target.definition.defense));
            if (target.animator != null) target.animator.SetTrigger(target.health == 0 ? "Defeat" : "Hit");
            RefreshUnit(target);
            if (target.health == 0)
            {
                if (target.targetButton != null) target.targetButton.interactable = false;
                if (target.battlefieldImage != null) target.battlefieldImage.gameObject.SetActive(false);
                if (target.root != null) target.root.SetActive(false);
            }
            yield return ScaledWait(actionRecovery);
        }
        static BattleSkillDefinition GetSkill(BattleUnitView actor, int index)
        {
            if (actor?.definition?.skills == null || actor.definition.skills.Count == 0) return new BattleSkillDefinition();
            return actor.definition.skills[Mathf.Clamp(index, 0, actor.definition.skills.Count - 1)];
        }
        void FinishTurn()
        {
            resolving = false; selectedSkillIndex = -1;
            if (!AnyAlive(enemies))
            {
                HideSkills();
                RefreshTurnOrder(true);
                if (winPanel != null) winPanel.SetActive(true);
                onVictory?.Invoke();
                return;
            }
            if (!AnyAlive(allies)) { HideSkills(); RefreshTurnOrder(true); onDefeat?.Invoke(); return; }
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
            navigator?.Open(MenuScreenId.StoryParty);
            storyModeController?.CompleteStoryBattle();
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
        static bool AnyAlive(List<BattleUnitView> list) => list.Exists(unit => unit != null && unit.IsAlive);
        static BattleUnitView FirstAlive(List<BattleUnitView> list) => list.Find(unit => unit != null && unit.IsAlive);
        static BattleUnitView LowestHealthAlive(List<BattleUnitView> list)
        {
            BattleUnitView result = null; var ratio = float.MaxValue;
            foreach (var unit in list) { if (unit == null || !unit.IsAlive || unit.definition == null) continue; var value = unit.health / (float)unit.definition.maxHealth; if (value < ratio) { ratio = value; result = unit; } }
            return result;
        }
    }
}
