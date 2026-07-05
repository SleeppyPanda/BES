using System.Collections;
using UnityEngine;

namespace BES.Gameplay
{
    public class SkillController : MonoBehaviour
    {
        [System.Serializable]
        public class SkillDefinition
        {
            public string skillName = "Skill";
            public float manaCost = 20f;
            public float damageMultiplier = 2f;
            public float cooldown = 3f;
            public float range = 4f;
        }

        [SerializeField] SkillDefinition skill1;
        [SerializeField] SkillDefinition skill2;
        [SerializeField] LayerMask enemyMask;

        PlayerInputReader input;
        PlayerStats stats;
        float skill1Cooldown;
        float skill2Cooldown;
        bool isCasting;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            stats = GetComponent<PlayerStats>();
            if (enemyMask.value == 0)
                enemyMask = LayerMask.GetMask("Enemy");
        }

        void Update()
        {
            if (skill1Cooldown > 0f) skill1Cooldown -= Time.deltaTime;
            if (skill2Cooldown > 0f) skill2Cooldown -= Time.deltaTime;

            if (isCasting || !CanCastSkills())
                return;

            if (input.Skill1Pressed)
                StartCoroutine(CastSkill(skill1, true));
            else if (input.Skill2Pressed)
                StartCoroutine(CastSkill(skill2, false));
        }

        static bool CanCastSkills() => !GameplayInputGate.IsGameplayBlocked;

        public float Skill1CooldownNormalized =>
            skill1 != null && skill1.cooldown > 0f ? Mathf.Clamp01(skill1Cooldown / skill1.cooldown) : 0f;

        public float Skill2CooldownNormalized =>
            skill2 != null && skill2.cooldown > 0f ? Mathf.Clamp01(skill2Cooldown / skill2.cooldown) : 0f;

        IEnumerator CastSkill(SkillDefinition skill, bool isSkill1)
        {
            var cooldown = isSkill1 ? skill1Cooldown : skill2Cooldown;
            if (cooldown > 0f || !stats.TrySpendMana(skill.manaCost))
                yield break;

            isCasting = true;
            yield return new WaitForSeconds(0.15f);

            var amount = DamageCalculator.Calculate(
                stats.AttackPower * skill.damageMultiplier,
                0f,
                stats.CritRate,
                stats.CritDamage,
                out var isCrit);

            var hits = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, skill.range, enemyMask);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(new DamageInfo(amount, gameObject, isCrit));
            }

            if (isSkill1)
                skill1Cooldown = skill.cooldown;
            else
                skill2Cooldown = skill.cooldown;

            yield return new WaitForSeconds(0.2f);
            isCasting = false;
        }
    }
}
