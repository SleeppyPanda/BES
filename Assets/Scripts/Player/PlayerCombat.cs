using UnityEngine;

[RequireComponent(typeof(PlayerInputHub))]
public class PlayerCombat : MonoBehaviour
{
    PlayerState state;
    PlayerInputHub input;
    System.Collections.IEnumerator activeAttackRoutine;

    void Start()
    {
        state = GetComponent<PlayerState>();
        input = GetComponent<PlayerInputHub>();
    }

    void Update()
    {
        if (state == null || input == null) return;

        if (!state.controller.isGrounded) return;
        if (state.isUsingSkill) return;
        if (state.disableMouseAttack) return;

        if (input.ConsumeAction(PlayerAction.AttackLight))
        {
            Debug.Log("<color=red>[COMBAT]</color> Đánh thường (Chuột trái)");
            StartAttackWindow();
            return;
        }

        if (input.ConsumeAction(PlayerAction.AttackHeavy))
        {
            Debug.Log("<color=yellow>[COMBAT]</color> Đòn đánh đặc biệt (Chuột phải)");
            StartAttackWindow();
        }
    }

    void StartAttackWindow()
    {
        CancelAttack();
        state.isAttacking = true;
        activeAttackRoutine = ResetAttackFlag();
        StartCoroutine(activeAttackRoutine);
    }

    public void CancelAttack()
    {
        if (activeAttackRoutine != null)
        {
            StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = null;
        }

        if (state != null)
            state.isAttacking = false;
    }

    System.Collections.IEnumerator ResetAttackFlag()
    {
        yield return new WaitForSeconds(0.25f);
        if (state != null)
            state.isAttacking = false;

        activeAttackRoutine = null;
    }
}
