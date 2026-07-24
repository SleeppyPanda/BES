using System.Collections;
using UnityEngine;

namespace BES.UI.Menu
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanelTransition : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] RectTransform animatedRoot;
        [SerializeField, Min(0.01f)] float duration = 0.2f;
        [SerializeField] Vector2 enterOffset = new(-40f, 0f);
        [SerializeField] AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        Vector2 shownPosition;
        Coroutine routine;

        void Awake()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
            animatedRoot ??= transform as RectTransform;
            if (animatedRoot != null) shownPosition = animatedRoot.anchoredPosition;
        }

        void OnEnable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(AnimateIn());
        }

        IEnumerator AnimateIn()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            var start = shownPosition + enterOffset;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = easing.Evaluate(Mathf.Clamp01(elapsed / duration));
                canvasGroup.alpha = t;
                if (animatedRoot != null) animatedRoot.anchoredPosition = Vector2.LerpUnclamped(start, shownPosition, t);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            if (animatedRoot != null) animatedRoot.anchoredPosition = shownPosition;
            routine = null;
        }
    }
}
