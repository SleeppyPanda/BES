using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerState))]
public class PlayerClimb : MonoBehaviour
{
    public float climbSpeed = 4f;
    public float wallDetectionDistance = 0.6f;
    public LayerMask climbableLayer;

    PlayerState state;
    PlayerInputHub input;

    void Start()
    {
        state = GetComponent<PlayerState>();
        input = GetComponent<PlayerInputHub>();
    }

    void Update()
    {
        if (state == null) return;

        HandleClimbingDetection();

        if (state.isClimbing)
            HandleClimbMovement();
    }

    Vector2 GetMoveAxes()
    {
        if (input != null)
            return input.MoveInput;

        return BESInputReader.GetMoveVector();
    }

    void HandleClimbingDetection()
    {
        Vector2 axes = GetMoveAxes();
        Vector3 rayOrigin = transform.position + Vector3.up * 1f;

        if (Physics.Raycast(rayOrigin, transform.forward, out _, wallDetectionDistance, climbableLayer))
        {
            if (!state.isClimbing && axes.y > 0)
            {
                state.isClimbing = true;
                state.velocity = Vector3.zero;
            }
        }
        else
        {
            state.isClimbing = false;
        }

        if (state.isClimbing && state.controller.isGrounded && axes.y < 0)
            state.isClimbing = false;
    }

    void HandleClimbMovement()
    {
        Vector2 axes = GetMoveAxes();

        Vector3 climbDirection = (transform.up * axes.y + transform.right * axes.x);
        if (climbDirection.magnitude > 1f)
            climbDirection.Normalize();

        state.controller.Move(climbDirection * climbSpeed * Time.deltaTime);

        if (BESInputReader.WasKeyPressedThisFrame(Key.Space))
        {
            state.isClimbing = false;
            state.velocity = -transform.forward * 5f + Vector3.up * 1.5f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        Gizmos.DrawLine(rayOrigin, rayOrigin + transform.forward * wallDetectionDistance);
    }
}
