using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class UIServerOption : MonoBehaviour
    {
        [SerializeField] TMP_Text serverName;
        [SerializeField] Button button;
        [SerializeField] Image highlight;

        public string ServerId { get; private set; }

        public void Setup(string id, string displayName, bool selected, System.Action<string> onSelect)
        {
            ServerId = id;
            if (serverName != null)
                serverName.text = displayName;
            if (highlight != null)
                highlight.enabled = selected;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onSelect?.Invoke(id));
            }
        }
    }
}
