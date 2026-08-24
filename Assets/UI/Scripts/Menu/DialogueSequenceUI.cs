using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public enum DialogueLayoutMode
    {
        Auto,
        SingleCenter,
        TwoSides,
        CustomSlots
    }

    public enum DialogueCastActionType
    {
        ShowOrUpdate,
        Leave,
        MoveToSlot
    }

    [Serializable]
    public class DialogueCastAction
    {
        public DialogueCastActionType actionType = DialogueCastActionType.ShowOrUpdate;
        public string characterId;
        public Sprite sprite;
        [Tooltip("Use -1 to find an existing slot by characterId.")]
        public int slotIndex = -1;
        public bool setAsSpeaker;
        public bool instant;
    }

    [Serializable]
    public class DialogueCharacterPlacement
    {
        public string characterId;
        [Tooltip("Use -1 to find by characterId.")]
        public int slotIndex = -1;
        public Sprite sprite;
        public DialogueSequenceUI.DialogueCharacterPose pose = new();
        public bool show = true;
        public bool instant;
    }

    [Serializable]
    public class DialogueMovementPoint
    {
        public DialogueSequenceUI.DialogueCharacterPose pose = new();
        [Min(0f)] public float duration = 0.25f;
        public AnimationCurve curve;
    }

    [Serializable]
    public class DialogueCharacterMovement
    {
        public string characterId;
        [Tooltip("Use -1 to find by characterId.")]
        public int slotIndex = -1;
        public List<DialogueMovementPoint> points = new();
    }

    [Serializable]
    public class DialogueCharacterVisualOverride
    {
        public string characterId;
        [Tooltip("Use -1 to find by characterId.")]
        public int slotIndex = -1;
        public bool show = true;
        public bool dim = true;
        [Range(0f, 1f)] public float dimAlpha = 0.35f;
        [Range(0f, 1f)] public float litAlpha = 1f;
        public Vector3 dimScale = Vector3.one;
        public Vector3 litScale = Vector3.one;
    }

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
        public bool hideAllCharacters;
        public bool fadeToBlackCheckpoint;
        [Header("Character layout and movement")]
        public DialogueLayoutMode layoutMode = DialogueLayoutMode.Auto;
        public bool instantLayout;
        public List<DialogueCastAction> castActions = new();
        [Tooltip("Per-beat final positions. These override the automatic layout for the listed characters.")]
        public List<DialogueCharacterPlacement> characterPlacements = new();
        [Tooltip("Multi-point movement paths played in this beat.")]
        public List<DialogueCharacterMovement> characterMovements = new();
        [Tooltip("Per-beat visibility/dim state for each character slot. Use this when more than one character appears in the same dialogue beat.")]
        public List<DialogueCharacterVisualOverride> characterVisuals = new();
        public UnityEvent onBeatStarted;
    }

    [Serializable]
    public class DialogueSequence
    {
        public string id;
        public string title;
        [TextArea(2, 5)] public string summary;
        public List<DialogueBeat> beats = new();
    }

    public class DialogueSequenceUI : MonoBehaviour
    {
        [Serializable]
        public class DialogueCharacterSlot
        {
            public string characterId;
            public Image image;
            public CanvasGroup group;
            public GameObject root;
            [Range(0f, 1f)] public float inactiveAlpha = 0.35f;
            [Range(0f, 1f)] public float activeAlpha = 1f;
            public Vector3 inactiveScale = Vector3.one;
            public Vector3 activeScale = Vector3.one;
            [NonSerialized] public bool present;
        }

        [Serializable]
        public class DialogueCharacterPose
        {
            public string id;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 offsetMin;
            public Vector2 offsetMax;
            public Vector3 scale = Vector3.one;
        }

        [SerializeField] Image background;
        [SerializeField] CanvasGroup checkpointFadeGroup;
        [SerializeField, Min(0f)] float checkpointFadeDuration = 0.2f;
        [Header("Character slots")]
        [SerializeField] List<DialogueCharacterSlot> characterSlots = new();
        [SerializeField] DialogueCharacterPose singleCenterPose = new()
        {
            id = "Single Center",
            anchorMin = new Vector2(0.28f, 0.08f),
            anchorMax = new Vector2(0.72f, 1f),
            scale = Vector3.one
        };
        [SerializeField] DialogueCharacterPose twoSideLeftPose = new()
        {
            id = "Two Side Left",
            anchorMin = new Vector2(0.04f, 0.08f),
            anchorMax = new Vector2(0.46f, 1f),
            scale = Vector3.one
        };
        [SerializeField] DialogueCharacterPose twoSideRightPose = new()
        {
            id = "Two Side Right",
            anchorMin = new Vector2(0.54f, 0.08f),
            anchorMax = new Vector2(0.96f, 1f),
            scale = Vector3.one
        };
        [SerializeField] List<DialogueCharacterPose> customSlotPoses = new();
        [SerializeField, Min(0f)] float characterMoveDuration = 0.28f;
        [SerializeField, Min(0f)] float characterFadeDuration = 0.18f;
        [SerializeField] AnimationCurve characterMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] Image leftCharacter;
        [SerializeField] Image rightCharacter;
        [SerializeField] CanvasGroup leftGroup;
        [SerializeField] CanvasGroup rightGroup;
        [SerializeField, Range(0f, 1f)] float legacyInactiveAlpha = 0.45f;
        [Header("Dialogue box")]
        [SerializeField] GameObject speakerNameRoot;
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
        Action runtimeCompleted;
        int index = -1;
        Coroutine typing;
        Coroutine beatRoutine;
        DialogueBeat currentBeat;
        bool waitingForBeatMovement;
        bool fullyShown;
        bool controlsWired;
        readonly List<Coroutine> characterRoutines = new();

        void Awake()
        {
            EnsureRuntimeView();
            WireControls();
            skipConfirmation?.SetActive(false);
        }

        void WireControls()
        {
            if (controlsWired) return;
            controlsWired = true;
            if (advanceButton != null) advanceButton.onClick.AddListener(Advance);
            if (skipButton != null) skipButton.onClick.AddListener(OpenSkipConfirmation);
            if (confirmSkipButton != null) confirmSkipButton.onClick.AddListener(SkipNow);
            if (cancelSkipButton != null) cancelSkipButton.onClick.AddListener(CloseSkipConfirmation);
        }

        public static DialogueSequenceUI CreateRuntimeOverlay(string objectName = "RuntimeStoryDialogueUI")
        {
            var canvasGo = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var ui = canvasGo.AddComponent<DialogueSequenceUI>();
            ui.EnsureRuntimeView();
            ui.WireControls();
            canvasGo.SetActive(false);
            return ui;
        }

        public void Play() { index = -1; gameObject.SetActive(true); Advance(); }
        public void SetBeats(List<DialogueBeat> value) { beats = value ?? new List<DialogueBeat>(); }
        public void Play(DialogueSequence sequence, Action completed = null)
        {
            SetBeats(sequence != null ? sequence.beats : null);
            Play(completed);
        }

        public void Play(List<DialogueBeat> sequenceBeats, Action completed = null)
        {
            SetBeats(sequenceBeats);
            Play(completed);
        }

        public void Play(Action completed)
        {
            runtimeCompleted = completed;
            Play();
        }

        public void Advance()
        {
            if (waitingForBeatMovement && beatRoutine != null)
                return;

            if (!fullyShown && index >= 0) { CompleteTyping(); return; }
            index++;
            if (index >= beats.Count) { CompleteSequence(); return; }
            currentBeat = beats[index];
            if (beatRoutine != null) StopCoroutine(beatRoutine);
            beatRoutine = StartCoroutine(ShowBeatRoutine(currentBeat));
        }

        IEnumerator ShowBeatRoutine(DialogueBeat beat)
        {
            if (beat.fadeToBlackCheckpoint)
                yield return PlayCheckpointFadeOut();
            if (background != null && beat.background != null) background.sprite = beat.background;
            StopCharacterRoutines();
            ApplyCharacters(beat);
            if (speakerNameRoot != null) speakerNameRoot.SetActive(!string.IsNullOrWhiteSpace(beat.speaker));
            if (speakerText != null) speakerText.text = beat.speaker;
            beat.onBeatStarted?.Invoke();
            waitingForBeatMovement = true;
            yield return PlayBeatMovements(beat);
            waitingForBeatMovement = false;
            if (beat.fadeToBlackCheckpoint)
                yield return PlayCheckpointFadeIn();
            StartBeatTyping(beat);
            beatRoutine = null;
        }

        void StartBeatTyping(DialogueBeat beat)
        {
            if (typing != null) StopCoroutine(typing);
            typing = StartCoroutine(TypeText(beat != null ? beat.text : string.Empty));
        }

        void ApplyCharacters(DialogueBeat beat)
        {
            if (characterSlots.Count > 0)
            {
                ApplyCharacterSlots(beat);
                return;
            }

            ApplyLegacyCharacters(beat);
        }

        void ApplyCharacterSlots(DialogueBeat beat)
        {
            if (beat != null && beat.hideAllCharacters)
            {
                HideAllCharacterSlots(true);
                return;
            }

            ApplyCastActions(beat);
            if (beat.characterPlacements == null || beat.characterPlacements.Count == 0)
                ApplyLegacySpritesToSlots(beat);
            ApplyCharacterPlacements(beat);
            var activeIndex = ResolveActiveSlot(beat);

            if (beat.characterPlacements == null || beat.characterPlacements.Count == 0)
                ApplyLayout(beat);

            for (var i = 0; i < characterSlots.Count; i++)
            {
                var slot = characterSlots[i];
                if (slot == null) continue;
                var visual = ResolveVisualOverride(beat, i, slot);
                if (visual != null)
                {
                    SetSlotVisible(slot, visual.show, true);
                    if (!visual.show) continue;
                    var alpha = visual.dim ? visual.dimAlpha : visual.litAlpha;
                    ApplySlotState(slot, alpha, visual.dim ? visual.dimScale : visual.litScale);
                    continue;
                }

                var active = !string.IsNullOrWhiteSpace(beat.speaker) && i == activeIndex;
                var alphaDefault = active ? slot.activeAlpha : slot.inactiveAlpha;
                ApplySlotState(slot, alphaDefault, active ? slot.activeScale : slot.inactiveScale);
            }
        }

        DialogueCharacterVisualOverride ResolveVisualOverride(DialogueBeat beat, int slotIndex, DialogueCharacterSlot slot)
        {
            if (beat?.characterVisuals == null) return null;
            foreach (var visual in beat.characterVisuals)
            {
                if (visual == null) continue;
                if (visual.slotIndex >= 0 && visual.slotIndex == slotIndex) return visual;
                if (!string.IsNullOrWhiteSpace(visual.characterId) &&
                    string.Equals(visual.characterId.Trim(), slot?.characterId?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return visual;
            }
            return null;
        }

        void HideAllCharacterSlots(bool instant)
        {
            for (var i = 0; i < characterSlots.Count; i++)
                SetSlotVisible(characterSlots[i], false, instant);
        }

        int ResolveActiveSlot(DialogueBeat beat)
        {
            if (!string.IsNullOrWhiteSpace(beat.speaker))
            {
                for (var i = 0; i < characterSlots.Count; i++)
                    if (string.Equals(characterSlots[i]?.characterId?.Trim(), beat.speaker.Trim(), StringComparison.OrdinalIgnoreCase))
                        return i;
            }

            if (beat.leftCharacter != null && !beat.dimLeft) return 0;
            if (beat.rightCharacter != null && !beat.dimRight) return characterSlots.Count - 1;
            return -1;
        }

        void ApplyCharacterPlacements(DialogueBeat beat)
        {
            if (beat.characterPlacements == null) return;
            foreach (var placement in beat.characterPlacements)
            {
                if (placement == null) continue;
                var slotIndex = ResolveSlot(placement.characterId, placement.slotIndex);
                if (slotIndex < 0 || slotIndex >= characterSlots.Count) continue;
                var slot = characterSlots[slotIndex];
                if (slot == null) continue;

                if (!string.IsNullOrWhiteSpace(placement.characterId))
                    slot.characterId = placement.characterId;
                if (placement.sprite != null)
                    SetSlotSprite(slot, placement.sprite);

                if (placement.show)
                {
                    SetSlotVisible(slot, true, placement.instant);
                    MoveSlotToPose(slot, placement.pose, placement.instant);
                }
                else
                {
                    MoveSlotToPose(slot, placement.pose, placement.instant);
                    SetSlotVisible(slot, false, placement.instant);
                }
            }
        }

        IEnumerator PlayBeatMovements(DialogueBeat beat)
        {
            if (beat.characterMovements == null || beat.characterMovements.Count == 0) yield break;

            var waiters = new List<Coroutine>();
            foreach (var movement in beat.characterMovements)
            {
                if (movement == null || movement.points == null || movement.points.Count == 0) continue;
                var slotIndex = ResolveSlot(movement.characterId, movement.slotIndex);
                if (slotIndex < 0 || slotIndex >= characterSlots.Count) continue;
                var slot = characterSlots[slotIndex];
                if (slot?.image == null) continue;
                var routine = StartCoroutine(MoveSlotPath(slot.image.rectTransform, movement.points));
                characterRoutines.Add(routine);
                waiters.Add(routine);
            }

            foreach (var waiter in waiters)
                yield return waiter;
        }

        void ApplyCastActions(DialogueBeat beat)
        {
            if (beat.castActions == null) return;
            foreach (var action in beat.castActions)
            {
                if (action == null) continue;
                var slotIndex = ResolveActionSlot(action);
                if (slotIndex < 0 || slotIndex >= characterSlots.Count) continue;
                var slot = characterSlots[slotIndex];
                if (slot == null) continue;

                switch (action.actionType)
                {
                    case DialogueCastActionType.ShowOrUpdate:
                        if (!string.IsNullOrWhiteSpace(action.characterId)) slot.characterId = action.characterId;
                        if (action.sprite != null) SetSlotSprite(slot, action.sprite);
                        SetSlotVisible(slot, true, action.instant);
                        break;
                    case DialogueCastActionType.Leave:
                        SetSlotVisible(slot, false, action.instant);
                        if (!string.IsNullOrWhiteSpace(action.characterId)) slot.characterId = string.Empty;
                        break;
                    case DialogueCastActionType.MoveToSlot:
                        MoveCharacterToSlot(action, slotIndex);
                        break;
                }

                if (action.setAsSpeaker)
                {
                    if (string.IsNullOrWhiteSpace(beat.speaker))
                        beat.speaker = slot.characterId;
                }
            }
        }

        int ResolveActionSlot(DialogueCastAction action)
        {
            return ResolveSlot(action.characterId, action.slotIndex);
        }

        int ResolveSlot(string characterId, int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < characterSlots.Count) return slotIndex;
            if (!string.IsNullOrWhiteSpace(characterId))
            {
                for (var i = 0; i < characterSlots.Count; i++)
                    if (string.Equals(characterSlots[i]?.characterId?.Trim(), characterId.Trim(), StringComparison.OrdinalIgnoreCase))
                        return i;
            }

            return -1;
        }

        void MoveCharacterToSlot(DialogueCastAction action, int targetIndex)
        {
            if (string.IsNullOrWhiteSpace(action.characterId)) return;
            var sourceIndex = -1;
            for (var i = 0; i < characterSlots.Count; i++)
            {
                if (string.Equals(characterSlots[i]?.characterId?.Trim(), action.characterId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    sourceIndex = i;
                    break;
                }
            }

            if (sourceIndex < 0 || sourceIndex == targetIndex) return;
            var source = characterSlots[sourceIndex];
            var target = characterSlots[targetIndex];
            if (source == null || target == null) return;

            target.characterId = source.characterId;
            if (target.image != null && source.image != null)
            {
                target.image.sprite = source.image.sprite;
                target.image.enabled = source.image.enabled;
            }

            SetSlotVisible(target, true, action.instant);
            source.characterId = string.Empty;
            if (source.image != null)
            {
                source.image.sprite = null;
                source.image.enabled = false;
            }
            SetSlotVisible(source, false, true);
        }

        void ApplyLayout(DialogueBeat beat)
        {
            var mode = beat.layoutMode == DialogueLayoutMode.Auto ? ResolveAutoLayoutMode() : beat.layoutMode;
            if (mode == DialogueLayoutMode.CustomSlots)
            {
                ApplyCustomSlotPoses(beat.instantLayout);
                return;
            }

            var visible = VisibleSlotIndices();
            if (mode == DialogueLayoutMode.SingleCenter && visible.Count >= 1)
            {
                MoveSlotToPose(characterSlots[visible[0]], singleCenterPose, beat.instantLayout);
                return;
            }

            if (mode == DialogueLayoutMode.TwoSides && visible.Count >= 1)
            {
                MoveSlotToPose(characterSlots[visible[0]], twoSideLeftPose, beat.instantLayout);
                if (visible.Count >= 2) MoveSlotToPose(characterSlots[visible[1]], twoSideRightPose, beat.instantLayout);
            }
        }

        DialogueLayoutMode ResolveAutoLayoutMode()
        {
            var count = VisibleSlotIndices().Count;
            if (count == 1) return DialogueLayoutMode.SingleCenter;
            if (count == 2) return DialogueLayoutMode.TwoSides;
            return DialogueLayoutMode.CustomSlots;
        }

        List<int> VisibleSlotIndices()
        {
            var result = new List<int>();
            for (var i = 0; i < characterSlots.Count; i++)
            {
                var slot = characterSlots[i];
                if (slot?.image != null && slot.image.sprite != null && slot.present)
                    result.Add(i);
            }
            return result;
        }

        void ApplyLegacySpritesToSlots(DialogueBeat beat)
        {
            if (characterSlots.Count == 0) return;
            if (beat.leftCharacter != null) SetSlotSprite(characterSlots[0], beat.leftCharacter);
            if (beat.rightCharacter != null) SetSlotSprite(characterSlots[characterSlots.Count - 1], beat.rightCharacter);
        }

        static void SetSlotSprite(DialogueCharacterSlot slot, Sprite sprite)
        {
            if (slot?.image == null || sprite == null) return;
            slot.image.sprite = sprite;
            slot.image.enabled = true;
            slot.present = true;
            slot.root?.SetActive(true);
        }

        static void ApplySlotState(DialogueCharacterSlot slot, float alpha, Vector3 scale)
        {
            if (!slot.present) return;
            if (slot.root != null) slot.root.SetActive(slot.image == null || slot.image.sprite != null);
            if (slot.group == null && slot.image != null)
                slot.group = slot.image.GetComponent<CanvasGroup>() ?? slot.image.gameObject.AddComponent<CanvasGroup>();
            if (slot.group != null) slot.group.alpha = alpha;
            if (slot.image != null) slot.image.transform.localScale = scale;
        }

        void SetSlotVisible(DialogueCharacterSlot slot, bool visible, bool instant)
        {
            if (slot == null) return;
            if (slot.root != null) slot.root.SetActive(true);
            if (slot.group == null && slot.image != null)
                slot.group = slot.image.GetComponent<CanvasGroup>() ?? slot.image.gameObject.AddComponent<CanvasGroup>();

            var targetAlpha = visible ? slot.inactiveAlpha : 0f;
            slot.present = visible;
            if (instant || characterFadeDuration <= 0f || slot.group == null)
            {
                if (slot.group != null) slot.group.alpha = targetAlpha;
                if (!visible)
                {
                    if (slot.image != null) slot.image.enabled = false;
                    if (slot.root != null) slot.root.SetActive(false);
                }
                return;
            }

            StartCharacterRoutine(FadeSlot(slot, targetAlpha, characterFadeDuration, () =>
            {
                if (!visible)
                {
                    if (slot.image != null) slot.image.enabled = false;
                    if (slot.root != null) slot.root.SetActive(false);
                }
            }));
        }

        void MoveSlotToPose(DialogueCharacterSlot slot, DialogueCharacterPose pose, bool instant)
        {
            if (slot?.image == null || pose == null) return;
            var rect = slot.image.rectTransform;
            if (instant || characterMoveDuration <= 0f)
            {
                ApplyPose(rect, pose);
                return;
            }

            StartCharacterRoutine(MoveSlot(rect, pose, characterMoveDuration));
        }

        void ApplyCustomSlotPoses(bool instant)
        {
            for (var i = 0; i < characterSlots.Count && i < customSlotPoses.Count; i++)
                MoveSlotToPose(characterSlots[i], customSlotPoses[i], instant);
        }

        void StartCharacterRoutine(IEnumerator routine)
        {
            characterRoutines.Add(StartCoroutine(routine));
        }

        void StopCharacterRoutines()
        {
            foreach (var routine in characterRoutines)
                if (routine != null) StopCoroutine(routine);
            characterRoutines.Clear();
        }

        IEnumerator FadeSlot(DialogueCharacterSlot slot, float targetAlpha, float duration, Action completed)
        {
            var group = slot.group;
            var start = group != null ? group.alpha : 0f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                if (group != null) group.alpha = Mathf.Lerp(start, targetAlpha, t);
                yield return null;
            }
            if (group != null) group.alpha = targetAlpha;
            completed?.Invoke();
        }

        IEnumerator MoveSlot(RectTransform rect, DialogueCharacterPose pose, float duration)
        {
            if (duration <= 0f)
            {
                ApplyPose(rect, pose);
                yield break;
            }

            var startAnchorMin = rect.anchorMin;
            var startAnchorMax = rect.anchorMax;
            var startOffsetMin = rect.offsetMin;
            var startOffsetMax = rect.offsetMax;
            var startScale = rect.localScale;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                var t = characterMoveCurve != null ? characterMoveCurve.Evaluate(normalized) : normalized;
                rect.anchorMin = Vector2.LerpUnclamped(startAnchorMin, pose.anchorMin, t);
                rect.anchorMax = Vector2.LerpUnclamped(startAnchorMax, pose.anchorMax, t);
                rect.offsetMin = Vector2.LerpUnclamped(startOffsetMin, pose.offsetMin, t);
                rect.offsetMax = Vector2.LerpUnclamped(startOffsetMax, pose.offsetMax, t);
                rect.localScale = Vector3.LerpUnclamped(startScale, pose.scale, t);
                yield return null;
            }

            ApplyPose(rect, pose);
        }

        IEnumerator MoveSlotPath(RectTransform rect, List<DialogueMovementPoint> points)
        {
            foreach (var point in points)
            {
                if (point == null || point.pose == null) continue;
                yield return MoveSlot(rect, point.pose, Mathf.Max(0f, point.duration), point.curve);
            }
        }

        IEnumerator MoveSlot(RectTransform rect, DialogueCharacterPose pose, float duration, AnimationCurve curve)
        {
            var startAnchorMin = rect.anchorMin;
            var startAnchorMax = rect.anchorMax;
            var startOffsetMin = rect.offsetMin;
            var startOffsetMax = rect.offsetMax;
            var startScale = rect.localScale;
            var elapsed = 0f;

            if (duration <= 0f)
            {
                ApplyPose(rect, pose);
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                var t = curve != null ? curve.Evaluate(normalized) : normalized;
                rect.anchorMin = Vector2.LerpUnclamped(startAnchorMin, pose.anchorMin, t);
                rect.anchorMax = Vector2.LerpUnclamped(startAnchorMax, pose.anchorMax, t);
                rect.offsetMin = Vector2.LerpUnclamped(startOffsetMin, pose.offsetMin, t);
                rect.offsetMax = Vector2.LerpUnclamped(startOffsetMax, pose.offsetMax, t);
                rect.localScale = Vector3.LerpUnclamped(startScale, pose.scale, t);
                yield return null;
            }

            ApplyPose(rect, pose);
        }

        static void ApplyPose(RectTransform rect, DialogueCharacterPose pose)
        {
            rect.anchorMin = pose.anchorMin;
            rect.anchorMax = pose.anchorMax;
            rect.offsetMin = pose.offsetMin;
            rect.offsetMax = pose.offsetMax;
            rect.localScale = pose.scale;
        }

        void ApplyLegacyCharacters(DialogueBeat beat)
        {
            if (beat != null && beat.hideAllCharacters)
            {
                ApplyCharacter(leftCharacter, null);
                ApplyCharacter(rightCharacter, null);
                return;
            }

            ApplyCharacter(leftCharacter, beat.leftCharacter);
            ApplyCharacter(rightCharacter, beat.rightCharacter);
            if (leftGroup != null) leftGroup.alpha = beat.dimLeft ? legacyInactiveAlpha : 1f;
            if (rightGroup != null) rightGroup.alpha = beat.dimRight ? legacyInactiveAlpha : 1f;
        }

        IEnumerator PlayCheckpointFadeOut()
        {
            yield return FadeCheckpoint(1f);
        }

        IEnumerator PlayCheckpointFadeIn()
        {
            yield return FadeCheckpoint(0f);
        }

        IEnumerator FadeCheckpoint(float targetAlpha)
        {
            if (checkpointFadeGroup == null || checkpointFadeDuration <= 0f)
            {
                if (checkpointFadeGroup != null) checkpointFadeGroup.alpha = targetAlpha;
                yield break;
            }

            checkpointFadeGroup.gameObject.SetActive(true);
            checkpointFadeGroup.blocksRaycasts = targetAlpha > 0f;
            var start = checkpointFadeGroup.alpha;
            var elapsed = 0f;
            while (elapsed < checkpointFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                checkpointFadeGroup.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(elapsed / checkpointFadeDuration));
                yield return null;
            }
            checkpointFadeGroup.alpha = targetAlpha;
            checkpointFadeGroup.blocksRaycasts = targetAlpha > 0f;
            if (Mathf.Approximately(targetAlpha, 0f))
                checkpointFadeGroup.gameObject.SetActive(false);
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
            if (bodyText == null)
            {
                fullyShown = true;
                yield break;
            }
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
        void CompleteSequence()
        {
            CompleteTyping();
            waitingForBeatMovement = false;
            currentBeat = null;
            onSequenceCompleted?.Invoke();
            var completed = runtimeCompleted;
            runtimeCompleted = null;
            gameObject.SetActive(false);
            completed?.Invoke();
        }

        void EnsureRuntimeView()
        {
            if (background != null && speakerText != null && bodyText != null && advanceButton != null) return;

            var rect = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            background ??= CreateImage(transform, "Background", new Color(0f, 0f, 0f, 0.55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (checkpointFadeGroup == null)
            {
                var fade = CreateImage(transform, "CheckpointFade", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                checkpointFadeGroup = fade.GetComponent<CanvasGroup>() ?? fade.gameObject.AddComponent<CanvasGroup>();
                checkpointFadeGroup.alpha = 0f;
                checkpointFadeGroup.blocksRaycasts = false;
                checkpointFadeGroup.gameObject.SetActive(false);
            }
            leftCharacter ??= CreateImage(transform, "LeftCharacter", Color.white, new Vector2(0f, 0f), new Vector2(0.45f, 1f), new Vector2(80f, 0f), new Vector2(-80f, 0f));
            rightCharacter ??= CreateImage(transform, "RightCharacter", Color.white, new Vector2(0.55f, 0f), new Vector2(1f, 1f), new Vector2(80f, 0f), new Vector2(-80f, 0f));
            leftGroup ??= leftCharacter.GetComponent<CanvasGroup>() ?? leftCharacter.gameObject.AddComponent<CanvasGroup>();
            rightGroup ??= rightCharacter.GetComponent<CanvasGroup>() ?? rightCharacter.gameObject.AddComponent<CanvasGroup>();
            if (characterSlots.Count == 0) CreateDefaultCharacterSlots();

            var box = CreateImage(transform, "DialogueBox", new Color(0.93f, 0.88f, 0.78f, 0.96f), new Vector2(0.18f, 0.06f), new Vector2(0.82f, 0.24f), Vector2.zero, Vector2.zero);
            var nameBox = CreateImage(transform, "NameBox", new Color(0.82f, 0.55f, 0.42f, 1f), new Vector2(0.21f, 0.22f), new Vector2(0.39f, 0.30f), Vector2.zero, Vector2.zero);
            speakerNameRoot ??= nameBox.gameObject;

            speakerText ??= CreateText(nameBox.transform, "SpeakerText", "ALMA", 42f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            bodyText ??= CreateText(box.transform, "BodyText", string.Empty, 26f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.22f, 0.15f, 0.1f, 1f));
            var bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0.08f, 0.18f);
            bodyRect.anchorMax = new Vector2(0.92f, 0.82f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;

            advanceButton ??= box.gameObject.AddComponent<Button>();
            skipButton ??= CreateButton(transform, "SkipButton", "Skip", new Vector2(0.88f, 0.90f), new Vector2(0.98f, 0.98f));
            skipConfirmation ??= CreateSkipConfirmation(transform);
        }

        void CreateDefaultCharacterSlots()
        {
            characterSlots.Clear();
            var anchors = new[]
            {
                new Vector4(0.02f, 0.12f, 0.30f, 0.98f),
                new Vector4(0.22f, 0.12f, 0.48f, 0.98f),
                new Vector4(0.52f, 0.12f, 0.78f, 0.98f),
                new Vector4(0.70f, 0.12f, 0.98f, 0.98f)
            };

            for (var i = 0; i < anchors.Length; i++)
            {
                var a = anchors[i];
                var image = CreateImage(transform, $"CharacterSlot{i + 1}", Color.white, new Vector2(a.x, a.y), new Vector2(a.z, a.w), Vector2.zero, Vector2.zero);
                image.enabled = false;
                var group = image.gameObject.AddComponent<CanvasGroup>();
                characterSlots.Add(new DialogueCharacterSlot
                {
                    characterId = string.Empty,
                    image = image,
                    group = group,
                    root = image.gameObject,
                    inactiveAlpha = 0.35f,
                    activeAlpha = 1f,
                    inactiveScale = Vector3.one,
                    activeScale = Vector3.one
                });
            }
        }

        static Image CreateImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.preserveAspect = true;
            return image;
        }

        static TMP_Text CreateText(Transform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = true;
            return text;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var image = CreateImage(parent, name, new Color(1f, 1f, 1f, 0f), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var button = image.gameObject.AddComponent<Button>();
            CreateText(image.transform, "Label", label, 36f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            return button;
        }

        GameObject CreateSkipConfirmation(Transform parent)
        {
            var panel = CreateImage(parent, "SkipConfirmation", new Color(0f, 0f, 0f, 0.75f), new Vector2(0.36f, 0.40f), new Vector2(0.64f, 0.60f), Vector2.zero, Vector2.zero);
            CreateText(panel.transform, "Message", "Bỏ qua hội thoại?", 26f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            confirmSkipButton = CreateButton(panel.transform, "ConfirmSkipButton", "Có", new Vector2(0.12f, 0.08f), new Vector2(0.44f, 0.32f));
            cancelSkipButton = CreateButton(panel.transform, "CancelSkipButton", "Không", new Vector2(0.56f, 0.08f), new Vector2(0.88f, 0.32f));
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }
    }
}
