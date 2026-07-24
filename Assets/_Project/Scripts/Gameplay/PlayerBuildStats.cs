using BES.Core;
using BES.UI;
using UnityEngine;

namespace BES.Gameplay
{
    /// <summary>
    /// Tổng hợp stat từ nhân vật active, vũ khí và artifact — áp vào PlayerStats.
    /// </summary>
    public class PlayerBuildStats : MonoBehaviour
    {
        [SerializeField] PlayerStats stats;

        public float ComputedAttack { get; private set; }
        public float ComputedDefense { get; private set; }
        public float ComputedMaxHealth { get; private set; }
        public float ComputedMaxMana { get; private set; }
        public float ComputedCritRate { get; private set; }
        public float ComputedCritDamage { get; private set; }

        void Awake()
        {
            stats ??= GetComponent<PlayerStats>();
        }

        void OnEnable()
        {
            GameEvents.OnGameLoaded += OnGameLoaded;
            Refresh();
        }

        void OnDisable() => GameEvents.OnGameLoaded -= OnGameLoaded;

        void OnGameLoaded() => Refresh();

        public void Refresh()
        {
            var characterId = PartyRoster.Instance?.ActiveCharacterId ?? "hero_01";
            var character = CharacterDatabaseLoader.Load()?.Get(characterId);
            GetCharacterBase(character, out var baseAtk, out var baseHp, out var baseDef, out var baseMana, out var baseCrit, out var baseCritDmg);

            var weaponAtk = EquippedWeaponState.Instance?.GetDisplayAtk() ?? 0;
            var artifactAtk = 0;
            var artifactHp = 0;
            var artifact = MetaProgressState.Instance?.GetEquippedArtifact();
            if (artifact != null)
            {
                artifactAtk = artifact.atkBonus + artifact.setBonusAtk;
                artifactHp = artifact.hpBonus;
            }

            ComputedAttack = baseAtk + weaponAtk + artifactAtk;
            ComputedDefense = baseDef + Mathf.RoundToInt(artifactAtk * 0.1f);
            ComputedMaxHealth = baseHp + artifactHp;
            ComputedMaxMana = baseMana;
            ComputedCritRate = baseCrit;
            ComputedCritDamage = baseCritDmg;

            stats?.ApplyBuild(
                ComputedMaxHealth,
                ComputedMaxMana,
                ComputedAttack,
                ComputedDefense,
                ComputedCritRate,
                ComputedCritDamage);
        }

        static void GetCharacterBase(CharacterDefinition character, out float atk, out float hp, out float def, out float mana, out float crit, out float critDmg)
        {
            if (character != null)
            {
                atk = character.baseAttack;
                hp = character.baseHealth;
                def = character.baseDefense;
                mana = character.baseMana;
                crit = character.critRate;
                critDmg = character.critDamage;
                return;
            }

            atk = 15f;
            hp = 100f;
            def = 5f;
            mana = 100f;
            crit = 0.1f;
            critDmg = 1.5f;
        }
    }
}
