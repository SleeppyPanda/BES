using BES.Core;
using UnityEngine;

namespace BES.Gameplay
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] string enemyId;
        [SerializeField] float maxHealth = 50f;
        [SerializeField] float defense = 2f;
        [SerializeField] int experienceReward = 10;

        float currentHealth;

        public string EnemyId => string.IsNullOrEmpty(enemyId) ? gameObject.name : enemyId;
        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;

        void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive)
                return;

            var reduced = Mathf.Max(1f, damage.Amount - defense * 0.4f);
            currentHealth -= reduced;

            if (currentHealth <= 0f)
                Die();
        }

        void Die()
        {
            GameEvents.RaiseEnemyDefeated(EnemyId);
            CombatManager.Instance?.RegisterKill(gameObject.name, experienceReward);
            Destroy(gameObject, 0.1f);
        }
    }
}
