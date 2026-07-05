using UnityEngine;

namespace BES.Gameplay
{
    public static class DamageCalculator
    {
        public static float Calculate(float attackPower, float defense, float critRate, float critDamage, out bool isCritical)
        {
            isCritical = Random.value <= critRate;
            var baseDamage = Mathf.Max(1f, attackPower - defense * 0.4f);
            return isCritical ? baseDamage * critDamage : baseDamage;
        }
    }
}
