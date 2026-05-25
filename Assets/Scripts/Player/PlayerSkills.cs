using UnityEngine;

[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(PlayerInputHub))]
public class PlayerSkills : MonoBehaviour
{
    PlayerState state;
    PlayerInputHub input;
    PlayerDash dashComponent;
    PlayerCombat combatComponent;

    void Start()
    {
        state = GetComponent<PlayerState>();
        input = GetComponent<PlayerInputHub>();
        dashComponent = GetComponent<PlayerDash>();
        combatComponent = GetComponent<PlayerCombat>();
    }

    void Update()
    {
        if (input == null) return;

        if (input.ConsumeAction(PlayerAction.Dash) && dashComponent != null)
            dashComponent.TryStartDash();

        if (input.ConsumeAction(PlayerAction.SkillQ))
            TryUseSkill("Q", input.EnhanceHeld);
        if (input.ConsumeAction(PlayerAction.SkillE))
            TryUseSkill("E", input.EnhanceHeld);
        if (input.ConsumeAction(PlayerAction.SkillR))
            TryUseSkill("R", input.EnhanceHeld);
        if (input.ConsumeAction(PlayerAction.SkillT))
            TryUseSkill("T", false);
    }

    [Header("Skill Costs & Durations")]
    public float manaCostQ = 10f;
    public float durationQ = 0.7f;

    public float manaCostE = 15f;
    public float durationE = 1.2f;

    public float manaCostR = 30f;
    public float durationR = 2.0f;

    public float manaCostT = 8f;
    public float durationT = 0.5f;

    public float energyCostEnhance = 20f;

    public bool allowQDuringE = true;

    public void TryUseSkill(string skillName, bool enhanced)
    {
        if (state == null) return;

        if (!state.controller.isGrounded)
        {
            Debug.Log("Can't use skills while in air");
            return;
        }

        if (state.isClimbing)
        {
            Debug.Log("Can't use skills while climbing");
            return;
        }

        bool canUseDuringAnotherSkill = skillName == "Q" && allowQDuringE && state.isSkillEActive;
        if (state.activeSkillCount > 0 && !canUseDuringAnotherSkill)
        {
            Debug.Log("Another skill is active");
            return;
        }

        float manaCost = 0f;
        float duration = 0.5f;
        switch (skillName)
        {
            case "Q": manaCost = manaCostQ; duration = durationQ; break;
            case "E": manaCost = manaCostE; duration = durationE; break;
            case "R": manaCost = manaCostR; duration = durationR; break;
            case "T": manaCost = manaCostT; duration = durationT; break;
        }

        float energyCost = enhanced ? energyCostEnhance : 0f;

        if (!state.CanUseMana(manaCost))
        {
            Debug.Log("Not enough Mana");
            return;
        }

        if (enhanced && !state.CanUseEnergy(energyCost))
        {
            Debug.Log("Not enough Energy for enhancement");
            return;
        }

        state.ConsumeMana(manaCost);
        if (enhanced)
            state.ConsumeEnergy(energyCost);

        if (state.isAttacking)
        {
            if (combatComponent != null)
                combatComponent.CancelAttack();
            else
                state.isAttacking = false;
        }

        StartCoroutine(PerformSkill(skillName, duration));
    }

    System.Collections.IEnumerator PerformSkill(string skillName, float duration)
    {
        bool isSkillE = skillName == "E";

        if (state.activeSkillCount == 0)
        {
            state.preSkillPosition = transform.position;
            state.preSkillRotation = transform.rotation;
        }

        state.activeSkillCount++;
        if (isSkillE)
            state.isSkillEActive = true;

        state.isUsingSkill = true;
        state.currentSkillName = skillName;
        state.disableMouseAttack = true;

        Debug.Log("<color=orange>[SKILL]</color> Bắt đầu: " + skillName);

        float startTime = Time.time;
        while (Time.time < startTime + duration)
            yield return null;

        Debug.Log("<color=orange>[SKILL]</color> Kết thúc: " + skillName);

        state.activeSkillCount--;
        if (isSkillE)
            state.isSkillEActive = false;

        if (state.activeSkillCount <= 0)
        {
            state.activeSkillCount = 0;
            state.isUsingSkill = false;
            state.currentSkillName = null;
            state.disableMouseAttack = false;
            transform.position = state.preSkillPosition;
            transform.rotation = state.preSkillRotation;
        }
        else
        {
            state.currentSkillName = state.isSkillEActive ? "E" : null;
        }
    }
}
