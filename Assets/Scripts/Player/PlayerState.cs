using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerState : MonoBehaviour
{
    [HideInInspector] public CharacterController controller;
    [HideInInspector] public Vector3 velocity = Vector3.zero;

    // Shared flags
    [HideInInspector] public bool isDashing = false;
    [HideInInspector] public bool canDash = true;
    [HideInInspector] public bool isClimbing = false;

    // Skill / combat state
    [HideInInspector] public bool isUsingSkill = false;
    [HideInInspector] public string currentSkillName = null;
    [HideInInspector] public bool disableMouseAttack = false;
    [HideInInspector] public bool isAttacking = false;
    [HideInInspector] public Quaternion preSkillRotation = Quaternion.identity;
    [HideInInspector] public int activeSkillCount = 0;

    // Camera shared
    [HideInInspector] public float currentZoom = 5f;
    [HideInInspector] public float xRotation = 0f;
    [HideInInspector] public float yRotation = 0f;

    // Player stats
    [Header("Stats")]
    public int level = 1;
    public float maxHP = 100f;
    [HideInInspector] public float currentHP;
    public float baseAtk = 10f;
    public float baseDef = 5f;
    public float elementalDamage = 0f;
    public float stamina = 100f;

    [Header("Resources")]
    public float maxMana = 50f;
    [HideInInspector] public float currentMana;
    public float maxEnergy = 50f;
    [HideInInspector] public float currentEnergy;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentHP = maxHP;
        currentMana = maxMana;
        currentEnergy = maxEnergy;
    }

    public bool CanUseMana(float amount)
    {
        return currentMana >= amount;
    }

    public bool CanUseEnergy(float amount)
    {
        return currentEnergy >= amount;
    }

    public bool ConsumeMana(float amount)
    {
        if (!CanUseMana(amount)) return false;
        currentMana -= amount;
        return true;
    }

    public bool ConsumeEnergy(float amount)
    {
        if (!CanUseEnergy(amount)) return false;
        currentEnergy -= amount;
        return true;
    }
}
