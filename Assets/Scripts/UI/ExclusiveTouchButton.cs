using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Touch button that participates in exclusive touch gate (one control at a time).
/// </summary>
[RequireComponent(typeof(Image))]
public class ExclusiveTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public string controlId;
    public PlayerAction actionOnPress = PlayerAction.AttackLight;
    public bool fireActionOnPress = true;
    public bool holdForEnhance;
    [Tooltip("Tap to arm enhance for a short window (mobile-friendly, one button at a time).")]
    public bool toggleEnhanceOnPress;
    public Color normalColor = new Color(1f, 1f, 1f, 0.35f);
    public Color pressedColor = new Color(1f, 1f, 1f, 0.65f);

    Image image;
    bool isHeld;

    void Awake()
    {
        image = GetComponent<Image>();
        if (image != null)
            image.color = normalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!TouchInputGate.TryAcquire(controlId, eventData.pointerId))
            return;

        isHeld = true;
        if (image != null)
            image.color = pressedColor;

        if (holdForEnhance && PlayerInputHub.Instance != null)
            PlayerInputHub.Instance.SetEnhanceHeld(true);

        if (toggleEnhanceOnPress && PlayerInputHub.Instance != null)
            PlayerInputHub.Instance.ArmEnhance();

        if (fireActionOnPress && PlayerInputHub.Instance != null)
            PlayerInputHub.Instance.QueueAction(actionOnPress);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release(eventData.pointerId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isHeld)
            Release(eventData.pointerId);
    }

    void Release(int pointerId)
    {
        if (!isHeld)
            return;

        isHeld = false;
        TouchInputGate.Release(controlId, pointerId);

        if (image != null)
            image.color = normalColor;

        if (holdForEnhance && PlayerInputHub.Instance != null)
            PlayerInputHub.Instance.SetEnhanceHeld(false);

        // toggleEnhanceOnPress does not need release handling
    }
}
