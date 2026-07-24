using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    [RequireComponent(typeof(Button))]
    public class PlayModeLaunchButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] PlayModePanelController panel;
        [SerializeField] PlayModeTab targetTab;

        void Awake()
        {
            button ??= GetComponent<Button>();
            button.onClick.AddListener(OpenTargetTab);
        }

        public void OpenTargetTab() => panel?.OpenTab(targetTab);

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OpenTargetTab);
        }
    }
}
