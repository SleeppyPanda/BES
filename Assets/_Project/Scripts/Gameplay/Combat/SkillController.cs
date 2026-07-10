using System.Collections;
using BES.Core;
using BES.UI;
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
            ApplyActiveCharacterSkills();
        }

        void OnEnable() => GameEvents.OnPartyChanged += ApplyActiveCharacterSkills;

        void OnDisable() => GameEvents.OnPartyChanged -= ApplyActiveCharacterSkills;

        void Update()
        {
            if (skill1Cooldown > 0f) skill1Cooldown -= Time.deltaTime;
            if (skill2Cooldown > 0f) skill2Cooldown -= Time.deltaTime;

            if (isCasting || !CanCastSkills())
                return;

            if (input == null || stats == null)
                return;

            if (input.Skill1Pressed)
                StartCoroutine(CastSkill(skill1, true));
            else if (input.Skill2Pressed)
                StartCoroutine(CastSkill(skill2, false));
        }

        void ApplyActiveCharacterSkills()
        {
            var character = PartyRoster.Instance?.ActiveCharacter;
            if (character == null)
            {
                skill1 ??= CreateSkill("Void Slash", 18f, 1.8f, 2.5f, 3f);
                skill2 ??= CreateSkill("Guard Break", 25f, 2.4f, 5f, 4f);
                return;
            }

            skill1 = CreateSkillFromId(character.skill1Id, true);
            skill2 = CreateSkillFromId(character.skill2Id, false);
            skill1Cooldown = 0f;
            skill2Cooldown = 0f;
        }

        static bool CanCastSkills() => !GameplayInputGate.IsGameplayBlocked;

        public float Skill1CooldownNormalized =>
            skill1 != null && skill1.cooldown > 0f ? Mathf.Clamp01(skill1Cooldown / skill1.cooldown) : 0f;

        public float Skill2CooldownNormalized =>
            skill2 != null && skill2.cooldown > 0f ? Mathf.Clamp01(skill2Cooldown / skill2.cooldown) : 0f;

        IEnumerator CastSkill(SkillDefinition skill, bool isSkill1)
        {
            if (skill == null)
                yield break;

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

        static SkillDefinition CreateSkillFromId(string skillId, bool isSkill1)
        {
            return skillId switch
            {
                "skill_void_slash" => CreateSkill("Void Slash", 18f, 1.8f, 2.5f, 3.5f),
                "skill_guard_break" => CreateSkill("Guard Break", 28f, 2.5f, 5.5f, 4.5f),
                "skill_quick_cut" => CreateSkill("Quick Cut", 12f, 1.35f, 1.2f, 2.4f),
                "skill_flare_dash" => CreateSkill("Flare Dash", 24f, 2.1f, 3.8f, 3.2f),
                "skill_shield_wave" => CreateSkill("Shield Wave", 16f, 1.45f, 2.8f, 4.8f),
                "skill_ground_lock" => CreateSkill("Ground Lock", 30f, 1.9f, 6f, 5.8f),
                "skill_arc_bolt" => CreateSkill("Arc Bolt", 20f, 2.0f, 2.2f, 5.5f),
                "skill_focus_shot" => CreateSkill("Focus Shot", 26f, 2.8f, 5.2f, 6.5f),
                "skill_starfall" => CreateSkill("Starfall", 32f, 3.1f, 6f, 6f),
                "skill_lunar_drive" => CreateSkill("Lunar Drive", 38f, 3.6f, 8f, 5f),
                _ => isSkill1
                    ? CreateSkill("Skill 1", 20f, 2f, 3f, 4f)
                    : CreateSkill("Skill 2", 25f, 2.4f, 5f, 4f)
            };
        }

        static SkillDefinition CreateSkill(string name, float manaCost, float damageMultiplier, float cooldown, float range)
        {
            return new SkillDefinition
            {
                skillName = name,
                manaCost = manaCost,
                damageMultiplier = damageMultiplier,
                cooldown = cooldown,
                range = range
            };
        }
    }
}
