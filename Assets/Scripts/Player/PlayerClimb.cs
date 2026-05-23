using UnityEngine;

[RequireComponent(typeof(PlayerState))]
public class PlayerClimb : MonoBehaviour
{
    public float climbSpeed = 4f;
    public float wallDetectionDistance = 0.6f;
    public LayerMask climbableLayer;

    PlayerState state;

    void Start()
    {
        state = GetComponent<PlayerState>();
    }

    void Update()
    {
        if (state == null) return;

        HandleClimbingDetection();

        if (state.isClimbing) HandleClimbMovement();
    }

    void HandleClimbingDetection()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 1f;

        if (Physics.Raycast(rayOrigin, transform.forward, out hit, wallDetectionDistance, climbableLayer))
        {
            if (!state.isClimbing && Input.GetAxis("Vertical") > 0)
            {
                state.isClimbing = true;
                state.velocity = Vector3.zero;
            }
        }
        else
        {
            state.isClimbing = false;
        }

        if (state.isClimbing && state.controller.isGrounded && Input.GetAxis("Vertical") < 0)
        {
            state.isClimbing = false;
        }
    }

    void HandleClimbMovement()
    {
        float verticalClimb = Input.GetAxis("Vertical");
        float horizontalClimb = Input.GetAxis("Horizontal");

        Vector3 climbDirection = (transform.up * verticalClimb + transform.right * horizontalClimb);
        if (climbDirection.magnitude > 1f) climbDirection.Normalize();

        state.controller.Move(climbDirection * climbSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space))
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
