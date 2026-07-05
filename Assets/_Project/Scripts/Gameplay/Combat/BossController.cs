using System.Collections;
using UnityEngine;

namespace BES.Gameplay
{
    public class BossController : MonoBehaviour
    {
        public enum BossPhase
        {
            Phase1,
            Phase2
        }

        [SerializeField] float phase2HealthThreshold = 0.5f;
        [SerializeField] float phase1AttackDamage = 12f;
        [SerializeField] float phase2AttackDamage = 18f;
        [SerializeField] float specialAttackInterval = 6f;

        EnemyHealth health;
        EnemyAI ai;
        BossPhase currentPhase = BossPhase.Phase1;
        float specialTimer;

        void Awake()
        {
            health = GetComponent<EnemyHealth>();
            ai = GetComponent<EnemyAI>();
        }

        void Update()
        {
            if (!health.IsAlive)
                return;

            var ratio = health.CurrentHealth / 50f; // matches default max in EnemyHealth
            if (currentPhase == BossPhase.Phase1 && ratio <= phase2HealthThreshold)
                EnterPhase2();

            specialTimer -= Time.deltaTime;
            if (specialTimer <= 0f)
            {
                StartCoroutine(SpecialAttack());
                specialTimer = specialAttackInterval;
            }
        }

        void EnterPhase2()
        {
            currentPhase = BossPhase.Phase2;
            transform.localScale *= 1.1f;
        }

        IEnumerator SpecialAttack()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                yield break;

            yield return new WaitForSeconds(0.5f);

            if (player.TryGetComponent<PlayerStats>(out var stats))
            {
                var damage = currentPhase == BossPhase.Phase1 ? phase1AttackDamage : phase2AttackDamage;
                stats.TakeDamage(damage * 1.5f);
            }
        }
    }
}
