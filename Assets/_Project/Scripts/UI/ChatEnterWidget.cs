using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    /// <summary>
    /// Chat bubble + Enter pill — góc trái dưới theo Main play.png.
    /// </summary>
    public class ChatEnterWidget : MonoBehaviour
    {
        [SerializeField] Image bubbleIcon;
        [SerializeField] Image enterPill;
        [SerializeField] TMP_Text enterLabel;

        public void Apply(HUDSpriteManifest manifest)
        {
            var rect = GetComponent<RectTransform>();
            if (rect == null)
                return;

            UIAnchorPresets.ApplyChatRegion(rect);

            if (bubbleIcon != null)
            {
                if (manifest?.chatBubbleIcon != null && HUDPrimitiveStyles.IsWhitelistedIconSprite(manifest.chatBubbleIcon))
                {
                    bubbleIcon.sprite = manifest.chatBubbleIcon;
                    bubbleIcon.color = Color.white;
                    bubbleIcon.preserveAspect = true;
                }
                else
                {
                    bubbleIcon.sprite = null;
                    bubbleIcon.color = new Color(1f, 1f, 1f, 0f);
                }
            }

            if (enterPill != null)
            {
                if (manifest?.chatEnterFrame != null && HUDPrimitiveStyles.IsWhitelistedFrameSprite(manifest.chatEnterFrame))
                {
                    HUDPrimitiveStyles.TryApplySmallFrame(enterPill, manifest.chatEnterFrame);
                    enterPill.color = new Color(0.96f, 0.96f, 0.94f, 0.95f);
                }
                else
                    HUDPrimitiveStyles.ApplySolidPanel(enterPill, new Color(0.96f, 0.96f, 0.94f, 0.95f));
            }

            if (enterLabel != null)
            {
                enterLabel.text = "Enter";
                enterLabel.color = new Color(0.1f, 0.12f, 0.16f, 1f);
                enterLabel.fontStyle = FontStyles.Bold;
            }
        }
    }
}
