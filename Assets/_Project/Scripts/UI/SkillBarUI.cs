using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class SkillBarUI : MonoBehaviour
    {
        public const int VisibleSlotCount = 2;

        [SerializeField] Image[] slotFrames = new Image[2];
        [SerializeField] Image[] skillIcons = new Image[2];
        [SerializeField] Image[] cooldownOverlays = new Image[2];
        [SerializeField] TMP_Text[] keyLabels = new TMP_Text[2];
        [SerializeField] Sprite[] characterSkillIcons = new Sprite[2];

        static readonly string[] DefaultKeyHints = { "E", "Q" };

        void Awake()
        {
            for (var i = 0; i < VisibleSlotCount; i++)
            {
                if (cooldownOverlays != null && i < cooldownOverlays.Length && cooldownOverlays[i] != null)
                {
                    cooldownOverlays[i].type = Image.Type.Filled;
                    cooldownOverlays[i].fillMethod = Image.FillMethod.Radial360;
                    cooldownOverlays[i].fillOrigin = (int)Image.Origin360.Top;
                    cooldownOverlays[i].fillClockwise = true;
                    cooldownOverlays[i].fillAmount = 0f;
                }

                if (keyLabels != null && i < keyLabels.Length && keyLabels[i] != null)
                    keyLabels[i].text = DefaultKeyHints[i];
            }

            ApplyCharacterSkillIcons();
        }

        public void SetFrameSprites(Sprite frame)
        {
            if (frame == null || slotFrames == null || !HUDPrimitiveStyles.IsWhitelistedFrameSprite(frame))
                return;

            foreach (var img in slotFrames)
            {
                if (img == null) continue;
                HUDPrimitiveStyles.TryApplySmallFrame(img, frame);
            }
        }

        public void SetSkillIcon(int index, Sprite icon)
        {
            if (skillIcons == null || index < 0 || index >= skillIcons.Length || skillIcons[index] == null)
                return;

            if (icon != null && HUDPrimitiveStyles.IsWhitelistedIconSprite(icon))
            {
                skillIcons[index].sprite = icon;
                skillIcons[index].color = Color.white;
            }
            else
            {
                skillIcons[index].sprite = null;
                skillIcons[index].color = new Color(0.5f, 0.5f, 0.55f, 0.6f);
            }
        }

        public void SetCooldown(int index, float normalized)
        {
            if (cooldownOverlays == null || index < 0 || index >= cooldownOverlays.Length || cooldownOverlays[index] == null)
                return;
            cooldownOverlays[index].fillAmount = Mathf.Clamp01(normalized);
        }

        public void SetKeyLabel(int index, string label)
        {
            if (keyLabels == null || index < 0 || index >= keyLabels.Length || keyLabels[index] == null)
                return;
            keyLabels[index].text = label;
        }

        public void ApplyCharacterSkillIcons()
        {
            if (characterSkillIcons == null)
                return;

            for (var i = 0; i < characterSkillIcons.Length && i < VisibleSlotCount; i++)
                SetSkillIcon(i, characterSkillIcons[i]);
        }
    }
}
