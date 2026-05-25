using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Virtual joystick for movement. Uses the same exclusive touch gate as action buttons.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerExitHandler
{
    public string controlId = "joystick";
    public RectTransform background;
    public RectTransform handle;
    public float handleRange = 60f;

    Canvas canvas;
    Camera uiCamera;
    bool isDragging;
    int activePointerId = -1;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!TouchInputGate.TryAcquire(controlId, eventData.pointerId))
            return;

        isDragging = true;
        activePointerId = eventData.pointerId;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData.pointerId != activePointerId)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            uiCamera,
            out Vector2 localPoint);

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
        if (handle != null)
            handle.anchoredPosition = clamped;

        Vector2 normalized = clamped / handleRange;
        if (PlayerInputHub.Instance != null)
            PlayerInputHub.Instance.SetMoveInput(normalized);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndDrag(eventData.pointerId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging && eventData.pointerId == activePointerId)
            EndDrag(eventData.pointerId);
    }

    void EndDrag(int pointerId)
    {
        if (!isDragging || pointerId != activePointerId)
            return;

        isDragging = false;
        activePointerId = -1;
        TouchInputGate.Release(controlId, pointerId);

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (PlayerInputHub.Instance != null)
            PlayerInputHub.Instance.SetMoveInput(Vector2.zero);
    }
}
