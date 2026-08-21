using System;
using System.Collections.Generic;
using UnityEngine;

namespace BES.UI.Menu
{
    [CreateAssetMenu(menuName = "BES/UI/Chapter 1 Story Cast Config", fileName = "ChapterOneStoryCastConfig")]
    public class ChapterOneStoryCastConfig : ScriptableObject
    {
        [Tooltip("Off by default so Play Mode does not overwrite StoryChapter data edited in Unity. Turn on only when you want Chapter 1 to be regenerated from the text files in Resources/Main Story.")]
        public bool autoGenerateChapterFromTextFiles;

        [Tooltip("Optional default character data for story-only/support characters. Speaker must match the name used in Assets/Resources/Main Story/chương 1.")]
        public List<ChapterOneCharacterProfile> characterProfiles = new();

        [Tooltip("Optional per-beat overrides. Use globalBeatIndex for exact index, or textContains to match a story line/paragraph.")]
        public List<ChapterOneBeatCastOverride> beatOverrides = new();
    }

    [Serializable]
    public class ChapterOneCharacterProfile
    {
        public string speaker;
        public string displayName;
        public string characterId;
        public Sprite sprite;
        [Tooltip("Default slot used when no beat override is found. Use 0-3+ according to DialogueSequenceUI characterSlots.")]
        public int defaultSlotIndex = -1;
        public bool defaultDimWhenNotSpeaking = true;
    }

    [Serializable]
    public class ChapterOneBeatCastOverride
    {
        [Tooltip("Exact parsed beat index across the full Chapter 1 source. Use -1 to ignore.")]
        public int globalBeatIndex = -1;
        [Tooltip("If set, override applies to the first beat whose text contains this value.")]
        public string textContains;
        [Tooltip("If true, matching by textContains can apply to every matching beat, not only the first.")]
        public bool applyToAllTextMatches;
        public DialogueLayoutMode layoutMode = DialogueLayoutMode.CustomSlots;
        public bool instantLayout;
        public bool hideUnlistedSlots = true;
        [Min(1)] public int controlledSlotCount = 4;
        [Tooltip("Characters participating in this dialogue beat. Add only the characters that should be visible in this beat.")]
        public List<ChapterOneBeatCharacterState> characters = new();
        public List<DialogueCharacterMovement> movements = new();
    }

    [Serializable]
    public class ChapterOneBeatCharacterState
    {
        public string speaker;
        public string characterId;
        public Sprite sprite;
        public int slotIndex = -1;
        [Tooltip("If false, this character/slot is hidden for this beat.")]
        public bool show = true;
        [Tooltip("If true, this character is darkened. If false, this character is lit.")]
        public bool dim = true;
        [Range(0f, 1f)] public float dimAlpha = 0.35f;
        [Range(0f, 1f)] public float litAlpha = 1f;
        public Vector3 dimScale = Vector3.one;
        public Vector3 litScale = Vector3.one;
        public DialogueSequenceUI.DialogueCharacterPose pose = new();
        public bool applyPose;
        public bool instant;
    }
}
