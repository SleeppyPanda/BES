using BES.Core;
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

        void OnEnable() => GameEvents.OnPartyChanged += ApplyIcons;

        void OnDisable() => GameEvents.OnPartyChanged -= ApplyIcons;

        void Start()
        {
            BindPlayer();
            ApplyIcons();
        }

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
                skillBar.SetSkillUnlocked(0, skills.Skill1Unlocked);
                skillBar.SetSkillUnlocked(1, skills.Skill2Unlocked);
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
            if (skillBar == null)
                return;

            var character = PartyRoster.Instance?.ActiveCharacter;
            var skill1Icon = character != null && character.skill1Icon != null
                ? character.skill1Icon
                : (manifest != null ? manifest.skillIconSkill1 : null);
            var skill2Icon = character != null && character.skill2Icon != null
                ? character.skill2Icon
                : (manifest != null ? manifest.skillIconSkill2 : null);

            skillBar.SetSkillIcon(0, skill1Icon);
            skillBar.SetSkillIcon(1, skill2Icon);
            skillBar.SetKeyLabel(0, "Q");
            skillBar.SetKeyLabel(1, "E");
            var id = PartyRoster.Instance?.ActiveCharacterId;
            skillBar.SetSkillUnlocked(0, CharacterProgressionState.GetActiveSkill(id, 0) != null);
            skillBar.SetSkillUnlocked(1, CharacterProgressionState.GetActiveSkill(id, 1) != null);
        }
    }
}
