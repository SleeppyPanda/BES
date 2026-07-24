using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public class RiftStageCardView : MonoBehaviour
    {
        [SerializeField] string stageId;
        [SerializeField] Image stageImage;
        [SerializeField] TMP_Text titleText;
        [SerializeField] List<TMP_Text> descriptionLines = new();
        [SerializeField] Button playButton;
        [SerializeField] UnityEvent<string> onPlay;

        void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
        }

        public void Play() => onPlay?.Invoke(stageId);
    }
}
