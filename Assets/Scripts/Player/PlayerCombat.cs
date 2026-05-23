using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    void Update()
    {
        PlayerState state = GetComponent<PlayerState>();
        if (state == null) return;

        // Don't allow attacks while using skills, climbing, or while in air
        if (!state.controller.isGrounded) return;
        if (state.isUsingSkill) return;
        if (state.disableMouseAttack) return;

        if (Input.GetMouseButtonDown(0))
        {
            state.isAttacking = true;
            Debug.Log("<color=red>[COMBAT]</color> Đánh thường (Chuột trái)");
            StartCoroutine(ResetAttackFlag());
        }

        if (Input.GetMouseButtonDown(1))
        {
            state.isAttacking = true;
            Debug.Log("<color=yellow>[COMBAT]</color> Đòn đánh đặc biệt (Chuột phải)");
            StartCoroutine(ResetAttackFlag());
        }
    }

    System.Collections.IEnumerator ResetAttackFlag()
    {
        yield return new WaitForSeconds(0.25f);
        PlayerState state = GetComponent<PlayerState>();
        if (state != null) state.isAttacking = false;
    }
}
