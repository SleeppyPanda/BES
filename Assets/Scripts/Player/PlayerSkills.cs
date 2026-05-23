using UnityEngine;

[RequireComponent(typeof(PlayerState))]
public class PlayerSkills : MonoBehaviour
{
    PlayerState state;
    PlayerDash dashComponent;

    void Start()
    {
        state = GetComponent<PlayerState>();
        dashComponent = GetComponent<PlayerDash>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && dashComponent != null)
        {
            if (state != null && state.canDash)
                StartCoroutine(dashComponent.DashRoutine());
        }

        // Skill keys with costs and enhancements
        // Enhancement modifier: hold C while pressing Q/E/R to consume Energy
        bool enhance = Input.GetKey(KeyCode.C);

        if (Input.GetKeyDown(KeyCode.Q))
        {
            bool isEnhanced = enhance;
            TryUseSkill("Q", isEnhanced);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            bool isEnhanced = enhance;
            TryUseSkill("E", isEnhanced);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            bool isEnhanced = enhance;
            TryUseSkill("R", isEnhanced);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            TryUseSkill("T", false);
        }
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

    // Allow Q while E is active
    public bool allowQDuringE = true;

    void TryUseSkill(string skillName, bool enhanced)
    {
        if (state == null) return;

        // Can't use skills while jumping or climbing
        if (!state.controller.isGrounded) { Debug.Log("Can't use skills while in air"); return; }
        if (state.isClimbing) { Debug.Log("Can't use skills while climbing"); return; }

        // If another skill active, only allow Q when E is ongoing
        if (state.activeSkillCount > 0)
        {
            if (!(skillName == "Q" && allowQDuringE && state.currentSkillName == "E"))
            {
                Debug.Log("Another skill is active");
                return;
            }
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

        if (!state.CanUseMana(manaCost)) { Debug.Log("Not enough Mana"); return; }
        if (enhanced && !state.CanUseEnergy(energyCost)) { Debug.Log("Not enough Energy for enhancement"); return; }

        // Consume resources
        state.ConsumeMana(manaCost);
        if (enhanced) state.ConsumeEnergy(energyCost);

        // If attacking, override it
        if (state.isAttacking)
        {
            state.isAttacking = false;
        }

        StartCoroutine(PerformSkill(skillName, duration));
    }

    System.Collections.IEnumerator PerformSkill(string skillName, float duration)
    {
        // Manage stacking of skills so Q can run during E
        if (state.activeSkillCount == 0)
        {
            state.preSkillRotation = transform.rotation;
        }
        state.activeSkillCount++;
        state.isUsingSkill = true;
        state.currentSkillName = skillName;
        state.disableMouseAttack = true;

        Debug.Log("<color=orange>[SKILL]</color> Bắt đầu: " + skillName);

        // Prevent movement implicitly via state.isUsingSkill (PlayerMovement checks it)
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            yield return null;
        }

        Debug.Log("<color=orange>[SKILL]</color> Kết thúc: " + skillName);

        state.activeSkillCount--;
        if (state.activeSkillCount <= 0)
        {
            state.activeSkillCount = 0;
            state.isUsingSkill = false;
            state.currentSkillName = null;
            state.disableMouseAttack = false;
            // restore rotation
            transform.rotation = state.preSkillRotation;
        }
        else
        {
            // If there are still skills active, keep flags but clear currentSkillName if this was the last one with that name
            state.currentSkillName = null;
        }
    }
}
