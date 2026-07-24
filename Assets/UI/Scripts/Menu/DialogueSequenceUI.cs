using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    [Serializable]
    public class DialogueBeat
    {
        public string speaker;
        [TextArea(2, 8)] public string text;
        public Sprite background;
        public Sprite leftCharacter;
        public Sprite rightCharacter;
        public bool dimLeft;
        public bool dimRight;
        public UnityEvent onBeatStarted;
    }

    public class DialogueSequenceUI : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] Image leftCharacter;
        [SerializeField] Image rightCharacter;
        [SerializeField] CanvasGroup leftGroup;
        [SerializeField] CanvasGroup rightGroup;
        [SerializeField] TMP_Text speakerText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button advanceButton;
        [SerializeField] Button skipButton;
        [SerializeField] GameObject skipConfirmation;
        [SerializeField] Button confirmSkipButton;
        [SerializeField] Button cancelSkipButton;
        [SerializeField, Min(0f)] float charactersPerSecond = 45f;
        [SerializeField] List<DialogueBeat> beats = new();
        [SerializeField] UnityEvent onSequenceCompleted;
        int index = -1;
        Coroutine typing;
        bool fullyShown;

        void Start()
        {
            if (advanceButton != null) advanceButton.onClick.AddListener(Advance);
            if (skipButton != null) skipButton.onClick.AddListener(OpenSkipConfirmation);
            if (confirmSkipButton != null) confirmSkipButton.onClick.AddListener(SkipNow);
            if (cancelSkipButton != null) cancelSkipButton.onClick.AddListener(CloseSkipConfirmation);
            skipConfirmation?.SetActive(false);
        }

        public void Play() { index = -1; gameObject.SetActive(true); Advance(); }
        public void SetBeats(List<DialogueBeat> value) { beats = value ?? new List<DialogueBeat>(); }

        public void Advance()
        {
            if (!fullyShown && index >= 0) { CompleteTyping(); return; }
            index++;
            if (index >= beats.Count) { CompleteSequence(); return; }
            ShowBeat(beats[index]);
        }

        void ShowBeat(DialogueBeat beat)
        {
            if (background != null && beat.background != null) background.sprite = beat.background;
            ApplyCharacter(leftCharacter, beat.leftCharacter);
            ApplyCharacter(rightCharacter, beat.rightCharacter);
            if (leftGroup != null) leftGroup.alpha = beat.dimLeft ? 0.45f : 1f;
            if (rightGroup != null) rightGroup.alpha = beat.dimRight ? 0.45f : 1f;
            if (speakerText != null) speakerText.text = beat.speaker;
            beat.onBeatStarted?.Invoke();
            if (typing != null) StopCoroutine(typing);
            typing = StartCoroutine(TypeText(beat.text));
        }

        static void ApplyCharacter(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.enabled = sprite != null;
            image.sprite = sprite;
        }

        IEnumerator TypeText(string value)
        {
            fullyShown = false;
            if (bodyText == null) yield break;
            bodyText.text = value ?? string.Empty;
            bodyText.maxVisibleCharacters = 0;
            var count = bodyText.text.Length;
            var shown = 0f;
            while (bodyText.maxVisibleCharacters < count)
            {
                shown += Time.unscaledDeltaTime * Mathf.Max(1f, charactersPerSecond);
                bodyText.maxVisibleCharacters = Mathf.Min(count, Mathf.FloorToInt(shown));
                yield return null;
            }
            fullyShown = true;
            typing = null;
        }

        void CompleteTyping()
        {
            if (typing != null) StopCoroutine(typing);
            typing = null;
            if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
            fullyShown = true;
        }

        public void OpenSkipConfirmation() { if (skipConfirmation != null) skipConfirmation.SetActive(true); }
        public void CloseSkipConfirmation() { if (skipConfirmation != null) skipConfirmation.SetActive(false); }
        public void SkipNow() { CloseSkipConfirmation(); CompleteSequence(); }
        void CompleteSequence() { CompleteTyping(); onSequenceCompleted?.Invoke(); gameObject.SetActive(false); }
    }
}
