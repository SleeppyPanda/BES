using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class UIGifPlayer : MonoBehaviour
    {
        [SerializeField] private RawImage targetImage;
        [SerializeField] private UIGifClip clip;
        [HideInInspector]
        [SerializeField] private Texture2D[] frames;
        [HideInInspector]
        [SerializeField] private float[] frameDurations;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool loop = true;

        private int frameIndex;
        private float elapsed;
        private bool playing;

        public UIGifClip Clip => clip;

        public void SetClip(UIGifClip newClip, bool restart = true)
        {
            clip = newClip;
            if (restart)
            {
                frameIndex = 0;
                elapsed = 0f;
                playing = playOnEnable;
                ShowCurrentFrame();
            }
        }

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<RawImage>();
            }
        }

        private void OnEnable()
        {
            frameIndex = 0;
            elapsed = 0f;
            playing = playOnEnable;
            ShowCurrentFrame();
        }

        private void Update()
        {
            if (!playing || GetFrameCount() < 2)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float duration = GetDuration(frameIndex);
            while (elapsed >= duration)
            {
                elapsed -= duration;
                frameIndex++;

                if (frameIndex >= GetFrameCount())
                {
                    if (!loop)
                    {
                        frameIndex = GetFrameCount() - 1;
                        playing = false;
                        break;
                    }

                    frameIndex = 0;
                }

                duration = GetDuration(frameIndex);
            }

            ShowCurrentFrame();
        }

        private float GetDuration(int index)
        {
            if (clip != null)
            {
                return clip.GetDuration(index);
            }

            if (frameDurations != null && index < frameDurations.Length)
            {
                return Mathf.Max(0.01f, frameDurations[index]);
            }

            return 0.1f;
        }

        private void ShowCurrentFrame()
        {
            int count = GetFrameCount();
            if (targetImage != null && count > 0)
            {
                int index = Mathf.Clamp(frameIndex, 0, count - 1);
                targetImage.texture = clip != null ? clip.GetFrame(index) : frames[index];
            }
        }

        private int GetFrameCount()
        {
            return clip != null ? clip.FrameCount : (frames != null ? frames.Length : 0);
        }
    }
}
