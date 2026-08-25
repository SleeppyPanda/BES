using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public enum PlayModeTab
    {
        SanctumOfRelics,
        ArenaOfEchoes,
        CharacterBreakthrough,
        WeaponBreakthrough,
        RiftOfTheHunt
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

    [Serializable]
    public class PlayModeBattleButtonBinding
    {
        public Button button;
        public MenuScreenId stageSelectionScreen = MenuScreenId.ResourceStages;
        [Tooltip("Optional direct stage id. If filled, this skips stage selection and opens the Play Mode party screen.")]
        public string directStageId;
    }

    public class PlayModePanelController : MonoBehaviour
    {
        [SerializeField] GameObject panelRoot;
        [SerializeField] MenuNavigator navigator;
        [SerializeField] Button closeButton;
        [SerializeField] PlayModeTab initialTab;
        [SerializeField] List<PlayModeTabBinding> tabs = new();
        [Header("Mode battle buttons")]
        [SerializeField] List<PlayModeBattleButtonBinding> battleButtons = new();
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
            foreach (var binding in battleButtons)
            {
                if (binding?.button == null) continue;
                var captured = binding;
                binding.button.onClick.AddListener(() => OpenBattleFlow(captured));
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

        public void OpenSanctumOfRelics() => OpenTab(PlayModeTab.SanctumOfRelics);
        public void OpenArenaOfEchoes() => OpenTab(PlayModeTab.ArenaOfEchoes);
        public void OpenCharacterBreakthrough() => OpenTab(PlayModeTab.CharacterBreakthrough);
        public void OpenWeaponBreakthrough() => OpenTab(PlayModeTab.WeaponBreakthrough);
        public void OpenRiftOfTheHunt() => OpenTab(PlayModeTab.RiftOfTheHunt);

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

        void OpenBattleFlow(PlayModeBattleButtonBinding binding)
        {
            if (binding == null) return;
            TurnBattleUI.IsPlayModeBattle = true;

            if (!string.IsNullOrWhiteSpace(binding.directStageId))
            {
                TurnBattleUI.ActiveStageId = binding.directStageId.Trim();
                navigator?.Open(MenuScreenId.PlayParty);
                return;
            }

            navigator?.Open(binding.stageSelectionScreen);
        }

        void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }
    }
}