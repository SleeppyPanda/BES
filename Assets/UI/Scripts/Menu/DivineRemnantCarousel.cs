using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public enum RemnantSectionLayout { VariantA, VariantB }

    [Serializable]
    public class DivineRemnantSectionView
    {
        public string enemyId;
        public RemnantSectionLayout layoutVariant;
        public RectTransform sectionRoot;
        public Image frameImage;
        public Image enemyImage;
        public List<Image> dropSlots = new();
        public Button selectButton;
    }

    public class DivineRemnantCarousel : MonoBehaviour
    {
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform content;
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [SerializeField, Min(0.05f)] float smoothTime = 0.18f;
        [SerializeField] List<DivineRemnantSectionView> sections = new();
        int currentIndex;
        Coroutine moveRoutine;

        public int CurrentIndex => currentIndex;
        public IReadOnlyList<DivineRemnantSectionView> Sections => sections;

        void Awake()
        {
            if (previousButton != null) previousButton.onClick.AddListener(Previous);
            if (nextButton != null) nextButton.onClick.AddListener(Next);
        }

        public void Previous() => ScrollTo(Mathf.Max(0, currentIndex - 1));
        public void Next() => ScrollTo(Mathf.Min(sections.Count - 1, currentIndex + 1));

        public void ScrollTo(int index)
        {
            if (sections.Count == 0 || scrollRect == null) return;
            currentIndex = Mathf.Clamp(index, 0, sections.Count - 1);
            var target = sections.Count <= 1 ? 0f : currentIndex / (float)(sections.Count - 1);
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(SmoothScroll(target));
        }

        System.Collections.IEnumerator SmoothScroll(float target)
        {
            var start = scrollRect.horizontalNormalizedPosition;
            var elapsed = 0f;
            while (elapsed < smoothTime)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / smoothTime));
                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(start, target, t);
                yield return null;
            }
            scrollRect.horizontalNormalizedPosition = target;
            moveRoutine = null;
        }
    }
}
