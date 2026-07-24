using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public enum HomeMode { Story, Play }

    public class HomeModeSwitcher : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Shared visible area")]
        [Tooltip("The RectTransform that defines the identical visible area for both tabs. Leave empty to use this object.")]
        [SerializeField] RectTransform viewport;
        [SerializeField] bool enforceViewportMask = true;
        [SerializeField] Vector4 maskPadding;

        [Header("Only these two objects move")]
        [SerializeField] GameObject storyModeContent;
        [SerializeField] GameObject playModeContent;
        [SerializeField] RectTransform storyAnimatedRoot;
        [SerializeField] RectTransform playAnimatedRoot;
        [SerializeField] CanvasGroup storyCanvasGroup;
        [SerializeField] CanvasGroup playCanvasGroup;
        [SerializeField] TMP_Text modeTitle;

        [Header("Controls")]
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [Tooltip("Attach this component to the intended swipe area, not the full Canvas.")]
        [SerializeField, Min(10f)] float swipeThreshold = 70f;

        [Header("Transition")]
        [SerializeField, Min(0.01f)] float transitionDuration = 0.32f;
        [Tooltip("Extra empty space between the visible tab and the hidden tab, in canvas pixels.")]
        [SerializeField, Min(0f)] float hiddenGap = 24f;
        [SerializeField, Range(0.5f, 2f)] float slideDistanceMultiplier = 1f;
        [SerializeField] bool crossFade = true;
        [SerializeField, Range(0f, 1f)] float hiddenAlpha = 0.15f;
        [SerializeField] AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] HomeMode initialMode = HomeMode.Story;
        [SerializeField] UnityEvent onStoryModeShown;
        [SerializeField] UnityEvent onPlayModeShown;

        Vector2 dragStart;
        Vector2 dragCurrent;
        Vector2 storyVisiblePosition;
        Vector2 playVisiblePosition;
        HomeMode current;
        Coroutine transition;
        bool layoutCached;

        public HomeMode Current => current;
        public bool IsTransitioning => transition != null;
        public RectTransform Viewport => viewport;

        void Awake()
        {
            ResolveReferences();
            ConfigureViewportMask();
            CacheVisiblePositions();
            if (previousButton != null) previousButton.onClick.AddListener(Previous);
            if (nextButton != null) nextButton.onClick.AddListener(Next);
            ShowImmediate(initialMode);
        }

        void OnEnable()
        {
            if (!layoutCached) return;
            ApplyStableLayout();
        }

        void OnRectTransformDimensionsChange()
        {
            if (!Application.isPlaying || !layoutCached || IsTransitioning) return;
            ApplyStableLayout();
        }

        void ResolveReferences()
        {
            viewport ??= transform as RectTransform;
            storyAnimatedRoot ??= storyModeContent != null ? storyModeContent.transform as RectTransform : null;
            playAnimatedRoot ??= playModeContent != null ? playModeContent.transform as RectTransform : null;
            storyCanvasGroup ??= GetOrAddCanvasGroup(storyModeContent);
            playCanvasGroup ??= GetOrAddCanvasGroup(playModeContent);
        }

        static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            if (target == null) return null;
            return target.TryGetComponent<CanvasGroup>(out var group) ? group : target.AddComponent<CanvasGroup>();
        }

        void ConfigureViewportMask()
        {
            if (!enforceViewportMask || viewport == null) return;
            var mask = viewport.GetComponent<RectMask2D>();
            if (mask == null) mask = viewport.gameObject.AddComponent<RectMask2D>();
            mask.padding = maskPadding;
        }

        void CacheVisiblePositions()
        {
            Canvas.ForceUpdateCanvases();
            if (storyAnimatedRoot != null) storyVisiblePosition = storyAnimatedRoot.anchoredPosition;
            if (playAnimatedRoot != null) playVisiblePosition = playAnimatedRoot.anchoredPosition;
            layoutCached = true;
        }

        [ContextMenu("Recalculate Visible Tab Positions")]
        public void RecalculateVisiblePositions()
        {
            if (IsTransitioning) return;
            ResolveReferences();
            ConfigureViewportMask();
            CacheVisiblePositions();
            ApplyStableLayout();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsTransitioning) return;
            dragStart = eventData.position;
            dragCurrent = dragStart;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsTransitioning) return;
            dragCurrent = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsTransitioning) return;
            dragCurrent = eventData.position;
            var delta = dragCurrent.x - dragStart.x;
            if (Mathf.Abs(delta) < swipeThreshold) return;
            if (delta < 0f) Next(); else Previous();
        }

        public void Previous() => Show(current == HomeMode.Story ? HomeMode.Play : HomeMode.Story, -1f);
        public void Next() => Show(current == HomeMode.Story ? HomeMode.Play : HomeMode.Story, 1f);
        public void ShowStory() => Show(HomeMode.Story, -1f);
        public void ShowPlay() => Show(HomeMode.Play, 1f);

        public void Show(HomeMode mode, float direction = 1f)
        {
            if (mode == current || IsTransitioning) return;
            transition = StartCoroutine(Transition(mode, Mathf.Approximately(direction, 0f) ? 1f : Mathf.Sign(direction)));
        }

        IEnumerator Transition(HomeMode targetMode, float direction)
        {
            var outgoingRoot = RootFor(current);
            var incomingRoot = RootFor(targetMode);
            var outgoingObject = ObjectFor(current);
            var incomingObject = ObjectFor(targetMode);
            var outgoingGroup = GroupFor(current);
            var incomingGroup = GroupFor(targetMode);
            var outgoingVisible = VisiblePositionFor(current);
            var incomingVisible = VisiblePositionFor(targetMode);
            var distance = GetSlideDistance();
            var offset = Vector2.right * distance * direction;

            incomingObject?.SetActive(true);
            SetInteraction(outgoingGroup, false);
            SetInteraction(incomingGroup, false);
            if (outgoingRoot != null) outgoingRoot.anchoredPosition = outgoingVisible;
            if (incomingRoot != null) incomingRoot.anchoredPosition = incomingVisible + offset;
            SetAlpha(outgoingGroup, 1f);
            SetAlpha(incomingGroup, crossFade ? hiddenAlpha : 1f);

            var elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = easing.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));
                if (outgoingRoot != null) outgoingRoot.anchoredPosition = Vector2.LerpUnclamped(outgoingVisible, outgoingVisible - offset, t);
                if (incomingRoot != null) incomingRoot.anchoredPosition = Vector2.LerpUnclamped(incomingVisible + offset, incomingVisible, t);
                if (crossFade)
                {
                    SetAlpha(outgoingGroup, Mathf.Lerp(1f, hiddenAlpha, t));
                    SetAlpha(incomingGroup, Mathf.Lerp(hiddenAlpha, 1f, t));
                }
                yield return null;
            }

            current = targetMode;
            if (outgoingRoot != null) outgoingRoot.anchoredPosition = outgoingVisible;
            if (incomingRoot != null) incomingRoot.anchoredPosition = incomingVisible;
            outgoingObject?.SetActive(false);
            SetAlpha(outgoingGroup, 1f);
            SetAlpha(incomingGroup, 1f);
            SetInteraction(incomingGroup, true);
            ApplyLabelAndEvent();
            transition = null;
        }

        float GetSlideDistance()
        {
            var width = viewport != null ? viewport.rect.width : 0f;
            if (width <= 0.01f && transform is RectTransform ownRect) width = ownRect.rect.width;
            if (width <= 0.01f) width = 600f;
            return width * slideDistanceMultiplier + hiddenGap;
        }

        void ShowImmediate(HomeMode mode)
        {
            current = mode;
            ApplyStableLayout();
            ApplyLabelAndEvent();
        }

        void ApplyStableLayout()
        {
            var storyActive = current == HomeMode.Story;
            if (storyAnimatedRoot != null) storyAnimatedRoot.anchoredPosition = storyVisiblePosition;
            if (playAnimatedRoot != null) playAnimatedRoot.anchoredPosition = playVisiblePosition;
            storyModeContent?.SetActive(storyActive);
            playModeContent?.SetActive(!storyActive);
            SetAlpha(storyCanvasGroup, 1f);
            SetAlpha(playCanvasGroup, 1f);
            SetInteraction(storyCanvasGroup, storyActive);
            SetInteraction(playCanvasGroup, !storyActive);
        }

        RectTransform RootFor(HomeMode mode) => mode == HomeMode.Story ? storyAnimatedRoot : playAnimatedRoot;
        GameObject ObjectFor(HomeMode mode) => mode == HomeMode.Story ? storyModeContent : playModeContent;
        CanvasGroup GroupFor(HomeMode mode) => mode == HomeMode.Story ? storyCanvasGroup : playCanvasGroup;
        Vector2 VisiblePositionFor(HomeMode mode) => mode == HomeMode.Story ? storyVisiblePosition : playVisiblePosition;

        static void SetAlpha(CanvasGroup group, float alpha) { if (group != null) group.alpha = alpha; }
        static void SetInteraction(CanvasGroup group, bool enabled)
        {
            if (group == null) return;
            group.interactable = enabled;
            group.blocksRaycasts = enabled;
        }

        void ApplyLabelAndEvent()
        {
            if (modeTitle != null) modeTitle.text = current == HomeMode.Story ? "STORY MODE" : "PLAY MODE";
            if (current == HomeMode.Story) onStoryModeShown?.Invoke();
            else onPlayModeShown?.Invoke();
        }
    }
}
