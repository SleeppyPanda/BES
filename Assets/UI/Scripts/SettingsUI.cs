using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

namespace BES.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string MusicVolumeKey = "BES_MusicVolume";
        private const string SfxVolumeKey = "BES_SfxVolume";
        private const string MusicEnabledKey = "BES_MusicEnabled";
        private const string SfxEnabledKey = "BES_SfxEnabled";

        [Header("Shared Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private string musicVolumeParameter = "MusicVolume";
        [SerializeField] private string sfxVolumeParameter = "SfxVolume";
        [SerializeField] private float mutedVolumeDb = -80f;

        [Header("Optional Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private float musicVolume = 0.8f;
        private float sfxVolume = 0.8f;
        private bool musicEnabled = true;
        private bool sfxEnabled = true;

        public event System.Action SettingsChanged;

        public AudioMixer Mixer => audioMixer;
        public AudioMixerGroup MusicMixerGroup => musicMixerGroup;
        public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;
        public bool MusicEnabled => musicEnabled;
        public bool SfxEnabled => sfxEnabled;

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
                ApplyMusicSettings();
                SettingsChanged?.Invoke();
            }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
                ApplySfxSettings();
                SettingsChanged?.Invoke();
            }
        }

        public void SetMusicEnabled(bool enabled)
        {
            if (musicEnabled == enabled) return;
            musicEnabled = enabled;
            PlayerPrefs.SetInt(MusicEnabledKey, musicEnabled ? 1 : 0);
            ApplyMusicSettings();
            SettingsChanged?.Invoke();
        }

        public void SetSfxEnabled(bool enabled)
        {
            if (sfxEnabled == enabled) return;
            sfxEnabled = enabled;
            PlayerPrefs.SetInt(SfxEnabledKey, sfxEnabled ? 1 : 0);
            ApplySfxSettings();
            SettingsChanged?.Invoke();
        }

        public void ToggleMusic() => SetMusicEnabled(!musicEnabled);
        public void ToggleSfx() => SetSfxEnabled(!sfxEnabled);

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
            musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
            sfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;

            // Initialize AudioSources if not present
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            musicSource.outputAudioMixerGroup = musicMixerGroup;

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;

            ApplyMusicSettings();
            ApplySfxSettings();
        }

        void OnValidate()
        {
            if (musicSource != null) musicSource.outputAudioMixerGroup = musicMixerGroup;
            if (sfxSource != null) sfxSource.outputAudioMixerGroup = sfxMixerGroup;
            ApplyMusicSettings();
            ApplySfxSettings();
        }

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null) return;
            if (!musicEnabled) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (sfxSource == null || clip == null) return;
            if (!sfxEnabled) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        void ApplyMusicSettings()
        {
            float linearVolume = musicEnabled ? musicVolume : 0f;
            if (musicSource != null) musicSource.volume = linearVolume;
            SetMixerVolume(musicVolumeParameter, linearVolume);
        }

        void ApplySfxSettings()
        {
            float linearVolume = sfxEnabled ? sfxVolume : 0f;
            if (sfxSource != null) sfxSource.volume = linearVolume;
            SetMixerVolume(sfxVolumeParameter, linearVolume);
        }

        void SetMixerVolume(string parameterName, float linearVolume)
        {
            if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName)) return;

            float db = linearVolume <= 0.0001f ? mutedVolumeDb : Mathf.Log10(linearVolume) * 20f;
            audioMixer.SetFloat(parameterName, db);
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

        [Header("Audio Toggle Buttons")]
        [SerializeField] Button musicToggleButton;
        [SerializeField] Button sfxToggleButton;
        [SerializeField] TMP_Text musicToggleLabel;
        [SerializeField] TMP_Text sfxToggleLabel;
        [SerializeField] string musicOnText = "Music: ON";
        [SerializeField] string musicOffText = "Music: OFF";
        [SerializeField] string sfxOnText = "SFX: ON";
        [SerializeField] string sfxOffText = "SFX: OFF";

        const string MusicVolumeKey = "BES_MusicVolume";
        const string SfxVolumeKey = "BES_SfxVolume";
        const string MusicEnabledKey = "BES_MusicEnabled";
        const string SfxEnabledKey = "BES_SfxEnabled";
        const string FullscreenKey = "BES_Settings_Fullscreen";

        void Awake()
        {
            if (root == null)
                root = gameObject;
            Hide();
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (musicToggleButton != null) musicToggleButton.onClick.AddListener(ToggleMusic);
            if (sfxToggleButton != null) sfxToggleButton.onClick.AddListener(ToggleSfx);
        }

        void OnEnable()
        {
            if (BES.Audio.AudioManager.Instance != null)
                BES.Audio.AudioManager.Instance.SettingsChanged += RefreshAudioToggleLabels;

            RefreshAudioToggleLabels();
        }

        void OnDisable()
        {
            if (BES.Audio.AudioManager.Instance != null)
                BES.Audio.AudioManager.Instance.SettingsChanged -= RefreshAudioToggleLabels;
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

            RefreshAudioToggleLabels();

            if (fullscreenRow != null)
            {
                fullscreenRow.SetupToggle("Fullscreen", Screen.fullScreen, v =>
                {
                    Screen.fullScreen = v;
                    PlayerPrefs.SetInt(FullscreenKey, v ? 1 : 0);
                });
            }
        }

        void ToggleMusic()
        {
            if (BES.Audio.AudioManager.Instance != null)
            {
                BES.Audio.AudioManager.Instance.ToggleMusic();
            }
            else
            {
                bool next = PlayerPrefs.GetInt(MusicEnabledKey, 1) != 1;
                PlayerPrefs.SetInt(MusicEnabledKey, next ? 1 : 0);
                RefreshAudioToggleLabels();
            }
        }

        void ToggleSfx()
        {
            if (BES.Audio.AudioManager.Instance != null)
            {
                BES.Audio.AudioManager.Instance.ToggleSfx();
            }
            else
            {
                bool next = PlayerPrefs.GetInt(SfxEnabledKey, 1) != 1;
                PlayerPrefs.SetInt(SfxEnabledKey, next ? 1 : 0);
                RefreshAudioToggleLabels();
            }
        }

        void RefreshAudioToggleLabels()
        {
            var audio = BES.Audio.AudioManager.Instance;
            bool musicOn = audio != null ? audio.MusicEnabled : PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
            bool sfxOn = audio != null ? audio.SfxEnabled : PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;

            if (musicToggleLabel != null)
                musicToggleLabel.text = musicOn ? musicOnText : musicOffText;

            if (sfxToggleLabel != null)
                sfxToggleLabel.text = sfxOn ? sfxOnText : sfxOffText;
        }
    }
}
