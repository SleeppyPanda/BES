using BES.Core;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace BES.UI
{
    public class QuestTrackerUI : MonoBehaviour
    {
        [SerializeField] TMP_Text questTitleText;
        [SerializeField] TMP_Text questStepText;
        [SerializeField] RawImage questImage;
        [SerializeField] RectTransform compassArrow;

        Transform player;

        void Start()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;

            Refresh();
            GameEvents.OnQuestUpdated += OnQuestUpdated;
        }

        void OnDestroy()
        {
            GameEvents.OnQuestUpdated -= OnQuestUpdated;
        }

        void OnQuestUpdated(string _) => Refresh();

        void Update()
        {
            UpdateCompass();
        }

        void Refresh()
        {
            var quests = GameManager.Instance?.Quests;
            if (quests == null)
                return;

            if (questTitleText != null)
            {
                var title = quests.GetActiveQuestTitle();
                questTitleText.text = string.IsNullOrEmpty(title)
                    ? "Tracked Quest"
                    : title;
            }

            if (questStepText != null)
                questStepText.text = quests.GetActiveQuestStepDescription();
        }

        void UpdateCompass()
        {
            if (player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            if (player == null || compassArrow == null)
                return;

            var targetId = GameManager.Instance?.Quests.GetActiveQuestTargetId();
            var marker = Narrative.QuestMarker.GetMarker(targetId);
            if (marker == null)
            {
                compassArrow.gameObject.SetActive(false);
                return;
            }

            compassArrow.gameObject.SetActive(true);
            var dir = marker.position - player.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                return;

            var angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            compassArrow.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }
    }
}
