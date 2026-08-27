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

        Animator animator;

        void Update()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0f)
                velocity.y = -2f;

            HandleMovement();
            HandleJump();
            ApplyGravity();
            UpdateAnimator();
        }

        void UpdateAnimator()
        {
            // Always find the animator on the active visual child (skipping the root GameObject itself to avoid dummy animators)
            animator = null;
            foreach (Transform child in transform)
            {
                animator = child.GetComponentInChildren<Animator>(true);
                if (animator != null) break;
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
                float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 12f * Time.deltaTime);
                animator.SetFloat("Speed", newSpeed);

                // Temporary professional debug logging
                if (Time.frameCount % 30 == 0)
                {
                    var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                    string clipName = clipInfo.Length > 0 ? clipInfo[0].clip.name : "None";
                    Debug.Log($"[BES Debug] Speed Param: {newSpeed:F2}, Target Speed: {targetSpeed:F2}, Active Clip: {clipName}");
                }
            }
        }

        void HandleMovement()
        {
            if (GameplayInputGate.IsMovementBlocked)
                return;

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

            controller.Move(moveDir.normalized * (speed * Time.deltaTime));

            if (moveDir.sqrMagnitude > 0.01f)
            {
                var targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
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
