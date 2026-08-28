using BES.Core;
using UnityEngine;
using UnityEngine.InputSystem;

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

        Animator animator;                 // Cached once — never searched per frame
        bool animatorCached;               // Guard flag so we only search once

        /// <summary>
        /// Call this from PartyCharacterVisualSwitcher whenever the active visual
        /// character model changes so PlayerMotor re-caches the correct Animator.
        /// </summary>
        public void InvalidateAnimatorCache()
        {
            animator = null;
            animatorCached = false;
        }

        void Update()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0f)
            {
                // Firm downward stick force so player feet stay glued to terrain/stairs
                velocity.y = -8f;
            }

            HandleMovementAndGravity();
            HandleJump();
            UpdateAnimator();
        }

        void UpdateAnimator()
        {
            // Cache the Animator ONCE (or after InvalidateAnimatorCache() is called)
            // Previous version: foreach + GetComponentInChildren EVERY FRAME = CPU waste
            if (!animatorCached)
            {
                animator = null;
                foreach (Transform child in transform)
                {
                    animator = child.GetComponentInChildren<Animator>(true);
                    if (animator != null) break;
                }
                animatorCached = true;
            }

            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                // Set speed based on input movement to ensure stable animation transitions (no physics noise/jitter)
                var moveInput = input != null ? input.Move : Vector2.zero;
                if (moveInput.sqrMagnitude < 0.001f)
                    moveInput = ReadKeyboardMove();

                float targetSpeed = 0f;
                if (moveInput.sqrMagnitude > 0.01f)
                {
                    targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;
                }
                
                // Smoothly interpolate the animator speed parameter to prevent instant snapping
                float currentSpeed = animator.GetFloat("Speed");
                float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 14f * Time.deltaTime);
                animator.SetFloat("Speed", newSpeed);
            }
        }

        void HandleMovementAndGravity()
        {
            if (GameplayInputGate.IsMovementBlocked)
            {
                velocity.y += gravity * Time.deltaTime;
                controller.Move(velocity * Time.deltaTime);
                return;
            }

            var moveInput = input != null ? input.Move : Vector2.zero;
            if (moveInput.sqrMagnitude < 0.001f)
                moveInput = ReadKeyboardMove();
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

            IsSprinting = input != null && input.SprintHeld && stamina != null && stamina.CanSpend && moveDir.sqrMagnitude > 0.01f;
            var speed = IsSprinting ? sprintSpeed : walkSpeed;

            if (IsSprinting)
                stamina?.SpendPerSecond();

            // Rotate player toward move direction
            if (moveDir.sqrMagnitude > 0.01f)
            {
                var targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;

            // Single unified Move call combines horizontal translation and vertical gravity
            Vector3 motion = (moveDir.normalized * speed + velocity) * Time.deltaTime;
            controller.Move(motion);
        }

        void HandleJump()
        {
            if (GameplayInputGate.IsMovementBlocked)
                return;

            var jumpPressed = input != null && input.JumpPressed;
            var keyboard = Keyboard.current;
            if (!jumpPressed && keyboard != null)
                jumpPressed = keyboard.spaceKey.wasPressedThisFrame;

            if (jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                isGrounded = false;
            }
        }

        public void ApplyExternalForce(Vector3 force)
        {
            velocity += force;
        }

        public void SetVerticalVelocity(float y)
        {
            velocity.y = y;
        }

        static Vector2 ReadKeyboardMove()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2.zero;

            var x = 0f;
            var y = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

            var move = new Vector2(x, y);
            return move.sqrMagnitude > 1f ? move.normalized : move;
        }
    }
}
