using System.Collections;
using UnityEngine;

namespace BES.Gameplay
{
    public class BasicAttackController : MonoBehaviour
    {
        [SerializeField] float attackRange = 2f;
        [SerializeField] float attackAngle = 70f;
        [SerializeField] float comboResetTime = 1f;
        [SerializeField] float[] comboMultipliers = { 1f, 1.1f, 1.3f };
        [SerializeField] LayerMask enemyMask;

        PlayerInputReader input;
        PlayerStats stats;
        int comboIndex;
        float comboTimer;
        bool isAttacking;

        public bool IsAttacking => isAttacking;

        public float AttackBusyNormalized => isAttacking ? 0.35f : 0f;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            stats = GetComponent<PlayerStats>();
            if (enemyMask.value == 0)
                enemyMask = LayerMask.GetMask("Enemy");
        }

        void Update()
        {
            if (comboTimer > 0f)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f)
                    comboIndex = 0;
            }

            if (input != null && stats != null && input.AttackPressed && !isAttacking && !GameplayInputGate.IsGameplayBlocked)
                StartCoroutine(AttackRoutine());
        }

        IEnumerator AttackRoutine()
        {
            isAttacking = true;
            yield return new WaitForSeconds(0.1f);

            var multiplier = comboMultipliers[Mathf.Min(comboIndex, comboMultipliers.Length - 1)];
            var amount = DamageCalculator.Calculate(
                stats.AttackPower * multiplier,
                0f,
                stats.CritRate,
                stats.CritDamage,
                out var isCrit);

            var hits = Physics.OverlapSphere(transform.position + transform.forward, attackRange, enemyMask);
            foreach (var hit in hits)
            {
                var dir = (hit.transform.position - transform.position).normalized;
                if (Vector3.Angle(transform.forward, dir) <= attackAngle * 0.5f &&
                    hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(new DamageInfo(amount, gameObject, isCrit));
                }
            }

            comboIndex = (comboIndex + 1) % comboMultipliers.Length;
            comboTimer = comboResetTime;
            yield return new WaitForSeconds(0.25f);
            isAttacking = false;
        }
    }
}
