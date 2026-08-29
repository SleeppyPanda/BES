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
        [Tooltip("Optional Play Mode stage group id. Use this for group 4+ or to reuse one StageSelection panel with different data.")]
        public string stageGroupId;
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
        [SerializeField] bool debugLogs = true;
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
            ResolveNavigator();
            Log($"Initialize panelRoot='{NameOf(panelRoot)}' navigator='{NameOf(navigator)}' tabs={tabs?.Count ?? 0} battleButtons={battleButtons?.Count ?? 0}");
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            else Log("CloseButton is not assigned.");

            foreach (var binding in tabs)
            {
                if (binding == null)
                {
                    Log("Tab binding is null.");
                    continue;
                }
                if (binding.tabButton == null)
                {
                    Log($"Tab '{binding.tab}' has no button assigned.");
                    continue;
                }
                var captured = binding.tab;
                binding.tabButton.onClick.AddListener(() => SelectTab(captured));
                Log($"Bound tab button '{binding.tabButton.name}' -> {captured}");
            }
            for (var i = 0; i < battleButtons.Count; i++)
            {
                var binding = battleButtons[i];
                if (binding == null)
                {
                    Log($"Battle button binding[{i}] is null.");
                    continue;
                }
                if (binding.button == null)
                {
                    Log($"Battle button binding[{i}] has no Button. stageGroupId='{binding.stageGroupId}' directStageId='{binding.directStageId}' screen={binding.stageSelectionScreen}");
                    continue;
                }
                var captured = binding;
                binding.button.onClick.AddListener(() => OpenBattleFlow(captured));
                Log($"Bound battle button[{i}] '{binding.button.name}' group='{binding.stageGroupId}' directStage='{binding.directStageId}' screen={binding.stageSelectionScreen}");
            }
        }

        public void OpenTab(PlayModeTab tab)
        {
            Initialize();
            Log($"OpenTab {tab}");
            panelRoot.SetActive(true);
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
            Log($"SelectTab {tab}");
            foreach (var binding in tabs)
            {
                if (binding == null) continue;
                var selected = binding.tab == tab;
                binding.contentRoot?.SetActive(selected);
                binding.selectedState?.SetActive(selected);
                Log($"Tab '{binding.tab}' selected={selected} content='{NameOf(binding.contentRoot)}' selectedState='{NameOf(binding.selectedState)}'");
                if (selected) binding.onSelected?.Invoke();
            }
            onTabChanged?.Invoke(tab);
        }

        public void Close()
        {
            if (!IsOpen) return;
            Log("Close");
            panelRoot.SetActive(false);
            onClosed?.Invoke();
        }

        void OpenBattleFlow(PlayModeBattleButtonBinding binding)
        {
            ResolveNavigator();
            if (binding == null)
            {
                Log("OpenBattleFlow blocked: binding is null.");
                return;
            }

            Log($"OpenBattleFlow clicked button='{NameOf(binding.button)}' group='{binding.stageGroupId}' directStage='{binding.directStageId}' screen={binding.stageSelectionScreen} navigator='{NameOf(navigator)}'");
            TurnBattleUI.IsPlayModeBattle = true;

            if (!string.IsNullOrWhiteSpace(binding.directStageId))
            {
                TurnBattleUI.ActiveStageId = binding.directStageId.Trim();
                TurnBattleUI.ActivePlayModeStageGroupId = binding.stageGroupId?.Trim();
                Log($"Direct stage flow -> ActiveStageId='{TurnBattleUI.ActiveStageId}' ActiveGroup='{TurnBattleUI.ActivePlayModeStageGroupId}', opening {MenuScreenId.PlayParty}");
                if (navigator == null)
                {
                    Log("OpenBattleFlow blocked: navigator is not assigned, cannot open PlayParty.");
                    return;
                }
                navigator.Open(MenuScreenId.PlayParty);
                return;
            }

            StageSelectionController.OpenGroupOnNextEnable(binding.stageGroupId);
            Log($"Stage selection flow -> pendingGroup='{binding.stageGroupId}', opening {binding.stageSelectionScreen}");
            if (navigator == null)
            {
                Log($"OpenBattleFlow blocked: navigator is not assigned, cannot open {binding.stageSelectionScreen}.");
                return;
            }
            navigator.Open(binding.stageSelectionScreen);
        }

        void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        void Log(string message)
        {
            if (!debugLogs) return;
            Debug.Log($"[BES][PlayModePanelController] {message}", this);
        }

        void ResolveNavigator()
        {
            if (navigator != null) return;
            navigator = GetComponentInParent<MenuNavigator>(true);
            if (navigator == null) navigator = FindFirstObjectByType<MenuNavigator>(FindObjectsInactive.Include);
        }

        static string NameOf(UnityEngine.Object target) => target != null ? target.name : "NULL";
    }
}
