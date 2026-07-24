using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public class DiscoverableRelicSlot : MonoBehaviour
    {
        [SerializeField] Image relicImage;
        [SerializeField] bool discovered;
        public bool Discovered => discovered;

        void Awake() => Refresh();
        void OnValidate() => Refresh();

        public void SetDiscovered(bool value)
        {
            discovered = value;
            Refresh();
        }

        public void SetSprite(Sprite sprite)
        {
            if (relicImage != null) relicImage.sprite = sprite;
        }

        public void Refresh()
        {
            if (relicImage == null) relicImage = GetComponent<Image>();
            if (relicImage == null) return;
            var color = relicImage.color;
            color.a = 1f;
            relicImage.color = color;
        }
    }
}
