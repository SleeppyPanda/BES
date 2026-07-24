using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class SettingsUI : UIScreenBase
    {
        [SerializeField] UISettingsRow volumeRow;
        [SerializeField] UISettingsRow fullscreenRow;
        [SerializeField] Button closeButton;

        const string VolumeKey = "BES_Settings_Volume";
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
            if (volumeRow != null)
                volumeRow.SetupSlider("Volume", PlayerPrefs.GetFloat(VolumeKey, 0.8f), v => PlayerPrefs.SetFloat(VolumeKey, v));
            if (fullscreenRow != null)
                fullscreenRow.SetupToggle("Fullscreen", Screen.fullScreen, v =>
                {
                    Screen.fullScreen = v;
                    PlayerPrefs.SetInt(FullscreenKey, v ? 1 : 0);
                });
        }
    }
}
