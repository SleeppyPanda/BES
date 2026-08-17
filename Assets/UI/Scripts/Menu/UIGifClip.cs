using UnityEngine;

namespace BES.UI.Menu
{
    [CreateAssetMenu(fileName = "NewGifClip", menuName = "BES/UI/GIF Clip")]
    public sealed class UIGifClip : ScriptableObject
    {
        [SerializeField] private Texture2D[] frames;
        [SerializeField] private float[] frameDurations;

        public int FrameCount => frames != null ? frames.Length : 0;
        public Texture2D GetFrame(int index) => frames[index];

        public float GetDuration(int index)
        {
            if (frameDurations != null && index >= 0 && index < frameDurations.Length)
            {
                return Mathf.Max(0.01f, frameDurations[index]);
            }

            return 0.1f;
        }
    }
}
