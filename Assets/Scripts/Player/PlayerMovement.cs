using UnityEngine;

[RequireComponent(typeof(PlayerState))]
public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f; 
    public float rotationSpeed = 10f; 
    public float jumpHeight = 1.5f;
    public float gravity = -25f;

    PlayerState state;
    public Transform cameraPivot;

    void Start()
    {
        state = GetComponent<PlayerState>();
    }

    void Update()
    {
        if (state == null) return;
        if (state.isClimbing) return;
        if (state.isDashing) return;
        if (state.isUsingSkill) return; // prevent movement while using skills

        CharacterController controller = state.controller;
        if (controller == null) return;

        if (controller.isGrounded && state.velocity.y < 0)
        {
            state.velocity.y = -2f;
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 camForward = cameraPivot.forward;
        Vector3 camRight = cameraPivot.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * moveZ + camRight * moveX);
        if (moveDirection.magnitude > 1f) moveDirection.Normalize();

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Input.GetButtonDown("Jump") && controller.isGrounded) 
        {
            state.velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        state.velocity.y += gravity * Time.deltaTime;

        Vector3 finalVelocity = (moveDirection * walkSpeed) + (Vector3.up * state.velocity.y);
        controller.Move(finalVelocity * Time.deltaTime);
    }
}
