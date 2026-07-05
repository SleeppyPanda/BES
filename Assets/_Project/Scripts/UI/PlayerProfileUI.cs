using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BES.UI
{
    public class PlayerProfileUI : UIScreenBase
    {
        [SerializeField] TMP_Text usernameText;
        [SerializeField] TMP_Text serverText;
        [SerializeField] TMP_Text uidText;
        [SerializeField] Button closeButton;

        void Awake()
        {
            if (root == null)
                root = gameObject;
            Hide();
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public override void Refresh()
        {
            if (usernameText != null)
                usernameText.text = "Username PLayer";
            if (serverText != null)
                serverText.text = $"Server: {ServerPickerUI.GetSelectedServer()}";
            if (uidText != null)
                uidText.text = "UID: 100000001";
        }
    }
}
