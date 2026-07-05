using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class ServerPickerUI : UIScreenBase
    {
        static readonly string[] Servers = { "Asian", "America", "Europe" };

        [SerializeField] Transform optionsContainer;
        [SerializeField] GameObject serverOptionPrefab;
        [SerializeField] Button closeButton;

        public System.Action<string> OnServerSelected;

        void Awake()
        {
            if (root == null)
                root = gameObject;
            Hide();
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public override void Refresh()
        {
            if (optionsContainer == null || serverOptionPrefab == null)
                return;

            for (var i = optionsContainer.childCount - 1; i >= 0; i--)
                Destroy(optionsContainer.GetChild(i).gameObject);

            var current = PlayerPrefs.GetString(UIAssetPaths.ServerPrefsKey, UIAssetPaths.DefaultServer);
            foreach (var server in Servers)
            {
                var go = Instantiate(serverOptionPrefab, optionsContainer);
                var option = go.GetComponent<UIServerOption>();
                option?.Setup(server, server, server == current, SelectServer);
            }
        }

        void SelectServer(string serverId)
        {
            PlayerPrefs.SetString(UIAssetPaths.ServerPrefsKey, serverId);
            OnServerSelected?.Invoke(serverId);
            Hide();
        }

        public static string GetSelectedServer() =>
            PlayerPrefs.GetString(UIAssetPaths.ServerPrefsKey, UIAssetPaths.DefaultServer);
    }
}
