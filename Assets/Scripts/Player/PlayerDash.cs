using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerState))]
public class PlayerDash : MonoBehaviour
{
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    PlayerState state;

    void Start()
    {
        state = GetComponent<PlayerState>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && state != null && state.canDash && !state.isClimbing)
        {
            StartCoroutine(DashRoutine());
        }
    }

    public IEnumerator DashRoutine()
    {
        state.isDashing = true;
        state.canDash = false;

        Vector3 dashDirection = transform.forward;
        state.velocity.y = 0;

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            state.controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            yield return null;
        }

        state.isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        state.canDash = true;
    }
}
