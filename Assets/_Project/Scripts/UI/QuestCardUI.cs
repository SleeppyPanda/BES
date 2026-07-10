using BES.Narrative;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BES.UI
{
    public class QuestCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] Image background;
        [SerializeField] Image border;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text infoText;
        [SerializeField] float hoverScale = 1.05f;

        QuestDefinition quest;
        System.Action<QuestDefinition> clicked;
        Vector3 defaultScale = Vector3.one;

        public string QuestId => quest != null ? quest.questId : string.Empty;

        void Awake()
        {
            defaultScale = transform.localScale;
            SetSelected(false);
        }

        public void Setup(QuestDefinition definition, string info, System.Action<QuestDefinition> onClick)
        {
            quest = definition;
            clicked = onClick;
            if (titleText != null)
                titleText.text = definition != null ? definition.questTitle : "Quest";
            if (infoText != null)
                infoText.text = info;
        }

        public void SetSelected(bool selected)
        {
            if (border != null)
                border.enabled = selected;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = defaultScale * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = defaultScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            clicked?.Invoke(quest);
        }
    }
}
