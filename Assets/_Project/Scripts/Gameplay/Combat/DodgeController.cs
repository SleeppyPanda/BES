using UnityEngine;

namespace BES.Gameplay
{
    public class DodgeController : MonoBehaviour
    {
        [SerializeField] float dodgeDistance = 4f;
        [SerializeField] float dodgeDuration = 0.35f;
        [SerializeField] float cooldown = 0.8f;
        [SerializeField] float staminaCost = 20f;
        [SerializeField] float invincibleDuration = 0.3f;

        PlayerInputReader input;
        StaminaSystem stamina;

        float cooldownTimer;
        float dodgeTimer;
        float invincibleTimer;
        Vector3 dodgeDirection;
        bool isDodging;

        public bool IsInvincible => invincibleTimer > 0f;

        public float CooldownNormalized => cooldown > 0f ? Mathf.Clamp01(cooldownTimer / cooldown) : 0f;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            stamina = GetComponent<StaminaSystem>();
        }

        void Update()
        {
            if (cooldownTimer > 0f)
                cooldownTimer -= Time.deltaTime;

            if (invincibleTimer > 0f)
                invincibleTimer -= Time.deltaTime;

            if (isDodging)
            {
                dodgeTimer -= Time.deltaTime;
                transform.position += dodgeDirection * (dodgeDistance / dodgeDuration) * Time.deltaTime;
                if (dodgeTimer <= 0f)
                    isDodging = false;
            }

            if (input != null && input.DodgePressed && !GameplayInputGate.IsGameplayBlocked && CanDodge())
                StartDodge();
        }

        bool CanDodge() =>
            !isDodging && cooldownTimer <= 0f && stamina != null && stamina.TrySpend(staminaCost);

        void StartDodge()
        {
            dodgeDirection = transform.forward;
            dodgeTimer = dodgeDuration;
            cooldownTimer = cooldown;
            invincibleTimer = invincibleDuration;
            isDodging = true;
        }
    }
}
