using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public class SmoothTabGroup : MonoBehaviour,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField] RectTransform viewport;
        [Tooltip("Only enable when viewport is a real clipping frame with a valid RectTransform.")]
        [SerializeField] bool clipToViewport;
        [SerializeField] List<Button> buttons = new();
        [SerializeField] List<GameObject> panels = new();
        [SerializeField] RectTransform indicator;
        [SerializeField] List<RectTransform> indicatorPositions = new();
        [SerializeField, Min(.05f)] float transitionDuration = .32f;
        [SerializeField, Min(10f)] float swipeThreshold = 70f;
        [SerializeField, Min(0f)] float hiddenGap = 24f;
        [SerializeField, Range(0f, 1f)] float hiddenAlpha = .15f;
        [Tooltip("Keeps hidden panels prewarmed outside the viewport to avoid an activation/layout spike.")]
        [SerializeField] bool keepPanelsActive;
        [Tooltip("Uses a nested Canvas per animated panel so moving it does not rebuild the full parent Canvas.")]
        [SerializeField] bool isolatePanelCanvases;
        [SerializeField] AnimationCurve easing =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Min(0)] int initialIndex;

        readonly List<RectTransform> roots = new();
        readonly List<CanvasGroup> groups = new();
        readonly List<Vector2> visiblePositions = new();
        Vector2 dragStart;
        int currentIndex;
        Coroutine transition;
        bool initialized;

        public int CurrentIndex => currentIndex;
        public bool IsTransitioning => transition != null;

        void Awake()
        {
            Initialize();
            for (var i = 0; i < buttons.Count; i++)
            {
                var index = i;
                if (buttons[i] != null) buttons[i].onClick.AddListener(() => Show(index));
            }
            ShowImmediate(initialIndex);
        }

        void OnEnable()
        {
            if (initialized) ApplyStableLayout();
        }

        void Initialize()
        {
            viewport ??= transform as RectTransform;
            if (clipToViewport &&
                viewport != null &&
                viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();

            roots.Clear();
            groups.Clear();
            visiblePositions.Clear();
            Canvas.ForceUpdateCanvases();
            foreach (var panel in panels)
            {
                var root = panel != null ? panel.transform as RectTransform : null;
                roots.Add(root);
                if (isolatePanelCanvases && panel != null &&
                    panel.GetComponent<Canvas>() == null)
                    panel.AddComponent<Canvas>();
                var group = panel != null
                    ? panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>()
                    : null;
                groups.Add(group);
                visiblePositions.Add(root != null ? root.anchoredPosition : Vector2.zero);
            }
            initialized = true;
        }

        public void Show(int index)
        {
            if (!initialized) Initialize();
            index = Mathf.Clamp(index, 0, Mathf.Max(0, panels.Count - 1));
            if (index == currentIndex || IsTransitioning)
            {
                MoveIndicator(index, false);
                return;
            }
            var direction = index > currentIndex ? 1f : -1f;
            transition = StartCoroutine(AnimateTo(index, direction));
        }

        public void ShowImmediate(int index)
        {
            if (!initialized) Initialize();
            if (transition != null)
            {
                StopCoroutine(transition);
                transition = null;
            }
            currentIndex = Mathf.Clamp(index, 0, Mathf.Max(0, panels.Count - 1));
            ApplyStableLayout();
            MoveIndicator(currentIndex, false);
        }

        public void Next()
        {
            if (panels.Count > 1) Show((currentIndex + 1) % panels.Count);
        }

        public void Previous()
        {
            if (panels.Count > 1) Show((currentIndex - 1 + panels.Count) % panels.Count);
        }

        IEnumerator AnimateTo(int targetIndex, float direction)
        {
            var outgoingIndex = currentIndex;
            var outgoing = roots[outgoingIndex];
            var incoming = roots[targetIndex];
            var outgoingObject = panels[outgoingIndex];
            var incomingObject = panels[targetIndex];
            var outgoingGroup = groups[outgoingIndex];
            var incomingGroup = groups[targetIndex];
            var outgoingVisible = visiblePositions[outgoingIndex];
            var incomingVisible = visiblePositions[targetIndex];
            var offset = HorizontalOffset(direction);
            var transitionHiddenAlpha = keepPanelsActive ? 0f : hiddenAlpha;
            var indicatorStart = indicator != null ? indicator.position : Vector3.zero;
            var indicatorTarget = IndicatorPosition(targetIndex);

            incomingObject?.SetActive(true);
            SetInteraction(outgoingGroup, false);
            SetInteraction(incomingGroup, false);
            if (outgoing != null) outgoing.anchoredPosition = outgoingVisible;
            if (incoming != null) incoming.anchoredPosition = incomingVisible + offset;
            SetAlpha(outgoingGroup, 1f);
            SetAlpha(incomingGroup, transitionHiddenAlpha);

            var elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / transitionDuration);
                var t = easing != null ? easing.Evaluate(normalized) : normalized;
                if (outgoing != null)
                    outgoing.anchoredPosition =
                        Vector2.LerpUnclamped(outgoingVisible, outgoingVisible - offset, t);
                if (incoming != null)
                    incoming.anchoredPosition =
                        Vector2.LerpUnclamped(incomingVisible + offset, incomingVisible, t);
                SetAlpha(outgoingGroup, Mathf.Lerp(1f, transitionHiddenAlpha, t));
                SetAlpha(incomingGroup, Mathf.Lerp(transitionHiddenAlpha, 1f, t));
                if (indicator != null && indicatorTarget.HasValue)
                    indicator.position =
                        Vector3.LerpUnclamped(indicatorStart, indicatorTarget.Value, t);
                yield return null;
            }

            currentIndex = targetIndex;
            if (outgoing != null) outgoing.anchoredPosition = outgoingVisible;
            if (incoming != null) incoming.anchoredPosition = incomingVisible;
            if (keepPanelsActive)
            {
                if (outgoing != null)
                    outgoing.anchoredPosition = outgoingVisible - offset;
                SetAlpha(outgoingGroup, 0f);
            }
            else
            {
                outgoingObject?.SetActive(false);
                SetAlpha(outgoingGroup, 1f);
            }
            SetAlpha(incomingGroup, 1f);
            SetInteraction(incomingGroup, true);
            MoveIndicator(currentIndex, false);
            transition = null;
        }

        void ApplyStableLayout()
        {
            for (var i = 0; i < panels.Count; i++)
            {
                var selected = i == currentIndex;
                if (i < roots.Count && roots[i] != null)
                {
                    var direction = i < currentIndex ? -1f : 1f;
                    roots[i].anchoredPosition = selected || !keepPanelsActive
                        ? visiblePositions[i]
                        : visiblePositions[i] + HorizontalOffset(direction);
                }
                panels[i]?.SetActive(selected || keepPanelsActive);
                if (i < groups.Count)
                {
                    SetAlpha(groups[i], selected || !keepPanelsActive ? 1f : 0f);
                    SetInteraction(groups[i], selected);
                }
            }
        }

        Vector2 HorizontalOffset(float direction)
        {
            var width = viewport != null ? viewport.rect.width : 0f;
            if (width <= .01f) width = 600f;
            return Vector2.right * (width + hiddenGap) * Mathf.Sign(direction);
        }

        void MoveIndicator(int index, bool unused)
        {
            var target = IndicatorPosition(index);
            if (indicator != null && target.HasValue) indicator.position = target.Value;
        }

        Vector3? IndicatorPosition(int index) =>
            index >= 0 && index < indicatorPositions.Count && indicatorPositions[index] != null
                ? indicatorPositions[index].position
                : null;

        public void OnInitializePotentialDrag(PointerEventData eventData) =>
            eventData.useDragThreshold = false;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsTransitioning) dragStart = eventData.position;
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsTransitioning || panels.Count <= 1) return;
            var delta = eventData.position.x - dragStart.x;
            if (Mathf.Abs(delta) < swipeThreshold) return;
            if (delta < 0f) Next(); else Previous();
        }

        static void SetAlpha(CanvasGroup group, float value)
        {
            if (group != null) group.alpha = value;
        }

        static void SetInteraction(CanvasGroup group, bool value)
        {
            if (group == null) return;
            group.interactable = value;
            group.blocksRaycasts = value;
        }
    }
}
