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

        public string StageId { get => stageId; set => stageId = value; }
        public Image StageImage { get => stageImage; set => stageImage = value; }
        public TMP_Text TitleText { get => titleText; set => titleText = value; }
        public List<TMP_Text> DescriptionLines => descriptionLines;
        public Button PlayButton { get => playButton; set => playButton = value; }
        public UnityEvent<string> OnPlay => onPlay;

        void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
        }

        public void Play() => onPlay?.Invoke(stageId);
    }
}
