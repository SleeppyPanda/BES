using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Fonts
{
    [DisallowMultipleComponent]
    public class GameFontApplier : MonoBehaviour
    {
        [Header("Fonts")]
        [Tooltip("Default font for almost every text in the game. Use TMP Font Asset generated from Assets/Art Ui/Game Việt hóa mới/hywenhei/zhcn.ttf.")]
        [SerializeField] TMP_FontAsset defaultGameFont;
        [Tooltip("Font for character information/detail UI. Use TMP Font Asset generated from Assets/Art Ui/Game Việt hóa mới/SVN-MoneyGame (1).otf.")]
        [SerializeField] TMP_FontAsset characterInfoFont;
        [SerializeField] Font defaultLegacyFont;
        [SerializeField] Font characterInfoLegacyFont;

        [Header("Apply")]
        [SerializeField] bool applyOnAwake = true;
        [SerializeField] bool applyOnEnable = true;
        [SerializeField] bool includeInactive = true;
        [SerializeField] List<string> characterInfoNameHints = new()
        {
            "CharacterProfile",
            "CharacterCollection",
            "CharacterDetail",
            "CharacterInfo",
            "CharacterStats",
            "CharacterDescription",
            "SelectedCharacterName",
            "LevelCharacterName",
            "DetailName",
            "DetailDescription"
        };

        public TMP_FontAsset DefaultGameFont => defaultGameFont;
        public TMP_FontAsset CharacterInfoFont => characterInfoFont;

        void Awake()
        {
            if (applyOnAwake) ApplyFonts();
        }

        void OnEnable()
        {
            if (applyOnEnable) ApplyFonts();
        }

        [ContextMenu("Apply Fonts Now")]
        public void ApplyFonts()
        {
            var texts = GetComponentsInChildren<TMP_Text>(includeInactive);
            foreach (var text in texts)
                ApplyFont(text);

            var legacyTexts = GetComponentsInChildren<Text>(includeInactive);
            foreach (var text in legacyTexts)
                ApplyFont(text);
        }

        public void ApplyFont(TMP_Text text)
        {
            if (text == null) return;
            var font = IsCharacterInfoText(text) ? characterInfoFont : defaultGameFont;
            if (font == null) return;
            text.font = font;
        }

        public void ApplyFont(Text text)
        {
            if (text == null) return;
            var font = IsCharacterInfoText(text.transform) ? characterInfoLegacyFont : defaultLegacyFont;
            if (font == null) return;
            text.font = font;
        }

        bool IsCharacterInfoText(TMP_Text text)
        {
            if (text == null || characterInfoNameHints == null) return false;
            var current = text.transform;
            while (current != null && current != transform.parent)
            {
                foreach (var hint in characterInfoNameHints)
                {
                    if (string.IsNullOrWhiteSpace(hint)) continue;
                    if (current.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                current = current.parent;
            }
            return false;
        }

        bool IsCharacterInfoText(Transform target)
        {
            if (target == null || characterInfoNameHints == null) return false;
            var current = target;
            while (current != null && current != transform.parent)
            {
                foreach (var hint in characterInfoNameHints)
                {
                    if (string.IsNullOrWhiteSpace(hint)) continue;
                    if (current.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                current = current.parent;
            }
            return false;
        }
    }
}
