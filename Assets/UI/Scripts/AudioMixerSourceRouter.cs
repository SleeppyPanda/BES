using UnityEngine;
using UnityEngine.Audio;

namespace BES.Audio
{
    public enum AudioBus
    {
        Music,
        Sfx
    }

    [RequireComponent(typeof(AudioSource))]
    public class AudioMixerSourceRouter : MonoBehaviour
    {
        [SerializeField] private AudioBus bus = AudioBus.Sfx;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private AudioMixerGroup fallbackMixerGroup;

        public AudioBus Bus
        {
            get => bus;
            set
            {
                bus = value;
                ApplyRoute();
            }
        }

        void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        void Awake()
        {
            ApplyRoute();
        }

        void OnValidate()
        {
            audioSource = GetComponent<AudioSource>();
            ApplyRoute();
        }

        public void ApplyRoute()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null) return;

            var manager = audioManager != null ? audioManager : AudioManager.Instance;
            AudioMixerGroup targetGroup = fallbackMixerGroup;

            if (manager != null)
            {
                targetGroup = bus == AudioBus.Music
                    ? manager.MusicMixerGroup
                    : manager.SfxMixerGroup;
            }

            if (targetGroup != null)
                audioSource.outputAudioMixerGroup = targetGroup;
        }
    }
}
