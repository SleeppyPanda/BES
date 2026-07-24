using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] Image target;
        [SerializeField] List<Sprite> frames = new();
        [SerializeField, Min(1f)] float framesPerSecond = 12f;
        [SerializeField] bool loop = true;
        [SerializeField] bool playOnEnable = true;
        [SerializeField] bool useUnscaledTime = true;
        float time;
        int frame;
        bool playing;

        void OnEnable() { if (playOnEnable) Play(); }
        void Update()
        {
            if (!playing || target == null || frames.Count == 0) return;
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var next = Mathf.FloorToInt(time * framesPerSecond);
            if (!loop && next >= frames.Count) { frame = frames.Count - 1; playing = false; }
            else frame = next % frames.Count;
            target.sprite = frames[frame];
        }
        public void Play() { time = 0f; frame = 0; playing = true; ApplyFrame(); }
        public void Stop() => playing = false;
        public void SetFrame(int index) { frame = Mathf.Clamp(index, 0, Mathf.Max(0, frames.Count - 1)); ApplyFrame(); }
        void ApplyFrame() { if (target != null && frames.Count > 0) target.sprite = frames[frame]; }
    }
}
