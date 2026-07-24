using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public class SimpleModalPanel : MonoBehaviour
    {
        [SerializeField] GameObject panelRoot;
        [SerializeField] Button closeButton;
        [SerializeField] bool closeOnEscape = true;
        [Tooltip("Enable only for a panel that must already be visible when the scene starts.")]
        [SerializeField] bool showOnStart;
        [SerializeField] UnityEvent onOpened;
        [SerializeField] UnityEvent onClosed;

        bool initialized;
        bool openRequested;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        void Awake()
        {
            Initialize();
        }

        void Start()
        {
            // An inactive panel receives Start only after Open activates it for the
            // first time. Do not hide it again when that activation was explicit.
            if (!showOnStart && !openRequested && panelRoot != null)
                panelRoot.SetActive(false);
        }

        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            if (panelRoot == null) panelRoot = gameObject;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        void Update()
        {
            if (closeOnEscape && IsOpen && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Close();
        }

        public void Open()
        {
            openRequested = true;
            Initialize();
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            onOpened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;
            openRequested = false;
            panelRoot.SetActive(false);
            onClosed?.Invoke();
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }
    }
}
