using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class BattlePassUI : UIScreenBase
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] Button closeButton;

        void Awake()
        {
            if (root == null)
                root = gameObject;

            Hide();
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        public override void Refresh()
        {
            if (titleText != null)
                titleText.text = "Battle Pass";
            if (progressText != null)
                progressText.text = "Progress 0 / 100";
        }
    }
}
