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
            profile = CharacterCombatProfile.Get(PartyRoster.Instance?.ActiveCharacterId);
            comboIndex = 0;
            comboTimer = 0f;
        }

        IEnumerator AttackRoutine(bool heavy)
        {
            isAttacking = true;
            yield return new WaitForSeconds(heavy ? profile.heavyStartup : profile.startup);

            var multipliers = profile.comboMultipliers != null && profile.comboMultipliers.Length > 0
                ? profile.comboMultipliers
                : comboMultipliers;
            var multiplier = heavy
                ? profile.heavyMultiplier
                : multipliers[Mathf.Min(comboIndex, multipliers.Length - 1)];
            var amount = DamageCalculator.Calculate(
                stats.AttackPower * multiplier,
                0f,
                stats.CritRate,
                stats.CritDamage,
                out var isCrit);

            var range = heavy ? profile.heavyRange : profile.range;
            var angle = heavy ? Mathf.Min(180f, profile.angle + 35f) : profile.angle;
            var center = transform.position + transform.forward * Mathf.Max(1f, range * 0.55f);
            CombatVfx.SpawnPulse(center + Vector3.up * 0.9f, profile.effectColor, heavy ? 1.35f : 0.85f, heavy ? 0.32f : 0.22f);

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

            if (!heavy)
                comboIndex = (comboIndex + 1) % multipliers.Length;
            comboTimer = comboResetTime;
            yield return new WaitForSeconds(heavy ? profile.heavyRecovery : profile.recovery);
            isAttacking = false;
        }
    }
}
