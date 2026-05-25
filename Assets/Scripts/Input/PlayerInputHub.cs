using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central input for PC (keyboard/mouse) and mobile UI.
/// Each action is consumed independently so systems do not steal each other's input.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerInputHub : MonoBehaviour
{
    public static PlayerInputHub Instance { get; private set; }

    [Header("Mobile")]
    [Tooltip("Show touch UI on PC when testing in Editor.")]
    public bool forceTouchUIInEditor;

    Vector2 moveInput;
    bool enhanceHeld;
    float enhanceArmedUntil;

    bool attackLightPending;
    bool attackHeavyPending;
    bool jumpPending;
    bool dashPending;
    bool skillQPending;
    bool skillEPending;
    bool skillRPending;
    bool skillTPending;

    public Vector2 MoveInput => moveInput;
    public bool EnhanceHeld => enhanceHeld || Time.time < enhanceArmedUntil;
    public bool UseTouchUI =>
        forceTouchUIInEditor ||
        Application.isMobilePlatform ||
        (Touchscreen.current != null && Application.isMobilePlatform);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!UseTouchUI)
            PollKeyboardMouse();
    }

    void PollKeyboardMouse()
    {
        moveInput = BESInputReader.GetMoveVector();
        enhanceHeld = BESInputReader.IsKeyHeld(Key.C);

        if (BESInputReader.WasMouseLeftPressedThisFrame())
            QueueAction(PlayerAction.AttackLight);
        if (BESInputReader.WasMouseRightPressedThisFrame())
            QueueAction(PlayerAction.AttackHeavy);
        if (BESInputReader.WasKeyPressedThisFrame(Key.Space))
            QueueAction(PlayerAction.Jump);
        if (BESInputReader.WasKeyPressedThisFrame(Key.F))
            QueueAction(PlayerAction.Dash);
        if (BESInputReader.WasKeyPressedThisFrame(Key.Q))
            QueueAction(PlayerAction.SkillQ);
        if (BESInputReader.WasKeyPressedThisFrame(Key.E))
            QueueAction(PlayerAction.SkillE);
        if (BESInputReader.WasKeyPressedThisFrame(Key.R))
            QueueAction(PlayerAction.SkillR);
        if (BESInputReader.WasKeyPressedThisFrame(Key.T))
            QueueAction(PlayerAction.SkillT);
    }

    public void SetMoveInput(Vector2 value)
    {
        moveInput = Vector2.ClampMagnitude(value, 1f);
    }

    public void SetEnhanceHeld(bool held)
    {
        enhanceHeld = held;
    }

    public void ArmEnhance(float durationSeconds = 2f)
    {
        enhanceArmedUntil = Time.time + durationSeconds;
    }

    public void QueueAction(PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.AttackLight: attackLightPending = true; break;
            case PlayerAction.AttackHeavy: attackHeavyPending = true; break;
            case PlayerAction.Jump: jumpPending = true; break;
            case PlayerAction.Dash: dashPending = true; break;
            case PlayerAction.SkillQ: skillQPending = true; break;
            case PlayerAction.SkillE: skillEPending = true; break;
            case PlayerAction.SkillR: skillRPending = true; break;
            case PlayerAction.SkillT: skillTPending = true; break;
        }
    }

    public bool ConsumeAction(PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.AttackLight:
                if (!attackLightPending) return false;
                attackLightPending = false;
                return true;
            case PlayerAction.AttackHeavy:
                if (!attackHeavyPending) return false;
                attackHeavyPending = false;
                return true;
            case PlayerAction.Jump:
                if (!jumpPending) return false;
                jumpPending = false;
                return true;
            case PlayerAction.Dash:
                if (!dashPending) return false;
                dashPending = false;
                return true;
            case PlayerAction.SkillQ:
                if (!skillQPending) return false;
                skillQPending = false;
                return true;
            case PlayerAction.SkillE:
                if (!skillEPending) return false;
                skillEPending = false;
                return true;
            case PlayerAction.SkillR:
                if (!skillRPending) return false;
                skillRPending = false;
                return true;
            case PlayerAction.SkillT:
                if (!skillTPending) return false;
                skillTPending = false;
                return true;
            default:
                return false;
        }
    }

    public void ClearBufferedActions()
    {
        moveInput = Vector2.zero;
        enhanceHeld = false;
        enhanceArmedUntil = 0f;
        attackLightPending = false;
        attackHeavyPending = false;
        jumpPending = false;
        dashPending = false;
        skillQPending = false;
        skillEPending = false;
        skillRPending = false;
        skillTPending = false;
        TouchInputGate.ForceReleaseAll();
    }
}
