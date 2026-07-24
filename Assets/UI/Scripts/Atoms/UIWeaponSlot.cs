using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class UIWeaponSlot : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text label;
        [SerializeField] Button button;
        [SerializeField] Image selectionFrame;

        public string WeaponId { get; private set; }

        public void Setup(string weaponId, string displayName, bool selected, System.Action<string> onClick)
        {
            WeaponId = weaponId;
            if (label != null)
                label.text = displayName;
            if (selectionFrame != null)
                selectionFrame.enabled = selected;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(weaponId));
            }
        }
    }
}
