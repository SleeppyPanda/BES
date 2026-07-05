using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    /// <summary>
    /// Fit the full 1920×1080 layout on screen (Shrink) — không cắt mất UI ở mép.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class UICanvasFit : MonoBehaviour
    {
        [SerializeField] CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        [SerializeField] float matchWidthOrHeight = 0.5f;

        void Awake() => Apply();

        void Apply()
        {
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UIAnchorPresets.RefWidth, UIAnchorPresets.RefHeight);
            scaler.screenMatchMode = screenMatchMode;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (isActiveAndEnabled)
                Apply();
        }
#endif
    }
}
