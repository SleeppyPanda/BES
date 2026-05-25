using UnityEngine;

[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(PlayerInputHub))]
public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float rotationSpeed = 10f;
    public float jumpHeight = 1.5f;
    public float gravity = -25f;

    PlayerState state;
    PlayerInputHub input;
    public Transform cameraPivot;

    void Start()
    {
        state = GetComponent<PlayerState>();
        input = GetComponent<PlayerInputHub>();
    }

    void Update()
    {
        if (state == null || input == null) return;
        if (state.isClimbing) return;
        if (state.isDashing) return;
        if (state.isUsingSkill) return;

        CharacterController controller = state.controller;
        if (controller == null) return;

        if (controller.isGrounded && state.velocity.y < 0)
            state.velocity.y = -2f;

        Vector2 move = input.MoveInput;
        float moveX = move.x;
        float moveZ = move.y;

        Vector3 camForward = cameraPivot.forward;
        Vector3 camRight = cameraPivot.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * moveZ + camRight * moveX);
        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (input.ConsumeAction(PlayerAction.Jump) && controller.isGrounded)
            state.velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        state.velocity.y += gravity * Time.deltaTime;

        float speed = BESInputReader.IsSprintHeld() ? sprintSpeed : walkSpeed;
        Vector3 finalVelocity = (moveDirection * speed) + (Vector3.up * state.velocity.y);
        controller.Move(finalVelocity * Time.deltaTime);
    }
}
