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
            GetCharacterBase(characterId, out var baseAtk, out var baseHp, out var baseDef, out var baseCrit, out var baseCritDmg);

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
            ComputedMaxMana = 100f;
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

        static void GetCharacterBase(string id, out float atk, out float hp, out float def, out float crit, out float critDmg)
        {
            switch (id)
            {
                case "hero_02":
                    atk = 18f; hp = 90f; def = 6f; crit = 0.12f; critDmg = 1.6f;
                    break;
                case "hero_03":
                    atk = 14f; hp = 110f; def = 8f; crit = 0.08f; critDmg = 1.4f;
                    break;
                case "hero_04":
                    atk = 16f; hp = 95f; def = 5f; crit = 0.15f; critDmg = 1.7f;
                    break;
                case "char_limited_01":
                    atk = 22f; hp = 100f; def = 6f; crit = 0.18f; critDmg = 1.8f;
                    break;
                default:
                    atk = 15f; hp = 100f; def = 5f; crit = 0.1f; critDmg = 1.5f;
                    break;
            }
        }
    }
}
