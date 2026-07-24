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
    public class BattleUnitView
    {
        public string id;
        public GameObject root;
        public Image portrait;
        public Slider healthBar;
        public Slider energyBar;
        public Animator animator;
        public int maxHealth = 100;
        public int attack = 10;
        [HideInInspector] public int health;
        public bool IsAlive => health > 0;
    }

    public class TurnBattleUI : MonoBehaviour
    {
        [SerializeField] List<BattleUnitView> allies = new();
        [SerializeField] List<BattleUnitView> enemies = new();
        [SerializeField] List<Button> skillButtons = new();
        [SerializeField] TMP_Text roundText;
        [SerializeField] TMP_Text turnText;
        [SerializeField] Button autoButton;
        [SerializeField] Button pauseButton;
        [SerializeField] GameObject pausePanel;
        [SerializeField] DialogueSequenceUI battleDialogue;
        [SerializeField, Min(0.05f)] float actionDelay = 0.4f;
        [SerializeField] UnityEvent onVictory;
        [SerializeField] UnityEvent onDefeat;
        int activeAlly;
        int round = 1;
        bool autoMode;
        bool resolving;

        void Start()
        {
            for (var i = 0; i < skillButtons.Count; i++)
            {
                var skill = i;
                if (skillButtons[i] != null) skillButtons[i].onClick.AddListener(() => UseSkill(skill));
            }
            if (autoButton != null) autoButton.onClick.AddListener(ToggleAuto);
            if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);
            ResetBattle();
        }

        void Update()
        {
            if (autoMode && !resolving && gameObject.activeInHierarchy) UseSkill(0);
        }

        public void ResetBattle()
        {
            foreach (var unit in allies) ResetUnit(unit);
            foreach (var unit in enemies) ResetUnit(unit);
            round = 1;
            activeAlly = 0;
            resolving = false;
            Refresh();
        }

        static void ResetUnit(BattleUnitView unit) { unit.health = unit.maxHealth; unit.root?.SetActive(true); }

        public void UseSkill(int skillIndex)
        {
            if (resolving || battleDialogue?.gameObject.activeSelf == true) return;
            var actor = NextAlive(allies, activeAlly);
            var target = NextAlive(enemies, 0);
            if (actor == null || target == null) return;
            StartCoroutine(ResolveTurn(actor, target, skillIndex));
        }

        IEnumerator ResolveTurn(BattleUnitView actor, BattleUnitView target, int skillIndex)
        {
            resolving = true;
            actor.animator?.SetTrigger(skillIndex == 0 ? "Attack" : "Skill");
            yield return new WaitForSecondsRealtime(actionDelay);
            Damage(target, Mathf.Max(1, actor.attack * (skillIndex + 1)));
            if (!AnyAlive(enemies)) { resolving = false; onVictory?.Invoke(); yield break; }
            var enemy = NextAlive(enemies, 0);
            var allyTarget = NextAlive(allies, 0);
            if (enemy != null && allyTarget != null)
            {
                enemy.animator?.SetTrigger("Attack");
                yield return new WaitForSecondsRealtime(actionDelay);
                Damage(allyTarget, Mathf.Max(1, enemy.attack));
            }
            if (!AnyAlive(allies)) { resolving = false; onDefeat?.Invoke(); yield break; }
            activeAlly = (allies.IndexOf(actor) + 1) % Mathf.Max(1, allies.Count);
            round++;
            resolving = false;
            Refresh();
        }

        void Damage(BattleUnitView unit, int amount)
        {
            unit.health = Mathf.Max(0, unit.health - amount);
            unit.animator?.SetTrigger(unit.health == 0 ? "Defeat" : "Hit");
            if (unit.health == 0) unit.root?.SetActive(false);
            RefreshUnit(unit);
        }

        static BattleUnitView NextAlive(List<BattleUnitView> list, int start)
        {
            if (list.Count == 0) return null;
            for (var i = 0; i < list.Count; i++) { var unit = list[(start + i) % list.Count]; if (unit.IsAlive) return unit; }
            return null;
        }
        static bool AnyAlive(List<BattleUnitView> list) => list.Exists(x => x.IsAlive);
        void ToggleAuto() => autoMode = !autoMode;
        void TogglePause() { if (pausePanel != null) pausePanel.SetActive(!pausePanel.activeSelf); Time.timeScale = pausePanel != null && pausePanel.activeSelf ? 0f : 1f; }
        void Refresh()
        {
            if (roundText != null) roundText.text = $"ROUND {round}";
            if (turnText != null) turnText.text = resolving ? "ACTION" : "PLAYER TURN";
            foreach (var unit in allies) RefreshUnit(unit);
            foreach (var unit in enemies) RefreshUnit(unit);
        }
        static void RefreshUnit(BattleUnitView unit)
        {
            if (unit.healthBar != null) { unit.healthBar.maxValue = unit.maxHealth; unit.healthBar.value = unit.health; }
        }
    }
}
