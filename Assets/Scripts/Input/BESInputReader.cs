using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads keyboard/mouse via the Input System package (compatible when legacy Input is disabled).
/// </summary>
public static class BESInputReader
{
    public static Vector2 GetMoveVector()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector2.zero;

        Vector2 move = Vector2.zero;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            move.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            move.x += 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            move.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            move.y -= 1f;

        return Vector2.ClampMagnitude(move, 1f);
    }

    public static bool IsKeyHeld(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[key].isPressed;
    }

    public static bool WasKeyPressedThisFrame(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[key].wasPressedThisFrame;
    }

    public static Vector2 GetMouseDelta()
    {
        Mouse mouse = Mouse.current;
        return mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
    }

    public static float GetScrollNormalized()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return 0f;

        // Input System reports scroll in pixels; ~120 per wheel notch on Windows.
        return mouse.scroll.ReadValue().y / 120f;
    }

    public static bool WasMouseLeftPressedThisFrame()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    public static bool WasMouseRightPressedThisFrame()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.rightButton.wasPressedThisFrame;
    }

    public static bool IsSprintHeld()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.leftShiftKey.isPressed;
    }

    public static bool UseTouchControls()
    {
        return Touchscreen.current != null &&
               (Application.isMobilePlatform || Application.isEditor == false);
    }
}
