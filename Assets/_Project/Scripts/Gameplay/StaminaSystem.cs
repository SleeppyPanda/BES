using BES.Core;
using UnityEngine;

namespace BES.Gameplay
{
    public class StaminaSystem : MonoBehaviour
    {
        [SerializeField] float maxStamina = 100f;
        [SerializeField] float drainPerSecond = 25f;
        [SerializeField] float regenPerSecond = 15f;
        [SerializeField] float regenDelay = 1f;

        float currentStamina;
        float regenTimer;

        public float Current => currentStamina;
        public float Max => maxStamina;
        public bool CanSpend => currentStamina > 0f;

        void Awake()
        {
            currentStamina = maxStamina;
        }

        void Update()
        {
            if (regenTimer > 0f)
            {
                regenTimer -= Time.deltaTime;
                return;
            }

            if (currentStamina < maxStamina)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + regenPerSecond * Time.deltaTime);
                GameEvents.RaisePlayerStaminaChanged(currentStamina, maxStamina);
            }
        }

        public void SpendPerSecond()
        {
            currentStamina = Mathf.Max(0f, currentStamina - drainPerSecond * Time.deltaTime);
            regenTimer = regenDelay;
            GameEvents.RaisePlayerStaminaChanged(currentStamina, maxStamina);
        }

        public bool TrySpend(float amount)
        {
            if (currentStamina < amount)
                return false;

            currentStamina -= amount;
            regenTimer = regenDelay;
            GameEvents.RaisePlayerStaminaChanged(currentStamina, maxStamina);
            return true;
        }

        public void Restore(float amount)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
            GameEvents.RaisePlayerStaminaChanged(currentStamina, maxStamina);
        }

        public void LoadState(float value)
        {
            currentStamina = Mathf.Clamp(value, 0f, maxStamina);
            GameEvents.RaisePlayerStaminaChanged(currentStamina, maxStamina);
        }
    }
}
