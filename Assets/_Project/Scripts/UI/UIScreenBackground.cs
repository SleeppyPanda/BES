using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    /// <summary>
    /// Gắn mockup PNG đúng màn hình lúc runtime (kể cả prefab cũ chưa gán sprite).
    /// </summary>
    public class UIScreenBackground : MonoBehaviour
    {
        [SerializeField] UIScreenBackgroundId screenId;
        [SerializeField] bool raycastTarget = true;
        [SerializeField] string backgroundChildName = "Background";

        void Awake()
        {
            Apply();
        }

        public void Configure(UIScreenBackgroundId id, bool raycast = true)
        {
            screenId = id;
            raycastTarget = raycast;
        }

        public void Apply()
        {
            var manifest = UIScreenBackgroundManifestLoader.Load();
            if (manifest == null)
                return;

            var sprite = manifest.GetSprite(screenId);
            if (sprite == null)
                return;

            var rawBg = FindBackgroundRawImage();
            if (rawBg != null)
            {
                rawBg.texture = sprite.texture;
                rawBg.color = Color.white;
                rawBg.raycastTarget = raycastTarget;
                return;
            }

            var bg = FindBackgroundImage();
            if (bg == null)
                return;

            bg.sprite = sprite;
            bg.type = Image.Type.Simple;
            bg.preserveAspect = false;
            bg.color = Color.white;
            bg.raycastTarget = raycastTarget;
        }

        Image FindBackgroundImage()
        {
            if (!string.IsNullOrEmpty(backgroundChildName))
            {
                var child = transform.Find(backgroundChildName);
                if (child != null)
                {
                    var childImg = child.GetComponent<Image>();
                    if (childImg != null)
                        return childImg;
                }
            }

            return GetComponent<Image>();
        }

        RawImage FindBackgroundRawImage()
        {
            if (!string.IsNullOrEmpty(backgroundChildName))
            {
                var child = transform.Find(backgroundChildName);
                if (child != null)
                {
                    var childImg = child.GetComponent<RawImage>();
                    if (childImg != null)
                        return childImg;
                }
            }

            return GetComponent<RawImage>();
        }
    }
}
