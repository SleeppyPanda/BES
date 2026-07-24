using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public enum PlayModeTab
    {
        ResonanceSanctum,
        SanctumOfLostEchoes,
        RiftOfTheHunt,
        DivineRemnant
    }

    [Serializable]
    public class PlayModeTabBinding
    {
        public PlayModeTab tab;
        public Button tabButton;
        public GameObject contentRoot;
        public GameObject selectedState;
        public UnityEvent onSelected;
    }

    public class PlayModePanelController : MonoBehaviour
    {
        [SerializeField] GameObject panelRoot;
        [SerializeField] Button closeButton;
        [SerializeField] PlayModeTab initialTab;
        [SerializeField] List<PlayModeTabBinding> tabs = new();
        [SerializeField] UnityEvent<PlayModeTab> onTabChanged;
        [SerializeField] UnityEvent onOpened;
        [SerializeField] UnityEvent onClosed;

        bool initialized;
        PlayModeTab currentTab;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public PlayModeTab CurrentTab => currentTab;

        void Awake() => Initialize();

        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            panelRoot ??= gameObject;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            foreach (var binding in tabs)
            {
                if (binding.tabButton == null) continue;
                var captured = binding.tab;
                binding.tabButton.onClick.AddListener(() => SelectTab(captured));
            }
        }

        public void OpenTab(PlayModeTab tab)
        {
            Initialize();
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            SelectTab(tab);
            onOpened?.Invoke();
        }

        public void OpenResonanceSanctum() => OpenTab(PlayModeTab.ResonanceSanctum);
        public void OpenSanctumOfLostEchoes() => OpenTab(PlayModeTab.SanctumOfLostEchoes);
        public void OpenRiftOfTheHunt() => OpenTab(PlayModeTab.RiftOfTheHunt);
        public void OpenDivineRemnant() => OpenTab(PlayModeTab.DivineRemnant);

        public void SelectTab(PlayModeTab tab)
        {
            Initialize();
            currentTab = tab;
            foreach (var binding in tabs)
            {
                var selected = binding.tab == tab;
                binding.contentRoot?.SetActive(selected);
                binding.selectedState?.SetActive(selected);
                if (selected) binding.onSelected?.Invoke();
            }
            onTabChanged?.Invoke(tab);
        }

        public void Close()
        {
            if (!IsOpen) return;
            panelRoot.SetActive(false);
            onClosed?.Invoke();
        }

        void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }
    }
}
