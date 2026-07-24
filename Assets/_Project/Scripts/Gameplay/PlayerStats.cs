using BES.Core;
using UnityEngine;

namespace BES.Gameplay
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float maxMana = 100f;
        [SerializeField] float attackPower = 15f;
        [SerializeField] float defense = 5f;
        [SerializeField] float critRate = 0.1f;
        [SerializeField] float critDamage = 1.5f;
        [SerializeField] float manaRegenPerSecond = 8f;

        float currentHealth;
        float currentMana;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float MaxMana => maxMana;
        public float CurrentMana => currentMana;
        public float AttackPower => attackPower;
        public float Defense => defense;
        public float CritRate => critRate;
        public float CritDamage => critDamage;
        public bool IsAlive => currentHealth > 0f;

        public void ApplyBuild(
            float newMaxHealth,
            float newMaxMana,
            float newAttack,
            float newDefense,
            float newCritRate,
            float newCritDamage)
        {
            var hpRatio = maxHealth > 0f ? currentHealth / maxHealth : 1f;
            var manaRatio = maxMana > 0f ? currentMana / maxMana : 1f;

            maxHealth = Mathf.Max(1f, newMaxHealth);
            maxMana = Mathf.Max(1f, newMaxMana);
            attackPower = newAttack;
            defense = newDefense;
            critRate = newCritRate;
            critDamage = newCritDamage;

            currentHealth = Mathf.Clamp(maxHealth * hpRatio, 1f, maxHealth);
            currentMana = Mathf.Clamp(maxMana * manaRatio, 0f, maxMana);
            GameEvents.RaisePlayerHealthChanged(currentHealth, maxHealth);
            GameEvents.RaisePlayerManaChanged(currentMana, maxMana);
        }

        void Awake()
        {
            currentHealth = maxHealth;
            currentMana = maxMana;
        }

        void Update()
        {
            if (currentMana < maxMana)
            {
                currentMana = Mathf.Min(maxMana, currentMana + manaRegenPerSecond * Time.deltaTime);
                GameEvents.RaisePlayerManaChanged(currentMana, maxMana);
            }
        }

        public void TakeDamage(float amount)
        {
            if (TryGetComponent<DodgeController>(out var dodge) && dodge.IsInvincible)
                return;

            var reduced = Mathf.Max(1f, amount - defense * 0.5f);
            currentHealth = Mathf.Max(0f, currentHealth - reduced);
            GameEvents.RaisePlayerHealthChanged(currentHealth, maxHealth);
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            GameEvents.RaisePlayerHealthChanged(currentHealth, maxHealth);
        }

        public bool TrySpendMana(float cost)
        {
            if (currentMana < cost)
                return false;

            currentMana -= cost;
            GameEvents.RaisePlayerManaChanged(currentMana, maxMana);
            return true;
        }

        public void RestoreMana(float amount)
        {
            currentMana = Mathf.Min(maxMana, currentMana + amount);
            GameEvents.RaisePlayerManaChanged(currentMana, maxMana);
        }

        public void LoadState(float health, float mana)
        {
            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            currentMana = Mathf.Clamp(mana, 0f, maxMana);
            GameEvents.RaisePlayerHealthChanged(currentHealth, maxHealth);
            GameEvents.RaisePlayerManaChanged(currentMana, maxMana);
        }
    }
}
