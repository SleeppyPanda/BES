using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BES.UI.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
    public sealed class UIVideoPlayer : MonoBehaviour
    {
        [SerializeField] private RawImage targetImage;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private bool playOnEnable = true;

        private void Awake()
        {
            ResolveComponents();
            videoPlayer.prepareCompleted -= HandlePrepared;
            videoPlayer.prepareCompleted += HandlePrepared;
        }

        private void OnEnable()
        {
            ResolveComponents();
            if (videoPlayer == null || videoPlayer.clip == null)
            {
                return;
            }

            if (videoPlayer.isPrepared)
            {
                ApplyTexture();
                if (playOnEnable)
                {
                    videoPlayer.Play();
                }
            }
            else
            {
                videoPlayer.Prepare();
            }
        }

        private void OnDisable()
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= HandlePrepared;
            }
        }

        private void HandlePrepared(VideoPlayer source)
        {
            ApplyTexture();
            if (playOnEnable && isActiveAndEnabled)
            {
                source.Play();
            }
        }

        private void ApplyTexture()
        {
            if (targetImage != null && videoPlayer != null)
            {
                targetImage.texture = videoPlayer.texture;
            }
        }

        private void ResolveComponents()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<RawImage>();
            }

            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
            }
        }
    }
}
