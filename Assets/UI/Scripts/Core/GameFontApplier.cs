using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Fonts
{
    [DisallowMultipleComponent]
    public class GameFontApplier : MonoBehaviour
    {
        [Header("Fonts")]
        [SerializeField] TMP_FontAsset gameFont;
        [SerializeField] Font legacyGameFont;

        [Header("Apply")]
        [SerializeField] bool applyOnAwake = true;
        [SerializeField] bool applyOnEnable = true;
        [SerializeField] bool includeInactive = true;
        [SerializeField] bool reapplyOneFrameLater = true;
        [SerializeField] string skipObjectNamePrefix = "PrefabFontTest_";

        public TMP_FontAsset GameFont => gameFont;
        public Font LegacyGameFont => legacyGameFont;

        void Awake()
        {
            if (applyOnAwake)
                ApplyFonts();
        }

        void OnEnable()
        {
            if (!applyOnEnable) return;
            ApplyFonts();
            if (reapplyOneFrameLater)
                StartCoroutine(ApplyFontsNextFrame());
        }

        [ContextMenu("Apply Fonts Now")]
        public void ApplyFonts()
        {
            if (gameFont != null)
            {
                var texts = GetComponentsInChildren<TMP_Text>(includeInactive);
                foreach (var text in texts)
                {
                    if (text == null || ShouldSkip(text) || text.font == gameFont) continue;
                    text.font = gameFont;
                }
            }

            if (legacyGameFont != null)
            {
                var legacyTexts = GetComponentsInChildren<Text>(includeInactive);
                foreach (var text in legacyTexts)
                {
                    if (text == null || ShouldSkip(text) || text.font == legacyGameFont) continue;
                    text.font = legacyGameFont;
                }
            }
        }

        bool ShouldSkip(Component component)
        {
            return !string.IsNullOrWhiteSpace(skipObjectNamePrefix)
                && component != null
                && component.name.StartsWith(skipObjectNamePrefix, System.StringComparison.Ordinal);
        }

        IEnumerator ApplyFontsNextFrame()
        {
            yield return null;
            ApplyFonts();
        }
    }
}
