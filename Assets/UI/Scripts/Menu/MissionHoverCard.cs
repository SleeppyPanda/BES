using UnityEngine;
using UnityEngine.EventSystems;

namespace BES.UI.Menu
{
    public class MissionHoverCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] MissionPanelController controller;
        [SerializeField] int cardIndex;

        public void Configure(MissionPanelController owner, int index)
        {
            controller = owner;
            cardIndex = index;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            controller?.SetHoveredCard(cardIndex);

        public void OnPointerExit(PointerEventData eventData) =>
            controller?.ClearHoveredCard(cardIndex);
    }
}
