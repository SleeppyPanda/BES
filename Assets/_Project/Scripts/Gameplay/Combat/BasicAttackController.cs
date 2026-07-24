using System.Collections;
using BES.Core;
using BES.UI;
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
        CharacterAttackProfile profile;
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
            ApplyActiveCharacterProfile();
        }

        void OnEnable() => GameEvents.OnPartyChanged += ApplyActiveCharacterProfile;

        void OnDisable() => GameEvents.OnPartyChanged -= ApplyActiveCharacterProfile;

        void Update()
        {
            if (comboTimer > 0f)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f)
                    comboIndex = 0;
            }

            if (input == null || stats == null || isAttacking || GameplayInputGate.IsGameplayBlocked)
                return;

            if (input.AttackPressed)
                StartCoroutine(AttackRoutine(false));
            else if (input.HeavyAttackPressed)
                StartCoroutine(AttackRoutine(true));
        }

        void ApplyActiveCharacterProfile()
        {
            profile = CharacterCombatProfile.Get(PartyRoster.Instance?.ActiveCharacter);
            comboIndex = 0;
            comboTimer = 0f;
        }

        IEnumerator AttackRoutine(bool rightClick)
        {
            var move = rightClick ? profile.rightClick : profile.leftClick;
            isAttacking = true;
            yield return new WaitForSeconds(move.startup);

            var multipliers = move.comboMultipliers != null && move.comboMultipliers.Length > 0
                ? move.comboMultipliers
                : comboMultipliers;
            var multiplier = move.useCombo
                ? multipliers[Mathf.Min(comboIndex, multipliers.Length - 1)]
                : move.damageMultiplier;
            var range = move.range > 0f ? move.range : attackRange;
            var angle = move.angle > 0f ? move.angle : attackAngle;
            var amount = DamageCalculator.Calculate(
                stats.AttackPower * multiplier,
                0f,
                stats.CritRate,
                stats.CritDamage,
                out var isCrit);

            var center = transform.position + transform.forward * Mathf.Max(1f, range * 0.55f);
            CombatVfx.SpawnPulse(
                center + Vector3.up * 0.9f,
                move.effectColor,
                Mathf.Max(0.1f, move.effectRadius),
                Mathf.Max(0.05f, move.effectDuration));

            var hits = Physics.OverlapSphere(center, range, enemyMask);
            foreach (var hit in hits)
            {
                var dir = (hit.transform.position - transform.position).normalized;
                if (Vector3.Angle(transform.forward, dir) <= angle * 0.5f &&
                    hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(new DamageInfo(amount, gameObject, isCrit));
                }
            }

            if (move.useCombo)
                comboIndex = (comboIndex + 1) % multipliers.Length;
            else
                comboIndex = 0;
            comboTimer = comboResetTime;
            yield return new WaitForSeconds(move.recovery);
            isAttacking = false;
        }
    }
}
