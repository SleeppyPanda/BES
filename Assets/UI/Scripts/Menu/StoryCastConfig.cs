using System;
using System.Collections.Generic;
using UnityEngine;

namespace BES.UI.Menu
{
    [CreateAssetMenu(menuName = "BES/UI/Story Cast Config", fileName = "StoryCastConfig")]
    public class StoryCastConfig : ScriptableObject
    {
        [Tooltip("Cấu hình theo từng Chương, phân biệt qua Chapter ID.")]
        public List<ChapterCastConfig> chapters = new();

        public ChapterCastConfig FindChapterConfig(string chapterId)
        {
            return chapters.Find(x => x != null && string.Equals(x.chapterId, chapterId, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Serializable]
    public class ChapterCastConfig
    {
        public string chapterId;
        public bool autoGenerateChapterFromTextFiles;
        public List<StoryCharacterProfile> characterProfiles = new();
        public List<StoryBeatCastOverride> beatOverrides = new();
    }

    [Serializable]
    public class StoryCharacterProfile
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
    public class StoryBeatCastOverride
    {
        [Tooltip("Exact parsed beat index across the full Chapter source. Use -1 to ignore.")]
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
        public List<StoryBeatCharacterState> characters = new();
        public List<DialogueCharacterMovement> movements = new();
    }

    [Serializable]
    public class StoryBeatCharacterState
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
