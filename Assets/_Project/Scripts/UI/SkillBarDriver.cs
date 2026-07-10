using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    public class SkillBarDriver : MonoBehaviour
    {
        [SerializeField] SkillBarUI skillBar;
        [SerializeField] HUDSpriteManifest manifest;

        SkillController skills;

        void Awake()
        {
            skillBar ??= GetComponent<SkillBarUI>();
            manifest ??= HUDSpriteManifestLoader.Load();
            ApplyIcons();
        }

        void Start() => BindPlayer();

        void Update()
        {
            if (skills == null)
                BindPlayer();

            if (skillBar == null)
                return;

            if (skills != null)
            {
                skillBar.SetCooldown(0, skills.Skill1CooldownNormalized);
                skillBar.SetCooldown(1, skills.Skill2CooldownNormalized);
            }
        }

        void BindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            skills = player.GetComponent<SkillController>();
        }

        void ApplyIcons()
        {
            if (skillBar == null || manifest == null)
                return;

            skillBar.SetSkillIcon(0, manifest.skillIconSkill1);
            skillBar.SetSkillIcon(1, manifest.skillIconSkill2);
        }
    }
}
