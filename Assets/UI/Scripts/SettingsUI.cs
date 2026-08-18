using UnityEngine;
using UnityEngine.UI;

namespace BES.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string MusicVolumeKey = "BES_MusicVolume";
        private const string SfxVolumeKey = "BES_SfxVolume";

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private float musicVolume = 0.8f;
        private float sfxVolume = 0.8f;

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
                if (musicSource != null) musicSource.volume = musicVolume;
            }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
                if (sfxSource != null) sfxSource.volume = sfxVolume;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load saved settings
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);

            // Initialize AudioSources if not present
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            musicSource.volume = musicVolume;
            sfxSource.volume = sfxVolume;
        }

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }
    }
}

namespace BES.UI
{
    public class SettingsUI : UIScreenBase
    {
        [SerializeField] UISettingsRow musicVolumeRow;
        [SerializeField] UISettingsRow sfxVolumeRow;
        [SerializeField] UISettingsRow fullscreenRow;
        [SerializeField] Button closeButton;

        const string MusicVolumeKey = "BES_MusicVolume";
        const string SfxVolumeKey = "BES_SfxVolume";
        const string FullscreenKey = "BES_Settings_Fullscreen";

        void Awake()
        {
            if (root == null)
                root = gameObject;
            Hide();
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public override void Refresh()
        {
            if (musicVolumeRow != null)
            {
                float savedMusic = PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);
                musicVolumeRow.SetupSlider("Music Volume", savedMusic, v =>
                {
                    if (BES.Audio.AudioManager.Instance != null)
                        BES.Audio.AudioManager.Instance.MusicVolume = v;
                    else
                        PlayerPrefs.SetFloat(MusicVolumeKey, v);
                });
            }

            if (sfxVolumeRow != null)
            {
                float savedSfx = PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
                sfxVolumeRow.SetupSlider("SFX Volume", savedSfx, v =>
                {
                    if (BES.Audio.AudioManager.Instance != null)
                        BES.Audio.AudioManager.Instance.SfxVolume = v;
                    else
                        PlayerPrefs.SetFloat(SfxVolumeKey, v);
                });
            }

            if (fullscreenRow != null)
            {
                fullscreenRow.SetupToggle("Fullscreen", Screen.fullScreen, v =>
                {
                    Screen.fullScreen = v;
                    PlayerPrefs.SetInt(FullscreenKey, v ? 1 : 0);
                });
            }
        }
    }
}
