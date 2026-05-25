using UnityEngine;

/// <summary>
/// Ensures only one touch UI control is active at a time (no overlapping presses).
/// Joystick and action buttons share the same gate.
/// </summary>
public static class TouchInputGate
{
    static string activeControlId;
    static int activePointerId = -1;

    public static bool HasActiveControl => !string.IsNullOrEmpty(activeControlId);

    public static string ActiveControlId => activeControlId;

    /// <summary>Try to acquire exclusive touch for a control. Returns false if another control is held.</summary>
    public static bool TryAcquire(string controlId, int pointerId)
    {
        if (string.IsNullOrEmpty(controlId))
            return false;

        if (!HasActiveControl)
        {
            activeControlId = controlId;
            activePointerId = pointerId;
            return true;
        }

        return activeControlId == controlId && activePointerId == pointerId;
    }

    public static void Release(string controlId, int pointerId)
    {
        if (!HasActiveControl)
            return;

        if (activeControlId != controlId)
            return;

        if (activePointerId != pointerId && activePointerId != -1)
            return;

        activeControlId = null;
        activePointerId = -1;
    }

    public static void ForceReleaseAll()
    {
        activeControlId = null;
        activePointerId = -1;
    }
}
