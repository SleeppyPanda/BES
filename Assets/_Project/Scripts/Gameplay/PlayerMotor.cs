using BES.Core;
using UnityEngine;

namespace BES.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float walkSpeed = 4f;
        [SerializeField] float sprintSpeed = 7f;
        [SerializeField] float rotationSpeed = 12f;
        [SerializeField] float jumpHeight = 1.2f;
        [SerializeField] float gravity = -20f;

        CharacterController controller;
        PlayerInputReader input;
        StaminaSystem stamina;
        Transform cameraTransform;

        Vector3 velocity;
        bool isGrounded;

        public bool IsSprinting { get; private set; }
        public Vector3 Velocity => velocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            stamina = GetComponent<StaminaSystem>();
        }

        void Start()
        {
            input ??= GetComponent<PlayerInputReader>();
            var cam = Camera.main;
            if (cam != null)
                cameraTransform = cam.transform;
        }

        void Update()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0f)
                velocity.y = -2f;

            HandleMovement();
            HandleJump();
            ApplyGravity();
        }

        void HandleMovement()
        {
            if (input == null || GameplayInputGate.IsGameplayBlocked)
                return;

            var moveInput = input.Move;
            var moveDir = new Vector3(moveInput.x, 0f, moveInput.y);

            if (cameraTransform != null && moveDir.sqrMagnitude > 0.01f)
            {
                var camForward = cameraTransform.forward;
                camForward.y = 0f;
                camForward.Normalize();
                var camRight = cameraTransform.right;
                camRight.y = 0f;
                camRight.Normalize();
                moveDir = camForward * moveInput.y + camRight * moveInput.x;
            }

            IsSprinting = input.SprintHeld && stamina != null && stamina.CanSpend && moveDir.sqrMagnitude > 0.01f;
            var speed = IsSprinting ? sprintSpeed : walkSpeed;

            if (IsSprinting)
                stamina?.SpendPerSecond();

            controller.Move(moveDir.normalized * (speed * Time.deltaTime));

            if (moveDir.sqrMagnitude > 0.01f)
            {
                var targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        void HandleJump()
        {
            if (input == null || GameplayInputGate.IsGameplayBlocked)
                return;

            if (input.JumpPressed && isGrounded)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        void ApplyGravity()
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        public void ApplyExternalForce(Vector3 force)
        {
            velocity += force;
        }
    }
}
